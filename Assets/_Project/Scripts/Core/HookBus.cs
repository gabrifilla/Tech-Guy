using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic per-run combat-event bus. Combat scripts raise gameplay events here
/// (crit, kill, freeze, burn, stance break, dash, explosion) and boons subscribe to
/// react. Owned and cleared per run by <c>RunBoons</c>; it is a plain C# class, not a
/// <see cref="MonoBehaviour"/>, and is obtained through <c>RunBoons</c> ownership rather
/// than scene lookups so no <c>FindObjectOfType</c>/<c>GameObject.Find</c> is needed.
///
/// Every raise-site isolates subscribers: each delegate in the invocation list is called
/// inside its own try/catch, a thrown subscriber is logged via <see cref="Debug.LogException"/>,
/// and the exception is never re-raised to the caller so one faulty listener cannot break
/// the combat pipeline.
/// </summary>
public sealed class HookBus
{
    /// <summary>
    /// Raised on every direct hit that dealt damage, with the struck actor and damage dealt.
    /// A Direct_Hit is a basic attack, area, or projectile hit resolved through the player's
    /// damage pipeline (not a discharge, explosion, ricochet, or cascade secondary hit).
    /// </summary>
    public event Action<Actor, float> OnHit;

    /// <summary>Raised after a critical hit is applied, with the struck actor and damage dealt.</summary>
    public event Action<Actor, float> OnCrit;

    /// <summary>Raised at most once per enemy when it dies (deduped by <see cref="_killed"/>).</summary>
    public event Action<Actor> OnKill;

    /// <summary>
    /// Raised on every Basic_Attack direct hit that dealt damage, with the struck actor and
    /// damage dealt. Fired <b>only</b> by the <c>HitboxDamage</c> path, so subscribers react to
    /// the basic attack and never to skill hits. Additional to <see cref="OnHit"/>, which still
    /// fires for the same hit.
    /// </summary>
    public event Action<Actor, float> OnBasicHit;

    /// <summary>
    /// Raised at most once per enemy when a Basic_Attack direct hit kills it (deduped by
    /// <see cref="_basicKilled"/>). Fired only by the <c>HitboxDamage</c> path.
    /// </summary>
    public event Action<Actor> OnBasicKill;

    /// <summary>Raised when an actor becomes frozen (chill at or above the freeze threshold).</summary>
    public event Action<Actor> OnFreeze;

    /// <summary>Raised when a burn is applied or refreshed on an actor.</summary>
    public event Action<Actor> OnBurn;

    /// <summary>Raised when an actor's stance is broken.</summary>
    public event Action<Actor> OnStanceBreak;

    /// <summary>Raised once per dash.</summary>
    public event Action OnDash;

    /// <summary>Raised at a detonation/explosion origin.</summary>
    public event Action<Vector3> OnExplosion;

    /// <summary>Enemies already reported dead this run, so <see cref="OnKill"/> fires once each.</summary>
    private readonly HashSet<Actor> _killed = new HashSet<Actor>();

    /// <summary>
    /// Enemies already reported dead via the basic-hit channel this run, so <see cref="OnBasicKill"/>
    /// fires once each. Kept separate from <see cref="_killed"/> so the two channels dedup independently.
    /// </summary>
    private readonly HashSet<Actor> _basicKilled = new HashSet<Actor>();

    /// <summary>Raises <see cref="OnHit"/> for a direct hit that dealt damage.</summary>
    public void RaiseHit(Actor actor, float damage) => Dispatch(OnHit, actor, damage);

    /// <summary>Raises <see cref="OnCrit"/> for a critical hit.</summary>
    public void RaiseCrit(Actor actor, float damage) => Dispatch(OnCrit, actor, damage);

    /// <summary>
    /// Raises <see cref="OnKill"/> the first time an actor is reported killed; repeat
    /// notifications for the same actor are ignored. Null actors are ignored.
    /// </summary>
    public void RaiseKill(Actor actor)
    {
        if (actor == null) return;
        if (!_killed.Add(actor)) return;
        Dispatch(OnKill, actor);
    }

    /// <summary>Raises <see cref="OnBasicHit"/> for a Basic_Attack direct hit that dealt damage.</summary>
    public void RaiseBasicHit(Actor actor, float damage) => Dispatch(OnBasicHit, actor, damage);

    /// <summary>
    /// Raises <see cref="OnBasicKill"/> the first time an actor is reported killed by a Basic_Attack;
    /// repeat notifications for the same actor are ignored. Null actors are ignored. Mirrors
    /// <see cref="RaiseKill"/>/<see cref="_killed"/> with its own dedup set.
    /// </summary>
    public void RaiseBasicKill(Actor actor)
    {
        if (actor == null) return;
        if (!_basicKilled.Add(actor)) return;
        Dispatch(OnBasicKill, actor);
    }

    /// <summary>Raises <see cref="OnFreeze"/> for a frozen actor.</summary>
    public void RaiseFreeze(Actor actor) => Dispatch(OnFreeze, actor);

    /// <summary>Raises <see cref="OnBurn"/> when a burn is applied or refreshed.</summary>
    public void RaiseBurn(Actor actor) => Dispatch(OnBurn, actor);

    /// <summary>Raises <see cref="OnStanceBreak"/> for a broken stance.</summary>
    public void RaiseStanceBreak(Actor actor) => Dispatch(OnStanceBreak, actor);

    /// <summary>Raises <see cref="OnDash"/> once for a dash.</summary>
    public void RaiseDash() => Dispatch(OnDash);

    /// <summary>Raises <see cref="OnExplosion"/> at the given origin.</summary>
    public void RaiseExplosion(Vector3 origin) => Dispatch(OnExplosion, origin);

    /// <summary>
    /// Clears every subscription and the kill dedupe set so a previous run's subscribers
    /// never fire in a later run. Called by <c>RunBoons</c> on run end.
    /// </summary>
    public void Clear()
    {
        OnHit = null;
        OnCrit = null;
        OnKill = null;
        OnBasicHit = null;
        OnBasicKill = null;
        OnFreeze = null;
        OnBurn = null;
        OnStanceBreak = null;
        OnDash = null;
        OnExplosion = null;
        _killed.Clear();
        _basicKilled.Clear();
    }

    private static void Dispatch(Action handler)
    {
        if (handler == null) return;
        foreach (Delegate d in handler.GetInvocationList())
        {
            try
            {
                ((Action)d)();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    private static void Dispatch<T>(Action<T> handler, T arg)
    {
        if (handler == null) return;
        foreach (Delegate d in handler.GetInvocationList())
        {
            try
            {
                ((Action<T>)d)(arg);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    private static void Dispatch<T1, T2>(Action<T1, T2> handler, T1 arg1, T2 arg2)
    {
        if (handler == null) return;
        foreach (Delegate d in handler.GetInvocationList())
        {
            try
            {
                ((Action<T1, T2>)d)(arg1, arg2);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
