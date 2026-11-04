using System.Collections.Generic;
using UnityEngine;

/// <summary>Quiet floor routing and beveled render geometry. No colliders, lights or navigation changes.</summary>
public sealed class FirstSectorRoomPresentation : MonoBehaviour
{
    private Mesh _lines,_bevel;
    private MeshRenderer _floor,_details;
    private Material _originalFloor,_revisedFloor;
    private MeshFilter[] _covers;
    private Mesh[] _originalCovers;
    public static void Create(Transform arena,ManualNodeDefinition node,bool covered,FirstSectorContentDefinition content)
    {
        var view=arena.gameObject.AddComponent<FirstSectorRoomPresentation>();
        view._floor=arena.Find("Walkable arena").GetComponent<MeshRenderer>();view._originalFloor=view._floor.sharedMaterial;view._revisedFloor=covered?content.CoveredFloor:content.OpenFloor;
        var filters=new List<MeshFilter>();for(int i=0;i<2;i++){var cover=arena.Find("Cobertura deslocada "+i);if(cover)filters.Add(cover.GetComponent<MeshFilter>());}
        view._covers=filters.ToArray();view._originalCovers=new Mesh[filters.Count];for(int i=0;i<filters.Count;i++)view._originalCovers[i]=filters[i].sharedMesh;
        view._bevel=BeveledBox();
        var vertices=new List<Vector3>();var triangles=new List<int>();float x=node.ArenaSize.x*.5f-1.1f,z=node.ArenaSize.y*.5f-1.1f;
        AddQuad(vertices,triangles,-x,-z,x,-z+.12f);AddQuad(vertices,triangles,-x,z-.12f,x,z);
        AddQuad(vertices,triangles,-x,-z,-x+.12f,z);AddQuad(vertices,triangles,x-.12f,-z,x,z);
        if(covered)
        {
            // Traces indicate the two circulation lanes. They are flat, matte and never threats.
            foreach(float lane in new[]{-6.6f,6.6f})AddQuad(vertices,triangles,lane-.06f,-z,lane+.06f,z);
            for(int i=0;i<4;i++)AddQuad(vertices,triangles,-x+i*.38f,-z+.4f,-x+i*.38f+.18f,-z+.95f);
        }
        else
        {
            // Open space gets a broken perimeter grid; the central combat floor stays quiet.
            for(int i=0;i<5;i++){float p=Mathf.Lerp(-x,x,(i+.5f)/5);AddQuad(vertices,triangles,p-.04f,-z,p+.04f,-z+1.2f);AddQuad(vertices,triangles,p-.04f,z-1.2f,p+.04f,z);}
        }
        view._lines=MeshOf("Trilhas de memória",vertices,triangles);
        var details=new GameObject("Trilhas visuais sem colisão");details.transform.SetParent(arena,false);details.AddComponent<MeshFilter>().sharedMesh=view._lines;view._details=details.AddComponent<MeshRenderer>();view._details.sharedMaterial=content.RoomTrim;view._details.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;view._details.receiveShadows=false;
        view.SetVisible(true);
    }
    public void SetVisible(bool visible)
    {
        if(_floor)_floor.sharedMaterial=visible?_revisedFloor:_originalFloor;
        if(_details)_details.enabled=visible;
        if(_covers!=null)for(int i=0;i<_covers.Length;i++)if(_covers[i])_covers[i].sharedMesh=visible?_bevel:_originalCovers[i];
    }
    private void OnDestroy(){SetVisible(false);if(_lines)Destroy(_lines);if(_bevel)Destroy(_bevel);}
    private static void AddQuad(List<Vector3> v,List<int> t,float x0,float z0,float x1,float z1)
    {int i=v.Count;v.Add(new Vector3(x0,.008f,z0));v.Add(new Vector3(x0,.008f,z1));v.Add(new Vector3(x1,.008f,z1));v.Add(new Vector3(x1,.008f,z0));t.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});}
    private static Mesh BeveledBox()
    {
        var v=new List<Vector3>();var t=new List<int>();
        var ring=new[]{new Vector2(-.475f,-.5f),new Vector2(-.5f,-.475f),new Vector2(-.5f,.475f),new Vector2(-.475f,.5f),new Vector2(.475f,.5f),new Vector2(.5f,.475f),new Vector2(.5f,-.475f),new Vector2(.475f,-.5f)};
        System.Action<Vector3,Vector3,Vector3> tri=(a,b,c)=>{int i=v.Count;v.Add(a);v.Add(b);v.Add(c);t.AddRange(new[]{i,i+1,i+2});};
        for(int side=0;side<8;side++)
        {
            int next=(side+1)%8;var a=new Vector3(ring[side].x,-.5f,ring[side].y);var b=new Vector3(ring[next].x,-.5f,ring[next].y);var c=new Vector3(a.x,.44f,a.z);var d=new Vector3(b.x,.44f,b.z);var e=new Vector3(a.x*.92f,.5f,a.z*.92f);var f=new Vector3(b.x*.92f,.5f,b.z*.92f);
            tri(a,b,c);tri(b,d,c);tri(c,d,e);tri(d,f,e);tri(new Vector3(0,.5f,0),e,f);tri(new Vector3(0,-.5f,0),b,a);
        }
        return MeshOf("Cobertura chanfrada somente visual",v,t);
    }
    private static Mesh MeshOf(string name,List<Vector3> vertices,List<int> triangles)
    {var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);var uv=new List<Vector2>();foreach(var v in vertices)uv.Add(new Vector2(v.x,v.z));mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;}
}
