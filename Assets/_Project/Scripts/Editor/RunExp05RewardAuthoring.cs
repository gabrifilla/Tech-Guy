using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Object=UnityEngine.Object;

public static class RunExp05RewardAuthoring
{
    [MenuItem("Tools/Tech Guy/RUN-EXP-05/Revisão/Abrir laboratório de recompensas")]
    public static void Open()=>RunExp03RewardsAuthoring.Open();
    [MenuItem("Tools/Tech Guy/RUN-EXP-05/Revisão/C05 temporário na run atual")]
    public static void Preview()
    {
        if(!EditorApplication.isPlaying)return;var run=Object.FindFirstObjectByType<RunBoons>();if(!run)return;
        var view=run.GetComponent<RewardSelectionView>();if(!view){view=run.gameObject.AddComponent<RewardSelectionView>();view.Bind(run,RewardSelectionView.Layout.Synthesis05,true);}else view.Concept=RewardSelectionView.Layout.Synthesis05;
    }
    [MenuItem("Tools/Tech Guy/RUN-EXP-05/Revisão/Restaurar recompensa publicada")]
    public static void Restore()=>RunExp03RewardsAuthoring.Restore();
    [MenuItem("Tools/Tech Guy/RUN-EXP-05/Revisão/Alternar movimento reduzido da prévia")]
    public static void ReduceMotion(){foreach(var view in Object.FindObjectsByType<RewardSelectionView>(FindObjectsSortMode.None))view.ReduceMotion=!view.ReduceMotion;}
    [MenuItem("Tools/Tech Guy/RUN-EXP-05/Revisão/Mostruário assistido de arte da arma atual (Play)")]
    public static void Showcase()
    {
        if(!EditorApplication.isPlaying)return;var lab=Object.FindFirstObjectByType<RunExp03RewardLab>();if(!lab)return;
        int weapon=lab.Run.WeaponModifiers.Family==RunWeaponFamily.Bow?1:lab.Run.WeaponModifiers.Family==RunWeaponFamily.Spear?2:0;
        lab.StartCoroutine(Showcase(lab,weapon));
    }
    private static IEnumerator Showcase(RunExp03RewardLab lab,int weapon)
    {
        yield return lab.Prepare(weapon);var run=lab.Run;
        string id=weapon==1?BowBoonCatalog.JudgmentArrowId:weapon==2?SpearBoonCatalog.OrbitId:TitanicFistsDefinition.Id;
        if(!BoonExpansionCatalog.TryDefinition(id,out var definition))yield break;
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var choices=(List<RunBoons.Offer>)typeof(RunBoons).GetField("_choices",flags).GetValue(run);
        choices.Clear();choices.Add(new RunBoons.Offer(definition.Id,definition.Title,definition.Description));
        choices.Add(ProtocolOfferPresentation.Create(run.Protocols,ProtocolKind.Conductor));choices.Add(ProtocolOfferPresentation.Create(run.Protocols,ProtocolKind.Frost));
        lab.View.Bind(run,RewardSelectionView.Layout.Synthesis05,false);lab.AssistedShowcase=true;lab.ShowControls=true;
    }
    public static void ImportArt()
    {
        foreach(string key in RewardIllustrationCatalog.Keys)
        {
            string path="Assets/_Project/Resources/UI/RunExp05/"+key+".png";AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;
            importer.sRGBTexture=true;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;
            importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.maxTextureSize=512;importer.SaveAndReimport();
        }
    }
}
