using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerArpgStats
{
    [Header("Core")]
    [Min(0f)] public float baseDamage = 5f;
    [Min(0f)] public float flatDamageBonus = 0f;
    [Min(0f)] public float increasedDamagePercent = 0f;
    [Min(0f)] public float damageMultiplier = 1f;

    [Header("Critical")]
    [Min(0f)] public float criticalChance = 5f;
    [Min(1f)] public float criticalDamageMultiplier = 1.5f;

    [Header("Speed")]
    [Min(0.01f)] public float attackSpeedMultiplier = 1f;
    [Min(0.01f)] public float movementSpeedMultiplier = 1f;
    [Range(0f, 95f)] public float cooldownReductionPercent = 0f;

    [Header("Defense")]
    [Min(0f)] public float armor = 0f;
    [Min(0f)] public float maxHealthBonus = 0f;
    [Min(0f)] public float maxManaBonus = 0f;

    private readonly List<PlayerStatModifier> runtimeModifiers = new List<PlayerStatModifier>();

    public IReadOnlyList<PlayerStatModifier> RuntimeModifiers => runtimeModifiers;

    public void AddModifier(PlayerStatModifier modifier, UnityEngine.Object source = null)
    {
        if (modifier == null) return;

        PlayerStatModifier runtimeModifier = new PlayerStatModifier(
            modifier.statType,
            modifier.mode,
            modifier.value,
            source != null ? source : modifier.source);
        runtimeModifiers.Add(runtimeModifier);
    }

    public void AddModifiers(IEnumerable<PlayerStatModifier> modifiers, UnityEngine.Object source = null)
    {
        if (modifiers == null) return;

        foreach (PlayerStatModifier modifier in modifiers)
        {
            AddModifier(modifier, source);
        }
    }

    public void RemoveModifiersFrom(UnityEngine.Object source)
    {
        if (source == null) return;

        for (int i = runtimeModifiers.Count - 1; i >= 0; i--)
        {
            if (runtimeModifiers[i].source == source)
            {
                runtimeModifiers.RemoveAt(i);
            }
        }
    }

    public float GetStat(PlayerStatType statType)
    {
        float baseValue = GetBaseStat(statType);
        float flat = 0f;
        float increasedPercent = 0f;
        float moreMultiplier = 1f;

        foreach (PlayerStatModifier modifier in runtimeModifiers)
        {
            if (modifier == null || modifier.statType != statType) continue;

            switch (modifier.mode)
            {
                case PlayerStatModifierMode.Flat:
                    flat += modifier.value;
                    break;
                case PlayerStatModifierMode.IncreasedPercent:
                    increasedPercent += modifier.value;
                    break;
                case PlayerStatModifierMode.MoreMultiplier:
                    moreMultiplier *= Mathf.Max(0f, modifier.value);
                    break;
            }
        }

        return (baseValue + flat) * (1f + increasedPercent / 100f) * moreMultiplier;
    }

    public AttackDamageRoll RollAttackDamage(float weaponDamage, float skillMultiplier = 1f, float addedDamage = 0f)
    {
        float baseAmount = Mathf.Max(0f, GetStat(PlayerStatType.BaseDamage))
            + Mathf.Max(0f, weaponDamage)
            + Mathf.Max(0f, GetStat(PlayerStatType.FlatDamageBonus))
            + Mathf.Max(0f, addedDamage);

        float increasedMultiplier = 1f + Mathf.Max(0f, GetStat(PlayerStatType.IncreasedDamagePercent)) / 100f;
        float resolvedSkillMultiplier = Mathf.Max(0f, skillMultiplier);
        float finalAmount = baseAmount
            * increasedMultiplier
            * Mathf.Max(0f, GetStat(PlayerStatType.DamageMultiplier))
            * resolvedSkillMultiplier;

        bool isCritical = UnityEngine.Random.value < CriticalChance01;
        if (isCritical)
        {
            finalAmount *= Mathf.Max(1f, GetStat(PlayerStatType.CriticalDamageMultiplier));
        }

        return new AttackDamageRoll(finalAmount, isCritical);
    }

    public float CriticalChance01 => Mathf.Clamp01(GetStat(PlayerStatType.CriticalChance) / 100f);
    public float AttackSpeedMultiplier => Mathf.Max(0.01f, GetStat(PlayerStatType.AttackSpeedMultiplier));
    public float MovementSpeedMultiplier => Mathf.Max(0.01f, GetStat(PlayerStatType.MovementSpeedMultiplier));
    public float CooldownMultiplier => 1f - Mathf.Clamp(GetStat(PlayerStatType.CooldownReductionPercent), 0f, 95f) / 100f;
    public float MaxHealthBonus => Mathf.Max(0f, GetStat(PlayerStatType.MaxHealthBonus));
    public float MaxManaBonus => Mathf.Max(0f, GetStat(PlayerStatType.MaxManaBonus));

    public float ReduceIncomingDamage(float amount)
    {
        float armorValue = Mathf.Max(0f, GetStat(PlayerStatType.Armor));
        return Mathf.Max(0f, amount) * 100f / (100f + armorValue);
    }

    private float GetBaseStat(PlayerStatType statType)
    {
        switch (statType)
        {
            case PlayerStatType.BaseDamage:
                return baseDamage;
            case PlayerStatType.FlatDamageBonus:
                return flatDamageBonus;
            case PlayerStatType.IncreasedDamagePercent:
                return increasedDamagePercent;
            case PlayerStatType.DamageMultiplier:
                return damageMultiplier;
            case PlayerStatType.CriticalChance:
                return criticalChance;
            case PlayerStatType.CriticalDamageMultiplier:
                return criticalDamageMultiplier;
            case PlayerStatType.AttackSpeedMultiplier:
                return attackSpeedMultiplier;
            case PlayerStatType.MovementSpeedMultiplier:
                return movementSpeedMultiplier;
            case PlayerStatType.CooldownReductionPercent:
                return cooldownReductionPercent;
            case PlayerStatType.Armor:
                return armor;
            case PlayerStatType.MaxHealthBonus:
                return maxHealthBonus;
            case PlayerStatType.MaxManaBonus:
                return maxManaBonus;
            default:
                return 0f;
        }
    }
}
