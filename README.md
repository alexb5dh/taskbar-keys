# taskbar-keys

Shows the keys you are pressing, modifiers included, in the Windows 11 taskbar next to the tray.

- Held keys are filled; released keys turn outlined and dim, then fade out after 1.5 s.
- Follows the light/dark taskbar theme and DPI; hides together with the taskbar (fullscreen apps, auto-hide).
- Reads keys through a raw input sink, not a keyboard hook, so it never delays input. No CPU use while idle.
- Tray icon menu: **Pause**, **Start with Windows**, **Exit**.

## Usage

```
taskbar-keys.exe [--width 300] [--gap 8] [--linger 1500]
taskbar-keys.exe --exit
```

| Option | Default | Meaning |
|---|---|---|
| `--width` | 300 | Display area width in DIP; keys are right-aligned in it |
| `--gap` | 8 | Space between the keys and the tray, in DIP |
| `--linger` | 1500 | How long released keys stay visible, in ms |
| `--exit` | | Stops the running instance |

## Build

Needs the .NET 10 SDK and the Visual Studio C++ build tools (for NativeAOT).

```
dotnet publish -c Release -o publish
```

The result is a single ~1.2 MB `publish\taskbar-keys.exe`.

## Limitations

- Primary monitor's taskbar only.
- Keys typed into elevated (admin) windows may not show.
- It shows everything you type, passwords included: pause it before sharing your screen.
