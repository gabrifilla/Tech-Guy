using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class RunExp04RoomAuthoring
{
    [MenuItem("Tools/Tech Guy/RUN-EXP-04/Salas/Aplicar apresentação intermediária")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit Mode required");
        const string root="Assets/_Project/Art/RunExp04/Rooms";Directory.CreateDirectory(root);AssetDatabase.Refresh();
        var open=Material(root,"Piso de memória aberta",new Color(.19f,.22f,.22f));var covered=Material(root,"Piso de canalização",new Color(.14f,.2f,.21f));var trim=Material(root,"Trilhas cerâmicas",new Color(.34f,.39f,.37f));
        var content=AssetDatabase.LoadAssetAtPath<FirstSectorContentDefinition>(RunExp03Authoring.Content);var data=new SerializedObject(content);data.FindProperty("_openFloor").objectReferenceValue=open;data.FindProperty("_coveredFloor").objectReferenceValue=covered;data.FindProperty("_roomTrim").objectReferenceValue=trim;data.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
    }
    private static Material Material(string root,string name,Color color)
    {string path=root+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat)return mat;var shader=Shader.Find("Universal Render Pipeline/Lit");if(!shader)throw new InvalidOperationException("Required URP/Lit shader missing");mat=new Material(shader);mat.name=name;mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.16f);AssetDatabase.CreateAsset(mat,path);return mat;}
}
