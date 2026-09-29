/// <summary>
/// Per-character energy; alternating stamina and shock rewards varied combos.
/// Preserves the Manoplas Asura activation/gain contract (Requisitos 8.6, 8.7, 8.10):
/// R only enters Asura when <see cref="IsReady"/> (Energy == <see cref="Maximum"/>), and
/// <see cref="TryConsume"/> then zeroes the meter (8.6); when below the cap the burst is denied
/// and the current energy is preserved untouched (8.7); <see cref="RegisterSkill"/> grants 25 on a
/// category (stamina/shock) switch and 10 when the category repeats, capped at <see cref="Maximum"/> (8.10).
/// </summary>
public sealed class AsuraMomentum
{
    public const int Maximum = 100;
    public int Energy { get; private set; }
    public bool IsReady => Energy >= Maximum;
    private bool? _lastShock;
    public void AddEnergy(int amount) => Energy = System.Math.Min(Maximum, Energy + System.Math.Max(0, amount));

    // Requisito 8.10: +25 when the category differs from the previous skill, +10 when it repeats, capped at 100.
    public void RegisterSkill(bool shock)
    {
        Energy = System.Math.Min(Maximum, Energy +
            (_lastShock.HasValue && _lastShock.Value != shock ? 25 : 10));
        _lastShock = shock;
    }

    // Requisitos 8.6/8.7: only a full meter enters Asura (returns true) and is zeroed; otherwise the
    // burst is denied (returns false) and the current energy is preserved unchanged.
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
