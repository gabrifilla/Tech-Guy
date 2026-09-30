using UnityEngine;

/// <summary>
/// Configurable source of the 64-bit Run_Seed that seeds a whole run's procedural
/// generation. The seed is authored in the Inspector as text so a designer can pin a
/// specific run for reproducible testing, or leave it blank to let the
/// <c>ProgressionDirector</c> fall back to a fresh random seed.
/// <para>
/// This is a scene-owned <see cref="MonoBehaviour"/> the director references through a
/// serialized field (no <c>GameObject.Find</c> / <c>FindObjectOfType</c> / magic strings,
/// per AGENTS.md). It only parses and hands out a seed; the fallback and persistence/log
/// policy live in the director (task 8.2).
/// </para>
/// </summary>
/// <remarks>
/// Feature: procedural-stage-room-generation. Requirements: 2.1, 2.2.
/// <para>
/// The text field is parsed as an unsigned 64-bit integer. A blank or unparseable value
/// is reported as "no valid seed" via <see cref="TryGetSeed"/> returning <c>false</c>, so
/// the caller can log the acquisition failure and generate a fallback seed (R2.2).
/// </para>
/// </remarks>
public sealed class RunSeedSource : MonoBehaviour
{
    [SerializeField] private bool _randomizeEachRun;
    [SerializeField, Tooltip("Optional fixed Run_Seed as an unsigned 64-bit integer. Leave blank for a random seed.")]
    private string _seed = string.Empty;

    /// <summary>
    /// Tries to produce the configured Run_Seed. Returns <c>true</c> and the parsed value
    /// when the authored text is a valid unsigned 64-bit integer; returns <c>false</c>
    /// (with <paramref name="seed"/> = 0) when the field is blank or cannot be parsed, so
    /// the caller can fall back to a generated seed and log the failure (R2.2).
    /// </summary>
    /// <param name="seed">The parsed 64-bit Run_Seed when this returns <c>true</c>; otherwise 0.</param>
    /// <returns><c>true</c> when a valid seed was provided; otherwise <c>false</c>.</returns>
    public bool TryGetSeed(out ulong seed)
    {
        if (_randomizeEachRun)
        {
            seed = System.BitConverter.ToUInt64(System.Guid.NewGuid().ToByteArray(), 0);
            return true;
        }
        if (!string.IsNullOrWhiteSpace(_seed) && ulong.TryParse(_seed.Trim(), out seed))
        {
            return true;
        }

        seed = 0UL;
        return false;
    }
}
