using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>Samples the actual contact pose at impact; combat owns the clock, including run modifiers.</summary>
public sealed class SkillAnimationPlayer : MonoBehaviour
{
    private Animator _animator;
    private SkillAnimationLibrary _library;
    private SkillAnimationLibrary.Entry _entry;
    private PlayableGraph _graph;
    private AnimationClipPlayable _clip;
    private bool _rootMotion;
    public bool IsPlaying => _graph.IsValid();
    public SkillMotion CurrentMotion { get; private set; }
    public float NormalizedTime { get; private set; }
    private void Awake()
    { _animator=GetComponent<Animator>(); _library=Resources.Load<SkillAnimationLibrary>(SkillAnimationLibrary.ResourcePath); }

    public void Strike(SkillMotion motion,float elapsed,float start,float impact,float end)
    {
        if (!_library) return;
        var entry=_library.Get(motion);
        if (entry==null || !entry.clip) return;
        float phase=elapsed<=impact ? Mathf.Lerp(0,entry.contact,Mathf.InverseLerp(start,impact,elapsed)) :
            Mathf.Lerp(entry.contact,1,Mathf.InverseLerp(impact,Mathf.Max(impact+.01f,end),elapsed));
        Sample(entry,phase);
    }
    public void Contact(SkillMotion motion)
    { var entry=_library ? _library.Get(motion) : null; if (entry!=null) Sample(entry,entry.contact); }
    private void Sample(SkillAnimationLibrary.Entry entry,float phase)
    {
        if (!_animator || !_animator.isActiveAndEnabled || !_animator.avatar || !entry.clip) return;
        if (_entry!=entry || !_graph.IsValid())
        {
            bool alreadyPlaying=_graph.IsValid();
            if (alreadyPlaying) _graph.Destroy(); else _rootMotion=_animator.applyRootMotion;
            _animator.applyRootMotion=false;
            _entry=entry; _graph=PlayableGraph.Create("Skill pose - "+name);
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            _clip=AnimationClipPlayable.Create(_graph,entry.clip); _clip.SetSpeed(0);
            _clip.SetApplyFootIK(false); _clip.SetApplyPlayableIK(false);
            AnimationPlayableOutput.Create(_graph,"Skill",_animator).SetSourcePlayable(_clip);
            _graph.Play();
        }
        CurrentMotion=entry.motion; NormalizedTime=Mathf.Clamp01(phase);
        _clip.SetTime(entry.clip.length*NormalizedTime); _graph.Evaluate(0);
    }
    public void Release()
    {
        if (!_graph.IsValid()) return;
        _graph.Destroy(); _entry=null;
        if (_animator)
        {
            _animator.applyRootMotion=_rootMotion;
            if (_animator.isActiveAndEnabled && _animator.HasState(0,Animator.StringToHash("Idle")))
                _animator.CrossFadeInFixedTime("Idle",.08f);
        }
    }
    private void OnDisable() => Release();
    private void OnDestroy() => Release();
}
