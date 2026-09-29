/// <summary>
/// The primary tactical function an archetype performs. Each archetype declares
/// exactly one primary <see cref="CombatRole"/>. Ally-Support and Priority-Threat
/// roles mark an archetype as a priority target that must be visually legible.
/// </summary>
/// <remarks>Feature: enemy-swarm-core-archetypes. Requirements: 22.1, 22.2.</remarks>
public enum CombatRole
{
    MeleePressure,
    RangedPressure,
    TerritoryControl,
    PlayerDisplacement,
    AllySupport,
    SwarmFuel,
    PriorityThreat,
    ComboFodder,
    ProjectileDenial
}
