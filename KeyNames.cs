namespace TaskbarKeys;

static class KeyNames
{
    public const ushort Shift = 0x10, Control = 0x11, Alt = 0x12, LeftWin = 0x5B, RightWin = 0x5C;

    public static ushort Normalize(ushort vk) => vk switch
    {
        0xA0 or 0xA1 => Shift,
        0xA2 or 0xA3 => Control,
        0xA4 or 0xA5 => Alt,
        RightWin => LeftWin,
        _ => vk,
    };

    public static int DisplayRank(ushort vk) => vk switch
    {
        LeftWin => 0,
        Control => 1,
        Alt => 2,
        Shift => 3,
        _ => 4,
    };

    public static bool IsDown(ushort vk) => vk == LeftWin ? IsPressed(LeftWin) || IsPressed(RightWin) : IsPressed(vk);

    static bool IsPressed(int vk) => Native.GetAsyncKeyState(vk) < 0;

    public static unsafe string Get(ushort vk, ushort makeCode, bool extended)
    {
        if (vk is >= 0x30 and <= 0x39 or >= 0x41 and <= 0x5A)
            return ((char)vk).ToString();
        if (vk is >= 0x60 and <= 0x69)
            return "Num" + (vk - 0x60);
        if (vk is >= 0x70 and <= 0x87)
            return "F" + (vk - 0x6F);

        if (Fixed(vk) is { } name)
            return name;

        var buffer = stackalloc char[64];
        var length = Native.GetKeyNameTextW(makeCode << 16 | (extended ? 1 << 24 : 0), buffer, 64);
        return length > 0 ? new string(buffer, 0, length) : $"0x{vk:X2}";
    }

    static string? Fixed(ushort vk) => vk switch
    {
        0x08 => "Backspace",
        0x09 => "Tab",
        0x0D => "Enter",
        Shift => "Shift",
        Control => "Ctrl",
        Alt => "Alt",
        0x13 => "Pause",
        0x14 => "CapsLock",
        0x1B => "Esc",
        0x20 => "Space",
        0x21 => "PgUp",
        0x22 => "PgDn",
        0x23 => "End",
        0x24 => "Home",
        0x25 => "←",
        0x26 => "↑",
        0x27 => "→",
        0x28 => "↓",
        0x2C => "PrtSc",
        0x2D => "Ins",
        0x2E => "Del",
        LeftWin => "Win",
        0x5D => "Menu",
        0x6A => "Num*",
        0x6B => "Num+",
        0x6D => "Num-",
        0x6E => "Num.",
        0x6F => "Num/",
        0x90 => "NumLock",
        0x91 => "ScrollLock",
        0xAD => "Mute",
        0xAE => "Vol-",
        0xAF => "Vol+",
        0xB0 => "Next",
        0xB1 => "Prev",
        0xB2 => "Stop",
        0xB3 => "Play",
        0xBA => ";",
        0xBB => "=",
        0xBC => ",",
        0xBD => "-",
        0xBE => ".",
        0xBF => "/",
        0xC0 => "`",
        0xDB => "[",
        0xDC or 0xE2 => "\\",
        0xDD => "]",
        0xDE => "'",
        _ => null,
    };
}
