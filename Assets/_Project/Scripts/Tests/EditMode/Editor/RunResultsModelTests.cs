using System;
using NUnit.Framework;
public sealed class RunResultsModelTests
{
    [TestCase("Victory","Extração concluída")]
    [TestCase("Death","Morte na incursão")]
    [TestCase("Aborted","Incursão abandonada")]
    public void OutcomeDoesNotDependOnKillsOrDamage(string outcome,string title)
    {var s=new RunSummary {Finalized=true,SessionId=Guid.NewGuid().ToString(),Outcome=outcome};Assert.AreEqual(title,new RunResultsModel(s).Title);}
    [Test] public void CopyCannotBeChangedByOriginalDtoOrNestedBuckets()
    {
        var s=new RunSummary {Finalized=true,SessionId=Guid.NewGuid().ToString(),Outcome="Victory",EffectiveDamage=10,ObservedEnemyHpLoss=10};
        s.Damage.Add(new RunDamageBucket {Ability="Q",Weapon="Gauntlet",PlayerAttributed=true,Effective=10});
        var m=new RunResultsModel(s);s.Damage[0].Effective=123;s.EffectiveDamage=123;
        Assert.AreEqual(10,m.Total);Assert.AreEqual(10,m.Abilities[0].Total);
    }
    [Test] public void MissingAndZeroRemainDistinctAndExpectedIdMustMatch()
    {
        Assert.AreEqual("—",new RunResultsModel(null).Damage);
        var s=new RunSummary {Finalized=true,SessionId=Guid.NewGuid().ToString(),Outcome="Aborted"};
        s.Damage.Add(new RunDamageBucket {PlayerAttributed=true,Weapon="Gauntlet",Ability="Q",Effective=0});
        Assert.IsEmpty(new RunResultsModel(s).Abilities);Assert.AreEqual("0",new RunResultsModel(s).Damage);Assert.IsFalse(new RunResultsModel(s,Guid.NewGuid().ToString()).Available);
    }
    [Test] public void TinyPositiveValueDoesNotBecomeZeroAndDurationIsNotDps()
    {Assert.AreEqual("<0,1",RunResultsModel.Number(.001));Assert.AreEqual("03:38",RunResultsModel.Time(218.74));}
}
