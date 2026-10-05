using System;

public enum RunRepairState { Pending, Committing, Resolved, Faulted }
public sealed class RunRepairResult
{
    public Guid SessionId { get; }
    public int NodeId { get; }
    public RunRepairState State { get; internal set; }
    public bool Used { get; internal set; }
    public float Amount { get; internal set; }
    public string Diagnostic { get; internal set; }
    internal RunRepairResult(Guid sessionId,int nodeId) { SessionId=sessionId; NodeId=nodeId; }
}

public static class RunResourceMath
{
    public static bool TryRepair(float health,float maxHealth,float fraction,out float amount)
    {
        amount=0;
        if (!Finite(health)||!Finite(maxHealth)||!Finite(fraction)||maxHealth<=0||health<0||health>maxHealth||fraction<=0||fraction>1) return false;
        amount=Math.Min(maxHealth-health,maxHealth*fraction); return amount>0;
    }
    private static bool Finite(float value) => !float.IsNaN(value)&&!float.IsInfinity(value);
}

/// <summary>Repair owns a single claim before healing callbacks. Skip consumes the same opportunity.</summary>
public sealed class RunRepairPolicy
{
    private readonly ManualRoutePolicy _route;
    public RunRepairResult Result { get; private set; }
    public RunRepairPolicy(ManualRoutePolicy route) { _route=route??throw new ArgumentNullException(nameof(route)); }
    public bool TryResolve(Guid sessionId,int nodeId,bool use,float health,float maxHealth,
        Func<RunActionContext> observe,Action<float> heal)
    {
        var session=_route.Session;
        if(observe==null||!observe().CanConfirm||session.Id!=sessionId||session.IsClosed||session.Phase!=RunPhase.RepairPending||
            session.CurrentNode!=nodeId||Result!=null) return false;
        var node=session.Route.Node(nodeId);
        float amount=0;
        if(node.Kind!=ManualNodeKind.Repair||use&&(heal==null||!RunResourceMath.TryRepair(health,maxHealth,node.RepairFraction,out amount))) return false;
        Result=new RunRepairResult(sessionId,nodeId){State=RunRepairState.Committing,Used=use,Amount=amount};
        try
        {
            if(use) heal(amount);
            if(!_route.TryCompleteRepair(sessionId,nodeId,observe())) throw new InvalidOperationException("Repair applied but session/player state changed.");
            Result.State=RunRepairState.Resolved; return true;
        }
        catch(Exception exception) { Result.State=RunRepairState.Faulted; Result.Diagnostic=exception.ToString(); return false; }
    }
}
