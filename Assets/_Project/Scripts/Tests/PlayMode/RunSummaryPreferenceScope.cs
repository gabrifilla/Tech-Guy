using System;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace TechGuy.Tests
{
    // Test-only: normal teardown plus Editor-session recovery if the runner is stopped/reloaded.
    public sealed class RunSummaryPreferenceScope : IDisposable
    {
        private const string Pending="TechGuy.Tests.SummaryRecovery.Pending";
        private const string Existed="TechGuy.Tests.SummaryRecovery.Existed";
        private const string Value="TechGuy.Tests.SummaryRecovery.Value";
        private static int _depth;
        private readonly string _handoff;
        private readonly bool _exists;
        private readonly string _value;
        private bool _disposed;
        [InitializeOnLoadMethod] private static void InstallRecovery()
        {
            EditorApplication.playModeStateChanged-=RecoverOnExit;
            EditorApplication.playModeStateChanged+=RecoverOnExit;
            if(!EditorApplication.isPlayingOrWillChangePlaymode)RecoverInterruptedTest();
        }
        private static void RecoverOnExit(PlayModeStateChange state)
        {if(state==PlayModeStateChange.EnteredEditMode)RecoverInterruptedTest();}
        private static void RecoverInterruptedTest()
        {
            if(!SessionState.GetBool(Pending,false))return;
            Restore(SessionState.GetBool(Existed,false),SessionState.GetString(Value,""));
            SessionState.EraseBool(Pending);SessionState.EraseBool(Existed);SessionState.EraseString(Value);_depth=0;
        }
        public RunSummaryPreferenceScope()
        {
            if(_depth==0){InstallRecovery();RecoverInterruptedTest();}
            _handoff=RunResultsHandoff.Consume();_exists=PlayerPrefs.HasKey(RunSummaryStore.PreferenceKey);_value=PlayerPrefs.GetString(RunSummaryStore.PreferenceKey);
            if(_depth++==0)
            {SessionState.SetBool(Existed,_exists);SessionState.SetString(Value,_value);SessionState.SetBool(Pending,true);}
        }
        private static void Restore(bool existed,string value)
        {
            if(existed)PlayerPrefs.SetString(RunSummaryStore.PreferenceKey,value);
            else PlayerPrefs.DeleteKey(RunSummaryStore.PreferenceKey);
            PlayerPrefs.Save();
        }
        public void Dispose()
        {
            if(_disposed)return;_disposed=true;
            Restore(_exists,_value);RunResultsHandoff.Consume();
            if(!string.IsNullOrEmpty(_handoff))RunResultsHandoff.Expect(_handoff);
            if(--_depth==0){SessionState.EraseBool(Pending);SessionState.EraseBool(Existed);SessionState.EraseString(Value);}
        }
    }
    public abstract class RunSummaryIsolatedPlayModeFixture
    {
        private RunSummaryPreferenceScope _summaryPreferences;
        [SetUp] public void CaptureSummaryPreferences() => _summaryPreferences = new RunSummaryPreferenceScope();
        [TearDown] public void RestoreSummaryPreferences()
        {
            try
            {
                // Some real-scene fixtures end in FirstSector; finalize before restoring preferences.
                foreach(var director in UnityEngine.Object.FindObjectsByType<ProgressionDirector>(FindObjectsSortMode.None))
                    if(director)UnityEngine.Object.DestroyImmediate(director);
            }
            finally {_summaryPreferences?.Dispose();}
        }
    }
}
