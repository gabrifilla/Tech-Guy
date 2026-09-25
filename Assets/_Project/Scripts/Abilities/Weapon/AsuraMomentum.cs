/// <summary>Per-character energy; alternating stamina and shock rewards varied combos.</summary>
public sealed class AsuraMomentum
{
    public const int Maximum = 100;
    public int Energy { get; private set; }
    public bool IsReady => Energy >= Maximum;
    private bool? _lastShock;

    public void RegisterSkill(bool shock)
    {
        Energy = System.Math.Min(Maximum, Energy +
            (_lastShock.HasValue && _lastShock.Value != shock ? 25 : 10));
        _lastShock = shock;
    }

    public bool TryConsume()
    {
        if (!IsReady) return false;
        Reset();
        return true;
    }

    public void Reset()
    {
        Energy = 0;
        _lastShock = null;
    }
}
