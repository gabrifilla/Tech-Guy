using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Planar geometry and generation-aware receipts shared by the spear projections.</summary>
public static class SpearBoonGeometry
{
    public static float DistanceToSegment(Vector3 point, Vector3 from, Vector3 to)
    {
        if (!ReactionNumeric.IsFinite(point) || !ReactionNumeric.IsFinite(from) || !ReactionNumeric.IsFinite(to)) return float.PositiveInfinity;
        point.y = from.y = to.y = 0f;
        Vector3 delta = to - from;
        float t = delta.sqrMagnitude > .000001f ? Mathf.Clamp01(Vector3.Dot(point - from, delta) / delta.sqrMagnitude) : 0f;
        return Vector3.Distance(point, from + delta * t);
    }

    /// <summary>A deliberate crossing; parallel/collinear casts do not cash out a horizon.</summary>
    public static bool Crosses(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        if (!ReactionNumeric.IsFinite(a) || !ReactionNumeric.IsFinite(b) || !ReactionNumeric.IsFinite(c) || !ReactionNumeric.IsFinite(d)) return false;
        Vector2 ab = new Vector2(b.x - a.x, b.z - a.z), cd = new Vector2(d.x - c.x, d.z - c.z);
        float determinant = ab.x * cd.y - ab.y * cd.x;
        if (ab.sqrMagnitude < .0001f || cd.sqrMagnitude < .0001f || Mathf.Abs(determinant) < .15f * ab.magnitude * cd.magnitude) return false;
        Vector2 offset = new Vector2(c.x - a.x, c.z - a.z);
        float t = (offset.x * cd.y - offset.y * cd.x) / determinant;
        float u = (offset.x * ab.y - offset.y * ab.x) / determinant;
        return t > .001f && t < .999f && u > .001f && u < .999f;
    }

    public static Vector3 OrbitPoint(Vector3 center, Vector3 direction, float turns, float radius = 2.5f)
    {
        if (!ReactionNumeric.IsFinite(center) || !ReactionNumeric.IsFinite(direction)) throw new ArgumentException("Orbit vectors must be finite.");
        if (!ReactionNumeric.IsFinite(turns)) throw new ArgumentOutOfRangeException(nameof(turns));
        if (!ReactionNumeric.IsFinite(radius) || radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        Vector3 heading = direction; heading.y = 0;
        if (heading.sqrMagnitude < .0001f) heading = Vector3.forward;
        return center + Quaternion.AngleAxis(turns * 360f, Vector3.up) * heading.normalized * radius;
    }
}

/// <summary>A victim can be confirmed once per physical passage, including after collider duplication.</summary>
public sealed class SpearBoonReceipts
{
    private readonly HashSet<(ulong, long, int)> _hits = new HashSet<(ulong, long, int)>();
    private readonly HashSet<(ulong, long)> _outgoing = new HashSet<(ulong, long)>();
    private readonly HashSet<(ulong, long)> _returns = new HashSet<(ulong, long)>();
    public int Count => _hits.Count;
    public bool TryClaim(ulong id, long generation, int passage) => _hits.Add((id, generation, passage));
    public void ConfirmOutgoing(ulong id, long generation) => _outgoing.Add((id, generation));
    public bool TryConfirmReturn(ulong id, long generation) => _outgoing.Contains((id, generation)) && _returns.Add((id, generation));
}
