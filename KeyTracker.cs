namespace TaskbarKeys;

readonly record struct KeyLabel(string Name, bool Held, float Opacity);

/// <summary> Held keys and labels to show; released keys linger dimmed, then fade out. </summary>
sealed class KeyTracker(int lingerMs)
{
    const int FadeMs = 300;

    readonly record struct Key(ushort Vk, string Name, bool ReportedDown);

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

        _held.Add(new Key(vk, KeyNames.Get(vk, makeCode, extended), ReportedDown: false));
        _shown = Ordered(_held);
        _lingerEndsAt = 0;
        return UpdateLabels(now);
    }

    /// <returns> Whether <see cref="Labels"/> changed. </returns>
    public bool OnTimer(long now)
    {
        if (DropMissedReleases() && !Lingering)
            _lingerEndsAt = now + lingerMs;

        if (Lingering && now >= _lingerEndsAt)
        {
            _shown = Ordered(_held);
            _lingerEndsAt = 0;
        }

        return UpdateLabels(now);
    }

    /// <summary> Drops keys with a missed key-up, if Windows ever reported them down (swallowed keys never are). </summary>
    bool DropMissedReleases()
    {
        var dropped = false;
        for (var i = _held.Count - 1; i >= 0; i--)
        {
            if (KeyNames.IsDown(_held[i].Vk))
            {
                _held[i] = _held[i] with { ReportedDown = true };
            }
            else if (_held[i].ReportedDown)
            {
                _held.RemoveAt(i);
                dropped = true;
            }
        }

        return dropped;
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
