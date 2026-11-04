using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Authorable relative weights; every eligible family remains possible. No run state.</summary>
[Serializable]
public sealed class FirstSectorRoomVarietyWeights
{
    public const int Normal = 100;
    [SerializeField, Range(1,100)] private int _previous = 50;
    [SerializeField, Range(1,100)] private int _twice = 20;
    [SerializeField, Range(1,100)] private int _threeOrMore = 5;
    [SerializeField, Range(1,100)] private int _twoRoomsAgo = 80;
    public int Previous => Mathf.Clamp(_previous,1,100);
    public int Twice => Mathf.Clamp(_twice,1,100);
    public int ThreeOrMore => Mathf.Clamp(_threeOrMore,1,100);
    public int TwoRoomsAgo => Mathf.Clamp(_twoRoomsAgo,1,100);
    public FirstSectorRoomVarietyWeights() { }
    public FirstSectorRoomVarietyWeights(int previous,int twice,int threeOrMore,int twoRoomsAgo)
    { _previous=previous;_twice=twice;_threeOrMore=threeOrMore;_twoRoomsAgo=twoRoomsAgo; }
    internal int Weight(int history,int family)
    {
        int a=(history&7)-1,b=((history>>3)&7)-1,c=((history>>6)&7)-1;
        return a==family ? b==family ? c==family ? ThreeOrMore : Twice : Previous : b==family ? TwoRoomsAgo : Normal;
    }
}

/// <summary>
/// A small planning pass inside the existing generator. A merge averages distinct possible
/// three-room ancestor histories; mutually exclusive siblings are never treated as consecutive rooms.
/// No reward/enemy RNG consumption, persistent history or per-frame work.
/// </summary>
public static class FirstSectorRoomDistribution
{
    public const int AllFamilies = 31;
    private const ulong Stream = 0x5350415449414C09UL;
    // Only these two asymmetric templates are enabled for the camera/door validation gate09B.
    public static int EligibleOrientations(FirstSectorRoomFamily family,Vector2 canonicalSize)
    {
        if(family==FirstSectorRoomFamily.LChamber)return 15;
        if(family==FirstSectorRoomFamily.AsymmetricCourtyard&&canonicalSize.x>=22&&canonicalSize.y>=18)return 15;
        return 1;
    }
    public static int Orientation(ulong seed,int nodeId,FirstSectorRoomFamily family,Vector2 canonicalSize)
    {
        int mask=EligibleOrientations(family,canonicalSize);if(mask==1)return 0;
        ulong hash=SeedDerivation.SplitMix64(seed ^ SeedDerivation.SplitMix64(unchecked((ulong)nodeId)) ^ ((ulong)family<<32) ^ 0x4F5249454E5439UL);
        int count=0;for(int i=0;i<4;i++)if((mask&(1<<i))!=0)count++;
        int chosen=(int)(hash%(ulong)count);for(int i=0;i<4;i++)if((mask&(1<<i))!=0&&chosen--==0)return i;
        throw new InvalidOperationException("No eligible orientation");
    }
    public static Dictionary<int,FirstSectorRoomFamily> Plan(ManualRouteSnapshot source,ulong seed,
        FirstSectorRoomVarietyWeights weights,Func<ManualNodeDefinition,int> eligibility=null)
    {
        if(source==null)throw new ArgumentNullException(nameof(source));
        if(weights==null)throw new ArgumentNullException(nameof(weights));
        var paths=source.Paths;var depths=new Dictionary<int,int>();
        foreach(var path in paths)for(int i=0;i<path.Length;i++)
            if(!depths.TryGetValue(path[i],out int old)||i>old)depths[path[i]]=i;
        var result=new Dictionary<int,FirstSectorRoomFamily>();
        var histories=new HashSet<int>();var totals=new long[5];
        foreach(var node in source.Nodes.Where(n=>n.Kind==ManualNodeKind.Combat).OrderBy(n=>depths[n.Id]).ThenBy(n=>n.Id))
        {
            int mask=(eligibility==null?AllFamilies:eligibility(node))&AllFamilies;
            if(mask==0)throw new InvalidOperationException("No eligible room family at node "+node.Id);
            histories.Clear();Array.Clear(totals,0,totals.Length);
            foreach(var path in paths)
            {
                int index=Array.IndexOf(path,node.Id);if(index<0)continue;
                int history=0,count=0;
                for(int i=index-1;i>=0&&count<3;i--)
                    if(result.TryGetValue(path[i],out var previous))history|=((int)previous+1)<<(count++*3);
                histories.Add(history);
            }
            // HashSet traversal cannot affect integer sums; candidates always use enum order 0..4.
            foreach(int history in histories)for(int f=0;f<5;f++)if((mask&(1<<f))!=0)totals[f]+=weights.Weight(history,f);
            long total=0;for(int f=0;f<5;f++)total+=totals[f];
            if(total==0)throw new InvalidOperationException("Room has no reachable ancestor history: "+node.Id);
            // Preserve the approved first compatible room and existing one-shot family review commands.
            if(histories.Count==1&&histories.Contains(0)&&mask==AllFamilies)
            {result.Add(node.Id,FirstSectorRoomLayout.SelectFamily(seed,node.Id));continue;}
            ulong hash=SeedDerivation.SplitMix64(seed ^ SeedDerivation.SplitMix64(unchecked((ulong)node.Id)) ^ Stream);
            long roll=(long)(hash%(ulong)total);
            for(int f=0;f<5;f++)
            {if(roll<totals[f]){result.Add(node.Id,(FirstSectorRoomFamily)f);break;}roll-=totals[f];}
        }
        return result;
    }
}
