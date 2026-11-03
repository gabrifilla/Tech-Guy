using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TechGuy.Tests
{
    public sealed partial class RunExp03RewardTests
    {
        [UnityTest] public IEnumerator Synthesis05AssistedShowcaseMenuIsExplicitReadOnlyAndResets()
        {
            yield return Load();var lab=Object.FindFirstObjectByType<RunExp03RewardLab>();
            foreach(int weapon in new[]{0,1,2})
            {
                yield return lab.Prepare(weapon);Assert.IsTrue(UnityEditor.EditorApplication.ExecuteMenuItem("Tools/Tech Guy/RUN-EXP-05/Revisão/Mostruário assistido de arte da arma atual (Play)"));
                float end=Time.realtimeSinceStartup+5;
                while(!lab.AssistedShowcase){Assert.Less(Time.realtimeSinceStartup,end);yield return null;}
                Assert.AreEqual(3,lab.Run.Choices.Count);Assert.AreEqual(0,lab.Run.Acquired.Count);Assert.AreEqual(RewardSelectionView.Layout.Synthesis05,lab.View.Concept);
                Assert.IsFalse(lab.View.Confirm());yield return Capture("05-assisted-menu-weapon-"+weapon);
                yield return lab.Prepare(weapon);Assert.IsFalse(lab.AssistedShowcase);
            }
        }
        [UnityTest] public IEnumerator Synthesis05UsesOriginalArtAcrossThreeWeaponsAndThreeAspectsWithoutChangingOffers()
        {
            yield return Load();var lab=Object.FindFirstObjectByType<RunExp03RewardLab>();lab.ShowControls=false;
            foreach(var resolution in new[]{new[]{1160,600},new[]{1280,800},new[]{1920,1080}})
            {
                yield return Size(resolution[0],resolution[1]);
                foreach(int weapon in new[]{0,1,2})
                {
                    yield return lab.Prepare(weapon);var run=lab.Run;var view=lab.View;var native=run.GetComponent<RunRewardUI>();
                    string prefix="05-"+resolution[0]+"x"+resolution[1]+"-"+run.WeaponModifiers.Family;var ids=run.Choices.Select(o=>o.Id).ToArray();
                    view.enabled=false;native.enabled=true;yield return Capture(prefix+"-published");native.enabled=false;view.enabled=true;
                    foreach(var concept in new[]{RewardSelectionView.Layout.Synthesis,RewardSelectionView.Layout.Synthesis05})
                    {
                        view.Concept=concept;view.Select(0);yield return new WaitForSecondsRealtime(.15f);yield return Capture(prefix+"-"+concept+"-selected-0");
                        view.Select(1);yield return new WaitForSecondsRealtime(.15f);yield return Capture(prefix+"-"+concept+"-selected-1");
                        view.ToggleDetails();yield return new WaitForSecondsRealtime(.12f);yield return Capture(prefix+"-"+concept+"-details");view.ToggleDetails();
                        CollectionAssert.AreEqual(ids,run.Choices.Select(o=>o.Id).ToArray());Assert.AreEqual(0,run.Acquired.Count);
                    }
                }
            }
            foreach(string key in RewardIllustrationCatalog.Keys){var texture=Resources.Load<Texture2D>("UI/RunExp05/"+key);Assert.IsNotNull(texture,key);Assert.AreEqual(512,texture.width);Assert.IsFalse(texture.isReadable);}
            yield return Size(1280,800);yield return lab.Prepare(0);
            var owner=lab.Run;var choices=(List<RunBoons.Offer>)typeof(RunBoons).GetField("_choices",Flags).GetValue(owner);choices.Clear();
            Assert.IsTrue(BoonExpansionCatalog.TryDefinition(TitanicFistsDefinition.Id,out var titan));
            choices.Add(new RunBoons.Offer(titan.Id,titan.Title,titan.Description));choices.Add(ProtocolOfferPresentation.Create(owner.Protocols,ProtocolKind.Conductor));choices.Add(ProtocolOfferPresentation.Create(owner.Protocols,ProtocolKind.Frost));
            lab.View.Concept=RewardSelectionView.Layout.Synthesis05;lab.View.Refresh();lab.View.Select(0);yield return Capture("05-assisted-titanic-illustration");
            lab.View.Select(1);Assert.IsEmpty(RewardChoicePresentation.For(owner,choices[1]).Context);yield return Capture("05-assisted-cooffer-is-not-owned");
            var titanOffer=choices[0];var conductorOffer=choices[1];
            Assert.IsTrue((bool)typeof(RunBoons).GetMethod("ApplyOffer",Flags).Invoke(owner,new object[]{titanOffer,false}));
            // ApplyOffer legitimately closes/clears the current offer. Reopen a real isolated request
            // before constructing the next explicitly assisted comparison list.
            owner.OfferReward(2);choices.Clear();choices.Add(conductorOffer);choices.Add(ProtocolOfferPresentation.Create(owner.Protocols,ProtocolKind.Frost));choices.Add(ProtocolOfferPresentation.Create(owner.Protocols,ProtocolKind.Ignite));
            lab.View.Refresh();lab.View.Select(0);Assert.IsTrue(RewardChoicePresentation.For(owner,choices[0]).Context.Contains("tornados"));yield return Capture("05-assisted-active-titan-conductor");
            Assert.IsTrue((bool)typeof(RunBoons).GetMethod("ApplyOffer",Flags).Invoke(owner,new object[]{conductorOffer,false}));
            owner.OfferReward(3);choices.Clear();choices.Add(ProtocolOfferPresentation.Create(owner.Protocols,ProtocolKind.Conductor));choices.Add(ProtocolOfferPresentation.Create(owner.Protocols,ProtocolKind.Frost));choices.Add(ProtocolOfferPresentation.Create(owner.Protocols,ProtocolKind.Ignite));lab.View.Refresh();lab.View.Select(0);
            Assert.AreEqual("NÍVEL 1 → 2",RewardChoicePresentation.For(owner,choices[0]).Level);yield return Capture("05-assisted-conductor-rank-upgrade");
            lab.View.ReduceMotion=true;yield return Capture("05-reduced-motion");
            foreach(int weapon in new[]{1,2})
            {
                yield return lab.Prepare(weapon);owner=lab.Run;
                string id=weapon==1?BowBoonCatalog.JudgmentArrowId:SpearBoonCatalog.OrbitId;
                Assert.IsTrue(BoonExpansionCatalog.TryDefinition(id,out var definition));
                choices=(List<RunBoons.Offer>)typeof(RunBoons).GetField("_choices",Flags).GetValue(owner);choices.Clear();
                choices.Add(new RunBoons.Offer(definition.Id,definition.Title,definition.Description));choices.Add(ProtocolOfferPresentation.Create(owner.Protocols,ProtocolKind.Frost));choices.Add(ProtocolOfferPresentation.Create(owner.Protocols,ProtocolKind.Ignite));
                lab.View.Bind(owner,RewardSelectionView.Layout.Synthesis05,false);lab.View.Select(0);
                yield return Capture("05-assisted-"+owner.WeaponModifiers.Family+"-mutation-illustration");
                Assert.IsFalse(lab.View.Confirm(),"Assisted art list is explicitly read-only, not a legal ticket claim.");
            }
            File.WriteAllText(Path.Combine(_folder,"05-capture-contract.txt"),"Published/C04/C05 use identical actual offers per comparison and no grants. The final named assisted fixtures explicitly replace a visual list using real definitions and ApplyOffer for ownership/rank; not evidence of RNG or normal acquisition. No new boon, ticket, weight or progression rule.");
        }

        [UnityTest] public IEnumerator Synthesis05ModalPauseStaleOffersAndGamepadConfirmRespectTheNativeAuthority()
        {
            yield return Load();var lab=Object.FindFirstObjectByType<RunExp03RewardLab>();lab.ShowControls=false;yield return Size(1280,800);
            var background=InputSystem.settings.backgroundBehavior;var editor=InputSystem.settings.editorInputBehaviorInPlayMode;
            var oldKeyboard=Keyboard.current;var oldPad=Gamepad.current;bool keyboardEnabled=oldKeyboard!=null&&oldKeyboard.enabled,padEnabled=oldPad!=null&&oldPad.enabled;Keyboard keyboard=null;Gamepad pad=null;
            try
            {
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                if(keyboardEnabled)InputSystem.DisableDevice(oldKeyboard);if(padEnabled)InputSystem.DisableDevice(oldPad);keyboard=InputSystem.AddDevice<Keyboard>();pad=InputSystem.AddDevice<Gamepad>();keyboard.MakeCurrent();pad.MakeCurrent();
                var run=lab.Run;var view=lab.View;view.Concept=RewardSelectionView.Layout.Synthesis05;view.Select(0);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.RightArrow));yield return null;yield return null;Assert.AreEqual(1,view.Selected);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Tab));yield return null;yield return null;
                Assert.IsTrue(view.DetailsVisible);Assert.IsFalse(view.Confirm());Assert.AreEqual(0,run.Acquired.Count);yield return Capture("05-modal-does-not-commit");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.West));yield return null;yield return null;Assert.IsFalse(view.DetailsVisible);
                InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;var pause=run.GetComponent<PauseMenuUI>();pause.Pause();Assert.IsFalse(view.Visible);Assert.IsFalse(view.Confirm());yield return null;pause.Resume();yield return null;yield return null;
                view.Bind(run,view.Concept,false);Assert.IsFalse(view.Confirm());view.Bind(run,view.Concept,true);
                var choices=(List<RunBoons.Offer>)typeof(RunBoons).GetField("_choices",Flags).GetValue(run);var saved=choices.ToArray();choices.Clear();Assert.IsFalse(view.Confirm());view.Refresh();yield return Capture("05-empty-offers");
                choices.AddRange(saved);view.Refresh();view.Select(0);choices[0]=ProtocolOfferPresentation.Create(run.Protocols,ProtocolKind.Frost);Assert.IsFalse(view.Confirm(),"An old displayed identity cannot authorize a different same-length list.");
                choices.Clear();choices.AddRange(saved);view.Refresh();string selected=run.Choices[0].Id;
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;
                Assert.AreEqual(1,run.Acquired.Count);Assert.AreEqual(selected,run.Acquired[0].Id);Assert.IsFalse(view.Confirm());yield return Capture("05-confirmed-exactly-once");
            }
            finally
            {if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(pad!=null)InputSystem.RemoveDevice(pad);if(keyboardEnabled)InputSystem.EnableDevice(oldKeyboard);if(padEnabled)InputSystem.EnableDevice(oldPad);oldKeyboard?.MakeCurrent();oldPad?.MakeCurrent();InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editor;}
        }
    }
}
