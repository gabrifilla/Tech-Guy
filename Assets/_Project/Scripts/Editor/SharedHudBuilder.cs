using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SharedHudBuilder
{
    public const string PrefabPath = "Assets/_Project/Prefabs/PlayerHUD.prefab";
    private const string OrbArt = "Assets/_ThirdParty/itsmars Health Orb 1.1/";
    private static readonly Color Bronze = new Color(.48f,.36f,.22f);
    private static readonly Color Ivory = new Color(.87f,.83f,.73f);
    private static Sprite _solid;
    private static TMP_FontAsset _font;

    [MenuItem("Tools/Tech Guy/UI/Build and Install Shared HUD")]
    public static void BuildAndInstall()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            BuildPrefab();
            ConfigureSkills();
            foreach (string path in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Project/Scenes" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                Scene scene = EditorSceneManager.OpenScene(path);
                if (InstallInScene(scene)) EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("SHARED_HUD_INSTALLED: reusable prefab, all player scenes and mana costs configured.");
        }
        finally { if (!Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(previous); }
    }

    public static bool InstallInScene(Scene scene)
    {
        PlayerActor player = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerActor>()).SingleOrDefault();
        if (!player) return false;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (!prefab) return false;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == "HUD" && root.GetComponent<Canvas>()) root.SetActive(false);
        PlayerHUD hud = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerHUD>(true)).SingleOrDefault();
        if (!hud) hud = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, scene)).GetComponent<PlayerHUD>();
        hud.Configure(player);
        Bind(hud, "_player", player);
        Bind(player, "healthBar", hud.HealthFill);
        Bind(player, "manaBar", hud.ManaFill);
        Bind(player.GetComponent<CharControlScript>(), "_playerHUD", hud);
        LobbyInteraction guide = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LobbyInteraction>()).FirstOrDefault();
        Bind(player.GetComponent<AbilityHolder>(), "_lobbyInteraction", guide);
        EditorUtility.SetDirty(hud);
        PrefabUtility.RecordPrefabInstancePropertyModifications(hud);
        Debug.Log("HUD_SCENE_OK: " + scene.path);
        return true;
    }

    private static void BuildPrefab()
    {
        const string art = "Assets/_Project/Art/UI";
        if (!AssetDatabase.IsValidFolder(art)) AssetDatabase.CreateFolder("Assets/_Project/Art", "UI");
        string solidPath = art + "/HUDSolid.png";
        if (!File.Exists(solidPath))
        {
            var texture = new Texture2D(4,4,TextureFormat.RGBA32,false);
            texture.SetPixels(Enumerable.Repeat(Color.white,16).ToArray()); texture.Apply();
            File.WriteAllBytes(solidPath,texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(solidPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(solidPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        _solid = AssetDatabase.LoadAssetAtPath<Sprite>(solidPath);
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_ThirdParty/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        var go = new GameObject("Player HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        try
        {
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 50;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
            var hud = go.AddComponent<PlayerHUD>();
            RectTransform dock = Rect(go.transform,"Action Dock",0,0,960,246);
            dock.anchorMin = dock.anchorMax = new Vector2(.5f,0); dock.pivot = new Vector2(.5f,0);
            // Human-readable names keep the shared prefab easy to edit in the Inspector.
            Panel(dock,"Dock Shadow",0,82,580,136,new Color(.01f,.012f,.016f,.9f));
            Panel(dock,"Dock Stone",0,86,556,126,new Color(.045f,.051f,.058f,.98f));
            Panel(dock,"Top Bronze Rail",0,151,556,2,Bronze);
            Panel(dock,"Bottom Bronze Rail",0,22,556,2,Bronze);
            for (int side = -1; side <= 1; side += 2)
            {
                Image wing = Panel(dock,"Orb Shoulder",side*298,91,56,84,new Color(.075f,.065f,.049f));
                wing.rectTransform.localRotation = Quaternion.Euler(0,0,side*12);
                Panel(dock,"Shoulder Inlay",side*293,144,44,2,Bronze);
            }
            Image hp = Orb(dock,"Life",-375,new Color(.72f,.045f,.055f));
            Image mana = Orb(dock,"Mana",375,new Color(.055f,.33f,.78f));
            TextMeshProUGUI hpValue = Label(dock,"Life Value",-375,20,200,24,"100 / 100",18,Ivory);
            TextMeshProUGUI manaValue = Label(dock,"Mana Value",375,20,200,24,"1500 / 1500",18,Ivory);
            Label(dock,"Life Caption",-375,196,180,20,"V I D A",12,Ivory);
            Label(dock,"Mana Caption",375,196,180,20,"M A N A",12,Ivory);
            var slots = new SkillSlotView[5];
            for (int i = 0; i < slots.Length; i++) slots[i] = Slot(dock,(i-2)*100,i);
            RectTransform asura = Rect(dock,"Asura Meter",0,178,440,30);
            Panel(asura,"Track",0,-7,400,5,new Color(.11f,.09f,.065f));
            Image asuraFill = Panel(asura,"Energy",0,-7,400,5,new Color(.93f,.59f,.18f));
            asuraFill.type = Image.Type.Filled; asuraFill.fillMethod = Image.FillMethod.Horizontal; asuraFill.fillAmount = 0;
            var asuraLabel = Label(asura,"Energy Label",0,9,420,20,"ASURA   0 / 100",11,Ivory);
            var tooltip = Label(dock,"Skill Details",0,210,900,23,"",15,Ivory);
            var feedback = Label(dock,"Cast Feedback",0,234,900,26,"",18,Ivory);
            Bind(hud,"_dock",dock); Bind(hud,"_healthFill",hp); Bind(hud,"_manaFill",mana);
            Bind(hud,"_healthValue",hpValue); Bind(hud,"_manaValue",manaValue);
            Bind(hud,"_asuraRoot",asura.gameObject); Bind(hud,"_asuraFill",asuraFill); Bind(hud,"_asuraLabel",asuraLabel);
            Bind(hud,"_tooltip",tooltip); Bind(hud,"_feedback",feedback);
            var serialized = new SerializedObject(hud);
            var array = serialized.FindProperty("_slots"); array.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            foreach (RectTransform child in dock) child.anchorMin = child.anchorMax = new Vector2(.5f,0);
            PrefabUtility.SaveAsPrefabAsset(go,PrefabPath);
        }
        finally { Object.DestroyImmediate(go); }
    }

    private static Image Orb(Transform parent,string name,float x,Color tint)
    {
        RectTransform root = Rect(parent,name+" Orb",x,101,156,156);
        Sprite Art(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(OrbArt+file+".png");
        Image back = Panel(root,"Bowl",0,0,152,152,new Color(.035f,.035f,.045f)); back.sprite = Art("itsmars_orb_fill");
        Image fill = Panel(root,"Fill",0,0,146,146,tint); fill.sprite = Art("itsmars_orb_fill");
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Vertical; fill.fillOrigin = 0;
        Image shadow = Panel(root,"Glass Shadow",0,0,148,148,new Color(1,1,1,.65f)); shadow.sprite = Art("itsmars_orb_shadow");
        Image highlight = Panel(root,"Glass Highlight",0,0,148,148,new Color(1,1,1,.32f)); highlight.sprite = Art("itsmars_orb_highlight");
        Image border = Panel(root,"Metal Rim",0,0,160,160,new Color(.88f,.73f,.5f)); border.sprite = Art("DarkOrbBorder");
        return fill;
    }

    private static SkillSlotView Slot(Transform parent,float x,int index)
    {
        RectTransform root = Rect(parent,"Skill "+index,x,89,88,112);
        var slot = root.gameObject.AddComponent<SkillSlotView>();
        Panel(root,"Frame",0,10,84,80,Bronze);
        Panel(root,"Inset",0,10,80,76,new Color(.019f,.024f,.033f));
        Panel(root,"Inner Edge",0,10,72,68,new Color(.095f,.093f,.081f));
        Panel(root,"Icon Well",0,10,70,66,new Color(.03f,.036f,.047f));
        RectTransform glyphRect = Rect(root,"Skill Glyph",0,15,43,43);
        var glyph = glyphRect.gameObject.AddComponent<SkillGlyphGraphic>(); glyph.raycastTarget = false;
        glyph.SetKind((SkillGlyphKind)index); glyph.color = new Color(.92f,.68f,.3f);
        Image cooldown = Panel(root,"Cooldown Sweep",0,10,78,74,new Color(0,0,0,.72f));
        cooldown.type = Image.Type.Filled; cooldown.fillMethod = Image.FillMethod.Radial360; cooldown.fillOrigin = 2; cooldown.fillAmount = 0;
        Image flash = Panel(root,"Cast Flash",0,10,78,74,Color.clear);
        var timer = Label(root,"State",0,12,78,40,"",28,Ivory);
        Panel(root,"Key Backplate",0,-23,42,19,new Color(.08f,.08f,.08f));
        var key = Label(root,"Key",0,-23,48,19,index==4?"SPACE":new[]{"Q","W","E","R"}[index],14,Ivory);
        var title = Label(root,"Name",0,-43,98,18,new[]{"AVANCO","PUNHOS","CHOQUE","ASURA","ESQUIVA"}[index],12,Ivory);
        var cost = Label(root,"Mana Cost",0,-59,95,17,"MANA",12,new Color(.52f,.73f,.92f));
        Bind(slot,"_glyph",glyph); Bind(slot,"_cooldown",cooldown); Bind(slot,"_flash",flash);
        Bind(slot,"_key",key); Bind(slot,"_name",title); Bind(slot,"_cost",cost); Bind(slot,"_timer",timer);
        return slot;
    }

    private static RectTransform Rect(Transform parent,string name,float x,float y,float width,float height)
    {
        var rect = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = 5; rect.SetParent(parent,false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
        rect.anchoredPosition = new Vector2(x,y); rect.sizeDelta = new Vector2(width,height);
        return rect;
    }
    private static Image Panel(Transform parent,string name,float x,float y,float width,float height,Color color)
    {
        Image image = Rect(parent,name,x,y,width,height).gameObject.AddComponent<Image>();
        image.sprite = _solid; image.color = color; image.raycastTarget = false; return image;
    }
    private static TextMeshProUGUI Label(Transform parent,string name,float x,float y,float width,float height,string value,int size,Color color)
    {
        var text = Rect(parent,name,x,y,width,height).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = _font; text.text = value; text.fontSize = size; text.alignment = TextAlignmentOptions.Center;
        text.color = color; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }
    private static void Bind(Object target,string property,Object value)
    {
        var data = new SerializedObject(target); data.FindProperty(property).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureSkills()
    {
        SetSkill("Weapon/BreakerAdvance",80,"Avanco",SkillGlyphKind.Strike,new Color(.95f,.65f,.23f));
        SetSkill("Weapon/BreakerFlurry",150,"Punhos",SkillGlyphKind.Flurry,new Color(.95f,.72f,.32f));
        SetSkill("Weapon/BreakerShock",140,"Choque",SkillGlyphKind.Shock,new Color(.32f,.8f,1f));
        SetSkill("Weapon/BreakerAsura",280,"Asura",SkillGlyphKind.Asura,new Color(1f,.33f,.17f));
        SetSkill("Weapon/FrontAreaStrike",100,"Impacto",SkillGlyphKind.Strike,new Color(.95f,.65f,.23f));
        SetSkill("Weapon/SwordSlash",120,"Corte",SkillGlyphKind.Sword,new Color(.6f,.8f,1f));
        SetSkill("Dash/Dash",0,"Esquiva",SkillGlyphKind.Dash,new Color(.68f,.77f,.85f));
    }
    private static void SetSkill(string path,float cost,string label,SkillGlyphKind glyph,Color color)
    {
        Ability ability = AssetDatabase.LoadAssetAtPath<Ability>("Assets/_Project/ScriptableObjects/Abilities/"+path+".asset");
        if (!ability) return;
        var data = new SerializedObject(ability);
        data.FindProperty("_manaCost").floatValue = cost; data.FindProperty("_displayName").stringValue = label;
        data.FindProperty("_glyph").enumValueIndex = (int)glyph; data.FindProperty("_accentColor").colorValue = color;
        data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(ability);
    }
}
