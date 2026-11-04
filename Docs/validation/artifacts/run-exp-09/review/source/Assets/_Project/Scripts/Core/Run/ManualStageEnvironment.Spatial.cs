using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;

public sealed partial class ManualStageEnvironment
{
    private static void BuildSpatialArena(Transform arena,ManualNodeDefinition node,List<Vector3> ports,Material floor,Material wall,FirstSectorContentDefinition content)
    {
        var layout=node.SpatialLayout;
        foreach(var r in layout.Floors)
            GeneratedBox(arena,"Walkable arena",node.Center+new Vector3(r.center.x,-.25f,r.center.y),new Vector3(r.width,.5f,r.height),floor).layer=LayerMask.NameToLayer("Ground");
        foreach(var b in layout.Boundaries)
        {
            if(layout.CentralStructure.HasValue)
            {
                var core=layout.CentralStructure.Value;
                var mid=(b.A+b.B)*.5f;
                if(mid.x>=core.xMin-.01f&&mid.x<=core.xMax+.01f&&mid.y>=core.yMin-.01f&&mid.y<=core.yMax+.01f)continue;
            }
            bool horizontal=Mathf.Abs(b.A.y-b.B.y)<.01f;
            float edge=horizontal?b.A.y:b.A.x;
            float start=horizontal?Mathf.Min(b.A.x,b.B.x):Mathf.Min(b.A.y,b.B.y);
            float end=horizontal?Mathf.Max(b.A.x,b.B.x):Mathf.Max(b.A.y,b.B.y);
            var cuts=ports.Select(p=>p-node.Center).Where(p=>Mathf.Abs((horizontal?p.z:p.x)-edge)<.01f)
                .Select(p=>horizontal?p.x:p.z).Where(p=>p>=start&&p<=end).OrderBy(p=>p).ToArray();
            float cursor=start;
            foreach(float cut in cuts)
            {
                GeneratedWall(arena,node.Center,horizontal,edge,cursor,Mathf.Max(cursor,cut-2),wall);
                cursor=Mathf.Max(cursor,cut+2);
            }
            GeneratedWall(arena,node.Center,horizontal,edge,cursor,end,wall);
        }
        if(layout.CentralStructure.HasValue)
        {
            var r=layout.CentralStructure.Value;
            var mass=GeneratedBox(arena,"Central archive mass / ring",node.Center+new Vector3(r.center.x,1.75f,r.center.y),new Vector3(r.width,3.5f,r.height),content&&content.ArchiveWall?content.ArchiveWall:wall);
            var modifier=mass.AddComponent<NavMeshModifier>();modifier.overrideArea=true;modifier.area=1;
        }
        for(int i=0;i<layout.Covers.Count;i++)
        {
            var r=layout.Covers[i];
            var cover=GeneratedBox(arena,"Cobertura deslocada "+i,node.Center+new Vector3(r.center.x,1.1f,r.center.y),new Vector3(r.width,2.2f,r.height),wall);
            var modifier=cover.AddComponent<NavMeshModifier>();modifier.overrideArea=true;modifier.area=1;
        }
        // Physical opening and its two shoulders use the same contour. No decorative colliders in the route.
        foreach(var port in ports)
        {
            var offset=port-node.Center;bool side=Mathf.Abs(offset.x)>.5f*node.ArenaSize.x-.1f;
            foreach(int sign in new[]{-1,1})
            {
                var go=new GameObject("Socket shoulder / visual only");go.transform.SetParent(arena,false);
                go.transform.position=port+(side?Vector3.forward:Vector3.right)*sign*2.4f+Vector3.up*1.7f;
                go.transform.localScale=side?new Vector3(.48f,3.4f,.3f):new Vector3(.3f,3.4f,.48f);
                go.AddComponent<MeshFilter>().sharedMesh=Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=floor;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }
}
