using System.IO;
using UnityEditor;
using UnityEngine;

namespace TechGuy.Tests
{
    internal static class GameplayTestEvidence
    {
        internal static string Directory(string defaultPhase)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../Docs/validation/artifacts/gameplay-rework/" + SessionState.GetString("TechGuy.Tests.GprPhase", defaultPhase)));
            System.IO.Directory.CreateDirectory(path);
            return path;
        }
    }
}
