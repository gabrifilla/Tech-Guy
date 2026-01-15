using System.Collections.Generic;
using UnityEngine;

public class HitboxDamage : MonoBehaviour
{
    public Actor owner;
    public float damage = 0f;
    public string hitEffectResourcePath;
    [SerializeField] private bool logDamage = true;
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

        if (logDamage)
        {
            Debug.Log($"HitboxDamage: {owner.name} hit {actor.name} for {damage}");
        }

        actor.TakeDamage(damage);
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
