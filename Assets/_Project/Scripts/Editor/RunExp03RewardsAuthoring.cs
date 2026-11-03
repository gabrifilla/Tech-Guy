using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>Original faceted glyphs and isolated IMGUI review scene. No production UI changes.</summary>
public static class RunExp03RewardsAuthoring
{
    public const string ScenePath="Assets/_Project/Scenes/Review/RunExp03Rewards.unity";
    private const string GlyphPath="Assets/_Project/Resources/UI/RunExp03";
    [MenuItem("Tools/Tech Guy/RUN-EXP-03/Recompensas/Abrir laboratório visual")]
    public static void Open()
    {if(!EditorApplication.isPlaying&&EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(ScenePath);}
    [MenuItem("Tools/Tech Guy/RUN-EXP-03/Recompensas/Conceito A na run atual")]
    public static void PreviewA()=>Attach(RewardSelectionView.Layout.Artifacts);
    [MenuItem("Tools/Tech Guy/RUN-EXP-03/Recompensas/Conceito B na run atual")]
    public static void PreviewB()=>Attach(RewardSelectionView.Layout.Reconfiguration);
    [MenuItem("Tools/Tech Guy/RUN-EXP-03/Recompensas/Restaurar interface atual")]
    public static void Restore()
    {foreach(var view in Object.FindObjectsByType<RewardSelectionView>(FindObjectsSortMode.None))Object.Destroy(view);}
    private static void Attach(RewardSelectionView.Layout layout)
    {
        if(!EditorApplication.isPlaying){Debug.LogWarning("Entre em Play numa incursão para usar a prévia temporária.");return;}
        var run=Object.FindFirstObjectByType<RunBoons>();if(!run){Debug.LogWarning("Nenhuma run ativa.");return;}
        var view=run.GetComponent<RewardSelectionView>();if(!view){view=run.gameObject.AddComponent<RewardSelectionView>();view.Bind(run,layout,true);}else view.Concept=layout;
    }
    public static void Build()
    {
        Directory.CreateDirectory(GlyphPath);Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));AssetDatabase.Refresh();
        for(int i=0;i<4;i++)BuildGlyph(i);
        if(File.Exists(ScenePath))return;
        if(!AssetDatabase.CopyAsset(RunExp02Builder.ArenaA,ScenePath))throw new Exception("Could not copy isolated review scene.");
        var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        var review=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RunExp02Review>(true)).Single();
        var old=new SerializedObject(review);var player=old.FindProperty("_player").objectReferenceValue;
        var lab=review.gameObject.AddComponent<RunExp03RewardLab>();var data=new SerializedObject(lab);data.FindProperty("_player").objectReferenceValue=player;data.ApplyModifiedPropertiesWithoutUndo();
        Object.DestroyImmediate(review);EditorSceneManager.SaveScene(scene);EditorSceneManager.CloseScene(scene,true);AssetDatabase.SaveAssets();
    }
    private static void BuildGlyph(int kind)
    {
        string name=kind==0?"Mutation":kind==1?"Protocol":kind==2?"Technique":"Frame",path=GlyphPath+"/Glyph"+name+".png";
        if(File.Exists(path))return;
        const int size=512;var pixels=new Color[size*size];
        void Polygon(float shade,params Vector2[] vertices)
        {
            int minX=Mathf.Max(0,Mathf.FloorToInt(vertices.Min(p=>p.x))),maxX=Mathf.Min(size-1,Mathf.CeilToInt(vertices.Max(p=>p.x)));
            int minY=Mathf.Max(0,Mathf.FloorToInt(vertices.Min(p=>p.y))),maxY=Mathf.Min(size-1,Mathf.CeilToInt(vertices.Max(p=>p.y)));
            for(int y=minY;y<=maxY;y++)for(int x=minX;x<=maxX;x++)
            {
                bool inside=false;
                for(int i=0,j=vertices.Length-1;i<vertices.Length;j=i++)
                {var a=vertices[i];var b=vertices[j];if((a.y>y)!=(b.y>y)&&x<(b.x-a.x)*(y-a.y)/(b.y-a.y)+a.x)inside=!inside;}
                if(inside)pixels[y*size+x]=new Color(shade,shade,shade,1);
            }
        }
        void Stroke(Vector2 a,Vector2 b,float width,float shade)
        {var v=(b-a).normalized;var n=new Vector2(-v.y,v.x)*width;Polygon(shade,a+n,b+n,b-n,a-n);}
        if(kind==3)
        {
            var points=new[]{new Vector2(256,16),new Vector2(496,256),new Vector2(256,496),new Vector2(16,256)};
            for(int i=0;i<4;i++)Stroke(points[i],points[(i+1)%4],2,1);
        }
        else if(kind==0)
        {
            Polygon(.47f,new Vector2(234,64),new Vector2(90,222),new Vector2(190,274),new Vector2(280,170));
            Polygon(.92f,new Vector2(246,84),new Vector2(380,218),new Vector2(290,262),new Vector2(216,184));
            Polygon(.7f,new Vector2(132,292),new Vector2(256,458),new Vector2(246,304),new Vector2(195,262));
            Polygon(1,new Vector2(274,450),new Vector2(407,286),new Vector2(307,277),new Vector2(271,326));
            Stroke(new Vector2(66,346),new Vector2(111,391),3,.45f);Stroke(new Vector2(404,115),new Vector2(449,160),3,.45f);
        }
        else if(kind==1)
        {
            var c=new Vector2(256,256);
            for(int i=0;i<3;i++)
            {
                float a=(i*120+90)*Mathf.Deg2Rad;var tip=c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*164;var bend=Vector2.Lerp(c,tip,.55f)+new Vector2(24,-14);
                Stroke(c,bend,7,.65f);Stroke(bend,tip,7,1);
                Polygon(.48f,tip+Vector2.up*47,tip+Vector2.right*39,tip-Vector2.up*47,tip-Vector2.right*39);
                Polygon(1,tip+Vector2.up*34,tip+Vector2.right*27,tip,tip-Vector2.right*27);
            }
            Polygon(.9f,c+new Vector2(-28,62),c+new Vector2(41,8),c+new Vector2(5,8),c+new Vector2(24,-63),c+new Vector2(-40,-1),c+new Vector2(-5,-1));
        }
        else
        {
            for(int i=0;i<3;i++)
            {
                float x=111+i*108,y=80+i*26;
                Polygon(.48f,new Vector2(x,y),new Vector2(x-28,y+214),new Vector2(x+89,y+345),new Vector2(x+32,y+117));
                Polygon(.94f,new Vector2(x,y),new Vector2(x+32,y+117),new Vector2(x+89,y+345),new Vector2(x+57,y+140));
            }
            Stroke(new Vector2(83,86),new Vector2(331,86),3,.4f);
        }
        var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);texture.SetPixels(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.sRGBTexture=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
    }
}
