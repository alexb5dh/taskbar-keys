using static TaskbarKeys.Native;

namespace TaskbarKeys;

sealed unsafe class TrayIcon(nint hwnd, uint callbackMessage) : IDisposable
{
    const uint Id = 1;
    const uint NIM_ADD = 0, NIM_MODIFY = 1, NIM_DELETE = 2, NIM_SETVERSION = 4;
    const uint NIF_MESSAGE = 0x1, NIF_ICON = 0x2, NIF_TIP = 0x4, NIF_SHOWTIP = 0x80;
    const string KeyboardGlyph = "", GlyphFont = "Segoe Fluent Icons";

    nint _icon;

    /// <summary> Adds or updates the icon (an Explorer restart drops it). </summary>
    public void Show(bool lightTheme, string tip)
    {
        var icon = CreateKeyboardIcon(lightTheme);
        var data = CreateData();
        data.Flags = NIF_MESSAGE | NIF_ICON | NIF_TIP | NIF_SHOWTIP;
        data.CallbackMessage = callbackMessage;
        data.Icon = icon;
        tip.AsSpan(0, Math.Min(tip.Length, 127)).CopyTo(new Span<char>(data.Tip, 128));

        if (Shell_NotifyIconW(NIM_MODIFY, &data) == 0)
        {
            Shell_NotifyIconW(NIM_ADD, &data);
            data.Version = 4; // WM_CONTEXTMENU / NIN_SELECT callbacks with anchor coordinates
            Shell_NotifyIconW(NIM_SETVERSION, &data);
        }

        if (_icon != 0)
            DestroyIcon(_icon);
        _icon = icon;
    }

    NOTIFYICONDATAW CreateData() => new() { Size = (uint)sizeof(NOTIFYICONDATAW), Hwnd = hwnd, Id = Id };

    static nint CreateKeyboardIcon(bool lightTheme)
    {
        var size = GetSystemMetrics(49 /* SM_CXSMICON */);
        var dc = CreateCompatibleDC(0);
        var bitmap = CreateDib(dc, size, size, out var bits);
        var font = CreateFontW(-size, 0, 0, 0, 400, 0, 0, 0, 1 /* DEFAULT_CHARSET */, 0, 0, 4 /* ANTIALIASED_QUALITY */, 0, GlyphFont);
        var oldBitmap = SelectObject(dc, bitmap);
        var oldFont = SelectObject(dc, font);
        SetBkMode(dc, 1 /* TRANSPARENT */);
        SetTextColor(dc, 0xFFFFFF);

        SIZE text;
        GetTextExtentPoint32W(dc, KeyboardGlyph, 1, &text);
        TextOutW(dc, (size - text.Cx) / 2, (size - text.Cy) / 2, KeyboardGlyph, 1);
        GdiFlush();

        var color = lightTheme ? 0x000000u : 0xFFFFFFu;
        var pixels = new Span<uint>(bits, size * size);
        foreach (ref var pixel in pixels)
            pixel = (pixel & 0xFF) << 24 | color;

        SelectObject(dc, oldFont);
        SelectObject(dc, oldBitmap);
        var mask = CreateBitmap(size, size, 1, 1, null);
        var info = new ICONINFO { IsIcon = 1, Mask = mask, Color = bitmap };
        var icon = CreateIconIndirect(&info);

        DeleteObject(mask);
        DeleteObject(bitmap);
        DeleteObject(font);
        DeleteDC(dc);
        return icon;
    }

    public void Dispose()
    {
        var data = CreateData();
        Shell_NotifyIconW(NIM_DELETE, &data);
        if (_icon != 0)
            DestroyIcon(_icon);
        _icon = 0;
    }
}
