using System.Runtime.InteropServices;

#pragma warning disable CS0649 // filled by Windows

namespace TaskbarKeys;

struct POINT { public int X, Y; }

struct RECT { public int Left, Top, Right, Bottom; }

struct SIZE { public int Cx, Cy; }

struct MSG
{
    public nint Hwnd;
    public uint Message;
    public nint WParam, LParam;
    public uint Time;
    public POINT Pt;
    public uint Private;
}

unsafe struct WNDCLASSEXW
{
    public uint Size, Style;
    public delegate* unmanaged<nint, uint, nint, nint, nint> WndProc;
    public int ClsExtra, WndExtra;
    public nint Instance, Icon, Cursor, Background;
    public char* MenuName, ClassName;
    public nint IconSmall;
}

struct RAWINPUTDEVICE
{
    public ushort UsagePage, Usage;
    public uint Flags;
    public nint Target;
}

struct RAWINPUTHEADER
{
    public uint Type, Size;
    public nint Device, WParam;
}

struct RAWKEYBOARD
{
    public ushort MakeCode, Flags, Reserved, VKey;
    public uint Message, ExtraInformation;
}

struct BITMAPINFOHEADER
{
    public uint Size;
    public int Width, Height;
    public ushort Planes, BitCount;
    public uint Compression, SizeImage;
    public int XPelsPerMeter, YPelsPerMeter;
    public uint ClrUsed, ClrImportant;
}

struct BLENDFUNCTION { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }

struct KBDLLHOOKSTRUCT
{
    public uint VkCode, ScanCode, Flags, Time;
    public nuint ExtraInfo;
}

struct ICONINFO
{
    public int IsIcon;
    public uint HotspotX, HotspotY;
    public nint Mask, Color;
}

unsafe struct NOTIFYICONDATAW
{
    public uint Size;
    public nint Hwnd;
    public uint Id, Flags, CallbackMessage;
    public nint Icon;
    public fixed char Tip[128];
    public uint State, StateMask;
    public fixed char Info[256];
    public uint Version;
    public fixed char InfoTitle[64];
    public uint InfoFlags;
    public Guid GuidItem;
    public nint BalloonIcon;
}

static unsafe partial class Native
{
    const string User = "user32.dll", Gdi = "gdi32.dll", Kernel = "kernel32.dll", AdvApi = "advapi32.dll", Shell = "shell32.dll",
        Wts = "wtsapi32.dll";

    public const uint WM_NULL = 0x0000, WM_DESTROY = 0x0002, WM_CLOSE = 0x0010, WM_SETTINGCHANGE = 0x001A, WM_CONTEXTMENU = 0x007B,
        WM_NCHITTEST = 0x0084, WM_INPUT = 0x00FF, WM_TIMER = 0x0113, WM_WTSSESSION_CHANGE = 0x02B1, WM_APP = 0x8000;
    public const uint WS_POPUP = 0x80000000, WS_CHILD = 0x40000000, WS_VISIBLE = 0x10000000, WS_CLIPSIBLINGS = 0x04000000;
    public const uint WS_EX_TRANSPARENT = 0x20, WS_EX_TOOLWINDOW = 0x80, WS_EX_LAYERED = 0x80000, WS_EX_NOACTIVATE = 0x08000000;
    public const uint SWP_NOSIZE = 0x1, SWP_NOMOVE = 0x2, SWP_NOACTIVATE = 0x10;
    public const uint GW_CHILD = 5;
    public const nint HTTRANSPARENT = -1;

    [LibraryImport(User)] public static partial ushort RegisterClassExW(WNDCLASSEXW* wc);

    [LibraryImport(User, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint CreateWindowExW(uint exStyle, string className, string? windowName, uint style, int x, int y, int width,
        int height, nint parent, nint menu, nint instance, nint param);

    [LibraryImport(User)] public static partial nint DefWindowProcW(nint hwnd, uint msg, nint wParam, nint lParam);
    [LibraryImport(User)] public static partial int GetMessageW(MSG* msg, nint hwnd, uint min, uint max);
    [LibraryImport(User)] public static partial int TranslateMessage(MSG* msg);
    [LibraryImport(User)] public static partial nint DispatchMessageW(MSG* msg);
    [LibraryImport(User)] public static partial void PostQuitMessage(int exitCode);
    [LibraryImport(User)] public static partial int PostMessageW(nint hwnd, uint msg, nint wParam, nint lParam);
    [LibraryImport(User)] public static partial int DestroyWindow(nint hwnd);

    [LibraryImport(User, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint FindWindowW(string? className, string? windowName);

    [LibraryImport(User, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint FindWindowExW(nint parent, nint childAfter, string? className, string? windowName);

    [LibraryImport(User)] public static partial int SetWindowPos(nint hwnd, nint insertAfter, int x, int y, int cx, int cy, uint flags);
    [LibraryImport(User)] public static partial int GetWindowRect(nint hwnd, RECT* rect);
    [LibraryImport(User)] public static partial int GetClientRect(nint hwnd, RECT* rect);
    [LibraryImport(User)] public static partial int MapWindowPoints(nint from, nint to, POINT* points, uint count);
    [LibraryImport(User)] public static partial int IsWindow(nint hwnd);
    [LibraryImport(User)] public static partial nint GetWindow(nint hwnd, uint cmd);
    [LibraryImport(User)] public static partial nuint SetTimer(nint hwnd, nuint id, uint elapseMs, nint timerProc);
    [LibraryImport(User)] public static partial int KillTimer(nint hwnd, nuint id);

    [LibraryImport(User, StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint RegisterWindowMessageW(string name);

    [LibraryImport(User)] public static partial int RegisterRawInputDevices(RAWINPUTDEVICE* devices, uint count, uint size);
    [LibraryImport(User)] public static partial uint GetRawInputData(nint rawInput, uint command, void* data, uint* size, uint headerSize);
    [LibraryImport(User)] public static partial short GetAsyncKeyState(int vk);
    [LibraryImport(User)] public static partial int GetKeyNameTextW(int lParam, char* buffer, int size);
    [LibraryImport(User)] public static partial uint GetDpiForWindow(nint hwnd);

    [LibraryImport(User)]
    public static partial int UpdateLayeredWindow(nint hwnd, nint hdcDst, POINT* dst, SIZE* size, nint hdcSrc, POINT* src, uint colorKey,
        BLENDFUNCTION* blend, uint flags);

    [LibraryImport(Gdi)] public static partial nint CreateCompatibleDC(nint hdc);
    [LibraryImport(Gdi)] public static partial nint CreateDIBSection(nint hdc, BITMAPINFOHEADER* info, uint usage, void** bits, nint section, uint offset);
    [LibraryImport(Gdi)] public static partial nint SelectObject(nint hdc, nint obj);
    [LibraryImport(Gdi)] public static partial int DeleteObject(nint obj);
    [LibraryImport(Gdi)] public static partial int DeleteDC(nint hdc);

    [LibraryImport(Gdi, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint CreateFontW(int height, int width, int escapement, int orientation, int weight, uint italic, uint underline,
        uint strikeOut, uint charSet, uint outPrecision, uint clipPrecision, uint quality, uint pitchAndFamily, string faceName);

    [LibraryImport(Gdi)] public static partial uint SetTextColor(nint hdc, uint color);
    [LibraryImport(Gdi)] public static partial int SetBkMode(nint hdc, int mode);

    [LibraryImport(Gdi, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int TextOutW(nint hdc, int x, int y, string text, int length);

    [LibraryImport(Gdi, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int GetTextExtentPoint32W(nint hdc, string text, int length, SIZE* size);

    [LibraryImport(Gdi)] public static partial int GdiFlush();
    [LibraryImport(Gdi)] public static partial nint CreateBitmap(int width, int height, uint planes, uint bitCount, void* bits);

    [LibraryImport(User)] public static partial nint CreateIconIndirect(ICONINFO* info);
    [LibraryImport(User)] public static partial int DestroyIcon(nint icon);
    [LibraryImport(User)] public static partial int GetSystemMetrics(int index);
    [LibraryImport(User)] public static partial nint CreatePopupMenu();

    [LibraryImport(User, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int AppendMenuW(nint menu, uint flags, nuint id, string? text);

    [LibraryImport(User)] public static partial int TrackPopupMenuEx(nint menu, uint flags, int x, int y, nint hwnd, nint parameters);
    [LibraryImport(User)] public static partial int DestroyMenu(nint menu);
    [LibraryImport(User)] public static partial int SetForegroundWindow(nint hwnd);

    [LibraryImport(User)]
    public static partial nint SetWindowsHookExW(int hookId, delegate* unmanaged<int, nint, nint, nint> proc, nint module, uint threadId);

    [LibraryImport(User)] public static partial int UnhookWindowsHookEx(nint hook);
    [LibraryImport(User)] public static partial nint CallNextHookEx(nint hook, int code, nint wParam, nint lParam);
    [LibraryImport(Wts)] public static partial int WTSRegisterSessionNotification(nint hwnd, uint flags);

    [LibraryImport(Shell)] public static partial int Shell_NotifyIconW(uint message, NOTIFYICONDATAW* data);

    [LibraryImport(Kernel, StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial nint CreateMutexW(nint attributes, int initialOwner, string name);

    [LibraryImport(Kernel)] public static partial nint GetModuleHandleW(nint moduleName);

    [LibraryImport(AdvApi, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int RegGetValueW(nint key, string subKey, string value, uint flags, uint* type, void* data, uint* size);

    [LibraryImport(AdvApi, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int RegSetKeyValueW(nint key, string subKey, string valueName, uint type, void* data, uint size);

    [LibraryImport(AdvApi, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int RegDeleteKeyValueW(nint key, string subKey, string valueName);

    /// <summary> Top-down 32-bit DIB section. </summary>
    public static nint CreateDib(nint dc, int width, int height, out uint* bits)
    {
        var header = new BITMAPINFOHEADER
        {
            Size = (uint)sizeof(BITMAPINFOHEADER), Width = width, Height = -height, Planes = 1, BitCount = 32,
        };
        void* pixels;
        var bitmap = CreateDIBSection(dc, &header, 0, &pixels, 0, 0);
        bits = (uint*)pixels;
        return bitmap;
    }
}
