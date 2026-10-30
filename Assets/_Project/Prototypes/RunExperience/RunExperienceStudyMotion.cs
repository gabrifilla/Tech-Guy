using UnityEngine;

/// <summary>Isolated art study. No Actor, targeting, damage, rewards or production AI.</summary>
public sealed class RunExperienceStudyMotion : MonoBehaviour
{
    [SerializeField] private int _role;
    [SerializeField] private Transform _body, _tool;
    [SerializeField] private GameObject _warning;
    private Vector3 _bodyOrigin, _toolOrigin;
    public void Configure(int role, Transform body, Transform tool, GameObject warning)
    { _role=role; _body=body; _tool=tool; _warning=warning; }
    private void Awake()
    { if(_body)_bodyOrigin=_body.localPosition; if(_tool)_toolOrigin=_tool.localPosition; }
    private void Update() => Pose(Time.time);
    public void Pose(float seconds)
    {
        if(!_body || !_tool)return;
        float t=seconds%4f;
        float windup=t<1f?t:t<1.25f?1f:Mathf.Max(0,1-(t-1.25f)/.9f);
        _body.localPosition=_bodyOrigin+Vector3.up*Mathf.Sin(seconds*(_role==0?12:3))*.045f;
        _body.localRotation=Quaternion.Euler(_role==0?-18*windup:0,0,_role==4?12*windup:0);
        _tool.localPosition=_toolOrigin+(_role==1?Vector3.right:Vector3.back)*windup*.3f;
        _tool.localRotation=Quaternion.Euler(_role==3?-65*windup:_role==2?-35*windup:0,_role==4?45*windup:0,0);
        if(_warning)_warning.SetActive(t<1.25f);
    }
}
