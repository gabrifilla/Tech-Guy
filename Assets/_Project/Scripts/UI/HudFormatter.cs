using UnityEngine;

/// <summary>
/// Pure, scene-free string formatter for the <see cref="PlayerHUD"/> labels. Extracted so the
/// "same state ⇒ same string" guarantee can be property-tested without a live Unity scene (task 7.1,
/// Property 4 — the formatter string is identical to the current inline formulas for health/mana/
/// asura, R2.6).
///
/// Every method here reproduces the EXACT format string that <see cref="PlayerHUD"/> used inline
/// before dirty-tracking, so the visual result is byte-identical for the same game state. The HUD
/// now calls these instead of interpolating in-place, and only reassigns the TMP text when the
/// matching <see cref="UiValueCache{T}"/> reports a change — eliminating the per-frame allocation
/// without changing a single rendered character.
/// </summary>
public static class HudFormatter
{
    /// <summary>Health label: "<c>{ceil(health)} / {ceil(maxHealth)}</c>".</summary>
    public static string Resource(float current, float max)
        => $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";

    /// <summary>
    /// Asura label: "<c>ASURA PRONTO</c>" when ready, otherwise "<c>ASURA   {energy} / 100</c>"
    /// (three spaces, matching the current inline string).
    /// </summary>
    public static string Asura(bool ready, int energy)
        => ready ? "ASURA PRONTO" : $"ASURA   {energy} / 100";

    /// <summary>
    /// Ability tooltip: "<c>{name}  ·  {manaCost:0} mana  ·  {cooldown:0.#}s recarga</c>", matching
    /// the current inline string (two spaces around each middle dot).
    /// </summary>
    public static string Tooltip(string abilityName, float manaCost, float cooldownTime)
        => $"{abilityName}  ·  {manaCost:0} mana  ·  {cooldownTime:0.#}s recarga";
}
