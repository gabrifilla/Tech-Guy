using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed class RunFrontPrototypeReviewTests
    {
        [UnityTest]
        public IEnumerator RUN02IsolatedStudyHasFiveMovingFormsAndTwoLayouts()
        {
            const string scene="Assets/_Project/Prototypes/RunExperience/RunExperienceStudy.unity";
            Assert.IsFalse(EditorBuildSettings.scenes.Any(s=>s.path==scene),"Review scene must stay outside published progression.");
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(scene,new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            Assert.AreEqual(0,UnityEngine.Object.FindObjectsByType<Actor>(FindObjectsSortMode.None).Length);
            var studies=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(c=>c.GetType().Name=="RunExperienceStudyMotion").ToArray();
            Assert.AreEqual(5,studies.Length);
            string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/validation/artifacts/run-exp-01/stage2/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")));
            Directory.CreateDirectory(directory);
            foreach(var study in studies)study.enabled=false;
            foreach(float pose in new[]{.15f,.95f,1.15f,2.15f})
            {
                foreach(var study in studies)study.GetType().GetMethod("Pose").Invoke(study,new object[]{pose});
                yield return null; yield return null;
                string path=Path.Combine(directory,"pose-"+pose.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+".png");
                ScreenCapture.CaptureScreenshot(path);
                float deadline=Time.realtimeSinceStartup+4;
                while(!File.Exists(path)){Assert.Less(Time.realtimeSinceStartup,deadline);yield return null;}
            }
            var camera=Camera.main;
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
                if(root.name.StartsWith("A -")||root.name.StartsWith("B -"))root.SetActive(false);
            camera.orthographicSize=12;camera.transform.position=new Vector3(0,23,-10);camera.transform.LookAt(new Vector3(0,1.3f,10));
            yield return null;yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,"silhouettes.png"));
            yield return new WaitForSeconds(.1f);
            File.WriteAllText(Path.Combine(directory,"context.txt"),"Unity Game View; isolated scene, 5 geometric primitive studies, 2 spatial layouts, pose samples .15/.95/1.15/2.15; no actors, damage, nav bake, production rewards or human acceptance. Camera overview17, silhouette12; not production camera certification.");
        }
    }
}
