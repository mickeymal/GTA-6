# Building an executable (.exe / .app / binary)

This repo has **two** games. Here's how to turn each into a double-click executable.
Neither can be cross-compiled — **build on the OS you want the executable for**
(build on Windows to get a `.exe`).

---

## 1. Vice Bay Stories (Python/Pygame) — easiest, ~20 MB standalone

This one packages into a single self-contained executable with **no Python required**
to run it. Verified building cleanly with PyInstaller 6.x.

### Windows (produces `ViceBayStories.exe`)
```bat
pip install pygame pyinstaller
python build_exe.py
```
Result: **`dist\ViceBayStories.exe`** — double-click to play. Copy that single file
anywhere; it needs nothing else installed.

### macOS / Linux
```bash
pip install pygame pyinstaller
python build_exe.py
```
Result: `dist/ViceBayStories` (Linux) or a Mac binary/`.app`.

### Notes
- The build config is `ViceBayStories.spec`. It bundles all game modules; the game
  draws everything procedurally so there are no asset files to include.
- `console=False` hides the terminal window. If the exe fails to start and you want
  to see the error, edit the spec and set `console=True`, then rebuild.
- Windows SmartScreen may warn on an unsigned exe the first time — "More info ▸ Run
  anyway", or code-sign it for distribution.

---

## 2. Vice Bay Empire (Unity/C#) — requires the Unity Editor

A Unity game **cannot** be built without the Unity Editor and the matching platform
Build Support module. There is no way to produce this `.exe` from source alone.

### Steps
1. Install **Unity 2022.3 LTS** via Unity Hub, and in the Hub add
   **Windows Build Support (IL2CPP/Mono)** to that Unity version (and/or Mac/Linux
   support for those targets).
2. Open the `Vice-Bay-Empire-Unity` project (copy its `Assets/` into a Unity project
   as described in that folder's README).
3. Menu bar → **ViceBay ▸ Create ViceBay_MainScene** (once).
4. Menu bar → **ViceBay ▸ Build ▸ Windows (.exe)**
   (or macOS / Linux). Output goes to `Builds/Windows/ViceBayEmpire.exe`.

### Headless / command line
```bat
"C:\Program Files\Unity\Hub\Editor\2022.3.x\Editor\Unity.exe" ^
  -quit -batchmode -projectPath "C:\path\to\Vice-Bay-Empire-Unity" ^
  -executeMethod ViceBayEmpire.EditorTools.BuildScript.BuildWindows
```
The build logic is `Assets/Editor/BuildScript.cs`.

> You can only build a Windows `.exe` on a machine whose Unity install has **Windows
> Build Support**; likewise for Mac/Linux. Unity does support building a Windows exe
> *from* macOS/Linux **if** Windows Build Support is installed for that Editor.

---

## Why I can't hand you a prebuilt .exe from here

These games were assembled in a headless Linux cloud container with **no Unity Editor
and no Windows toolchain**. PyInstaller and Unity both build *natively* — they don't
cross-compile a Windows binary from Linux. So the reliable path is the one-command
build above on your own machine. The Pygame packaging was test-built here (as a Linux
binary) to confirm the spec is correct, so `python build_exe.py` should "just work".
