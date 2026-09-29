using UnityEngine;

public enum EnemyAttackShape { Circle, Cone, Lane, Ring }

/// <summary>A world-space snapshot shared by the warning outline and the damage test.</summary>
public readonly struct EnemyAttackArea
{
    public readonly EnemyAttackShape Shape;
    public readonly Vector3 Center, Forward;
    public readonly float Reach, Width, InnerRadius, Angle;

    public EnemyAttackArea(EnemyAttackShape shape, Vector3 center, Vector3 forward,
        float reach, float width = 1f, float innerRadius = 0f, float angle = 100f)
    {
        Shape = shape; Center = center;
        forward.y = 0;
        Forward = forward.sqrMagnitude > .001f ? forward.normalized : Vector3.forward;
        Reach = Mathf.Max(.1f, reach); Width = Mathf.Max(.1f, width);
        InnerRadius = Mathf.Clamp(innerRadius, 0, Reach); Angle = Mathf.Clamp(angle, 1, 360);
    }

    public bool Contains(Vector3 point)
    {
        Vector3 delta = point - Center;
        if (Mathf.Abs(delta.y) > 2.5f) return false;
        delta.y = 0;
        if (Shape == EnemyAttackShape.Lane)
        {
            float along = Vector3.Dot(delta, Forward);
            return along >= 0 && along <= Reach &&
                Mathf.Abs(Vector3.Dot(delta, Vector3.Cross(Vector3.up, Forward))) <= Width * .5f;
        }
        if (delta.sqrMagnitude > Reach * Reach) return false;
        if (Shape == EnemyAttackShape.Ring) return delta.sqrMagnitude >= InnerRadius * InnerRadius;
        return Shape != EnemyAttackShape.Cone || delta.sqrMagnitude < .0001f ||
            Vector3.Dot(delta.normalized, Forward) >= Mathf.Cos(Angle * .5f * Mathf.Deg2Rad);
    }

    public Vector3[] Outline(bool inner = false)
    {
        Vector3 right = Vector3.Cross(Vector3.up, Forward);
        if (Shape == EnemyAttackShape.Lane)
            return new[] { Center-right*Width*.5f, Center+right*Width*.5f,
                Center+Forward*Reach+right*Width*.5f, Center+Forward*Reach-right*Width*.5f,
                Center-right*Width*.5f };
        bool cone = Shape == EnemyAttackShape.Cone;
        const int segments = 64;
        var points = new Vector3[cone ? segments + 3 : segments + 1];
        if (cone) points[0] = Center;
        for (int i = 0; i <= segments; i++)
        {
            float degrees = cone ? -Angle*.5f + Angle*i/segments : 360f*i/segments;
            points[i + (cone ? 1 : 0)] = Center + Quaternion.AngleAxis(degrees, Vector3.up) *
                Forward * (inner ? InnerRadius : Reach);
        }
        if (cone) points[points.Length-1] = Center;
        return points;
    }
}
