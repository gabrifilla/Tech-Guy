using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TechGuy.Tests
{
    [Category("RUNEXP03Rewards")]
    public sealed partial class RunExp03RewardTests : RunSummaryIsolatedPlayModeFixture
    {
        private EditorWindow _gameView;private int _oldSize;private readonly List<int> _sizes=new List<int>();private string _folder;
        private const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        private static object Group()
        {var type=typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");return type.GetMethod("GetGroup").Invoke(type.BaseType.GetProperty("instance").GetValue(null),new object[]{0});}
        [SetUp] public void Begin()
        {
            var type=typeof(Editor).Assembly.GetType("UnityEditor.GameView");_gameView=Resources.FindObjectsOfTypeAll(type).Cast<EditorWindow>().First();
            _oldSize=(int)type.GetProperty("selectedSizeIndex",Flags).GetValue(_gameView);_sizes.Clear();
            _folder=Path.GetFullPath("Docs/validation/artifacts/run-exp-03/rewards/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));Directory.CreateDirectory(_folder);
        }
        [TearDown] public void End()
        {
            if(_gameView)_gameView.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(_gameView,_oldSize);
            var group=Group();foreach(int index in _sizes.OrderByDescending(i=>i))group.GetType().GetMethod("RemoveCustomSize").Invoke(group,new object[]{index});Time.timeScale=1;
        }
        private IEnumerator Size(int width,int height)
        {
            var group=Group();var asm=typeof(Editor).Assembly;
            object size=Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"),new[]{Enum.ToObject(asm.GetType("UnityEditor.GameViewSizeType"),1),(object)width,height,"RUNEXP03 review"});
            group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});int index=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1;_sizes.Add(index);
            _gameView.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(_gameView,index);_gameView.Focus();yield return null;yield return null;
            Assert.AreEqual(width,Screen.width);Assert.AreEqual(height,Screen.height);
        }
        private IEnumerator Capture(string name)
        {string path=Path.Combine(_folder,name+".png");ScreenCapture.CaptureScreenshot(path);float deadline=Time.realtimeSinceStartup+6;while(!File.Exists(path)){Assert.Less(Time.realtimeSinceStartup,deadline);yield return null;}yield return null;}
        private IEnumerator Load()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Project/Scenes/Review/RunExp03Rewards.unity",new LoadSceneParameters(LoadSceneMode.Single));
            for(int i=0;i<6;i++)yield return null;
            Assert.IsNotNull(Object.FindFirstObjectByType<RunExp03RewardLab>());
        }
        [UnityTest] public IEnumerator BothConceptsCompareRealOffersAcrossThreeWeaponsAndTwoAspectsWithoutGranting()
        {
            yield return Load();var lab=Object.FindFirstObjectByType<RunExp03RewardLab>();lab.ShowControls=false;
            foreach(var resolution in new[]{new[]{1160,600},new[]{1280,800}})
            {
                yield return Size(resolution[0],resolution[1]);
                for(int weapon=0;weapon<3;weapon++)
                {
                    yield return lab.Prepare(weapon);var run=lab.Run;var view=lab.View;var original=run.GetComponent<RunRewardUI>();var ids=run.Choices.Select(o=>o.Id).ToArray();
                    Assert.Greater(ids.Length,0);Assert.AreEqual(0,run.Acquired.Count);string prefix=resolution[0]+"x"+resolution[1]+"-"+run.WeaponModifiers.Family;
                    view.enabled=false;original.enabled=true;yield return Capture(prefix+"-original");original.enabled=false;view.enabled=true;
                    foreach(RewardSelectionView.Layout concept in Enum.GetValues(typeof(RewardSelectionView.Layout)))
                    {
                        view.Concept=concept;view.Select(0);yield return Capture(prefix+"-"+concept+"-selected-0");
                        view.Select(1);yield return Capture(prefix+"-"+concept+"-selected-1");
                        view.ToggleDetails();yield return Capture(prefix+"-"+concept+"-details");view.ToggleDetails();
                        CollectionAssert.AreEqual(ids,run.Choices.Select(o=>o.Id).ToArray());Assert.AreEqual(0,run.Acquired.Count);
                    }
                }
            }
        }
        [UnityTest] public IEnumerator ActualOwnedContextNeverTreatsCoOfferAsOwnedAndConfirmUsesTheNativeChoiceOnce()
        {
            yield return Load();var lab=Object.FindFirstObjectByType<RunExp03RewardLab>();lab.ShowControls=false;yield return Size(1160,600);
            var run=lab.Run;var conductor=ProtocolOfferPresentation.Create(run.Protocols,ProtocolKind.Conductor);
            var before=RewardChoicePresentation.For(run,conductor);Assert.IsEmpty(before.Context);
            // Explicit isolated visual fixture: apply a real existing mutation, not a natural acquisition claim.
            Assert.IsTrue((bool)typeof(RunBoons).GetMethod("ApplyOffer",Flags).Invoke(run,new object[]{new RunBoons.Offer(TitanicFistsDefinition.Id,"Punhos Titânicos","Contexto visual assistido"),false}));
            var active=RewardChoicePresentation.For(run,conductor);Assert.IsTrue(active.Context.Contains("tornados"));
            run.OfferReward(2);Assert.IsTrue(run.IsChoosing);
            var choices=(List<RunBoons.Offer>)typeof(RunBoons).GetField("_choices",Flags).GetValue(run);choices.Clear();choices.Add(conductor);
            choices.Add(ProtocolOfferPresentation.Create(run.Protocols,ProtocolKind.Frost));choices.Add(ProtocolOfferPresentation.Create(run.Protocols,ProtocolKind.Detonation));
            // Card list is an explicit visual fixture, not evidence of RNG/ticket offers; no choice here.
            lab.View.Refresh();lab.View.Select(0);
            foreach(RewardSelectionView.Layout concept in Enum.GetValues(typeof(RewardSelectionView.Layout)))
            {lab.View.Concept=concept;yield return Capture("assisted-owned-titan-"+concept);}
            yield return lab.Prepare(1);run=lab.Run;var view=lab.View;Assert.AreEqual(0,run.Acquired.Count);
            var ids=run.Choices.Select(o=>o.Id).ToArray();Assert.IsTrue(view.Visible);view.Select(1);
            Assert.IsTrue(view.Confirm());Assert.IsFalse(view.Confirm());Assert.AreEqual(1,run.Acquired.Count);Assert.AreEqual(ids[1],run.Acquired[0].Id);
            Assert.IsFalse(view.Confirm());yield return null;
        }
    }
}
