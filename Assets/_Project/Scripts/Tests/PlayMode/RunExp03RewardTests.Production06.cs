using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TechGuy.Tests
{
    public sealed partial class RunExp03RewardTests
    {
        [Serializable] private sealed class CatalogEntry06
        {public string id,title,family,art,scope,status;public int weapon,glyph;public string[] prerequisites;public bool dedicatedArt;}
        [Serializable] private sealed class CatalogAudit06
        {public string fixture;public CatalogEntry06[] entries;public string[] disabledExpansionIds,unusedIllustrationIds;}

        [UnityTest] public IEnumerator Published06CatalogUsesLegalEligibilityWitnessesAndCoherentFamilyFallbacks()
        {
            _folder=Path.GetFullPath("Docs/validation/artifacts/run-exp-06/catalogue/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));Directory.CreateDirectory(_folder);
            yield return Load();yield return Size(1280,800);var lab=Object.FindFirstObjectByType<RunExp03RewardLab>();
            var entries=new List<CatalogEntry06>();var renderedFamilies=new HashSet<string>();var candidateIds=new HashSet<string>();
            for(int weapon=0;weapon<3;weapon++)
            {
                yield return lab.Prepare(weapon);var current=lab.Run.GetComponent<PlayerActor>().CurrentWeapon;var family=lab.Run.WeaponModifiers.Family;
                var candidates=WeaponRunModifiers.Catalog.Where(d=>d.Family==family&&RunBoonOfferCatalog.IncludesOffer(d.Id)&&RunBoonOfferCatalog.HasTarget(d.Kind,current)).Select(d=>d.Id)
                    .Concat(BoonExpansionCatalog.Definitions().Where(d=>d.EnabledForOffers&&BoonExpansionCatalog.HasTarget(d,current)).Select(d=>d.Id))
                    .Concat(new[]{"ignite","frost","conductor","detonation"}).ToList();
                if(weapon==0)candidates.AddRange(new[]{BoonReworkCatalog.UppercutId,GauntletContentAuthoring.ForMutation(GauntletMutation.CadencedAsura).Id,BoonReworkCatalog.ViolentFallId});
                foreach(string id in candidates.Distinct().OrderBy(x=>x,StringComparer.Ordinal))
                {
                    candidateIds.Add(id);yield return lab.Prepare(weapon);var run=lab.Run;var sequence=new List<string>();
                    bool prerequisites=PreparePrerequisites06(run,id,sequence,new HashSet<string>());var eligible=Eligible06(run).FirstOrDefault(o=>o.Id==id);
                    bool witnessed=prerequisites&&eligible!=null;
                    RunBoons.Offer offer=eligible;
                    if(offer==null)
                    {
                        if(BoonExpansionCatalog.TryDefinition(id,out var definition))offer=new RunBoons.Offer(id,definition.Title,definition.Description);
                        else if(id==BoonReworkCatalog.ViolentFallId)offer=EclipseBoonCatalog.FallOffer();
                        else Assert.Fail("Native declaration missing for enabled candidate "+id);
                    }
                    var model=RewardChoicePresentation.For(run,offer);string art=RewardIllustrationCatalog.KeyFor(id);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(model.Name));Assert.IsFalse(string.IsNullOrWhiteSpace(model.Effect));Assert.IsFalse(string.IsNullOrWhiteSpace(model.Family));Assert.That(model.Glyph,Is.InRange(0,5));
                    if(art!=null)Assert.IsNotNull(Resources.Load<Texture2D>("UI/RunExp05/"+art));
                    entries.Add(new CatalogEntry06{id=id,title=model.Name,family=model.Family,scope=model.Scope,weapon=weapon,glyph=model.Glyph,art=art??"family-mark",dedicatedArt=art!=null,prerequisites=sequence.ToArray(),status=witnessed?"Native eligibility witness; prerequisites applied only in isolated audit fixture":"Enabled declaration; no eligibility witness in this recipe, not claimed naturally obtainable"});
                    // Read-only catalog surface uses real definitions and explicit fixture offers,
                    // never tickets/RNG from a normal expedition or a promise of natural acquisition.
                    run.OfferReward(20+entries.Count);var choices=(List<RunBoons.Offer>)typeof(RunBoons).GetField("_choices",Flags).GetValue(run);choices.Clear();choices.Add(offer);
                    lab.AssistedShowcase=true;lab.ShowControls=true;lab.View.ShowReviewControls=false;lab.View.Bind(run,RewardSelectionView.Layout.Synthesis05,false);lab.View.Select(0);
                    yield return null;Assert.IsFalse(lab.View.Confirm());
                    string key=weapon+"-"+model.Family;
                    if(renderedFamilies.Add(key)){yield return Capture("06-assisted-family-"+weapon+"-"+model.Glyph);lab.View.ToggleDetails();yield return Capture("06-assisted-family-"+weapon+"-"+model.Glyph+"-details");lab.View.ToggleDetails();}
                    if(witnessed)Assert.IsTrue(ApplyAudit06(run,offer),"Eligible native effect application "+id);
                }
            }
            yield return lab.Prepare(0);var exhausted=RunBoonOfferCatalog.ExhaustedOffer();var emptyModel=RewardChoicePresentation.For(lab.Run,exhausted);Assert.AreEqual("SEM AQUISIÇÃO",emptyModel.Level);Assert.AreEqual(5,emptyModel.Glyph);
            var audit=new CatalogAudit06{fixture="Full retained manual-catalog declarations sourced from native weapon/expansion/mutation/protocol branches. Eligibility recipe witnesses use native current eligibility and ApplyOffer on disposable Lab inventory. Read-only presentation is explicit assistance, not normal RNG or end-to-end ticket evidence. Disabled declarations and out-of-pool art reported separately.",entries=entries.ToArray(),disabledExpansionIds=BoonExpansionCatalog.Definitions().Where(d=>!d.EnabledForOffers).Select(d=>d.Id).ToArray(),unusedIllustrationIds=new[]{"weapon_Piercing"}.Where(id=>!candidateIds.Contains(id)).ToArray()};
            File.WriteAllText(Path.Combine(_folder,"catalogue06.json"),JsonUtility.ToJson(audit,true));
        }
        private static List<RunBoons.Offer> Eligible06(RunBoons run)
        {
            var weapon=run.GetComponent<PlayerActor>().CurrentWeapon;
            return new[]{RunRewardKind.Technique,RunRewardKind.Mutation,RunRewardKind.Protocol}.SelectMany(kind=>ManualRewardCatalog.Eligible(kind,run.WeaponModifiers,weapon,run.Protocols)).ToList();
        }
        private static bool ApplyAudit06(RunBoons run,RunBoons.Offer offer)=>(bool)typeof(RunBoons).GetMethod("ApplyOffer",Flags).Invoke(run,new object[]{offer,false});
        private static bool PreparePrerequisites06(RunBoons run,string id,List<string> sequence,HashSet<string> visiting)
        {
            if(!visiting.Add(id))return false;
            IEnumerable<string> dependencies=Array.Empty<string>();
            if(id==BoonReworkCatalog.ViolentFallId)dependencies=new[]{BoonReworkCatalog.UppercutId};
            else if(BoonExpansionCatalog.TryDefinition(id,out var definition))dependencies=definition.RequiredBoonIds??Array.Empty<string>();
            foreach(string dependency in dependencies)
            {
                if(RunModifierPresentation.Count(run,dependency)>0)continue;
                if(!PreparePrerequisites06(run,dependency,sequence,visiting))return false;
                var offer=Eligible06(run).FirstOrDefault(o=>o.Id==dependency);if(offer==null||!ApplyAudit06(run,offer))return false;sequence.Add(dependency);
            }
            visiting.Remove(id);return true;
        }
    }
}
