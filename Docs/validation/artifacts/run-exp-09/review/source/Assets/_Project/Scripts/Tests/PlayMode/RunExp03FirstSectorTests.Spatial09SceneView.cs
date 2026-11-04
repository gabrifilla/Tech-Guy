using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace TechGuy.Tests
{
    public sealed partial class RunExp03FirstSectorTests
    {
        public static string SpatialScene09Ready;
        public static bool SpatialScene09Advance;
        [UnityTest, Explicit("Manual MCP viewport capture; requires screenshot acknowledgements. Select this test directly.")]
        public IEnumerator Spatial09SceneViewInspectionHandshake()
        {
            var view=SceneView.GetWindow<SceneView>();var pivot=view.pivot;var rotation=view.rotation;float size=view.size;
            _dir=Path.GetFullPath("Docs/validation/artifacts/run-exp-09/scene-inspection/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));Directory.CreateDirectory(_dir);
            try
            {
                foreach(var f in new[]{FirstSectorRoomFamily.AsymmetricCourtyard,FirstSectorRoomFamily.LChamber,FirstSectorRoomFamily.ElongatedGallery,FirstSectorRoomFamily.FragmentedRing})
                {
                    ulong seed=0;while(FirstSectorRoomLayout.SelectFamily(seed,1)!=f)seed++;
                    SessionState.SetString(RunSeedSource.NextEditorSeedKey,seed.ToString());EditorSceneManager.LoadSceneInPlayMode(SpatialReview,new LoadSceneParameters(LoadSceneMode.Single));for(int i=0;i<4;i++)yield return null;
                    var d=Object.FindAnyObjectByType<ProgressionDirector>();var p=Object.FindAnyObjectByType<PlayerActor>();var s=d.ManualSession;var n=s.Route.Node(1);p.AddDamageImmunity(d);
                    Assert.IsTrue(d.TryCommitDestination(s.Id,1));Assert.IsTrue(p.GetComponent<NavMeshAgent>().Warp(n.Center));Assert.IsTrue(d.TryEnterNode(s.Id,1));yield return null;
                    foreach(var coin in d.GetComponentsInChildren<CoinDrop>())Object.DestroyImmediate(coin);
                    view.Show();view.Focus();view.LookAtDirect(n.Center,Quaternion.Euler(65,0,0),n.ArenaSize.y);view.Repaint();
                    SpatialScene09Advance=false;SpatialScene09Ready=f.ToString();float until=Time.realtimeSinceStartup+60;
                    while(!SpatialScene09Advance){Assert.Less(Time.realtimeSinceStartup,until,"External Scene View capture acknowledgement: "+f);yield return null;}
                }
                File.WriteAllText(Path.Combine(_dir,"passed.txt"),"Four actual Scene View viewport captures coordinated via MCP screenshot source scene_view; not Camera.Render preview. Immunity only for inspection. Normal run, input, rewards not evaluated here.");
            }
            finally{SpatialScene09Ready=null;SpatialScene09Advance=false;view.LookAtDirect(pivot,rotation,size);}
        }
    }
}
