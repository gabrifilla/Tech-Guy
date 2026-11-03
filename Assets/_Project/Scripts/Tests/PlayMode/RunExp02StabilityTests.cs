using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed partial class BasicAttackR1PlayModeTests
    {
        [UnityTest, Category("RUNEXP02Stability")]
        public IEnumerator RUNEXP02RepeatedEvidenceRootNeverReusesCaptureDestination()
        {
            const string key="TechGuy.Tests.TitanicCompositionEvidence";
            string old=SessionState.GetString(key,"");
            try
            {
                SessionState.SetString(key,"run-exp-02/stability/repeated-root-"+Guid.NewGuid().ToString("N"));
                _compositionEvidenceDirectory=null;
                string first=CompositionEvidenceDirectory;
                _compositionEvidenceDirectory=null;
                string second=CompositionEvidenceDirectory;
                Assert.AreNotEqual(first,second,"Repeated runs must keep the caller's evidence root without overwriting an earlier capture.");
                yield return null;
            }
            finally
            { if(old.Length==0)SessionState.EraseString(key);else SessionState.SetString(key,old);_compositionEvidenceDirectory=null; }
        }
    }
}
