using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Runs a cancellable warning and one damage check for the union of its areas.</summary>
public sealed class EnemyAttackExecution
{
    private readonly List<CombatGroundRing> _outlines = new List<CombatGroundRing>();
    private readonly List<FillBinding> _fills = new List<FillBinding>();
    [SerializeField] private bool _showFill = true;
    private int _version;
    public bool IsWindingUp { get; private set; }
    public bool Completed { get; private set; }

    /// <summary>Pairs a <see cref="CombatGroundFill"/> with the area it renders so the windup loop can grow it.</summary>
    private readonly struct FillBinding
    {
        public readonly CombatGroundFill Fill;
        public readonly EnemyAttackArea Area;
        public FillBinding(CombatGroundFill fill, in EnemyAttackArea area) { Fill = fill; Area = area; }
    }

    public IEnumerator Execute(EnemyAttackArea[] areas, Actor target, float windup, float damage,
        Color color, Func<bool> canAttack, Action onImpact = null, Action<float> animateWindup = null,
        bool showWarning = true, float impactHold = .16f)
    {
        Cancel();
        int version = _version;
        if (!target || target.IsDead || !canAttack()) yield break;
        IsWindingUp = true;
        // Warning rendering is forced on for every routed attack so previously-suppressed attacks
        // (e.g. the straight shot) still render a danger-zone telegraph (R6.1/R7.2); the
        // controlling-vs-not and showWarning distinctions no longer gate rendering. When no valid
        // area resolves (empty union) nothing is created and the attack proceeds unrendered (R6.6).
        if (areas != null)
            foreach (var area in areas)
            {
                AddOutline(area.Outline(), TelegraphFill.RedBase);
                if (area.Shape == EnemyAttackShape.Ring) AddOutline(area.Outline(true), TelegraphFill.RedBase);
                if (_showFill) AddFill(area);
            }
        float duration = Mathf.Max(.25f, windup);
        for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
        {
            if (version != _version) yield break;
            if (!target || target.IsDead || !canAttack()) { Cancel(); yield break; }
            float fraction = elapsed / duration;
            animateWindup?.Invoke(fraction);
            // Shared red base lerped toward white by the windup fraction, identical for every attack type (R5.3).
            Color telegraphColor = TelegraphIntensity.ColorAt(TelegraphFill.RedBase, fraction);
            foreach (var outline in _outlines) outline.SetColor(telegraphColor);
            // Grow each fill from origin to the area's exact extent, reaching full coverage at impact (R6.2/R6.4).
            foreach (var binding in _fills)
            {
                binding.Fill.SetArea(binding.Area, TelegraphFill.ProgressAt(fraction));
                binding.Fill.SetColor(telegraphColor);
            }
            yield return null;
        }
        if (version != _version) yield break;
        if (!target || target.IsDead || !canAttack()) { Cancel(); yield break; }
        IsWindingUp = false;
        Completed = true;
        onImpact?.Invoke();
        foreach (var area in areas ?? Array.Empty<EnemyAttackArea>())
        {
            if (damage <= 0 || !area.Contains(target.transform.position)) continue;
            target.TakeDamage(damage);
            break; // Crosses and overlapping circles still hit only once per beat.
        }
        foreach (var outline in _outlines) outline.SetColor(Color.white);
        foreach (var binding in _fills)
        {
            binding.Fill.SetArea(binding.Area, 1f); // Fill matches the exact damage-test extent at impact (R6.4/R8.5).
            binding.Fill.SetColor(Color.white);
        }
        if (impactHold > 0) yield return new WaitForSeconds(impactHold);
        if (version == _version) ClearVisuals();
    }

    private void AddOutline(Vector3[] points, Color color)
    {
        var outline = CombatGroundRing.Create(null, "Enemy attack warning", color);
        outline.DrawPath(points, .12f);
        _outlines.Add(outline);
    }

    private void AddFill(in EnemyAttackArea area)
    {
        var fill = CombatGroundFill.Create(null, "Enemy attack fill", TelegraphFill.RedBase);
        fill.SetArea(area, 0f);
        _fills.Add(new FillBinding(fill, area));
    }

    public void Cancel()
    {
        _version++;
        IsWindingUp = false;
        Completed = false;
        ClearVisuals();
    }

    private void ClearVisuals()
    {
        // Ring and fill are torn down in the same frame on impact or cancel (R6.6/R8.3).
        foreach (var outline in _outlines)
            if (outline) { outline.gameObject.SetActive(false); UnityEngine.Object.Destroy(outline.gameObject); }
        _outlines.Clear();
        foreach (var binding in _fills)
            if (binding.Fill) { binding.Fill.Clear(); binding.Fill.gameObject.SetActive(false); UnityEngine.Object.Destroy(binding.Fill.gameObject); }
        _fills.Clear();
    }
}
