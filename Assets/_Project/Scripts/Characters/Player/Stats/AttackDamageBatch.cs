/// <summary>Damage parameters captured before target callbacks; critical rolls remain per target.</summary>
internal readonly struct AttackDamageBatch
{
    public float Amount { get; }
    public float CriticalChance { get; }
    public float CriticalMultiplier { get; }
    public AttackDamageBatch(float amount, float chance, float multiplier)
    { Amount = amount; CriticalChance = chance; CriticalMultiplier = multiplier; }
}
