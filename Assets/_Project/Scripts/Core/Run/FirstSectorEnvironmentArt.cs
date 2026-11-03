using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Optional archive render layer. Never creates or alters collision/navigation.</summary>
public sealed class FirstSectorEnvironmentArt : MonoBehaviour
{
    private readonly List<MeshFilter> _covers=new List<MeshFilter>();
    private readonly List<Mesh> _oldMeshes=new List<Mesh>();
    private readonly List<MeshFilter> _walls=new List<MeshFilter>();
    private readonly List<Mesh> _oldWalls=new List<Mesh>();
    private readonly List<Mesh> _recessedWalls=new List<Mesh>();
    private readonly List<MeshRenderer> _surfaces=new List<MeshRenderer>();
    private readonly List<Material> _oldMaterials=new List<Material>();
    private MeshRenderer _racks;
    private Mesh _combinedRacks;
    private Mesh _cover;
    private Material _archive,_floor,_wall;
    public bool Visible {get;private set;}

    public static void Create(Transform arena,FirstSectorContentDefinition content)
    {
        if(!content.ArchiveCover||!content.ArchiveRack||!content.ArchiveMaterial||!content.ArchiveFloor||!content.ArchiveWall)return;
        var view=arena.gameObject.AddComponent<FirstSectorEnvironmentArt>();
        view._cover=content.ArchiveCover;view._archive=content.ArchiveMaterial;view._floor=content.ArchiveFloor;view._wall=content.ArchiveWall;
        var racks=new List<CombineInstance>();
        foreach(var filter in arena.GetComponentsInChildren<MeshFilter>())
        {
            string label=filter.name;
            if(label=="Walkable arena"||label=="Arena wall"||label.StartsWith("Cobertura deslocada"))
            {
                var renderer=filter.GetComponent<MeshRenderer>();view._surfaces.Add(renderer);view._oldMaterials.Add(renderer.sharedMaterial);
                if(label.StartsWith("Cobertura deslocada")){view._covers.Add(filter);view._oldMeshes.Add(filter.sharedMesh);}
                if(label=="Arena wall")
                {
                    var size=filter.transform.localScale;bool horizontal=size.x>size.z;float length=horizontal?size.x:size.z;
                    var recessed=Instantiate(filter.sharedMesh);recessed.name="Recessed archive wall core";
                    var vertices=recessed.vertices;
                    for(int i=0;i<vertices.Length;i++){if(horizontal)vertices[i].z*=.55f;else vertices[i].x*=.55f;}
                    recessed.vertices=vertices;recessed.RecalculateBounds();
                    view._walls.Add(filter);view._oldWalls.Add(filter.sharedMesh);view._recessedWalls.Add(recessed);
                    int count=Mathf.FloorToInt(length/2.7f);
                    for(int i=0;i<count;i++)
                    {
                        float along=(i-(count-1)*.5f)*2.7f;
                        var center=filter.transform.localPosition+(horizontal?Vector3.right:Vector3.forward)*along;
                        var rotation=horizontal?Quaternion.identity:Quaternion.Euler(0,90,0);
                        // The rack fits INSIDE the existing wall's solid volume, including its height.
                        racks.Add(new CombineInstance{mesh=content.ArchiveRack,transform=Matrix4x4.TRS(center,rotation,new Vector3(2.45f,3.8f,.48f))});
                    }
                }
            }
        }
        if(racks.Count>0)
        {
            var root=new GameObject("Archive service racks — visual only");root.transform.SetParent(arena,false);
            view._combinedRacks=new Mesh{name="Archive perimeter combined"};view._combinedRacks.indexFormat=IndexFormat.UInt32;view._combinedRacks.CombineMeshes(racks.ToArray(),true,true);
            root.AddComponent<MeshFilter>().sharedMesh=view._combinedRacks;view._racks=root.AddComponent<MeshRenderer>();view._racks.sharedMaterial=view._archive;
        }
        view.SetVisible(true);
    }
    public void SetVisible(bool visible)
    {
        Visible=visible;
        for(int i=0;i<_covers.Count;i++)if(_covers[i])_covers[i].sharedMesh=visible?_cover:_oldMeshes[i];
        for(int i=0;i<_walls.Count;i++)if(_walls[i])_walls[i].sharedMesh=visible?_recessedWalls[i]:_oldWalls[i];
        for(int i=0;i<_surfaces.Count;i++)if(_surfaces[i])_surfaces[i].sharedMaterial=visible?(_surfaces[i].name=="Walkable arena"?_floor:_surfaces[i].name=="Arena wall"?_wall:_archive):_oldMaterials[i];
        if(_racks)_racks.enabled=visible;
    }
    private void OnDestroy(){SetVisible(false);if(_combinedRacks)Destroy(_combinedRacks);foreach(var mesh in _recessedWalls)if(mesh)Destroy(mesh);}
}
