using System.Collections.Generic;
using UnityEngine;

/// <summary>Geometric data only. Never passed to the procedural materializer or reward loop.</summary>
public static class ManualRoomProjection
{
    public static RoomGraph Create(ManualRouteSnapshot route, ManualStageEnvironment environment)
    {
        var depths = new Dictionary<int,int> { [route.StartNode] = 0 };
        var queue = new Queue<int>(); queue.Enqueue(route.StartNode);
        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            foreach (var edge in route.Connections)
                if (edge.From == current && !depths.ContainsKey(edge.To))
                { depths.Add(edge.To, depths[current] + 1); queue.Enqueue(edge.To); }
        }
        var rooms = new Dictionary<int,Room>(); var edges = new List<RoomConnection>();
        foreach (var node in route.Nodes)
        {
            var type = node.Kind == ManualNodeKind.Start ? RoomType.Start : node.Kind == ManualNodeKind.Boss ? RoomType.Boss :
                node.Kind == ManualNodeKind.Repair ? RoomType.Treasure : RoomType.Combat;
            rooms.Add(node.Id, new Room(node.Id, type, new Vector2(node.Center.x,node.Center.z),node.ArenaSize,depths[node.Id]));
        }
        foreach (var connection in route.Connections)
        {
            Vector3 delta = Vector3.zero;
            foreach (var binding in environment.Connections)
                if (binding.ConnectionId == connection.Id) delta = binding.FromAnchor.position - route.Node(connection.From).Center;
            var direction = Mathf.Abs(delta.x) > Mathf.Abs(delta.z) ? (delta.x > 0 ? Direction.East : Direction.West) :
                delta.z > 0 ? Direction.North : Direction.South;
            var edge = new RoomConnection(connection.From,connection.To,direction);
            edges.Add(edge); rooms[connection.From].Connections.Add(edge); rooms[connection.To].Connections.Add(edge);
        }
        return new RoomGraph(rooms.Values,edges,route.StartNode,route.BossNode);
    }
}
