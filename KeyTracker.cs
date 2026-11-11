namespace TaskbarKeys;

readonly record struct KeyLabel(string Name, bool Held, float Opacity);

/// <summary> Held keys and labels to show; released keys linger dimmed, then fade out. </summary>
sealed class KeyTracker(int lingerMs)
{
    // Missed key-ups (Win+L, UAC) leave keys stuck; keys held longer than this are rechecked.
    const int StaleCheckDelayMs = 300;
    const int FadeMs = 300;

    readonly record struct Key(ushort Vk, string Name, long DownAt);

    readonly List<Key> _held = [];
    List<Key> _shown = [];
    KeyLabel[] _labels = [];
    long _lingerEndsAt;

    public IReadOnlyList<KeyLabel> Labels => _labels;

    public bool NeedsTimer => _held.Count > 0 || Lingering;

    public bool Lingering => _lingerEndsAt != 0;

    /// <returns> Whether <see cref="Labels"/> changed. </returns>
    public bool OnKey(ushort vk, ushort makeCode, bool extended, bool up, long now)
    {
        var index = _held.FindIndex(k => k.Vk == vk);
        if (up)
        {
            if (index < 0)
                return false;
            _held.RemoveAt(index);
            _lingerEndsAt = now + lingerMs;
            return UpdateLabels(now);
        }

        if (index >= 0)
            return false;

        _held.Add(new Key(vk, KeyNames.Get(vk, makeCode, extended), now));
        _shown = Ordered(_held);
        _lingerEndsAt = 0;
        return UpdateLabels(now);
    }

    /// <returns> Whether <see cref="Labels"/> changed. </returns>
    public bool OnTimer(long now)
    {
        if (_held.RemoveAll(k => now - k.DownAt > StaleCheckDelayMs && !KeyNames.IsDown(k.Vk)) > 0 && !Lingering)
            _lingerEndsAt = now + lingerMs;

        if (Lingering && now >= _lingerEndsAt)
        {
            _shown = Ordered(_held);
            _lingerEndsAt = 0;
        }

        return UpdateLabels(now);
    }

    public void Clear()
    {
        _held.Clear();
        _shown = [];
        _labels = [];
        _lingerEndsAt = 0;
    }

    static List<Key> Ordered(List<Key> keys) => keys.OrderBy(k => KeyNames.DisplayRank(k.Vk)).ToList();

    bool UpdateLabels(long now)
    {
        var releasedOpacity = Lingering ? Math.Clamp((_lingerEndsAt - now) / (float)FadeMs, 0, 1) : 1;
        var labels = _shown
            .Select(k => _held.Exists(h => h.Vk == k.Vk) ? new KeyLabel(k.Name, true, 1) : new KeyLabel(k.Name, false, releasedOpacity))
            .ToArray();
        if (labels.AsSpan().SequenceEqual(_labels))
            return false;
        _labels = labels;
        return true;
    }
}
