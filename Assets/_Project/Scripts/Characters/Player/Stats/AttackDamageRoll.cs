public readonly struct AttackDamageRoll
{
    public AttackDamageRoll(float amount, bool isCritical)
    {
        Amount = amount;
        IsCritical = isCritical;
    }

    public float Amount { get; }
    public bool IsCritical { get; }
}
