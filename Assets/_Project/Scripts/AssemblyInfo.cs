using System.Runtime.CompilerServices;

// Grants the test assemblies access to internal gameplay members
// (e.g. PlayerOnHitEffects.SetChillRollForTests, PlayerArpgStats.SetCritRollForTests,
// ChillStatus.FreezeThreshold) invoked directly by the modifier-synergies-theme17 tests.
[assembly: InternalsVisibleTo("TechGuy.Tests.PlayMode")]
[assembly: InternalsVisibleTo("TechGuy.Tests.EditMode")]
