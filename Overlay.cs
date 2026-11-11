using static TaskbarKeys.Native;

namespace TaskbarKeys;

/// <summary> Child of the taskbar left of the tray: hides with it (fullscreen, auto-hide), no topmost fights. </summary>
sealed unsafe class Overlay(nint instance, string windowClass, int widthDip, int gapDip) : IDisposable
{
    const int FontSizeDip = 14, PillHeightDip = 26, PillPaddingDip = 7, PillGapDip = 4;
    const float PillRadiusDip = 5;

    nint _taskbar, _hwnd, _dc, _bitmap, _font;
    uint* _bits;
    int _x, _y, _width, _height, _bitmapWidth, _bitmapHeight;
    uint _dpi;
    bool _light;
    IReadOnlyList<string> _labels = [];

    public void Show(IReadOnlyList<string> labels)
    {
        _labels = labels;
        Render();
    }

    public void SetLightTheme(bool light)
    {
        if (light == _light)
            return;
        _light = light;
        Render();
    }

    /// <summary> Re-attaches after Explorer restarts; follows taskbar size, DPI and tray position. </summary>
    public void Sync()
    {
        var taskbar = FindWindowW("Shell_TrayWnd", null);
        if (taskbar == 0)
        {
            DestroyOverlay();
            return;
        }

        var dirty = false;
        if (taskbar != _taskbar || IsWindow(_hwnd) == 0)
        {
            DestroyOverlay();
            _taskbar = taskbar;
            _hwnd = CreateWindowExW(WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE, windowClass, null,
                WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS, 0, 0, 0, 0, taskbar, 0, instance, 0);
            _width = _height = 0;
            dirty = true;
        }

        if (_hwnd == 0)
            return;

        var dpi = GetDpiForWindow(taskbar);
        if (dpi != _dpi)
        {
            _dpi = dpi;
            CreateFont();
            dirty = true;
        }

        var (x, y, width, height) = ComputeBounds();
        if ((x, y, width, height) != (_x, _y, _width, _height))
        {
            (_x, _y, _width, _height) = (x, y, width, height);
            SetWindowPos(_hwnd, 0, x, y, width, height, SWP_NOACTIVATE);
            dirty = true;
        }
        else if (GetWindow(taskbar, GW_CHILD) != _hwnd)
        {
            SetWindowPos(_hwnd, 0, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        if (dirty)
            Render();
    }

    (int X, int Y, int Width, int Height) ComputeBounds()
    {
        RECT client;
        GetClientRect(_taskbar, &client);
        var width = Scale(widthDip);

        int right;
        var tray = FindWindowExW(_taskbar, 0, "TrayNotifyWnd", null);
        if (tray != 0)
        {
            RECT trayRect;
            GetWindowRect(tray, &trayRect);
            var topLeft = new POINT { X = trayRect.Left, Y = trayRect.Top };
            MapWindowPoints(0, _taskbar, &topLeft, 1);
            right = topLeft.X - Scale(gapDip);
        }
        else
        {
            right = client.Right - Scale(300);
        }

        return (right - width, 0, width, client.Bottom);
    }

    void Render()
    {
        if (_hwnd == 0 || _width <= 0 || _height <= 0)
            return;

        EnsureBitmap();
        var pixels = new Span<uint>(_bits, _width * _height);
        pixels.Clear();

        var pills = LayoutPills();
        foreach (var pill in pills)
            TextOutW(_dc, pill.TextX, pill.TextY, pill.Label, pill.Label.Length);
        GdiFlush();

        foreach (var pill in pills)
            ComposePill(pixels, pill);

        var size = new SIZE { Cx = _width, Cy = _height };
        var source = new POINT();
        var blend = new BLENDFUNCTION { SourceConstantAlpha = 255, AlphaFormat = 1 /* AC_SRC_ALPHA */ };
        UpdateLayeredWindow(_hwnd, 0, null, &size, _dc, &source, 0, &blend, 2 /* ULW_ALPHA */);
    }

    readonly record struct Pill(string Label, int X, int Y, int Width, int Height, int TextX, int TextY);

    List<Pill> LayoutPills()
    {
        int height = Scale(PillHeightDip), padding = Scale(PillPaddingDip), gap = Scale(PillGapDip);
        var y = (_height - height) / 2;
        var sizes = _labels.Select(label =>
        {
            SIZE text;
            GetTextExtentPoint32W(_dc, label, label.Length, &text);
            return text;
        }).ToList();

        var x = _width - sizes.Sum(s => Math.Max(s.Cx + 2 * padding, height)) - gap * Math.Max(sizes.Count - 1, 0);
        var pills = new List<Pill>(sizes.Count);
        for (var i = 0; i < sizes.Count; i++)
        {
            var width = Math.Max(sizes[i].Cx + 2 * padding, height);
            pills.Add(new Pill(_labels[i], x, y, width, height, x + (width - sizes[i].Cx) / 2, y + (height - sizes[i].Cy) / 2));
            x += width + gap;
        }

        return pills;
    }

    /// <summary> GDI's white-on-black text → premultiplied ARGB over an antialiased rounded rect. </summary>
    void ComposePill(Span<uint> pixels, Pill pill)
    {
        var (textR, textG, textB) = _light ? (0x1B, 0x1B, 0x1B) : (0xFF, 0xFF, 0xFF);
        var (fillR, fillG, fillB, fillAlpha) = _light ? (0, 0, 0, 0.08f) : (0xFF, 0xFF, 0xFF, 0.14f);
        var radius = PillRadiusDip * _dpi / 96f;

        for (var y = Math.Max(pill.Y, 0); y < Math.Min(pill.Y + pill.Height, _height); y++)
        for (var x = Math.Max(pill.X, 0); x < Math.Min(pill.X + pill.Width, _width); x++)
        {
            ref var pixel = ref pixels[y * _width + x];
            var textAlpha = (pixel & 0xFF) / 255f;
            var fill = RoundedRectCoverage(x + 0.5f, y + 0.5f, pill, radius) * fillAlpha * (1 - textAlpha);
            var alpha = textAlpha + fill;
            pixel = (uint)(alpha * 255 + 0.5f) << 24
                | (uint)(textR * textAlpha + fillR * fill + 0.5f) << 16
                | (uint)(textG * textAlpha + fillG * fill + 0.5f) << 8
                | (uint)(textB * textAlpha + fillB * fill + 0.5f);
        }
    }

    static float RoundedRectCoverage(float x, float y, Pill pill, float radius)
    {
        var cx = Math.Clamp(x, pill.X + radius, pill.X + pill.Width - radius);
        var cy = Math.Clamp(y, pill.Y + radius, pill.Y + pill.Height - radius);
        var distance = MathF.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - radius;
        return Math.Clamp(0.5f - distance, 0, 1);
    }

    void EnsureBitmap()
    {
        if (_dc == 0)
        {
            _dc = CreateCompatibleDC(0);
            SetBkMode(_dc, 1 /* TRANSPARENT */);
            SetTextColor(_dc, 0xFFFFFF);
            if (_font != 0)
                SelectObject(_dc, _font);
        }

        if (_bitmap != 0 && _bitmapWidth == _width && _bitmapHeight == _height)
            return;

        var bitmap = CreateDib(_dc, _width, _height, out var bits);
        SelectObject(_dc, bitmap);
        if (_bitmap != 0)
            DeleteObject(_bitmap);
        _bitmap = bitmap;
        _bits = bits;
        (_bitmapWidth, _bitmapHeight) = (_width, _height);
    }

    void CreateFont()
    {
        var font = CreateFontW(-Scale(FontSizeDip), 0, 0, 0, 600, 0, 0, 0, 1 /* DEFAULT_CHARSET */, 0, 0, 4 /* ANTIALIASED_QUALITY */, 0,
            "Segoe UI");
        if (_dc != 0)
            SelectObject(_dc, font);
        if (_font != 0)
            DeleteObject(_font);
        _font = font;
    }

    int Scale(float dip) => (int)MathF.Round(dip * _dpi / 96f);

    void DestroyOverlay()
    {
        if (_hwnd != 0 && IsWindow(_hwnd) != 0)
            DestroyWindow(_hwnd);
        _hwnd = 0;
    }

    public void Dispose()
    {
        DestroyOverlay();
        if (_dc != 0)
            DeleteDC(_dc);
        if (_bitmap != 0)
            DeleteObject(_bitmap);
        if (_font != 0)
            DeleteObject(_font);
    }
}
