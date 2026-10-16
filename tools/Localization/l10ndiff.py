"""Usage (from the repository root): python l10ndiff.py <locale> <page> [--list]

Finds the English revision a translated page follows (the banner's source-sha256),
and prints a unified diff from that English text to the current English page.
With --list prints only the status line. Exit 0 always."""
import hashlib, re, subprocess, sys, difflib, pathlib

locale, page = sys.argv[1], sys.argv[2]
root = pathlib.Path.cwd()
translated = root / "docs" / "localized" / locale / page
english = root / "docs" / page

def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()

def git(*args):
    return subprocess.run(["git", *args], capture_output=True, cwd=root)

text = translated.read_text(encoding="utf-8") if translated.exists() else ""
m = re.search(r"source-sha256=([0-9a-f]{64}|unknown)", text)
recorded = m.group(1) if m else None
current = english.read_bytes()
if recorded == sha(current):
    print("STATUS current: nothing to do"); sys.exit(0)

revisions = git("log", "--format=%H", "--", f"docs/{page}").stdout.decode().split()
match = None
for rev in revisions:
    blob = git("show", f"{rev}:docs/{page}")
    if blob.returncode == 0 and (recorded == sha(blob.stdout) or recorded == sha(blob.stdout.replace(b"\r\n", b"\n"))):
        match = (rev, blob.stdout); break
if match is None:
    print(f"STATUS no-revision-match (recorded={recorded}): translate the page fully from the English page"); sys.exit(0)
if "--list" in sys.argv:
    print(f"STATUS update from {match[0][:7]}"); sys.exit(0)
old = match[1].decode("utf-8").replace("\r\n", "\n").splitlines()
new = current.decode("utf-8").replace("\r\n", "\n").splitlines()
print(f"STATUS update from {match[0][:7]}: English diff follows (old English -> current English)")
for line in difflib.unified_diff(old, new, "old-english", "current-english", n=1, lineterm=""):
    print(line)
