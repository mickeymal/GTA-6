# PyInstaller spec for Vice Bay Stories (Pygame).
# Build a standalone executable:
#     pyinstaller ViceBayStories.spec
# Output: dist/ViceBayStories(.exe on Windows)
#
# Run this on the OS you want the executable for:
#   * Windows  -> ViceBayStories.exe   (build on Windows)
#   * macOS    -> ViceBayStories.app / binary (build on macOS)
#   * Linux    -> ViceBayStories       (build on Linux)
# PyInstaller does NOT cross-compile; build on the target OS.

block_cipher = None

a = Analysis(
    ['main.py'],
    pathex=[],
    binaries=[],
    datas=[],                      # game draws everything procedurally; no asset files to bundle
    hiddenimports=[
        'settings', 'camera', 'world', 'player', 'particles', 'hud',
        'police', 'dialogue', 'missions', 'economy', 'activities',
        'npc', 'vehicle', 'weapons', 'savegame',
    ],
    hookspath=[],
    runtime_hooks=[],
    excludes=['numpy', 'PIL', 'tkinter'],
    cipher=block_cipher,
)

pyz = PYZ(a.pure, a.zipped_data, cipher=block_cipher)

exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.zipfiles,
    a.datas,
    [],
    name='ViceBayStories',
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    runtime_tmpdir=None,
    console=False,                 # no console window; set True to see tracebacks
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
)
