using System.Collections.Generic;
using UnityEngine;

/// <summary>Optional FirstSector authoring. No run state, stat modifiers or reward decisions.</summary>
[CreateAssetMenu(menuName="Tech Guy/Run/FirstSector Content")]
public sealed class FirstSectorContentDefinition : ScriptableObject
{
    [SerializeField] private GameObject[] _additionalEnemies = System.Array.Empty<GameObject>();
    [SerializeField,Range(0,1)] private float _coverChance=.45f;
    [SerializeField] private Material _openFloor,_coveredFloor,_roomTrim;
    [SerializeField] private Mesh _archiveCover,_archiveRack;
    [SerializeField] private Material _archiveMaterial,_archiveFloor,_archiveWall;
    [SerializeField] private FirstSectorRoomVarietyWeights _spatialVariety = new FirstSectorRoomVarietyWeights();
    public FirstSectorRoomVarietyWeights SpatialVariety => _spatialVariety ?? (_spatialVariety = new FirstSectorRoomVarietyWeights());
    public Mesh ArchiveCover=>_archiveCover;
    public Mesh ArchiveRack=>_archiveRack;
    public Material ArchiveMaterial=>_archiveMaterial;
    public Material ArchiveFloor=>_archiveFloor;
    public Material ArchiveWall=>_archiveWall;
    public Material OpenFloor=>_openFloor;
    public Material CoveredFloor=>_coveredFloor;
    public Material RoomTrim=>_roomTrim;
    public IReadOnlyList<GameObject> AdditionalEnemies => _additionalEnemies;
    public bool HasCover(ulong seed, ManualNodeDefinition node)
    {
        if(node.Kind!=ManualNodeKind.Combat)return false;
        var rng=new SeededRng(SeedDerivation.SplitMix64(seed ^ 0x434F564552UL ^ unchecked((ulong)node.Id)));
        return rng.Roll(_coverChance);
    }
    public static readonly Vector3 CoverSize=new Vector3(4.2f,2.2f,4.2f);
    public static Vector3 CoverCenter(int index) => new Vector3(index==0?-3:3,1.1f,index==0?-1.5f:1.5f);
    public static bool IsClear(Vector3 offset,float margin=.9f)
    {
        for(int i=0;i<2;i++)
        {var c=CoverCenter(i);if(Mathf.Abs(offset.x-c.x)<CoverSize.x*.5f+margin && Mathf.Abs(offset.z-c.z)<CoverSize.z*.5f+margin)return false;}
        return true;
    }
    public static List<ManualEnemySpawn> SafeSpawns(List<ManualEnemySpawn> spawns,Vector2 size)
    {
        var safe=new List<Vector3>();float x=size.x*.5f-2.2f,z=size.y*.5f-2.2f;
        for(int i=0;i<4;i++){float t=(i+.5f)/4f;safe.Add(new Vector3(-x,0,Mathf.Lerp(-z,z,t)));safe.Add(new Vector3(x,0,Mathf.Lerp(-z,z,t)));}
        for(int i=0;i<4;i++){float t=(i+.5f)/4f;safe.Add(new Vector3(Mathf.Lerp(-x,x,t),0,-z));safe.Add(new Vector3(Mathf.Lerp(-x,x,t),0,z));}
        var result=new List<ManualEnemySpawn>();var used=new List<Vector3>();
        foreach(var spawn in spawns)
        {
            var offset=spawn.Offset;
            if(!IsClear(offset) || used.Exists(p=>(p-offset).sqrMagnitude<2.25f))
            {
                bool found=false;
                foreach(var candidate in safe)
                {if(used.Exists(p=>(p-candidate).sqrMagnitude<2.25f))continue;offset=candidate;found=true;break;}
                if(!found)throw new System.InvalidOperationException("Cover room has no safe spawn slot.");
            }
            used.Add(offset);result.Add(new ManualEnemySpawn(spawn.Prefab,offset,spawn.Rank));
        }
        return result;
    }
}
