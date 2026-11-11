namespace TaskbarKeys;

/// <summary> Held keys and labels to show; a combo lingers after release. </summary>
sealed class KeyTracker(int lingerMs)
{
    // Missed key-ups (Win+L, UAC) leave keys stuck; keys held longer than this are rechecked.
    const int StaleCheckDelayMs = 300;

    readonly record struct Key(ushort Vk, string Name, long DownAt);

    readonly List<Key> _held = [];
    string[] _shown = [];
    long _refreshAt;

    public IReadOnlyList<string> Shown => _shown;

    public bool NeedsTimer => _held.Count > 0 || _refreshAt != 0;

    /// <returns> Whether <see cref="Shown"/> changed. </returns>
    public bool OnKey(ushort vk, ushort makeCode, bool extended, bool up, long now)
    {
        var index = _held.FindIndex(k => k.Vk == vk);
        if (up)
        {
            if (index < 0)
                return false;
            _held.RemoveAt(index);
            _refreshAt = now + lingerMs;
            return false;
        }

        if (index >= 0)
            return false;

        _held.Add(new Key(vk, KeyNames.Get(vk, makeCode, extended), now));
        _refreshAt = 0;
        return UpdateShown();
    }

    /// <returns> Whether <see cref="Shown"/> changed. </returns>
    public bool OnTimer(long now)
    {
        if (_held.RemoveAll(k => now - k.DownAt > StaleCheckDelayMs && !KeyNames.IsDown(k.Vk)) > 0 && _refreshAt == 0)
            _refreshAt = now + lingerMs;

        if (_refreshAt == 0 || now < _refreshAt)
            return false;

        _refreshAt = 0;
        return UpdateShown();
    }

    public void Clear()
    {
        _held.Clear();
        _shown = [];
        _refreshAt = 0;
    }

    bool UpdateShown()
    {
        var shown = _held.OrderBy(k => KeyNames.DisplayRank(k.Vk)).Select(k => k.Name).ToArray();
        if (shown.AsSpan().SequenceEqual(_shown))
            return false;
        _shown = shown;
        return true;
    }
}
