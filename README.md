# taskbar-keys

![Ctrl+Shift+K: pressed, released, fading out](docs/keys.gif)

Shows the keys you are pressing, modifiers included, in the Windows 11 taskbar next to the tray.

- Held keys are filled; released keys turn outlined and dim, then fade out after 1.5 s.
- Follows the light/dark taskbar theme and DPI; hides together with the taskbar (fullscreen apps, auto-hide).
- By default reads keys through a raw input sink, not a keyboard hook, so it never delays input. No CPU use while idle.
- Tray icon menu: **Pause**, **Start with Windows**, **Exit**.

## Usage

```
taskbar-keys.exe [--width 300] [--gap 8] [--linger 1500] [--hook]
taskbar-keys.exe --exit
```

| Option | Default | Meaning |
|---|---|---|
| `--width` | 300 | Display area width in DIP; keys are right-aligned in it |
| `--gap` | 8 | Space between the keys and the tray, in DIP |
| `--linger` | 1500 | How long released keys stay visible, in ms |
| `--hook` | off | Use a low-level keyboard hook instead of raw input, see below |
| `--exit` | | Stops the running instance |

### `--hook`

By default taskbar-keys reads raw input, which only sees keys after every other app's keyboard hook has passed them on.
Remapping tools such as PowerToys hold some keys back: with Win shortcuts remapped, Win only shows up as a short tap on release.
`--hook` installs a low-level keyboard hook instead. Windows calls the newest hook first, so it sees keys before those tools do.

Pros:
- Keys held back by remapping tools show while they are held.
- Shows the physical keys you press, not the keys remaps and macros send.

Cons:
- Every key event waits for the hook. It only posts a message, so the delay is microseconds, but no longer zero.
- If taskbar-keys hangs, typing lags until Windows gives up on the hook.
- If the other tool restarts after taskbar-keys, its hook runs first again. Use **Restart** in the tray menu.
- Keys sent by other apps (on-screen keyboard, remote tools, macros) are not shown.

## Build

Needs the .NET 10 SDK and the Visual Studio C++ build tools (for NativeAOT).

```
dotnet publish -c Release -o publish
```

The result is a single ~1.2 MB `publish\taskbar-keys.exe`.

## Limitations

- Primary monitor's taskbar only.
- Keys typed into elevated (admin) windows may not show.
- Without `--hook`, keys held back by remapping tools show only on release.
