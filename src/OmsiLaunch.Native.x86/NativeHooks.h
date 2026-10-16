#pragma once
#include <windows.h>
#include <stdint.h>

// Native hook infrastructure (Beta 4): one registry for every hook the bridge
// installs for the product or a runtime module. Every hook has an owner; a
// hook is installed only over the value or bytes the caller expects (or over
// the registry's own top hook on that site, which stacks); hooks on a site are
// removed last-in first-out; a site overwritten by someone else is reported.

enum OlHookResult : int {
    OL_HOOK_OK = 0,
    OL_HOOK_E_ARGUMENT = 1,
    OL_HOOK_E_NOT_FOUND = 2,
    OL_HOOK_E_UNEXPECTED = 3,   // the site does not hold the expected value or bytes
    OL_HOOK_E_FOREIGN = 4,      // someone outside the registry hooked the site
    OL_HOOK_E_PROTECT = 5,
    OL_HOOK_E_FULL = 6,
    OL_HOOK_E_ORDER = 7,        // another hook sits on top of this one
    OL_HOOK_E_MEMORY = 8,
    OL_HOOK_E_BUSY = 9,         // a thread was executing the bytes to patch
    OL_HOOK_E_NO_DEVICE = 10,
    OL_HOOK_E_NOT_ALLOWED = 11, // outside start/gameplay-ready, or the capability is not declared
};

enum OlHookKind : int { OL_HOOK_KIND_IAT = 1, OL_HOOK_KIND_SLOT = 2, OL_HOOK_KIND_PROLOGUE = 3 };

// Records a pointer-slot hook the product installed itself, so hooks
// requested later can stack on it and removal order is enforced.
int HookAdoptSlot(const char* owner, void** slot, void* original, void* detour, int* id);
// Forgets an adopted hook before its owner restores the slot itself.
int HookForget(int id);
// The live Direct3D 9 device the bridge observes, or null.
void* HookCurrentD3DDevice();
