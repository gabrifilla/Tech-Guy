using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class RunExp04ReviewMenus
{
    [MenuItem("Tools/Tech Guy/RUN-EXP-04/Revisão/Abrir laboratório de recompensas")]
    public static void OpenRewards()=>RunExp03RewardsAuthoring.Open();
    [MenuItem("Tools/Tech Guy/RUN-EXP-04/Revisão/Próxima incursão seed63")]
    public static void Seed63()=>RunExp03Authoring.NextSeed63();
    [MenuItem("Tools/Tech Guy/RUN-EXP-04/Revisão/Síntese C temporária na run atual")]
    public static void PreviewC()
    {
        if(!EditorApplication.isPlaying){Debug.LogWarning("Use Play para a prévia temporária.");return;}
        var run=Object.FindFirstObjectByType<RunBoons>();if(!run)return;var view=run.GetComponent<RewardSelectionView>();
        if(!view){view=run.gameObject.AddComponent<RewardSelectionView>();view.Bind(run,RewardSelectionView.Layout.Synthesis,true);}else view.Concept=RewardSelectionView.Layout.Synthesis;
    }
    [MenuItem("Tools/Tech Guy/RUN-EXP-04/Revisão/Restaurar recompensa publicada")]
    public static void RestoreRewards()=>RunExp03RewardsAuthoring.Restore();
    [MenuItem("Tools/Tech Guy/RUN-EXP-04/Revisão/Salas antes do polimento (Play)")]
    public static void RoomsBefore()=>Rooms(false);
    [MenuItem("Tools/Tech Guy/RUN-EXP-04/Revisão/Salas com polimento (Play)")]
    public static void RoomsAfter()=>Rooms(true);
    private static void Rooms(bool revised)
    {if(!EditorApplication.isPlaying)return;foreach(var room in Object.FindObjectsByType<FirstSectorRoomPresentation>(FindObjectsSortMode.None))room.SetVisible(revised);}
}
