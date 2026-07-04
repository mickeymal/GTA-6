"""Build Vice Bay Stories into a standalone executable.

Usage:
    pip install pygame pyinstaller
    python build_exe.py

Produces:
    Windows : dist/ViceBayStories.exe
    macOS   : dist/ViceBayStories        (or an .app bundle)
    Linux   : dist/ViceBayStories

IMPORTANT: PyInstaller does not cross-compile. Run this on the operating system
you want the executable for — build on Windows to get a .exe, on macOS for a Mac
binary, on Linux for a Linux binary.
"""
import subprocess
import sys
import platform


def main():
    try:
        import PyInstaller  # noqa: F401
    except ImportError:
        print("PyInstaller is not installed. Run:  pip install pyinstaller")
        sys.exit(1)

    print(f"Building Vice Bay Stories for {platform.system()} ({platform.machine()})...")
    cmd = [sys.executable, "-m", "PyInstaller", "ViceBayStories.spec", "--noconfirm", "--clean"]
    result = subprocess.run(cmd)
    if result.returncode != 0:
        print("Build failed.")
        sys.exit(result.returncode)

    ext = ".exe" if platform.system() == "Windows" else ""
    print("\nDone. Your executable is:")
    print(f"    dist/ViceBayStories{ext}")
    print("Double-click it (or run from a terminal) to play — no Python needed.")


if __name__ == "__main__":
    main()
