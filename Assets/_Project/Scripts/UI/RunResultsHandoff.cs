using UnityEngine;

/// <summary>One transient presentation intent, not a second run session or persistent result.</summary>
public static class RunResultsHandoff
{
    private static string _expected;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => _expected = null;
    public static void Expect(string sessionId) => _expected = sessionId;
    public static string Consume() { var id = _expected; _expected = null; return id; }
}
