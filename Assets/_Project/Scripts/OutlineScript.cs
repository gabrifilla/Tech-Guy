using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class OutlineScript : MonoBehaviour
{
    [Tooltip("Run the hover raycast at most once every N frames. 1 = every frame.")]
    [SerializeField] private int _raycastFrameInterval = 2;

    private Transform highlight;
    private Transform selection;
    private RaycastHit raycastHit;

    // Cached main camera; refreshed on demand when it becomes null (scene reload, camera swap).
    private Camera _cam;

    // Throttle for the per-frame Physics.Raycast. Pure, frame-driven: fires on the first frame
    // and then at most once every _raycastFrameInterval frames (see Core/FrameThrottle).
    private FrameThrottle _raycastThrottle;

    // Caches the Outline component per Transform so we resolve/add it once instead of calling
    // GetComponent<Outline>() several times per frame. A null entry means "known to have none yet".
    private readonly Dictionary<Transform, Outline> _outlineCache = new Dictionary<Transform, Outline>();

    private void Awake()
    {
        _raycastThrottle = new FrameThrottle(_raycastFrameInterval);
    }

    void Update()
    {
        Camera cam = ResolveCamera();
        if (cam == null)
        {
            return;
        }

        // Highlight — only re-run the raycast on throttle ticks; between ticks the perceived
        // hover highlight is left exactly as the last raycast decided (no visual change).
        if (_raycastThrottle.Tick())
        {
            if (highlight != null)
            {
                SetOutlineEnabled(highlight, false);
                highlight = null;
            }

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!EventSystem.current.IsPointerOverGameObject() && Physics.Raycast(ray, out raycastHit)) //Make sure you have EventSystem in the hierarchy before using EventSystem
            {
                highlight = raycastHit.transform;
                if (highlight.CompareTag("Interactable") && highlight != selection)
                {
                    Outline outline = GetOrCreateOutline(highlight);
                    if (outline != null)
                    {
                        outline.enabled = true;
                    }
                }
                else
                {
                    highlight = null;
                }
            }
        }

        // Selection
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            if (highlight)
            {
                if (selection != null)
                {
                    SetOutlineEnabled(selection, false);
                }
                selection = raycastHit.transform;
                SetOutlineEnabled(selection, true);
                highlight = null;
            }
            else
            {
                if (selection)
                {
                    SetOutlineEnabled(selection, false);
                    selection = null;
                }
            }
        }
    }

    private Camera ResolveCamera()
    {
        if (_cam == null)
        {
            _cam = Camera.main;
        }
        return _cam;
    }

    // Returns the cached Outline for the transform, creating one with the hover style the first
    // time an Interactable is highlighted (mirrors the original red / 2.0f width behavior).
    private Outline GetOrCreateOutline(Transform target)
    {
        if (_outlineCache.TryGetValue(target, out Outline cached) && cached != null)
        {
            return cached;
        }

        if (target.TryGetComponent(out Outline existing))
        {
            _outlineCache[target] = existing;
            return existing;
        }

        Outline added = target.gameObject.AddComponent<Outline>();
        added.OutlineColor = Color.red;
        added.OutlineWidth = 2.0f;
        _outlineCache[target] = added;
        return added;
    }

    // Toggles the Outline on a transform using the cache, resolving it once if not yet seen.
    private void SetOutlineEnabled(Transform target, bool enabled)
    {
        if (target == null)
        {
            return;
        }

        if (!_outlineCache.TryGetValue(target, out Outline outline) || outline == null)
        {
            if (!target.TryGetComponent(out outline))
            {
                return;
            }
            _outlineCache[target] = outline;
        }

        outline.enabled = enabled;
    }
}
