# taskbar-keys

![Ctrl+Shift+K: pressed, released, fading out](docs/keys.gif)

Shows the keys you're pressing (modifiers too) in the Windows 11 taskbar, next to the tray.

Held keys are filled, released ones get an outline and fade out after 1.5 s. Light/dark theme and DPI are picked up
automatically, and it hides along with the taskbar in fullscreen apps.

Keys are read via raw input, so typing isn't slowed down at all. Idle CPU is zero.

Tray menu has Pause, Start with Windows, Restart and Exit.

## Usage

```
taskbar-keys.exe [--width 300] [--gap 8] [--linger 1500] [--hook]
taskbar-keys.exe --exit
```

- `--width` - width of the area keys are shown in, DIP (default 300)
- `--gap` - space before the tray, DIP (default 8)
- `--linger` - how long released keys stay, ms (default 1500)
- `--hook` - use a keyboard hook instead of raw input, see below
- `--exit` - close the running instance

### `--hook`

Raw input gets keys only after other apps' keyboard hooks are done with them. PowerToys Keyboard Manager, for example, holds
Win back when you have Win shortcuts remapped, so Win only flashes on release.

With `--hook` taskbar-keys sets its own hook. The newest hook runs first, so it sees Win before PowerToys does.

The downsides: every keypress has to go through the hook (cheap, but not free), keys sent by other programs aren't shown,
and if PowerToys restarts it's first in line again - use Restart from the tray menu then.

## Build

.NET 10 SDK plus the Visual Studio C++ build tools (NativeAOT):

```
dotnet publish -c Release -o publish
```

You get a single ~1.2 MB `publish\taskbar-keys.exe`.

## Limitations

- Only the main monitor's taskbar.
- Keys typed into admin windows may not show up.
