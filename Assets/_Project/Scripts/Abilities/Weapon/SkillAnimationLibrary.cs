using System;
using UnityEngine;

public enum SkillMotion { PunchLeft, PunchRight, Slam, BowShot, BowRain, SpearThrust, SpearSweep }

public sealed class SkillAnimationLibrary : ScriptableObject
{
    public const string ResourcePath = "Combat/SkillAnimations";
    [Serializable] public sealed class Entry
    {
        public SkillMotion motion;
        public AnimationClip clip;
        [Range(.05f,.95f)] public float contact = .45f;
    }
    [SerializeField] private Entry[] _entries;
    public Entry Get(SkillMotion motion) => Array.Find(_entries ?? Array.Empty<Entry>(),entry=>entry.motion==motion);
    public void Configure(Entry[] entries) => _entries=entries;
}
