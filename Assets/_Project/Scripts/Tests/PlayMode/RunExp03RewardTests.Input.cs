using System.Collections;
using System.Collections.Generic;
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
        [UnityTest] public IEnumerator RankUpgradeAndReboundScopeDescribeTheActualRuntimeWithoutSavingBindings()
        {
            yield return Load();var lab=Object.FindFirstObjectByType<RunExp03RewardLab>();var run=lab.Run;
            var offer=ProtocolOfferPresentation.Create(run.Protocols,ProtocolKind.Conductor);
            Assert.IsTrue((bool)typeof(RunBoons).GetMethod("ApplyOffer",Flags).Invoke(run,new object[]{offer,false}));
            var upgrade=RewardChoicePresentation.For(run,ProtocolOfferPresentation.Create(run.Protocols,ProtocolKind.Conductor));
            Assert.AreEqual("NÍVEL 1 → 2",upgrade.Level);Assert.IsTrue(upgrade.Effect.Contains("mais alvos"));
            var special=RewardChoicePresentation.For(run,new RunBoons.Offer("weapon_AsuraReserve","Reserva Asura","R: contexto do especial"));Assert.AreEqual("ESPECIAL · "+GamePreferences.BindingLabel(GameControl.Move),special.Scope);
            run.OfferReward(2);var visualChoices=(List<RunBoons.Offer>)typeof(RunBoons).GetField("_choices",Flags).GetValue(run);visualChoices.Clear();visualChoices.Add(ProtocolOfferPresentation.Create(run.Protocols,ProtocolKind.Conductor));visualChoices.Add(ProtocolOfferPresentation.Create(run.Protocols,ProtocolKind.Ignite));visualChoices.Add(ProtocolOfferPresentation.Create(run.Protocols,ProtocolKind.Detonation));
            lab.View.Concept=RewardSelectionView.Layout.Synthesis;lab.View.Refresh();lab.View.Select(0);yield return Capture("04-assisted-rank-upgrade-Synthesis");
            const string key="TechGuy.Settings.DirectBinding.v2.Skill1";bool existed=PlayerPrefs.HasKey(key);int old=PlayerPrefs.GetInt(key);
            try
            {
                var mutation=new RunBoons.Offer(BoonReworkCatalog.ViolentFallId,"Queda Violenta","Detalhes originais");
                // Explicit readonly presentation fixture, not a claim of legal Q-augment eligibility.
                lab.View.Bind(run,RewardSelectionView.Layout.Synthesis,false);visualChoices[2]=mutation;lab.View.Refresh();lab.View.Select(2);
                var pause=run.GetComponent<PauseMenuUI>();pause.Pause();yield return null;PlayerPrefs.SetInt(key,(int)KeyCode.F9);yield return null;pause.Resume();yield return null;yield return null;
                Assert.IsTrue(lab.View.SelectedScope.Contains("F9"),lab.View.SelectedScope);
                var model=RewardChoicePresentation.For(run,mutation);Assert.IsTrue(model.Scope.Contains("F9"),model.Scope);
            }
            finally{if(existed)PlayerPrefs.SetInt(key,old);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
        }
        [UnityTest] public IEnumerator KeyboardGamepadPauseReadOnlyAndChangedOfferRespectNativeChoice()
        {
            yield return Load();var lab=Object.FindFirstObjectByType<RunExp03RewardLab>();lab.ShowControls=false;yield return Size(1280,800);
            var background=InputSystem.settings.backgroundBehavior;var editor=InputSystem.settings.editorInputBehaviorInPlayMode;
            var oldKeyboard=Keyboard.current;var oldPad=Gamepad.current;bool keyboardEnabled=oldKeyboard!=null&&oldKeyboard.enabled,padEnabled=oldPad!=null&&oldPad.enabled;
            Keyboard keyboard=null;Gamepad pad=null;
            try
            {
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                if(keyboardEnabled)InputSystem.DisableDevice(oldKeyboard);if(padEnabled)InputSystem.DisableDevice(oldPad);
                keyboard=InputSystem.AddDevice<Keyboard>("RUNEXP03 UI fixture");pad=InputSystem.AddDevice<Gamepad>("RUNEXP03 UI fixture");keyboard.MakeCurrent();pad.MakeCurrent();
                var run=lab.Run;var view=lab.View;view.Concept=RewardSelectionView.Layout.Synthesis;view.Select(0);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.RightArrow));yield return null;yield return null;Assert.AreEqual(1,view.Selected);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Tab));yield return null;yield return null;Assert.IsTrue(view.DetailsVisible);yield return Capture("keyboard-details");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.West));yield return null;yield return null;Assert.IsFalse(view.DetailsVisible);
                InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.DpadRight));yield return null;yield return null;Assert.AreEqual(2,view.Selected);
                InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;
                var pause=run.GetComponent<PauseMenuUI>();pause.Pause();Assert.IsFalse(view.Visible);Assert.IsFalse(view.Confirm());Assert.AreEqual(0,run.Acquired.Count);yield return Capture("pause-priority");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));yield return null;yield return null;Assert.AreEqual(0,run.Acquired.Count);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());pause.Resume();yield return null;yield return null;
                view.Bind(run,view.Concept,false);Assert.IsFalse(view.Confirm());Assert.AreEqual(0,run.Acquired.Count);
                view.Bind(run,view.Concept,true);
                var choices=(List<RunBoons.Offer>)typeof(RunBoons).GetField("_choices",Flags).GetValue(run);
                var saved=choices.ToArray();choices.Clear();Assert.IsFalse(view.Confirm());view.Refresh();view.Select(1);Assert.AreEqual(0,view.Selected);yield return Capture("no-eligible-disabled");
                choices.AddRange(saved);view.Refresh();view.Select(0);string chosen=run.Choices[0].Id;
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;
                Assert.AreEqual(1,run.Acquired.Count);Assert.AreEqual(chosen,run.Acquired[0].Id);Assert.IsFalse(view.Confirm());
            }
            finally
            {
                if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(pad!=null)InputSystem.RemoveDevice(pad);
                if(keyboardEnabled)InputSystem.EnableDevice(oldKeyboard);if(padEnabled)InputSystem.EnableDevice(oldPad);oldKeyboard?.MakeCurrent();oldPad?.MakeCurrent();
                InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editor;
            }
        }
    }
}
