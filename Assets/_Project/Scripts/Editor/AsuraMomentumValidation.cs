using System;

/// <summary>Pure C# checks, runnable without an Editor or via -executeMethod.</summary>
public static class AsuraMomentumValidation
{
    public static void Main() => Run();

    public static void Run()
    {
        var first = new AsuraMomentum();
        var second = new AsuraMomentum();
        Require(first.Energy == 0 && !first.IsReady, "Starts empty");
        Require(!first.TryConsume() && first.Energy == 0, "Cannot spend empty energy");
        first.RegisterSkill(false);
        Require(first.Energy == 10, "First impulse grants 10");
        Require(!first.TryConsume() && first.Energy == 10, "Failed burst preserves partial energy");
        first.RegisterSkill(false);
        Require(first.Energy == 20, "Repeated category grants 10");
        first.RegisterSkill(true);
        Require(first.Energy == 45, "Switch to shock grants 25");
        first.RegisterSkill(false);
        Require(first.Energy == 70, "Switch back grants 25");
        first.RegisterSkill(true);
        Require(!first.IsReady && first.Energy == 95, "No premature burst");
        first.RegisterSkill(false);
        Require(first.IsReady && first.Energy == 100, "Energy caps at 100");
        first.RegisterSkill(true);
        Require(first.Energy == 100, "Overflow stays capped");
        Require(second.Energy == 0, "Players do not share energy");
        Require(first.TryConsume() && first.Energy == 0, "Burst consumes the full meter");
        Require(!first.TryConsume(), "Cannot burst twice");
        first.RegisterSkill(false);
        Require(first.Energy == 10, "Burst clears alternation history");
        first.Reset();
        first.RegisterSkill(true);
        Require(first.Energy == 10, "Death/equipment reset clears alternation history");
        Console.WriteLine("ASURA_MOMENTUM_SUCCESS: 15 checks passed.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
