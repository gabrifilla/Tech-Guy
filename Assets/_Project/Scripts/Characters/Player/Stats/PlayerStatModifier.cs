using System;
using UnityEngine;

[Serializable]
public class PlayerStatModifier
{
    public PlayerStatType statType;
    public PlayerStatModifierMode mode;
    public float value;
    [NonSerialized] public UnityEngine.Object source;

    public PlayerStatModifier()
    {
    }

    public PlayerStatModifier(PlayerStatType statType, PlayerStatModifierMode mode, float value, UnityEngine.Object source = null)
    {
        this.statType = statType;
        this.mode = mode;
        this.value = value;
        this.source = source;
    }
}
