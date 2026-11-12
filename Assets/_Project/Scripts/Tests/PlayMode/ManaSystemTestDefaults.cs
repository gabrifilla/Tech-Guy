using NUnit.Framework;

/// <summary>
/// Player mana is switched off in the game since 11/11/2026 (CombatBalanceConfig.playerManaEnabled = false), but the mana
/// system still exists and is to be revisited. This assembly-wide fixture (global namespace) turns it back on for every
/// PlayMode test, so the existing mana contracts keep being exercised. Tests about the switched-off game state clear the
/// override themselves (see PlayerManaSwitchPlayModeTests).
/// </summary>
[SetUpFixture]
public sealed class ManaSystemTestDefaults
{
    [OneTimeSetUp] public void EnableManaForTests() => PlayerActor.ManaEnabledOverride = true;
    [OneTimeTearDown] public void RestoreGameDefault() => PlayerActor.ManaEnabledOverride = null;
}
