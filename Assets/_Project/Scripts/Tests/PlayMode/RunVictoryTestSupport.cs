using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Assembly-wide (no namespace): every PlayMode test that runs a real ProgressionDirector writes the meta-progress
/// profile into a throwaway folder, never into Application.persistentDataPath or Docs/.
/// </summary>
[SetUpFixture]
public sealed class MetaProgressTestIsolation
{
    public static string Directory { get; private set; }

    [OneTimeSetUp]
    public void Redirect()
    {
        Directory = Path.Combine(Path.GetTempPath(), "TechGuyTests", "MetaProgress-" + System.Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(Directory);
        MetaProgressStore.DirectoryOverride = Directory;
    }

    [OneTimeTearDown]
    public void Restore()
    {
        MetaProgressStore.DirectoryOverride = null;
        try { System.IO.Directory.Delete(Directory, true); } catch (System.Exception) { }
    }
}

namespace TechGuy.Tests
{
    /// <summary>Fase 1a: the extraction portal opens only after the victory banner.</summary>
    public static class RunVictoryTestSupport
    {
        public static IEnumerator WaitForPortal(ProgressionDirector director, float timeout = 8f)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (director && !director.ManualExtractionPortal)
            {
                Assert.Less(Time.realtimeSinceStartup, end, "The victory portal did not open after the banner.");
                yield return null;
            }
        }
    }
}
