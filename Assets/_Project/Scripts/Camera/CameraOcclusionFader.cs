using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TechGuy.Cameras
{
    /// <summary>Camera-owned visual occlusion. Never changes colliders or source materials.</summary>
    internal sealed class CameraOcclusionFader : IDisposable
    {
        private readonly float _fadedOpacity, _fadeOutDuration, _fadeInDuration;
        private readonly float _probeRadius, _pathRevealDistance, _restoreDelay;
        private RaycastHit[] _hits = new RaycastHit[64];
        private Collider[] _overlaps = new Collider[32];
        private readonly Dictionary<Renderer, FadeState> _states = new Dictionary<Renderer, FadeState>();
        private readonly Dictionary<Collider, Renderer[]> _bindings = new Dictionary<Collider, Renderer[]>();
        private readonly HashSet<Renderer> _obstructing = new HashSet<Renderer>();
        private readonly List<Renderer> _finished = new List<Renderer>();
        private Transform _lastTarget;
        private Vector3 _lastPosition, _travelDirection;
        private float _bindingRefreshTime;

        internal CameraOcclusionFader(float fadedOpacity = .12f, float fadeOutDuration = .12f,
            float fadeInDuration = .25f, float probeRadius = .5f,
            float pathRevealDistance = 2.5f, float restoreDelay = .12f)
        {
            _fadedOpacity = FiniteClamp(fadedOpacity, .12f, 0f, 1f);
            _fadeOutDuration = FiniteClamp(fadeOutDuration, .12f, .01f, 5f);
            _fadeInDuration = FiniteClamp(fadeInDuration, .25f, .01f, 5f);
            _probeRadius = FiniteClamp(probeRadius, .5f, .05f, 2f);
            _pathRevealDistance = FiniteClamp(pathRevealDistance, 2.5f, 0f, 8f);
            _restoreDelay = FiniteClamp(restoreDelay, .12f, 0f, 2f);
        }

        internal static float FiniteClamp(float value, float fallback, float min, float max)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
        }

        internal void Tick(Vector3 cameraPosition, Transform target, float unscaledDeltaTime)
        {
            if (!target || target != _lastTarget)
            {
                RestoreAll();
                _lastTarget = target;
                if (!target) return;
                _lastPosition = target.position;
                _travelDirection = target.forward;
            }
            float delta = float.IsNaN(unscaledDeltaTime) || float.IsInfinity(unscaledDeltaTime)
                ? 0f : Mathf.Max(0f, unscaledDeltaTime);
            if (_fadedOpacity >= 1f) { RestoreAll(); return; }
            // Bound cached hierarchy references when maps replace colliders under the same camera.
            _bindingRefreshTime += delta;
            if (_bindingRefreshTime >= 1f) { _bindings.Clear(); _bindingRefreshTime = 0f; }
            Vector3 movement = target.position - _lastPosition;
            movement.y = 0f;
            if (movement.sqrMagnitude > .0001f) _travelDirection = movement.normalized;
            _travelDirection.y = 0f;
            _lastPosition = target.position;
            _obstructing.Clear();

            int overlapCount;
            while ((overlapCount = Physics.OverlapSphereNonAlloc(cameraPosition, _probeRadius,
                _overlaps, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) == _overlaps.Length)
                Array.Resize(ref _overlaps, _overlaps.Length * 2);
            for (int i = 0; i < overlapCount; i++) Collect(_overlaps[i], target);
            Probe(cameraPosition, target.position + Vector3.up * .15f, target);
            Probe(cameraPosition, target.position + Vector3.up * .9f, target);
            Probe(cameraPosition, target.position + Vector3.up * 1.7f, target);
            if (_pathRevealDistance > 0f)
                Probe(cameraPosition, target.position + _travelDirection.normalized * _pathRevealDistance
                    + Vector3.up * .35f, target);

            _finished.Clear();
            foreach (var pair in _states)
            {
                FadeState state = pair.Value;
                if (!pair.Key) { state.Restore(); _finished.Add(pair.Key); continue; }
                bool obstructs = _obstructing.Contains(pair.Key);
                state.ClearTime = obstructs ? 0f : state.ClearTime + delta;
                float destination = obstructs || state.ClearTime < _restoreDelay ? _fadedOpacity : 1f;
                float duration = destination < state.Opacity ? _fadeOutDuration : _fadeInDuration;
                state.SetOpacity(Mathf.MoveTowards(state.Opacity, destination,
                    delta * (1f - _fadedOpacity) / duration));
                if (!obstructs && state.ClearTime >= _restoreDelay && state.Opacity >= 1f)
                { state.Restore(); _finished.Add(pair.Key); }
            }
            foreach (Renderer renderer in _finished) _states.Remove(renderer);
        }

        private void Probe(Vector3 origin, Vector3 destination, Transform target)
        {
            Vector3 offset = destination - origin;
            float distance = offset.magnitude;
            if (distance < .001f) return;
            int count;
            while ((count = Physics.SphereCastNonAlloc(origin, _probeRadius, offset / distance,
                _hits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) == _hits.Length)
                Array.Resize(ref _hits, _hits.Length * 2);
            for (int i = 0; i < count; i++) Collect(_hits[i].collider, target);
        }

        private void Collect(Collider collider, Transform target)
        {
            if (!collider || collider.isTrigger || collider.transform == target || collider.transform.IsChildOf(target)
                || collider.GetComponentInParent<Actor>() || collider.GetComponentInParent<Interactable>()) return;
            foreach (Renderer renderer in ResolveRenderers(collider))
            {
                if (!renderer || !renderer.enabled || !renderer.gameObject.activeInHierarchy
                    || renderer.transform == target || renderer.transform.IsChildOf(target)
                    || renderer.GetComponentInParent<Actor>() || renderer.GetComponentInParent<Interactable>()
                    || renderer.bounds.max.y <= target.position.y + .3f) continue;
                if (!_states.ContainsKey(renderer))
                {
                    FadeState state = FadeState.Create(renderer);
                    if (state == null) continue;
                    _states.Add(renderer, state);
                }
                _obstructing.Add(renderer);
            }
        }

        private Renderer[] ResolveRenderers(Collider collider)
        {
            if (_bindings.TryGetValue(collider, out Renderer[] renderers)) return renderers;
            // Do not climb an arbitrary environment root and fade an entire room.
            renderers = collider.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0)
            {
                var parent = collider.GetComponentInParent<MeshRenderer>();
                if (parent) renderers = new Renderer[] { parent };
                else if (collider.attachedRigidbody)
                    renderers = collider.attachedRigidbody.GetComponentsInChildren<MeshRenderer>();
            }
            _bindings.Add(collider, renderers);
            return renderers;
        }

        internal bool IsFaded(Collider collider)
        {
            if (!collider) return false;
            foreach (Renderer renderer in ResolveRenderers(collider))
                if (renderer && _states.TryGetValue(renderer, out FadeState state) && state.Opacity < .98f)
                    return true;
            return false;
        }

        private void RestoreAll()
        {
            foreach (FadeState state in _states.Values) state.Restore();
            _states.Clear();
            _bindings.Clear();
            _obstructing.Clear();
            _bindingRefreshTime = 0f;
        }

        public void Dispose() { RestoreAll(); _lastTarget = null; }

        private sealed class FadeState
        {
            private static readonly int OcclusionOpacity = Shader.PropertyToID("_OcclusionOpacity");
            private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
            private static readonly int ColorProperty = Shader.PropertyToID("_Color");
            private readonly Renderer _renderer;
            private readonly Material[] _originals, _displayed;
            private readonly List<Material> _owned = new List<Material>();
            private readonly List<Color> _colors = new List<Color>();
            private readonly List<int> _properties = new List<int>();
            private readonly List<float> _originalOpacity = new List<float>();
            internal float Opacity { get; private set; } = 1f;
            internal float ClearTime;

            private FadeState(Renderer renderer)
            {
                _renderer = renderer;
                _originals = renderer.sharedMaterials;
                _displayed = (Material[])_originals.Clone();
            }

            internal static FadeState Create(Renderer renderer)
            {
                var state = new FadeState(renderer);
                for (int i = 0; i < state._originals.Length; i++)
                {
                    Material source = state._originals[i];
                    if (!source) continue;
                    bool dither = source.HasProperty(OcclusionOpacity);
                    string shader = source.shader.name;
                    if (!dither && shader != "Universal Render Pipeline/Lit"
                        && shader != "Universal Render Pipeline/Simple Lit"
                        && shader != "Universal Render Pipeline/Unlit" && shader != "Standard") continue;
                    var material = new Material(source) { name = source.name + " (Camera fade)", hideFlags = HideFlags.HideAndDontSave };
                    int property = material.HasProperty(BaseColor) ? BaseColor : ColorProperty;
                    state._colors.Add(material.GetColor(property));
                    state._properties.Add(property);
                    state._originalOpacity.Add(dither ? material.GetFloat(OcclusionOpacity) : 1f);
                    if (!dither) ConfigureTransparency(material, shader == "Standard");
                    state._owned.Add(material);
                    state._displayed[i] = material;
                }
                if (state._owned.Count == 0) return null;
                renderer.sharedMaterials = state._displayed;
                return state;
            }

            private static void ConfigureTransparency(Material material, bool standard)
            {
                material.SetFloat(standard ? "_Mode" : "_Surface", standard ? 2f : 1f);
                if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                if (material.HasProperty("_SrcBlendAlpha")) material.SetInt("_SrcBlendAlpha", (int)BlendMode.One);
                if (material.HasProperty("_DstBlendAlpha")) material.SetInt("_DstBlendAlpha", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.DisableKeyword("_ALPHAMODULATE_ON");
                material.EnableKeyword(standard ? "_ALPHABLEND_ON" : "_SURFACE_TYPE_TRANSPARENT");
                material.SetShaderPassEnabled("DepthOnly", false);
                material.SetShaderPassEnabled("DepthNormals", false);
                material.renderQueue = (int)RenderQueue.Transparent;
            }

            internal void SetOpacity(float opacity)
            {
                Opacity = opacity;
                for (int i = 0; i < _owned.Count; i++)
                {
                    Material material = _owned[i];
                    if (material.HasProperty(OcclusionOpacity)) material.SetFloat(OcclusionOpacity, opacity * _originalOpacity[i]);
                    else { Color color = _colors[i]; color.a *= opacity; material.SetColor(_properties[i], color); }
                }
            }

            internal void Restore()
            {
                if (_renderer)
                {
                    Material[] current = _renderer.sharedMaterials;
                    // Preserve material replacements made by another owner during the fade.
                    for (int i = 0; i < current.Length && i < _displayed.Length; i++)
                        if (current[i] == _displayed[i]) current[i] = _originals[i];
                    _renderer.sharedMaterials = current;
                }
                foreach (Material material in _owned)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(material);
                    else UnityEngine.Object.DestroyImmediate(material);
                }
                _owned.Clear();
            }
        }
    }
}
