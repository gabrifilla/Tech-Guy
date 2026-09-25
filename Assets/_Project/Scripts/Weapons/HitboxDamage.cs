using System.Collections.Generic;
using UnityEngine;

public class HitboxDamage : MonoBehaviour
{
    public Actor owner;
    public float damage = 0f;
    public string hitEffectResourcePath;
    [SerializeField] private bool logDamage = true;

    [Header("Reaction (basic swing)")]
    [Tooltip("Basic swings only Push/Stagger and chip stance; they never stun/knock-up directly.")]
    [SerializeField] private HitReactionType reactionType = HitReactionType.Stagger;
    [SerializeField] private HitStrength reactionStrength = HitStrength.Light;
    [SerializeField, Min(0f)] private float stanceDamage = 12f;
    [SerializeField, Min(0f)] private float pushDistance = 0.35f;
    [Tooltip("Hard CC only if a basic hit actually breaks stance. Keep None for basic swings.")]
    [SerializeField] private StanceBreakEffect breakEffect = StanceBreakEffect.None;
    private readonly HashSet<Actor> hitActors = new HashSet<Actor>();
    private static readonly Dictionary<string, List<ParticleSystem>> effectPools = new Dictionary<string, List<ParticleSystem>>();
    private static Transform effectPoolRoot;

    private void OnEnable()
    {
        hitActors.Clear();
    }

    private void OnDisable()
    {
        hitActors.Clear();
    }

    public void Configure(Actor newOwner, float newDamage, string newHitEffectResourcePath)
    {
        owner = newOwner;
        damage = newDamage;
        hitEffectResourcePath = newHitEffectResourcePath;
        hitActors.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryApplyDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryApplyDamage(other);
    }

    public bool TryDamageActor(Actor actor)
    {
        if (owner == null || actor == null || actor == owner) return false;
        if (!hitActors.Add(actor)) return false;

        SpawnHitEffect(actor);

        float finalDamage = damage;
        if (owner is PlayerActor playerActor)
        {
            finalDamage = playerActor.RollAttackDamage(damage).Amount;
        }

        if (logDamage)
        {
            Debug.Log($"HitboxDamage: {owner.name} hit {actor.name} for {finalDamage}");
        }

        actor.TakeDamage(finalDamage);

        // Basic swings apply crowd-control too, so weak mobs get pushed back / staggered
        // instead of the player having to dash out of their range.
        if (owner is PlayerActor player)
        {
            if (reactionType != HitReactionType.None || stanceDamage > 0f)
                player.ApplyHitReactionTo(actor, reactionType, reactionStrength, stanceDamage, breakEffect, pushDistance);

            // On-hit run modifiers (e.g. basic attacks Burn / Freeze) also proc on basic swings.
            if (player.OnHitEffects.HasAnyEffect) player.OnHitEffects.ApplyTo(actor);
        }

        return true;
    }

    private void SpawnHitEffect(Actor actor)
    {
        if (string.IsNullOrEmpty(hitEffectResourcePath)) return;

        ParticleSystem fx = GetEffectInstance(hitEffectResourcePath);
        if (fx == null) return;

        fx.transform.position = actor.transform.position + new Vector3(0, 1, 0);
        fx.transform.rotation = Quaternion.identity;
        fx.gameObject.SetActive(true);
        fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        fx.Play(true);
    }

    private void TryApplyDamage(Collider other)
    {
        if (owner == null || other == null) return;

        // Ignore collisions with the owner
        if (other.gameObject == owner.gameObject) return;
        if (other.attachedRigidbody != null && other.attachedRigidbody.gameObject == owner.gameObject) return;

        Actor actor = ResolveActor(other);
        if (actor != null)
        {
            TryDamageActor(actor);
        }
    }

    private Actor ResolveActor(Collider other)
    {
        Actor actor = other.GetComponentInParent<Actor>();
        if (actor != null) return actor;

        actor = other.GetComponentInChildren<Actor>();
        if (actor != null) return actor;

        Interactable interactable = other.GetComponentInParent<Interactable>();
        if (interactable != null && interactable.myActor != null)
        {
            return interactable.myActor;
        }

        interactable = other.GetComponentInChildren<Interactable>();
        if (interactable != null)
        {
            return interactable.myActor;
        }

        return null;
    }

    private static ParticleSystem GetEffectInstance(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath)) return null;

        if (!effectPools.TryGetValue(resourcePath, out List<ParticleSystem> pool))
        {
            pool = new List<ParticleSystem>();
            effectPools[resourcePath] = pool;
        }

        for (int i = 0; i < pool.Count; i++)
        {
            ParticleSystem ps = pool[i];
            if (ps != null && !ps.gameObject.activeSelf)
            {
                return ps;
            }
        }

        ParticleSystem prefab = Resources.Load<ParticleSystem>(resourcePath);
        if (prefab == null) return null;

        EnsureEffectPoolRoot();

        ParticleSystem instance = Object.Instantiate(prefab, effectPoolRoot);
        ConfigureEffectInstance(instance);
        pool.Add(instance);
        return instance;
    }

    private static void EnsureEffectPoolRoot()
    {
        if (effectPoolRoot != null) return;

        GameObject root = new GameObject("HitEffectPool");
        effectPoolRoot = root.transform;
    }

    private static void ConfigureEffectInstance(ParticleSystem ps)
    {
        if (ps == null) return;

        var main = ps.main;
        main.stopAction = ParticleSystemStopAction.Callback;

        if (ps.GetComponent<AutoDisableOnStop>() == null)
        {
            ps.gameObject.AddComponent<AutoDisableOnStop>();
        }

        ps.gameObject.SetActive(false);
    }
}
