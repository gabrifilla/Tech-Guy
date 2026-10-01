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

    /// <summary>
    /// Primes the hit-effect pool once at boot so the first combat hit does not pay a synchronous
    /// <see cref="Resources.Load"/> from disk mid-fight (Requisito 5.2). The pool itself already exists;
    /// this just creates the first pooled instance ahead of time by loading each equippable weapon's
    /// hit effect through the normal <see cref="GetEffectInstance"/> path, so observable behavior is
    /// unchanged (Requisito 5.3). If an effect cannot be preloaded, it logs a clear warning and leaves
    /// the on-demand path intact, without breaking combat (Requisito 5.5).
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void WarmUpHitEffects()
    {
        string[] weaponPaths = WeaponLoadout.ResourcePaths;
        if (weaponPaths == null) return;

        for (int i = 0; i < weaponPaths.Length; i++)
        {
            WeaponScript weapon = Resources.Load<WeaponScript>(weaponPaths[i]);
            if (weapon == null) continue;

            string effectPath = weapon.HitEffectResourcePath;
            if (string.IsNullOrEmpty(effectPath)) continue;

            // Mirrors the on-demand call used in combat; priming it now moves the disk load to boot.
            if (GetEffectInstance(effectPath) == null)
            {
                Debug.LogWarning(
                    $"HitboxDamage: could not warm up hit effect at Resources path '{effectPath}'. " +
                    "Falling back to on-demand loading on first use.");
            }
        }
    }

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
        hitEffectResourcePath = newHitEffectResourcePath;
        damage = newDamage;
        hitActors.Clear();
    }

    /// <summary>
    /// Overrides the basic-swing push distance (metres). Used to make a weapon's basic attack land in
    /// place with no shove (e.g. the Gauntlet/Manopla), while keeping its stagger/stance reaction. The
    /// value is clamped non-negative; it does not affect any Stance_Break Knockback, which is a separate
    /// deliberate effect.
    /// </summary>
    public void SetPushDistance(float value)
    {
        pushDistance = Mathf.Max(0f, value);
    }

    /// <summary>
    /// Configures the basic swing's stance-break effect (e.g. a deliberate Knockback opt-in boon).
    /// This is the break effect applied only when a basic hit actually breaks stance; it does not
    /// change the per-hit <see cref="SetPushDistance"/> shove (Requisitos 7.1/7.4).
    /// </summary>
    public void SetStanceBreakEffect(StanceBreakEffect effect) => breakEffect = effect;

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
        if (owner == null || actor == null || actor == owner || actor.IsDead) return false;
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

        if (owner is PlayerActor attacker)
        {
            // Only the hitbox (Basic_Attack) path raises the dedicated basic-hit channel. Measure the
            // damage actually dealt around DealResolvedAttackDamage (which already raises the generic
            // OnHit/OnKill — R3.4) and surface it as a basic hit (R3.1/R3.2). Skill hits never reach
            // this branch, so they never fire the basic channel (R3.3).
            float before = actor.health;
            attacker.DealResolvedAttackDamage(actor, finalDamage);
            attacker.RaiseBasicAttackHit(actor, before - actor.health);
        }
        else actor.TakeDamage(finalDamage);

        // Basic swings apply crowd-control too, so weak mobs get pushed back / staggered
        // instead of the player having to dash out of their range.
        if (owner is PlayerActor player)
        {
            if (reactionType != HitReactionType.None || stanceDamage > 0f)
                player.ApplyHitReactionTo(actor, reactionType, reactionStrength, stanceDamage, breakEffect, pushDistance);

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
