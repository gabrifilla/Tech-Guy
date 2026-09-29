using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Runs a cancellable warning and one damage check for the union of its areas.</summary>
public sealed class EnemyAttackExecution
{
    private readonly List<CombatGroundRing> _outlines = new List<CombatGroundRing>();
    private int _version;
    public bool IsWindingUp { get; private set; }
    public bool Completed { get; private set; }

    public IEnumerator Execute(EnemyAttackArea[] areas, Actor target, float windup, float damage,
        Color color, Func<bool> canAttack, Action onImpact = null, Action<float> animateWindup = null,
        bool showWarning = true, float impactHold = .16f)
    {
        Cancel();
        int version = _version;
        if (!target || target.IsDead || !canAttack()) yield break;
        IsWindingUp = true;
        foreach (var area in showWarning ? areas : Array.Empty<EnemyAttackArea>())
        {
            AddOutline(area.Outline(), color);
            if (area.Shape == EnemyAttackShape.Ring) AddOutline(area.Outline(true), color);
        }
        float duration = Mathf.Max(.25f, windup);
        for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
        {
            if (version != _version) yield break;
            if (!target || target.IsDead || !canAttack()) { Cancel(); yield break; }
            animateWindup?.Invoke(elapsed / duration);
            foreach (var outline in _outlines)
                outline.SetColor(Color.Lerp(color, Color.white, .65f * elapsed / duration));
            yield return null;
        }
        if (version != _version) yield break;
        if (!target || target.IsDead || !canAttack()) { Cancel(); yield break; }
        IsWindingUp = false;
        Completed = true;
        onImpact?.Invoke();
        foreach (var area in areas)
        {
            if (damage <= 0 || !area.Contains(target.transform.position)) continue;
            target.TakeDamage(damage);
            break; // Crosses and overlapping circles still hit only once per beat.
        }
        foreach (var outline in _outlines) outline.SetColor(Color.white);
        if (impactHold > 0) yield return new WaitForSeconds(impactHold);
        if (version == _version) ClearVisuals();
    }

    private void AddOutline(Vector3[] points, Color color)
    {
        var outline = CombatGroundRing.Create(null, "Enemy attack warning", color);
        outline.DrawPath(points, .12f);
        _outlines.Add(outline);
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
        foreach (var outline in _outlines)
            if (outline) { outline.gameObject.SetActive(false); UnityEngine.Object.Destroy(outline.gameObject); }
        _outlines.Clear();
    }
}
