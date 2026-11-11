using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static TaskbarKeys.Native;

namespace TaskbarKeys;

static unsafe class App
{
    const string HostClass = "TaskbarKeysHost", OverlayClass = "TaskbarKeysOverlay", Title = "Taskbar Keys";
    const nuint SyncTimer = 1, KeysTimer = 2;
    const uint SyncIntervalMs = 1000, KeysIntervalMs = 100;
    const uint TrayMessage = WM_APP + 1;
    const uint NIN_SELECT = 0x400, NIN_KEYSELECT = 0x401;
    const nuint PauseCommand = 1, StartupCommand = 2, ExitCommand = 3;

    static nint _host;
    static uint _taskbarCreatedMessage;
    static Overlay _overlay = null!;
    static KeyTracker _keys = null!;
    static TrayIcon _tray = null!;
    static string _startupCommand = "";
    static bool _keysTimerRunning, _paused, _lightTheme;

    static int Main(string[] args)
    {
        var options = Options.Parse(args);
        if (options.Exit)
        {
            var running = FindWindowW(HostClass, null);
            if (running != 0)
                PostMessageW(running, WM_CLOSE, 0, 0);
            return 0;
        }

        CreateMutexW(0, 0, @"Local\TaskbarKeys");
        if (Marshal.GetLastPInvokeError() == 183 /* ERROR_ALREADY_EXISTS */)
            return 0;

        _startupCommand = string.Join(" ", new[] { Environment.ProcessPath! }.Concat(args).Select(a => $"\"{a}\""));
        var instance = GetModuleHandleW(0);
        RegisterWindowClass(HostClass, instance);
        RegisterWindowClass(OverlayClass, instance);
        _taskbarCreatedMessage = RegisterWindowMessageW("TaskbarCreated");

        _lightTheme = Settings.IsLightTheme();
        _keys = new KeyTracker(options.LingerMs);
        _overlay = new Overlay(instance, OverlayClass, options.WidthDip, options.GapDip);
        _overlay.SetLightTheme(_lightTheme);

        // Hidden top-level window: raw input, tray callbacks, broadcasts.
        _host = CreateWindowExW(WS_EX_TOOLWINDOW, HostClass, Title, WS_POPUP, 0, 0, 0, 0, 0, 0, instance, 0);
        _tray = new TrayIcon(_host, TrayMessage);
        RegisterKeyboard(enable: true);
        _overlay.Sync();
        UpdateTray();
        SetTimer(_host, SyncTimer, SyncIntervalMs, 0);

        MSG msg;
        while (GetMessageW(&msg, 0, 0, 0) > 0)
        {
            TranslateMessage(&msg);
            DispatchMessageW(&msg);
        }

        _overlay.Dispose();
        return 0;
    }

    static void RegisterWindowClass(string name, nint instance)
    {
        fixed (char* className = name)
        {
            var wc = new WNDCLASSEXW
            {
                Size = (uint)sizeof(WNDCLASSEXW),
                WndProc = &WndProc,
                Instance = instance,
                ClassName = className,
            };
            RegisterClassExW(&wc);
        }
    }

    /// <summary> Raw input sink: gets async copies of key events, so unlike a low-level hook it never delays input. </summary>
    static void RegisterKeyboard(bool enable)
    {
        var device = new RAWINPUTDEVICE
        {
            UsagePage = 0x01,
            Usage = 0x06,
            Flags = enable ? 0x100u /* RIDEV_INPUTSINK */ : 0x1u /* RIDEV_REMOVE */,
            Target = enable ? _host : 0,
        };
        RegisterRawInputDevices(&device, 1, (uint)sizeof(RAWINPUTDEVICE));
    }

    [UnmanagedCallersOnly]
    static nint WndProc(nint hwnd, uint msg, nint wParam, nint lParam)
    {
        if (hwnd != _host)
            return msg == WM_NCHITTEST ? HTTRANSPARENT : DefWindowProcW(hwnd, msg, wParam, lParam);

        switch (msg)
        {
            case WM_INPUT:
                OnRawInput(lParam);
                break;
            case WM_TIMER when wParam == (nint)SyncTimer:
                _overlay.Sync();
                return 0;
            case WM_TIMER when wParam == (nint)KeysTimer:
                if (_keys.OnTimer(Environment.TickCount64))
                    _overlay.Show(_keys.Shown);
                UpdateKeysTimer();
                return 0;
            case TrayMessage:
                if ((uint)(lParam & 0xFFFF) is WM_CONTEXTMENU or NIN_SELECT or NIN_KEYSELECT)
                    ShowMenu((short)(wParam & 0xFFFF), (short)((wParam >> 16) & 0xFFFF));
                return 0;
            case WM_SETTINGCHANGE:
                OnSettingChange();
                break;
            case WM_CLOSE:
                DestroyWindow(hwnd);
                return 0;
            case WM_DESTROY:
                _tray.Dispose();
                PostQuitMessage(0);
                return 0;
            default:
                if (msg == _taskbarCreatedMessage && msg != 0)
                {
                    _overlay.Sync();
                    UpdateTray();
                    return 0;
                }
                break;
        }

        return DefWindowProcW(hwnd, msg, wParam, lParam);
    }

    static void ShowMenu(int x, int y)
    {
        const uint MF_CHECKED = 0x8, MF_SEPARATOR = 0x800, TPM_RIGHTBUTTON = 0x2, TPM_RETURNCMD = 0x100;

        var menu = CreatePopupMenu();
        AppendMenuW(menu, _paused ? MF_CHECKED : 0, PauseCommand, "Pause");
        AppendMenuW(menu, Settings.RunsAtStartup() ? MF_CHECKED : 0, StartupCommand, "Start with Windows");
        AppendMenuW(menu, MF_SEPARATOR, 0, null);
        AppendMenuW(menu, 0, ExitCommand, "Exit");

        // Otherwise the menu doesn't close on outside clicks.
        SetForegroundWindow(_host);
        var command = (nuint)TrackPopupMenuEx(menu, TPM_RIGHTBUTTON | TPM_RETURNCMD, x, y, _host, 0);
        PostMessageW(_host, WM_NULL, 0, 0);
        DestroyMenu(menu);

        switch (command)
        {
            case PauseCommand:
                SetPaused(!_paused);
                break;
            case StartupCommand:
                Settings.SetRunAtStartup(!Settings.RunsAtStartup(), _startupCommand);
                break;
            case ExitCommand:
                DestroyWindow(_host);
                break;
        }
    }

    static void SetPaused(bool paused)
    {
        _paused = paused;
        RegisterKeyboard(enable: !paused);
        if (paused)
        {
            _keys.Clear();
            _overlay.Show(_keys.Shown);
            UpdateKeysTimer();
        }

        UpdateTray();
    }

    static void OnSettingChange()
    {
        var light = Settings.IsLightTheme();
        if (light == _lightTheme)
            return;
        _lightTheme = light;
        _overlay.SetLightTheme(light);
        UpdateTray();
    }

    static void UpdateTray() => _tray.Show(_lightTheme, _paused ? $"{Title} (paused)" : Title);

    static void OnRawInput(nint rawInput)
    {
        const int bufferSize = 64;
        var buffer = stackalloc byte[bufferSize];
        var size = (uint)bufferSize;
        if (GetRawInputData(rawInput, 0x10000003 /* RID_INPUT */, buffer, &size, (uint)sizeof(RAWINPUTHEADER)) == uint.MaxValue)
            return;
        if (((RAWINPUTHEADER*)buffer)->Type != 1 /* RIM_TYPEKEYBOARD */)
            return;

        var keyboard = Unsafe.Read<RAWKEYBOARD>(buffer + sizeof(RAWINPUTHEADER));
        var up = (keyboard.Flags & 0x1) != 0;
        var extended = (keyboard.Flags & 0x2) != 0;

        // 0xFF: fake key (Pause, media keys); E0 Shift: fake Shift around numpad navigation keys.
        if (keyboard.VKey is 0 or 0xFF || keyboard.VKey == KeyNames.Shift && extended)
            return;

        var vk = KeyNames.Normalize(keyboard.VKey);
        if (_keys.OnKey(vk, keyboard.MakeCode, extended, up, Environment.TickCount64))
            _overlay.Show(_keys.Shown);
        UpdateKeysTimer();
    }

    static void UpdateKeysTimer()
    {
        if (_keys.NeedsTimer == _keysTimerRunning)
            return;
        _keysTimerRunning = _keys.NeedsTimer;
        if (_keysTimerRunning)
            SetTimer(_host, KeysTimer, KeysIntervalMs, 0);
        else
            KillTimer(_host, KeysTimer);
    }
}

sealed record Options(int WidthDip = 300, int GapDip = 8, int LingerMs = 1500, bool Exit = false)
{
    public static Options Parse(string[] args)
    {
        var options = new Options();
        for (var i = 0; i < args.Length; i++)
        {
            int Next() => i + 1 < args.Length && int.TryParse(args[++i], out var value) ? value : throw new ArgumentException(args[i]);

            options = args[i].ToLowerInvariant() switch
            {
                "--width" => options with { WidthDip = Next() },
                "--gap" => options with { GapDip = Next() },
                "--linger" => options with { LingerMs = Next() },
                "--exit" => options with { Exit = true },
                _ => throw new ArgumentException($"Unknown argument: {args[i]}"),
            };
        }

        return options;
    }
}
