using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public interface IManualRunSceneLoader
{
    bool CanLoad(string scene);
    IEnumerator Load(string scene, Action<bool> completed);
}

public sealed class ManualRunSceneLoader : IManualRunSceneLoader
{
    public bool CanLoad(string scene) => Application.CanStreamedLevelBeLoaded(scene);
    public IEnumerator Load(string scene, Action<bool> completed)
    {
        AsyncOperation operation = null;
        try { operation = SceneManager.LoadSceneAsync(scene); }
        catch (Exception exception) { Debug.LogError("Manual Nexus load failed: " + exception.Message); }
        if (operation == null) { completed(false); yield break; }
        // Never retry while a previous operation can still complete.
        while (!operation.isDone) yield return null;
        completed(true);
    }
}

public enum ManualReturnState { Idle, Loading, Failed, Completed }
