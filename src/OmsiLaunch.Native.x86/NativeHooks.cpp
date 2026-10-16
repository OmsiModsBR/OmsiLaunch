#include <cstdarg>
#include <cstdio>
#include "NativeHooks.h"
#include <tlhelp32.h>
#include <string.h>

// Native hook infrastructure (Beta 4). See NativeHooks.h.
//
// Kinds:
// - IAT: the import address table entries of a function in a loaded module
//   (Omsi.exe unless a host module is named). A module can import the same
//   function through several descriptors (Omsi.exe imports kernel32's file
//   functions twice); every entry is hooked, as one group with one id. The
//   entries must all hold the exported function (or the same top detour).
// - SLOT: a pointer slot (a VMT or COM vtable entry). The slot must hold the
//   value the caller expects (or the registry's top detour for that slot).
// - PROLOGUE: the first bytes of a function are replaced by a jmp to the
//   detour; the displaced bytes run from a trampoline. The caller supplies
//   the exact expected bytes and a length that ends on an instruction boundary
//   with no relative operands (the build's target catalog vouches for both).
//   Other threads are suspended while the bytes change.
//
// Pointer writes are single aligned 32-bit stores (atomic on x86). All
// registry changes are serialized by one lock.

namespace {

constexpr int MaxHooks = 128;
constexpr int MaxPrologue = 16;
constexpr int MaxImportSlots = 8;
constexpr int OwnerCapacity = 64;

struct HookEntry {
    int id;
    int group;                   // the id the caller got (all IAT entries of one request)
    int kind;
    char owner[OwnerCapacity];
    void** slot;                 // IAT and SLOT
    unsigned char* target;       // PROLOGUE
    void* original;              // the value or entry the chain continues to
    void* detour;
    unsigned char saved[MaxPrologue];
    int length;
    unsigned char* trampoline;
    bool active;
};

SRWLOCK gHookLock = SRWLOCK_INIT;
HookEntry gHooks[MaxHooks]{};
int gNextHookId = 1;

struct Locked {
    Locked() { AcquireSRWLockExclusive(&gHookLock); }
    ~Locked() { ReleaseSRWLockExclusive(&gHookLock); }
};

bool CopyOwner(char* destination, const char* owner) {
    if (owner == nullptr || owner[0] == 0) return false;
    const auto length = strnlen(owner, OwnerCapacity);
    if (length >= OwnerCapacity) return false;
    memcpy(destination, owner, length + 1);
    return true;
}

HookEntry* FindEntry(int id) {
    for (auto& entry : gHooks) if (entry.active && entry.id == id) return &entry;
    return nullptr;
}

HookEntry* FreeEntry() {
    for (auto& entry : gHooks) if (!entry.active) return &entry;
    return nullptr;
}

// The most recent active hook on a pointer site or a prologue target.
HookEntry* TopOfSlot(void** slot) {
    HookEntry* top = nullptr;
    for (auto& entry : gHooks) if (entry.active && entry.slot == slot && (top == nullptr || entry.id > top->id)) top = &entry;
    return top;
}

HookEntry* TopOfTarget(unsigned char* target) {
    for (auto& entry : gHooks) if (entry.active && entry.kind == OL_HOOK_KIND_PROLOGUE && entry.target == target) return &entry;
    return nullptr;
}

bool Readable(const void* pointer, size_t bytes) {
    MEMORY_BASIC_INFORMATION info{};
    if (pointer == nullptr || VirtualQuery(pointer, &info, sizeof(info)) != sizeof(info) || info.State != MEM_COMMIT) return false;
    if ((info.Protect & (PAGE_NOACCESS | PAGE_GUARD)) != 0) return false;
    const auto end = reinterpret_cast<uintptr_t>(info.BaseAddress) + info.RegionSize;
    return reinterpret_cast<uintptr_t>(pointer) + bytes <= end;
}

bool WritePointer(void** slot, void* value) {
    DWORD oldProtection = 0;
    if (!VirtualProtect(slot, sizeof(void*), PAGE_READWRITE, &oldProtection)) return false;
    InterlockedExchangePointer(slot, value);
    DWORD ignored = 0;
    VirtualProtect(slot, sizeof(void*), oldProtection, &ignored);
    FlushInstructionCache(GetCurrentProcess(), slot, sizeof(void*));
    return true;
}

// Suspends every other thread of the process; fails (and resumes them) when
// one is executing inside [start, start + length).
int SuspendOthers(HANDLE* handles, int capacity, int* count, const unsigned char* start, int length) {
    *count = 0;
    const auto self = GetCurrentThreadId();
    const auto process = GetCurrentProcessId();
    const auto snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, 0);
    if (snapshot == INVALID_HANDLE_VALUE) return OL_HOOK_E_MEMORY;
    THREADENTRY32 entry{}; entry.dwSize = sizeof(entry);
    int result = OL_HOOK_OK;
    for (auto more = Thread32First(snapshot, &entry); more; more = Thread32Next(snapshot, &entry)) {
        if (entry.th32OwnerProcessID != process || entry.th32ThreadID == self) continue;
        if (*count == capacity) { result = OL_HOOK_E_FULL; break; }
        const auto thread = OpenThread(THREAD_SUSPEND_RESUME | THREAD_GET_CONTEXT, FALSE, entry.th32ThreadID);
        if (thread == nullptr) continue;
        if (SuspendThread(thread) == static_cast<DWORD>(-1)) { CloseHandle(thread); continue; }
        handles[(*count)++] = thread;
        CONTEXT context{}; context.ContextFlags = CONTEXT_CONTROL;
        if (GetThreadContext(thread, &context)) {
            const auto ip = static_cast<uintptr_t>(context.Eip);
            if (ip >= reinterpret_cast<uintptr_t>(start) && ip < reinterpret_cast<uintptr_t>(start) + static_cast<uintptr_t>(length)) { result = OL_HOOK_E_BUSY; break; }
        }
    }
    CloseHandle(snapshot);
    return result;
}

void ResumeAll(HANDLE* handles, int count) {
    for (int index = 0; index < count; ++index) { ResumeThread(handles[index]); CloseHandle(handles[index]); }
}

int WriteCode(unsigned char* target, const unsigned char* bytes, int length) {
    constexpr int ThreadCapacity = 512;
    static HANDLE threads[ThreadCapacity];
    for (int attempt = 0; attempt < 8; ++attempt) {
        int count = 0;
        const auto suspended = SuspendOthers(threads, ThreadCapacity, &count, target, length);
        if (suspended == OL_HOOK_OK) {
            DWORD oldProtection = 0;
            int result = OL_HOOK_OK;
            if (VirtualProtect(target, static_cast<SIZE_T>(length), PAGE_EXECUTE_READWRITE, &oldProtection)) {
                memcpy(target, bytes, static_cast<size_t>(length));
                DWORD ignored = 0;
                VirtualProtect(target, static_cast<SIZE_T>(length), oldProtection, &ignored);
                FlushInstructionCache(GetCurrentProcess(), target, static_cast<SIZE_T>(length));
            } else result = OL_HOOK_E_PROTECT;
            ResumeAll(threads, count);
            return result;
        }
        ResumeAll(threads, count);
        if (suspended != OL_HOOK_E_BUSY) return suspended;
        Sleep(1);
    }
    return OL_HOOK_E_BUSY;
}

// Every import address table entry of `function` from `importModule`, in
// descriptor order. Returns how many were found (at most capacity).
int FindImports(HMODULE host, const char* importModule, const char* function, void*** slots, int capacity) {
    auto* base = reinterpret_cast<unsigned char*>(host);
    auto* dos = reinterpret_cast<IMAGE_DOS_HEADER*>(base);
    if (dos->e_magic != IMAGE_DOS_SIGNATURE) return 0;
    auto* nt = reinterpret_cast<IMAGE_NT_HEADERS32*>(base + dos->e_lfanew);
    if (nt->Signature != IMAGE_NT_SIGNATURE) return 0;
    const auto& directory = nt->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT];
    if (directory.VirtualAddress == 0) return 0;
    int count = 0;
    for (auto* descriptor = reinterpret_cast<IMAGE_IMPORT_DESCRIPTOR*>(base + directory.VirtualAddress); descriptor->Name != 0 && count < capacity; ++descriptor) {
        if (_stricmp(reinterpret_cast<const char*>(base + descriptor->Name), importModule) != 0) continue;
        if (descriptor->OriginalFirstThunk == 0) continue;
        auto* names = reinterpret_cast<IMAGE_THUNK_DATA32*>(base + descriptor->OriginalFirstThunk);
        auto* addresses = reinterpret_cast<IMAGE_THUNK_DATA32*>(base + descriptor->FirstThunk);
        for (; names->u1.AddressOfData != 0 && count < capacity; ++names, ++addresses) {
            if (IMAGE_SNAP_BY_ORDINAL32(names->u1.Ordinal)) continue;
            auto* byName = reinterpret_cast<IMAGE_IMPORT_BY_NAME*>(base + names->u1.AddressOfData);
            if (strcmp(reinterpret_cast<const char*>(byName->Name), function) == 0) slots[count++] = reinterpret_cast<void**>(&addresses->u1.Function);
        }
    }
    return count;
}

// Installs a pointer hook on one or more sites, all or nothing. Every site
// must hold the same value: `expected`, or the detour of the registry's top
// hook on that site (the new hook stacks on it). All entries share the id
// returned (the group).
int InstallPointers(const char* owner, int kind, void*** slots, int count, void* expected, void* detour, void** original, int* id) {
    if (slots == nullptr || count < 1 || count > MaxImportSlots || detour == nullptr || original == nullptr || id == nullptr) return OL_HOOK_E_ARGUMENT;
    for (int index = 0; index < count; ++index) if (slots[index] == nullptr || !Readable(slots[index], sizeof(void*))) return OL_HOOK_E_ARGUMENT;
    Locked lock;
    HookEntry* entries[MaxImportSlots]{};
    int available = 0;
    for (auto& entry : gHooks) if (!entry.active && available < count) entries[available++] = &entry;
    if (available < count) return OL_HOOK_E_FULL;
    HookEntry candidate{};
    if (!CopyOwner(candidate.owner, owner)) return OL_HOOK_E_ARGUMENT;
    auto* current = *slots[0];
    for (int index = 0; index < count; ++index) {
        auto* now = *slots[index];
        auto* top = TopOfSlot(slots[index]);
        if (top != nullptr) {
            if (now != top->detour) return OL_HOOK_E_FOREIGN;
        } else if (now != expected) {
            return expected == nullptr ? OL_HOOK_E_ARGUMENT : OL_HOOK_E_UNEXPECTED;
        }
        // One original continues the chain, so every site must agree.
        if (now != current) return OL_HOOK_E_FOREIGN;
    }
    int written = 0;
    while (written < count && WritePointer(slots[written], detour)) ++written;
    if (written != count) {
        for (int index = 0; index < written; ++index) WritePointer(slots[index], current);
        return OL_HOOK_E_PROTECT;
    }
    const auto group = gNextHookId;
    for (int index = 0; index < count; ++index) {
        auto entry = candidate;
        entry.id = gNextHookId++;
        entry.group = group;
        entry.kind = kind;
        entry.slot = slots[index];
        entry.original = current;
        entry.detour = detour;
        entry.active = true;
        *entries[index] = entry;
    }
    *original = current;
    *id = group;
    return OL_HOOK_OK;
}

int InstallPointer(const char* owner, int kind, void** slot, void* expected, void* detour, void** original, int* id) {
    void** slots[1] = { slot };
    return InstallPointers(owner, kind, slots, 1, expected, detour, original, id);
}

// The intact state of a group: 0 when every entry still holds its site,
// OL_HOOK_E_FOREIGN when one was overwritten; *changed receives that entry.
int VerifyGroup(int group, HookEntry** changed) {
    HookEntry* first = nullptr;
    *changed = nullptr;
    for (auto& entry : gHooks) {
        if (!entry.active || entry.group != group) continue;
        if (first == nullptr) first = &entry;
        bool intact;
        if (entry.kind == OL_HOOK_KIND_PROLOGUE)
            intact = entry.target[0] == 0xE9 && entry.target + 5 + *reinterpret_cast<int32_t*>(entry.target + 1) == entry.detour;
        else {
            // A hook with another registered hook on top is intact when the top one is.
            auto* top = TopOfSlot(entry.slot);
            intact = *entry.slot == top->detour;
        }
        if (!intact) { *changed = &entry; return OL_HOOK_E_FOREIGN; }
    }
    if (first == nullptr) return OL_HOOK_E_NOT_FOUND;
    *changed = first;
    return OL_HOOK_OK;
}

int RemoveEntry(HookEntry* entry) {
    if (entry->kind == OL_HOOK_KIND_PROLOGUE) {
        if (memcmp(entry->target, "\xE9", 1) != 0 ||
            reinterpret_cast<unsigned char*>(entry->target + 5 + *reinterpret_cast<int32_t*>(entry->target + 1)) != entry->detour) return OL_HOOK_E_FOREIGN;
        const auto written = WriteCode(entry->target, entry->saved, entry->length);
        if (written != OL_HOOK_OK) return written;
        // The trampoline is kept: a thread may still be returning through it.
        entry->active = false;
        return OL_HOOK_OK;
    }
    auto* top = TopOfSlot(entry->slot);
    if (top != entry) return OL_HOOK_E_ORDER;
    if (*entry->slot != entry->detour) return OL_HOOK_E_FOREIGN;
    if (!WritePointer(entry->slot, entry->original)) return OL_HOOK_E_PROTECT;
    entry->active = false;
    return OL_HOOK_OK;
}

// Diagnostics: "<folder>\\<file>" of the module that contains an address
// (the last two path components only), or "(no module)".
void OwnerName(const void* address, wchar_t* name, int capacity) {
    if (name == nullptr || capacity <= 0) return;
    name[0] = 0;
    HMODULE owner = nullptr;
    if (address == nullptr || !GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT, reinterpret_cast<LPCWSTR>(address), &owner)) {
        wcsncpy_s(name, static_cast<size_t>(capacity), L"(no module)", _TRUNCATE);
        return;
    }
    wchar_t path[MAX_PATH]{};
    GetModuleFileNameW(owner, path, MAX_PATH);
    const wchar_t* last = wcsrchr(path, L'\\');
    const wchar_t* start = path;
    if (last != nullptr) {
        start = last;
        while (start > path && *(start - 1) != L'\\') --start;
    }
    wcsncpy_s(name, static_cast<size_t>(capacity), start, _TRUNCATE);
}

// True for a module whose file name is d3d9.dll: the system Direct3D 9
// runtime or a wrapper that replaces it. Several can be loaded at once (a
// wrapper in the OMSI folder and the system runtime it forwards to), so the
// module is identified by name, not by GetModuleHandle("d3d9.dll"), which
// returns only one of them.
bool IsD3D9Module(HMODULE module) {
    wchar_t path[MAX_PATH]{};
    if (module == nullptr || GetModuleFileNameW(module, path, MAX_PATH) == 0) return false;
    const wchar_t* name = wcsrchr(path, L'\\');
    name = name == nullptr ? path : name + 1;
    return _wcsicmp(name, L"d3d9.dll") == 0;
}

// --- Self check fixtures (engine only; no OMSI code is involved) ---
int __stdcall SelfCheckSquare(int value) { return value * value; }
int __stdcall SelfCheckDetourA(int value);
int __stdcall SelfCheckDetourB(int value);
using SelfCheckFunction = int (__stdcall*)(int);
SelfCheckFunction gSelfCheckOriginalA = nullptr;
SelfCheckFunction gSelfCheckOriginalB = nullptr;
int __stdcall SelfCheckDetourA(int value) { return gSelfCheckOriginalA(value) + 1; }
int __stdcall SelfCheckDetourB(int value) { return gSelfCheckOriginalB(value) + 100; }
void* gSelfCheckTable[2] = { reinterpret_cast<void*>(&SelfCheckSquare), nullptr };

// push ebp; mov ebp, esp; mov eax, [ebp+8]; imul eax, eax; pop ebp; ret 4
__declspec(naked) int __stdcall SelfCheckPrologue(int) {
    __asm {
        push ebp
        mov ebp, esp
        mov eax, [ebp + 8]
        imul eax, eax
        pop ebp
        ret 4
    }
}

// Not inlined: the optimizer may otherwise keep the import address in a
// register across calls and never read the patched entry.
__declspec(noinline) DWORD SelfCheckImportedCall() { return GetCurrentProcessId(); }
DWORD __stdcall SelfCheckImportDetour() { return 0x0BADF00Du; }

} // namespace

static int InstallIatCore(const char* owner, const wchar_t* hostModule, const char* importModule, const char* function, void* detour, void** original, int* id) {
    if (importModule == nullptr || function == nullptr) return OL_HOOK_E_ARGUMENT;
    const auto host = hostModule == nullptr ? GetModuleHandleW(nullptr) : GetModuleHandleW(hostModule);
    const auto imported = GetModuleHandleA(importModule);
    if (host == nullptr || imported == nullptr) return OL_HOOK_E_NOT_FOUND;
    void** slots[MaxImportSlots]{};
    const auto count = FindImports(host, importModule, function, slots, MaxImportSlots);
    auto* exported = reinterpret_cast<void*>(GetProcAddress(imported, function));
    if (count == 0 || exported == nullptr) return OL_HOOK_E_NOT_FOUND;
    return InstallPointers(owner, OL_HOOK_KIND_IAT, slots, count, exported, detour, original, id);
}

// How many import address table entries a function has (diagnostics).
extern "C" __declspec(dllexport) int __cdecl NativeHookImportSlots(const wchar_t* hostModule, const char* importModule, const char* function) {
    if (importModule == nullptr || function == nullptr) return 0;
    const auto host = hostModule == nullptr ? GetModuleHandleW(nullptr) : GetModuleHandleW(hostModule);
    if (host == nullptr) return 0;
    void** slots[MaxImportSlots]{};
    return FindImports(host, importModule, function, slots, MaxImportSlots);
}

extern "C" __declspec(dllexport) int __cdecl NativeHookInstallSlot(const char* owner, void** slot, void* expected, void* detour, void** original, int* id) {
    return InstallPointer(owner, OL_HOOK_KIND_SLOT, slot, expected, detour, original, id);
}

// A method of the live Direct3D 9 device (vtable slot index). The slot must
// point into a module named d3d9.dll, or hold the registry's top detour.
static int InstallD3DDeviceCore(const char* owner, int slotIndex, void* detour, void** original, int* id) {
    if (slotIndex < 0 || slotIndex > 118) return OL_HOOK_E_ARGUMENT;
    auto* device = HookCurrentD3DDevice();
    if (device == nullptr) return OL_HOOK_E_NO_DEVICE;
    auto** vtable = *reinterpret_cast<void***>(device);
    if (!Readable(vtable + slotIndex, sizeof(void*))) return OL_HOOK_E_NOT_FOUND;
    auto** slot = vtable + slotIndex;
    void* expected = *slot;
    HMODULE owning = nullptr;
    {
        Locked lock;
        if (TopOfSlot(slot) == nullptr &&
            (!GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT, reinterpret_cast<LPCWSTR>(expected), &owning) ||
             !IsD3D9Module(owning))) return OL_HOOK_E_FOREIGN;
    }
    return InstallPointer(owner, OL_HOOK_KIND_SLOT, slot, expected, detour, original, id);
}

static int InstallPrologueCore(const char* owner, void* target, const unsigned char* expected, int length, void* detour, void** original, int* id) {
    if (target == nullptr || expected == nullptr || detour == nullptr || original == nullptr || id == nullptr || length < 5 || length > MaxPrologue) return OL_HOOK_E_ARGUMENT;
    auto* code = static_cast<unsigned char*>(target);
    if (!Readable(code, static_cast<size_t>(length))) return OL_HOOK_E_ARGUMENT;
    // A trampoline can hold at most the displaced bytes and a jmp back.
    auto* trampoline = static_cast<unsigned char*>(VirtualAlloc(nullptr, 64, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE));
    if (trampoline == nullptr) return OL_HOOK_E_MEMORY;
    Locked lock;
    auto* entry = FreeEntry();
    HookEntry candidate{};
    auto fail = [&](int code) { VirtualFree(trampoline, 0, MEM_RELEASE); return code; };
    if (entry == nullptr) return fail(OL_HOOK_E_FULL);
    if (!CopyOwner(candidate.owner, owner)) return fail(OL_HOOK_E_ARGUMENT);
    if (TopOfTarget(code) != nullptr) return fail(OL_HOOK_E_ORDER);
    if (memcmp(code, expected, static_cast<size_t>(length)) != 0) return fail(code[0] == 0xE9 || code[0] == 0xE8 || code[0] == 0xEB ? OL_HOOK_E_FOREIGN : OL_HOOK_E_UNEXPECTED);
    memcpy(trampoline, code, static_cast<size_t>(length));
    trampoline[length] = 0xE9;
    *reinterpret_cast<int32_t*>(trampoline + length + 1) = static_cast<int32_t>(reinterpret_cast<intptr_t>(code + length) - reinterpret_cast<intptr_t>(trampoline + length + 5));
    DWORD ignored = 0;
    if (!VirtualProtect(trampoline, 64, PAGE_EXECUTE_READ, &ignored)) return fail(OL_HOOK_E_PROTECT);
    FlushInstructionCache(GetCurrentProcess(), trampoline, 64);
    unsigned char patch[MaxPrologue];
    memset(patch, 0x90, sizeof(patch));
    patch[0] = 0xE9;
    *reinterpret_cast<int32_t*>(patch + 1) = static_cast<int32_t>(reinterpret_cast<intptr_t>(detour) - reinterpret_cast<intptr_t>(code + 5));
    const auto written = WriteCode(code, patch, length);
    if (written != OL_HOOK_OK) return fail(written);
    candidate.id = gNextHookId++;
    candidate.group = candidate.id;
    candidate.kind = OL_HOOK_KIND_PROLOGUE;
    candidate.target = code;
    memcpy(candidate.saved, expected, static_cast<size_t>(length));
    candidate.length = length;
    candidate.trampoline = trampoline;
    candidate.original = trampoline;
    candidate.detour = detour;
    candidate.active = true;
    *entry = candidate;
    *original = trampoline;
    *id = candidate.id;
    return OL_HOOK_OK;
}

// Removes every entry of the group the id names; the first failure is returned.
extern "C" __declspec(dllexport) int __cdecl NativeHookRemove(int id) {
    Locked lock;
    int result = OL_HOOK_E_NOT_FOUND;
    for (auto& entry : gHooks) {
        if (!entry.active || entry.group != id) continue;
        const auto removed = RemoveEntry(&entry);
        if (result == OL_HOOK_E_NOT_FOUND || (result == OL_HOOK_OK && removed != OL_HOOK_OK)) result = removed;
    }
    return result;
}

// Removes every hook of an owner, newest first. *remaining receives how many
// of its hooks are still installed (a hook of another owner on top, or a
// site overwritten by someone else).
static int RemoveOwnerCore(const char* owner, int* remaining) {
    if (owner == nullptr) return OL_HOOK_E_ARGUMENT;
    Locked lock;
    int first = OL_HOOK_OK;
    for (;;) {
        HookEntry* newest = nullptr;
        for (auto& entry : gHooks)
            if (entry.active && strncmp(entry.owner, owner, OwnerCapacity) == 0 && entry.length >= 0 && (newest == nullptr || entry.id > newest->id)) newest = &entry;
        if (newest == nullptr) break;
        const auto result = RemoveEntry(newest);
        if (result != OL_HOOK_OK) {
            if (first == OL_HOOK_OK) first = result;
            newest->length = -1 - newest->length;   // skip it in this pass
        }
    }
    int left = 0;
    for (auto& entry : gHooks) {
        if (!entry.active || strncmp(entry.owner, owner, OwnerCapacity) != 0) continue;
        if (entry.length < 0) entry.length = -1 - entry.length;
        ++left;
    }
    if (remaining != nullptr) *remaining = left;
    return first;
}

// 0 while every site of the hook still holds it; OL_HOOK_E_FOREIGN when one
// was overwritten.
// ---- trace (/trace-native): every install and removal is reported through
// the callback the plugin registers; the callback runs outside the registry lock.
using NativeTraceCallback = void(__cdecl*)(const char* boundary, const char* data);
static NativeTraceCallback volatile gTraceCallback = nullptr;

extern "C" __declspec(dllexport) void __cdecl NativeSetTraceCallback(NativeTraceCallback callback) { gTraceCallback = callback; }

static void TraceHook(const char* boundary, const char* format, ...) {
    const auto callback = gTraceCallback;
    if (callback == nullptr) return;
    char data[512];
    va_list arguments;
    va_start(arguments, format);
    vsnprintf_s(data, sizeof(data), _TRUNCATE, format, arguments);
    va_end(arguments);
    callback(boundary, data);
}

static const char* Text(const char* value) { return value == nullptr ? "" : value; }

extern "C" __declspec(dllexport) int __cdecl NativeHookInstallIat(const char* owner, const wchar_t* hostModule, const char* importModule, const char* function, void* detour, void** original, int* id) {
    const auto result = InstallIatCore(owner, hostModule, importModule, function, detour, original, id);
    TraceHook("hook.install.iat", "owner=%s target=%s!%s result=%d id=%d", Text(owner), Text(importModule), Text(function), result, result == OL_HOOK_OK && id != nullptr ? *id : 0);
    return result;
}

extern "C" __declspec(dllexport) int __cdecl NativeHookInstallD3DDevice(const char* owner, int slotIndex, void* detour, void** original, int* id) {
    const auto result = InstallD3DDeviceCore(owner, slotIndex, detour, original, id);
    TraceHook("hook.install.d3d9", "owner=%s slot=%d result=%d id=%d", Text(owner), slotIndex, result, result == OL_HOOK_OK && id != nullptr ? *id : 0);
    return result;
}

extern "C" __declspec(dllexport) int __cdecl NativeHookInstallPrologue(const char* owner, void* target, const unsigned char* expected, int length, void* detour, void** original, int* id) {
    const auto result = InstallPrologueCore(owner, target, expected, length, detour, original, id);
    TraceHook("hook.install.prologue", "owner=%s length=%d result=%d id=%d", Text(owner), length, result, result == OL_HOOK_OK && id != nullptr ? *id : 0);
    return result;
}

extern "C" __declspec(dllexport) int __cdecl NativeHookRemoveOwner(const char* owner, int* remaining) {
    int left = 0;
    const auto result = RemoveOwnerCore(owner, &left);
    if (remaining != nullptr) *remaining = left;
    TraceHook("hook.remove.owner", "owner=%s result=%d remaining=%d", Text(owner), result, left);
    return result;
}

extern "C" __declspec(dllexport) int __cdecl NativeHookVerify(int id) {
    Locked lock;
    HookEntry* changed = nullptr;
    return VerifyGroup(id, &changed);
}

// Diagnostics: the module that owns the value currently at a hook's site
// (the first overwritten site of the group, when NativeHookVerify reports
// OL_HOOK_E_FOREIGN).
extern "C" __declspec(dllexport) int __cdecl NativeHookSiteOwner(int id, wchar_t* name, int capacity) {
    Locked lock;
    HookEntry* entry = nullptr;
    if (VerifyGroup(id, &entry) == OL_HOOK_E_NOT_FOUND || entry == nullptr) return OL_HOOK_E_NOT_FOUND;
    const void* value = entry->kind == OL_HOOK_KIND_PROLOGUE
        ? (entry->target[0] == 0xE9 ? static_cast<const void*>(entry->target + 5 + *reinterpret_cast<int32_t*>(entry->target + 1)) : static_cast<const void*>(entry->target))
        : *entry->slot;
    OwnerName(value, name, capacity);
    return OL_HOOK_OK;
}

// Diagnostics: the module that owns a method of the live Direct3D 9 device.
extern "C" __declspec(dllexport) int __cdecl NativeHookD3DSlotOwner(int slotIndex, wchar_t* name, int capacity) {
    if (slotIndex < 0 || slotIndex > 118) return OL_HOOK_E_ARGUMENT;
    auto* device = HookCurrentD3DDevice();
    if (device == nullptr) return OL_HOOK_E_NO_DEVICE;
    auto** vtable = *reinterpret_cast<void***>(device);
    if (!Readable(vtable + slotIndex, sizeof(void*))) return OL_HOOK_E_NOT_FOUND;
    OwnerName(vtable[slotIndex], name, capacity);
    return OL_HOOK_OK;
}

// Hooks installed for an owner (null: all owners).
extern "C" __declspec(dllexport) int __cdecl NativeHookCount(const char* owner) {
    Locked lock;
    int count = 0;
    for (auto& entry : gHooks) if (entry.active && (owner == nullptr || strncmp(entry.owner, owner, OwnerCapacity) == 0)) ++count;
    return count;
}

int HookAdoptSlot(const char* owner, void** slot, void* original, void* detour, int* id) {
    if (slot == nullptr || id == nullptr) return OL_HOOK_E_ARGUMENT;
    Locked lock;
    auto* entry = FreeEntry();
    if (entry == nullptr) return OL_HOOK_E_FULL;
    HookEntry candidate{};
    if (!CopyOwner(candidate.owner, owner)) return OL_HOOK_E_ARGUMENT;
    candidate.id = gNextHookId++;
    candidate.group = candidate.id;
    candidate.kind = OL_HOOK_KIND_SLOT;
    candidate.slot = slot;
    candidate.original = original;
    candidate.detour = detour;
    candidate.active = true;
    *entry = candidate;
    *id = candidate.id;
    return OL_HOOK_OK;
}

int HookForget(int id) {
    Locked lock;
    auto* entry = FindEntry(id);
    if (entry == nullptr) return OL_HOOK_E_NOT_FOUND;
    if (TopOfSlot(entry->slot) != entry) return OL_HOOK_E_ORDER;
    entry->active = false;
    return OL_HOOK_OK;
}

// Exercises the engine on fixtures inside this DLL: IAT, slot (with stacking,
// order and foreign detection) and prologue hooks, each removed afterwards.
// Returns a bit per passed check; 0x7F when all pass.
extern "C" __declspec(dllexport) int __cdecl NativeHookSelfCheck() {
    int passed = 0;
    HMODULE self = nullptr;
    GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT, reinterpret_cast<LPCWSTR>(&NativeHookSelfCheck), &self);
    wchar_t selfPath[MAX_PATH]{};
    GetModuleFileNameW(self, selfPath, MAX_PATH);
    const wchar_t* selfName = wcsrchr(selfPath, L'\\');
    selfName = selfName == nullptr ? selfPath : selfName + 1;

    // 1: IAT hook of this DLL's own kernel32 import, then removal.
    void* original = nullptr; int iat = 0;
    const auto realId = GetCurrentProcessId();
    if (NativeHookInstallIat("selfcheck", selfName, "kernel32.dll", "GetCurrentProcessId", reinterpret_cast<void*>(&SelfCheckImportDetour), &original, &iat) == OL_HOOK_OK &&
        SelfCheckImportedCall() == 0x0BADF00Du && NativeHookRemove(iat) == OL_HOOK_OK && SelfCheckImportedCall() == realId) passed |= 0x01;

    // 2: two stacked slot hooks; the lower one cannot be removed first.
    int a = 0, b = 0;
    auto** slot = &gSelfCheckTable[0];
    if (NativeHookInstallSlot("selfcheck", slot, reinterpret_cast<void*>(&SelfCheckSquare), reinterpret_cast<void*>(&SelfCheckDetourA), reinterpret_cast<void**>(&gSelfCheckOriginalA), &a) == OL_HOOK_OK &&
        NativeHookInstallSlot("selfcheck.b", slot, reinterpret_cast<void*>(&SelfCheckSquare), reinterpret_cast<void*>(&SelfCheckDetourB), reinterpret_cast<void**>(&gSelfCheckOriginalB), &b) == OL_HOOK_OK &&
        reinterpret_cast<SelfCheckFunction>(*slot)(3) == 110) passed |= 0x02;
    if (NativeHookRemove(a) == OL_HOOK_E_ORDER && NativeHookVerify(a) == OL_HOOK_OK) passed |= 0x04;

    // 3: a site overwritten from outside is reported, and blocks removal.
    auto* saved = *slot;
    *slot = reinterpret_cast<void*>(&SelfCheckSquare);
    if (NativeHookVerify(b) == OL_HOOK_E_FOREIGN && NativeHookRemove(b) == OL_HOOK_E_FOREIGN) passed |= 0x08;
    *slot = saved;
    int left = -1;
    if (NativeHookRemoveOwner("selfcheck.b", &left) == OL_HOOK_OK && left == 0 && NativeHookRemoveOwner("selfcheck", &left) == OL_HOOK_OK && left == 0 &&
        *slot == reinterpret_cast<void*>(&SelfCheckSquare) && NativeHookCount("selfcheck") == 0) passed |= 0x10;

    // 4: prologue hook with a trampoline; wrong bytes are refused.
    const unsigned char prologue[6] = { 0x55, 0x8B, 0xEC, 0x8B, 0x45, 0x08 };
    const unsigned char wrong[6] = { 0x55, 0x8B, 0xEC, 0x8B, 0x45, 0x09 };
    int p = 0; void* unused = nullptr;
    if (NativeHookInstallPrologue("selfcheck", reinterpret_cast<void*>(&SelfCheckPrologue), wrong, 6, reinterpret_cast<void*>(&SelfCheckDetourA), &unused, &p) == OL_HOOK_E_UNEXPECTED) passed |= 0x20;
    if (NativeHookInstallPrologue("selfcheck", reinterpret_cast<void*>(&SelfCheckPrologue), prologue, 6, reinterpret_cast<void*>(&SelfCheckDetourA), reinterpret_cast<void**>(&gSelfCheckOriginalA), &p) == OL_HOOK_OK &&
        SelfCheckPrologue(4) == 17 && NativeHookVerify(p) == OL_HOOK_OK && NativeHookRemove(p) == OL_HOOK_OK && SelfCheckPrologue(4) == 16 &&
        memcmp(reinterpret_cast<void*>(&SelfCheckPrologue), prologue, 6) == 0) passed |= 0x40;
    return passed;
}
