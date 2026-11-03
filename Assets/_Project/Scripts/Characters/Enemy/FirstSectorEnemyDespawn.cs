using UnityEngine;

/// <summary>Short scaled-time visual collapse. Encounter teardown owns this object too.</summary>
public sealed class FirstSectorEnemyDespawn : MonoBehaviour
{
    private float _time;private Vector3 _scale;
    private Transform[] _parts;
    private Vector3[] _rest;
    public void EnableDisassembly()
    {
        var renderers=GetComponentsInChildren<MeshRenderer>();_parts=new Transform[renderers.Length];_rest=new Vector3[renderers.Length];
        for(int i=0;i<renderers.Length;i++){_parts[i]=renderers[i].transform;_rest[i]=_parts[i].localPosition;}
    }
    private void Awake(){_scale=transform.localScale;}
    private void Update()
    {
        if(Time.timeScale<=0||Time.deltaTime<=0)return;
        _time+=Time.deltaTime;float t=Mathf.Clamp01(_time/.28f);
        transform.localScale=Vector3.Scale(_scale,new Vector3(1-t*.24f,1-t*.93f,1-t*.24f));
        if(_parts!=null)for(int i=0;i<_parts.Length;i++)
            if(_parts[i])_parts[i].localPosition=_rest[i]+new Vector3((i%2==0?-1:1)*.12f*t,.08f*t,(i%3-1)*.1f*t);
        if(t>=1)Destroy(gameObject);
    }
}
