using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>Creates only a review scene and assets under Prototypes/RunExperience.</summary>
public static class RunExperienceStudyBuilder
{
    public const string Folder="Assets/_Project/Prototypes/RunExperience";
    public const string ScenePath=Folder+"/RunExperienceStudy.unity";
    private static Material _body,_dark,_floor,_danger;
    private static Material Material(string name,Color color)
    {
        string path=Folder+"/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
        material.color=color;return material;
    }
    private static Transform Shape(Transform parent,string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
    {
        var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);
        go.transform.localPosition=position;go.transform.localScale=scale;
        go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;
    }
    private static void Label(Transform parent,string value,Vector3 position,float size=.34f)
    {
        var go=new GameObject(value);go.transform.SetParent(parent,false);go.transform.localPosition=position;
        go.transform.localRotation=Quaternion.Euler(50,0,0);
        var text=go.AddComponent<TextMesh>();text.text=value;text.characterSize=size;text.fontSize=56;
        text.anchor=TextAnchor.MiddleCenter;text.color=new Color(.85f,.91f,.95f);
    }
    [MenuItem("Tools/Tech Guy/Run Experience/Build isolated study")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Build in Edit Mode only.");
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        if(File.Exists(ScenePath))throw new System.InvalidOperationException("Study already exists; preserve review assets.");
        var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            _body=Material("StudyIvory",new Color(.66f,.72f,.75f));
            _dark=Material("StudyGraphite",new Color(.13f,.18f,.23f));
            _floor=Material("StudyFloor",new Color(.065f,.09f,.12f));
            _danger=Material("StudyWarning",new Color(.95f,.28f,.08f));
            string[] names={"FRAGMENTO / swarm","EMISSOR / ranged","CARRETEL / pull","BASTIAO / heavy","ARBITRO / elite"};
            for(int role=0;role<5;role++)
            {
                var root=new GameObject(names[role]).transform;root.position=new Vector3((role-2)*8,0,10);
                Shape(root,"plinth",PrimitiveType.Cube,new Vector3(0,-.2f,0),new Vector3(6,.3f,6),_floor);
                var body=new GameObject("visual body").transform;body.SetParent(root,false);
                Transform tool;
                if(role==0)
                {
                    Shape(body,"low shard",PrimitiveType.Cube,new Vector3(0,.65f,0),new Vector3(.9f,.45f,1.5f),_body).localRotation=Quaternion.Euler(0,45,15);
                    for(int i=0;i<4;i++)Shape(body,"angular leg",PrimitiveType.Cube,new Vector3(i%2==0?-.7f:.7f,.3f,i<2?-.5f:.5f),new Vector3(.18f,.6f,.18f),_body).localRotation=Quaternion.Euler(0,0,i%2==0?-45:45);
                    tool=Shape(body,"bite fork",PrimitiveType.Cube,new Vector3(0,.55f,-.9f),new Vector3(.65f,.12f,.65f),_dark);
                }
                else if(role==1)
                {
                    Shape(body,"left suspended block",PrimitiveType.Cube,new Vector3(-.6f,1.8f,0),new Vector3(.6f,1.2f,.9f),_body);
                    tool=Shape(body,"right suspended block",PrimitiveType.Cube,new Vector3(.6f,1.8f,0),new Vector3(.6f,1.2f,.9f),_body);
                    Shape(body,"floating core",PrimitiveType.Sphere,new Vector3(0,1.8f,0),Vector3.one*.35f,_dark);
                    Shape(body,"projector barrel",PrimitiveType.Cube,new Vector3(0,1.45f,-.8f),new Vector3(.3f,.3f,1.3f),_body);
                }
                else if(role==2)
                {
                    Shape(body,"spool",PrimitiveType.Cylinder,new Vector3(0,.65f,0),new Vector3(1.6f,.3f,1.6f),_body);
                    for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Shape(body,"outrigger",PrimitiveType.Cube,new Vector3(Mathf.Cos(a),.3f,Mathf.Sin(a)),new Vector3(.2f,.6f,.2f),_body);}
                    tool=Shape(body,"asymmetric hook boom",PrimitiveType.Cube,new Vector3(-.65f,1.4f,-.3f),new Vector3(.3f,1.4f,.35f),_dark);
                    Shape(tool,"hook jaw",PrimitiveType.Cube,new Vector3(.7f,.45f,0),new Vector3(1.8f,.15f,.8f),_body);
                }
                else if(role==3)
                {
                    Shape(body,"slab torso",PrimitiveType.Cube,new Vector3(0,1.4f,0),new Vector3(2.1f,1.8f,1.1f),_body);
                    for(int i=0;i<2;i++)Shape(body,"short leg",PrimitiveType.Cube,new Vector3(i==0?-.65f:.65f,.35f,0),new Vector3(.6f,.7f,.8f),_dark);
                    Shape(body,"offset helmet",PrimitiveType.Cube,new Vector3(-.35f,2.55f,0),new Vector3(.7f,.5f,.7f),_dark);
                    tool=Shape(body,"hammer arm",PrimitiveType.Cube,new Vector3(1.4f,1.4f,-.3f),new Vector3(.9f,1.6f,.9f),_dark);
                }
                else
                {
                    Shape(body,"pointer tower",PrimitiveType.Capsule,new Vector3(0,2.1f,0),new Vector3(.65f,1.5f,.65f),_body);
                    for(int i=0;i<3;i++){float a=i*Mathf.PI*2/3;Shape(body,"tripod",PrimitiveType.Cube,new Vector3(Mathf.Cos(a)*.8f,.65f,Mathf.Sin(a)*.8f),new Vector3(.2f,1.6f,.2f),_dark).localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,30);}
                    tool=Shape(body,"suspended cross arm",PrimitiveType.Cube,new Vector3(0,3.15f,0),new Vector3(3.5f,.22f,.3f),_body);
                    Shape(body,"upper split arc",PrimitiveType.Cube,new Vector3(-.8f,3.7f,0),new Vector3(.2f,1.1f,.25f),_dark).localRotation=Quaternion.Euler(0,0,-35);
                }
                var warning=Shape(root,"candidate committed lane",PrimitiveType.Cube,new Vector3(0,.025f,-1.6f),new Vector3(role==3?3:.4f,.025f,2.3f),_danger).gameObject;
                root.gameObject.AddComponent<RunExperienceStudyMotion>().Configure(role,body,tool,warning);
                Label(root,new[]{"01 FRAGMENTO\nswarm","02 EMISSOR\nranged","03 CARRETEL\npull","04 BASTIAO\nheavy","05 ARBITRO\nelite"}[role],new Vector3(0,.3f,-3.6f),.15f);
            }
            for(int room=0;room<2;room++)
            {
                var root=new GameObject(room==0?"A - open arena":"B - split sightlines").transform;root.position=new Vector3(room==0?-11:11,0,-7);
                Shape(root,"arena floor",PrimitiveType.Cube,Vector3.down*.2f,new Vector3(18,.3f,12),_floor);
                Shape(root,"back boundary",PrimitiveType.Cube,new Vector3(0,.3f,6),new Vector3(18,.6f,.3f),_body);
                if(room==1)for(int i=0;i<2;i++)Shape(root,"LOS obstacle",PrimitiveType.Cube,new Vector3(i==0?-3:3,.6f,i==0?-1:1),new Vector3(2,1.2f,5),_dark);
                Shape(root,"player scale proxy",PrimitiveType.Capsule,new Vector3(0,.9f,-3),new Vector3(.65f,.9f,.65f),_body);
                for(int i=0;i<3;i++)Shape(root,"spawn study",PrimitiveType.Cube,new Vector3((i-1)*6,.5f,3),Vector3.one,_body);
                Label(root,room==0?"A / AREA ABERTA":"B / LINHAS PARTIDAS",new Vector3(0,.5f,-7),.20f);
            }
            var light=new GameObject("Study light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.transform.rotation=Quaternion.Euler(55,-30,0);
            var camera=new GameObject("Study camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=17;
            camera.transform.position=new Vector3(0,31,-29);camera.transform.LookAt(new Vector3(0,0,0));camera.backgroundColor=new Color(.025f,.035f,.055f);camera.clearFlags=CameraClearFlags.SolidColor;
            Label(null,"RUN-EXP-01 / ESTUDO ISOLADO",new Vector3(0,1,17),.3f);
            EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
        }
        finally { SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true); }
    }
}
