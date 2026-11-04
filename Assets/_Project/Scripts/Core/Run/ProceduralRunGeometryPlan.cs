using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Orthogonal paths between the new arena templates. No scene or random state.</summary>
public static class ProceduralRunGeometryPlan
{
    public const float CorridorWidth = 4f;
    public static Dictionary<int, Vector3[]> Create(ManualRouteSnapshot route)
    {
        if (route == null) throw new ArgumentNullException(nameof(route));
        var result = new Dictionary<int, Vector3[]>();
        foreach (var edge in route.Connections)
        {
            var from = route.Node(edge.From);
            var to = route.Node(edge.To);
            float dx = to.Center.x - from.Center.x;
            var points = new List<Vector3>();
            int incoming = 0, nextOutgoing = 0;
            foreach (var candidate in route.Connections)
            {
                if (candidate.To == to.Id) incoming++;
                if (candidate.From == to.Id) nextOutgoing++;
            }
            // A merge that is also a decision needs distinct entry and exit ports.
            // Sharing its lateral ports would overlap the two branch corridors and their gates.
            if (incoming > 1 && nextOutgoing > 1)
            {
                Vector3 source = from.Center + Vector3.forward * from.ArenaSize.y * .5f;
                Vector3 target = to.Center + new Vector3(Mathf.Sign(from.Center.x - to.Center.x) *
                    to.ArenaSize.x * .25f, 0f, -to.ArenaSize.y * .5f);
                float bendZ = (source.z + target.z) * .5f;
                points.Add(source);
                points.Add(new Vector3(source.x, source.y, bendZ));
                points.Add(new Vector3(target.x, target.y, bendZ));
                points.Add(target);
            }
            else if (Mathf.Abs(dx) < .01f)
            {
                points.Add(from.Center + Vector3.forward * from.ArenaSize.y * .5f);
                points.Add(to.Center - Vector3.forward * to.ArenaSize.y * .5f);
            }
            else
            {
                int outgoing = 0;
                foreach (var candidate in route.Connections) if (candidate.From == from.Id) outgoing++;
                float side = Mathf.Sign(dx);
                if (outgoing > 1)
                {
                    points.Add(from.Center + Vector3.right * side * from.ArenaSize.x * .5f);
                    points.Add(new Vector3(to.Center.x, from.Center.y, from.Center.z));
                    points.Add(to.Center - Vector3.forward * to.ArenaSize.y * .5f);
                }
                else
                {
                    points.Add(from.Center + Vector3.forward * from.ArenaSize.y * .5f);
                    points.Add(new Vector3(from.Center.x, to.Center.y, to.Center.z));
                    points.Add(to.Center - Vector3.right * side * to.ArenaSize.x * .5f);
                }
            }
            if (from.SpatialLayout != null || to.SpatialLayout != null)
            {
                Vector3 start = from.SpatialLayout == null ? points[0] : from.Center + from.SpatialLayout.Port(points[0]-from.Center);
                Vector3 end = to.SpatialLayout == null ? points[points.Count-1] : to.Center + to.SpatialLayout.Port(points[points.Count-1]-to.Center);
                Vector3 startNormal=(points[1]-points[0]).normalized;
                Vector3 endNormal=(points[points.Count-2]-points[points.Count-1]).normalized;
                Vector3 a=start+startNormal*4, b=end+endNormal*4;
                var resolved=new List<Vector3>{start,a};
                bool firstHorizontal=Mathf.Abs(startNormal.x)>.5f, lastHorizontal=Mathf.Abs(endNormal.x)>.5f;
                if(firstHorizontal==lastHorizontal)
                {
                    if(firstHorizontal){float x=(a.x+b.x)*.5f;resolved.Add(new Vector3(x,0,a.z));resolved.Add(new Vector3(x,0,b.z));}
                    else{float z=(a.z+b.z)*.5f;resolved.Add(new Vector3(a.x,0,z));resolved.Add(new Vector3(b.x,0,z));}
                }
                else resolved.Add(firstHorizontal?new Vector3(b.x,0,a.z):new Vector3(a.x,0,b.z));
                resolved.Add(b);resolved.Add(end);
                points=new List<Vector3>();
                foreach(var p in resolved)if(points.Count==0||(points[points.Count-1]-p).sqrMagnitude>.0001f)points.Add(p);
            }
            result.Add(edge.Id, points.ToArray());
        }
        return result;
    }
}
