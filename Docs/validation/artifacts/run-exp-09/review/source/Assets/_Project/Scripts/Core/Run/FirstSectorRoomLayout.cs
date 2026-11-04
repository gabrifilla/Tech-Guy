using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum FirstSectorRoomFamily { Rectangle, AsymmetricCourtyard, LChamber, ElongatedGallery, FragmentedRing }

/// <summary>Immutable, metre-grid footprint consumed by the existing room materializer. No scene or RNG ownership.</summary>
public sealed class FirstSectorRoomLayout
{
    public readonly struct Boundary
    {
        public readonly Vector2 A, B, Outward;
        public Boundary(Vector2 a, Vector2 b, Vector2 outward) { A = a; B = b; Outward = outward; }
    }
    private readonly bool[,] _cells;
    private readonly List<Rect> _floors;
    private readonly List<Boundary> _boundaries;
    private readonly List<Rect> _covers;
    public FirstSectorRoomFamily Family { get; }
    public Vector2 Size { get; }
    public IReadOnlyList<Rect> Floors => _floors.AsReadOnly();
    public IReadOnlyList<Boundary> Boundaries => _boundaries.AsReadOnly();
    public IReadOnlyList<Rect> Covers => _covers.AsReadOnly();
    public int Area { get; }
    public int Width => _cells.GetLength(0);
    public int Depth => _cells.GetLength(1);
    public float MinX => -Width * .5f;
    public float MinZ => -Depth * .5f;
    public Rect? CentralStructure { get; }

    public static FirstSectorRoomFamily SelectFamily(ulong seed, int nodeId)
        => (FirstSectorRoomFamily)(SeedDerivation.SplitMix64(seed ^ unchecked((ulong)nodeId) ^ 0x5350415449414CUL) % 5);

    public static Vector2 Dimensions(FirstSectorRoomFamily family, Vector2 original)
    {
        if (family == FirstSectorRoomFamily.ElongatedGallery) return new Vector2(28, 30);
        if (family == FirstSectorRoomFamily.LChamber || family == FirstSectorRoomFamily.FragmentedRing) return new Vector2(28, 26);
        return new Vector2(Mathf.Ceil(original.x / 2) * 2, Mathf.Ceil(original.y / 2) * 2);
    }

    public FirstSectorRoomLayout(FirstSectorRoomFamily family, Vector2 size, bool covered)
    {
        if (family == FirstSectorRoomFamily.Rectangle) throw new ArgumentException("Rectangle uses the preserved native path.");
        Family = family; Size = Dimensions(family, size);
        int width = (int)Size.x, depth = (int)Size.y;
        _cells = new bool[width, depth];
        if (family == FirstSectorRoomFamily.FragmentedRing) CentralStructure = new Rect(2, -5, 7, 10);
        int area = 0;
        for (int x = 0; x < width; x++) for (int z = 0; z < depth; z++)
        {
            float px = x + MinX + .5f, pz = z + MinZ + .5f;
            bool filled = true;
            if (family == FirstSectorRoomFamily.AsymmetricCourtyard)
                filled = !(px < MinX + 4 && pz > -MinZ - 5) && !(px > -MinX - 5 && pz < MinZ + 3);
            else if (family == FirstSectorRoomFamily.LChamber) filled = px < 0 || pz < 4;
            else if (family == FirstSectorRoomFamily.ElongatedGallery)
                filled = Mathf.Abs(px) < 6 || px < -6 && pz > -8 && pz < 1 || px > 6 && pz > 2 && pz < 11;
            else if (family == FirstSectorRoomFamily.FragmentedRing) filled = !CentralStructure.Value.Contains(new Vector2(px, pz));
            _cells[x, z] = filled; if (filled) area++;
        }
        Area = area; _floors = MergeFloors(); _boundaries = BuildBoundaries(); _covers = new List<Rect>();
        if (covered)
        {
            if (family == FirstSectorRoomFamily.LChamber) { _covers.Add(new Rect(-10,-3,3,3)); _covers.Add(new Rect(5,-8,3,3)); }
            else if (family == FirstSectorRoomFamily.ElongatedGallery) { _covers.Add(new Rect(-3,-5,3,3)); _covers.Add(new Rect(0,5,3,3)); }
            else if (family == FirstSectorRoomFamily.FragmentedRing) { _covers.Add(new Rect(-9,-7,3,3)); _covers.Add(new Rect(-8,5,3,3)); }
            else { _covers.Add(new Rect(-5.1f,-3.6f,4.2f,4.2f)); _covers.Add(new Rect(.9f,-.6f,4.2f,4.2f)); }
        }
    }

    public bool Cell(int x, int z) => x >= 0 && z >= 0 && x < Width && z < Depth && _cells[x,z];
    private bool FloorPoint(float x, float z) => Cell(Mathf.FloorToInt(x-MinX), Mathf.FloorToInt(z-MinZ));
    public bool Contains(Vector3 point, float inset = 0)
    {
        return FloorPoint(point.x-inset,point.z-inset) && FloorPoint(point.x-inset,point.z+inset)
            && FloorPoint(point.x+inset,point.z-inset) && FloorPoint(point.x+inset,point.z+inset);
    }
    public bool Clear(Vector3 point, float margin)
    {
        if (!Contains(point, margin)) return false;
        foreach (var r in _covers)
            if (point.x > r.xMin-margin && point.x < r.xMax+margin && point.z > r.yMin-margin && point.z < r.yMax+margin) return false;
        return true;
    }

    public Vector3 Port(Vector3 requested)
    {
        bool horizontal = Mathf.Abs(requested.x) > Mathf.Abs(requested.z);
        Vector2 normal = horizontal ? new Vector2(Mathf.Sign(requested.x),0) : new Vector2(0,Mathf.Sign(requested.z));
        float best = float.PositiveInfinity; Vector2 chosen = default; bool found = false;
        foreach (var b in _boundaries)
        {
            if (b.Outward != normal || Vector2.Distance(b.A,b.B) < 5f) continue;
            // Only outside sockets, never a doorway into the ring's central solid.
            float edge = horizontal ? b.A.x : b.A.y;
            if (Mathf.Abs(edge - (horizontal ? normal.x*Size.x*.5f : normal.y*Size.y*.5f)) > .01f) continue;
            var mid = (b.A+b.B)*.5f;
            if (horizontal) mid.y = Mathf.Clamp(requested.z,Mathf.Min(b.A.y,b.B.y)+2.5f,Mathf.Max(b.A.y,b.B.y)-2.5f);
            else mid.x = Mathf.Clamp(requested.x,Mathf.Min(b.A.x,b.B.x)+2.5f,Mathf.Max(b.A.x,b.B.x)-2.5f);
            float score = (mid-new Vector2(requested.x,requested.z)).sqrMagnitude;
            if (score < best) { best = score; chosen = mid; found = true; }
        }
        if (!found) throw new InvalidOperationException("Layout has no safe external socket: "+Family+" "+normal);
        return new Vector3(chosen.x,0,chosen.y);
    }

    public List<ManualEnemySpawn> Arrange(IReadOnlyList<ManualEnemySpawn> spawns)
    {
        var slots = new List<Vector3>();
        var ports = new[] { Port(Vector3.left*Size.x), Port(Vector3.right*Size.x), Port(Vector3.forward*Size.y), Port(Vector3.back*Size.y) };
        for (float x = MinX+2; x < -MinX-1; x += 2)
            for (float z = MinZ+2; z < -MinZ-1; z += 2)
            {
                var p = new Vector3(x,0,z);
                if (Clear(p,1.25f) && p.sqrMagnitude > 16 && ports.All(port => (port-p).sqrMagnitude > 25)) slots.Add(p);
            }
        var result = new List<ManualEnemySpawn>(); var used = new List<Vector3>();
        foreach (var spawn in spawns)
        {
            bool ranged = FirstSectorEncounterLayout.IsBackline(spawn.Prefab);
            var candidates = slots.Where(p => used.All(q => (q-p).sqrMagnitude >= 5.76f));
            candidates = ranged ? candidates.OrderByDescending(p => RoleScore(p)).ThenBy(p=>p.x) : candidates.OrderBy(p=>RoleScore(p)).ThenByDescending(p=>p.x);
            bool found = false;
            foreach (var p in candidates) { result.Add(new ManualEnemySpawn(spawn.Prefab,p,spawn.Rank)); used.Add(p); found=true; break; }
            if (!found) throw new InvalidOperationException("No valid spatial spawn slot: "+Family);
        }
        return result;
    }
    private float RoleScore(Vector3 p)
    {
        if (Family == FirstSectorRoomFamily.LChamber) return p.z*2-p.x*.5f;
        if (Family == FirstSectorRoomFamily.ElongatedGallery) return p.z+Mathf.Abs(p.x)*.6f;
        if (Family == FirstSectorRoomFamily.FragmentedRing) return p.x*2+Mathf.Abs(p.z)*.3f;
        return p.z+Mathf.Abs(p.x)*.25f;
    }

    private List<Rect> MergeFloors()
    {
        var used = new bool[Width,Depth]; var result = new List<Rect>();
        for (int z=0;z<Depth;z++) for (int x=0;x<Width;x++)
        {
            if (!Cell(x,z)||used[x,z]) continue;
            int w=1;while(x+w<Width&&Cell(x+w,z)&&!used[x+w,z])w++;
            int h=1;bool extend=true;
            while(z+h<Depth&&extend){for(int k=0;k<w;k++)if(!Cell(x+k,z+h)||used[x+k,z+h]){extend=false;break;}if(extend)h++;}
            for(int i=0;i<w;i++)for(int j=0;j<h;j++)used[x+i,z+j]=true;
            result.Add(new Rect(MinX+x,MinZ+z,w,h));
        }
        return result;
    }
    private List<Boundary> BuildBoundaries()
    {
        var result=new List<Boundary>();
        // Merge unit edges along the same grid line to avoid one collider/renderer per metre.
        for(int sign=-1;sign<=1;sign+=2)
        {
            for(int z=0;z<Depth;z++)for(int x=0;x<Width;)
            {
                if(!Cell(x,z)||Cell(x,z+sign)){x++;continue;}
                int start=x;while(x<Width&&Cell(x,z)&&!Cell(x,z+sign))x++;
                float edge=MinZ+z+(sign>0?1:0);result.Add(new Boundary(new Vector2(MinX+start,edge),new Vector2(MinX+x,edge),new Vector2(0,sign)));
            }
            for(int x=0;x<Width;x++)for(int z=0;z<Depth;)
            {
                if(!Cell(x,z)||Cell(x+sign,z)){z++;continue;}
                int start=z;while(z<Depth&&Cell(x,z)&&!Cell(x+sign,z))z++;
                float edge=MinX+x+(sign>0?1:0);result.Add(new Boundary(new Vector2(edge,MinZ+start),new Vector2(edge,MinZ+z),new Vector2(sign,0)));
            }
        }
        return result;
    }
}
