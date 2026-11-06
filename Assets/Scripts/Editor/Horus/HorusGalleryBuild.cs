using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

// 매 석상 + 매의 회랑(같은 씬 안에서 열리는 숨은 공간) + 연출 감독 + 매 길잡이 (HorusRoomObjects가 부름)
// - 회랑 타일은 처음엔 없음. 서쪽 벽 틈(조사 대사)만 보임 -> 우자트 부적을 왼쪽 매 석상에 끼우면 열림 (HorusHiddenGallery)
// - 회랑 안 히든 자리 표시는 열리기 전엔 꺼져 있음 (히든 아이템은 다른 개발자가 연결)
public static class HorusGalleryBuild
{
    private const string Bubble = "Assets/image/ui/UI_Prompt_New.png";
    private const string ObjectArt = "Assets/image/Horus/Object/";
    private const string CrackArt = ObjectArt + "HO_Wall_Crack.png";
    private static readonly Color Stone = new Color(0.85f, 0.78f, 0.62f);
    private static readonly string[] Overlays = { "H1_WingOverlay", "H2_ChartOverlay", "H3_WedjatOverlay", "H4_PathOverlay", "H5_SkyOverlay" };

    public static void Build(Transform root)
    {
        Sprite falcon = HorusArtSetup.Art("H1/H1_03");
        // [수정] 제단 둘레를 별이 돌게 되면서(H5 HorusSkyRing) 별 궤도와 겹치지 않게 북쪽 벽 가까이로
        GameObject left = Statue(root, "OBJ_H12_FalconStatue_L", new Vector2(9.2f, 14.4f), falcon);
        Statue(root, "OBJ_H12_FalconStatue_R", new Vector2(14.8f, 14.4f), falcon);
        GameObject crack = WallCrack(root);
        HorusHiddenGallery gallery = Gallery(left.transform, crack, falcon);
        HorusFalconStatue statue = left.AddComponent<HorusFalconStatue>();
        var so = new SerializedObject(statue);
        HorusArtSetup.Prop(so, "gallery").objectReferenceValue = gallery;
        so.ApplyModifiedPropertiesWithoutUndo();
        Director(falcon);
        FalconGuide(falcon);
    }

    // 그림 크기를 먼저 맞추고 충돌 상자를 붙임 (붙일 때 그림 크기에 자동으로 맞음)
    private static GameObject Statue(Transform root, string objName, Vector2 pos, Sprite art)
    {
        GameObject go = HorusRoomObjects.Shape(root, objName, pos, art != null ? art : HorusRoomObjects.Square, Vector2.one, Stone);
        if (art != null) HorusWorldArt.FitObject(go, art, new Vector2(1.4f, 1f), Stone);
        go.AddComponent<BoxCollider2D>();
        return go;
    }

    // 서쪽 벽(-1, 7~8)의 금 간 자국. 벽 타일 위에 그림 (바닥 타일 순서보다 위, 물건보다 아래)
    // [수정] 예전엔 어두운 바닥 무늬라 거의 안 보였음 -> 푸른빛이 새는 금(HO_Wall_Crack) + 숨 쉬는 빛 + 먼지 (HorusWallWhisper)
    private static GameObject WallCrack(Transform root)
    {
        Sprite glow = HorusArtSetup.Pixel(CrackArt);
        Sprite fallback = HorusTileArt.SpriteOf(HorusTileArt.FloorC);
        Sprite s = glow != null ? glow : (fallback != null ? fallback : HorusRoomObjects.Square);
        GameObject go = HorusRoomObjects.Shape(root, "OBJ_HG0_WallCrack", new Vector2(-0.5f, 8f), s, Vector2.one, glow != null ? Color.white : new Color(0.45f, 0.38f, 0.32f));
        Vector2 b = s.bounds.size;
        go.transform.localScale = new Vector3(1f / b.x, 2f / b.y, 1f);
        go.GetComponent<SpriteRenderer>().sortingOrder = -8;
        Whisper(go);
        go.AddComponent<BoxCollider2D>().isTrigger = true; // 벽 타일이 막고 있으니 이건 조사용
        HorusRoomObjects.Inspect(go, "HO_WALL_CRACK", "벽 틈 살펴보기", new[,]
        {
            { "나", "벽 틈으로 차가운 바람이 새어 나온다... 이 너머에 뭔가 있는 것 같아." },
            { "나", "그러고 보니 저 매 석상, 아까부터 이쪽 벽을 보고 있는 것 같은데?" }
        });
        return go;
    }

    // 틈 사이로 새는 푸른빛 + 먼지 + 처음 다가가면 혼잣말 (숨은 방 복선)
    private static void Whisper(GameObject crack)
    {
        var lightGo = new GameObject("CrackLeak", typeof(UnityEngine.Rendering.Universal.Light2D));
        lightGo.transform.SetParent(crack.transform, false);
        lightGo.transform.position = crack.transform.position + Vector3.right * 0.4f;
        var light = lightGo.GetComponent<UnityEngine.Rendering.Universal.Light2D>();
        light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
        light.color = new Color(0.5f, 0.75f, 1f);
        light.intensity = 0.5f;
        light.pointLightOuterRadius = 2.6f;
        light.pointLightInnerRadius = 0.3f;
        HorusWallWhisper w = crack.AddComponent<HorusWallWhisper>();
        var so = new SerializedObject(w);
        HorusArtSetup.Prop(so, "crack").objectReferenceValue = crack.GetComponent<SpriteRenderer>();
        HorusArtSetup.Prop(so, "leak").objectReferenceValue = light;
        HorusArtSetup.Prop(so, "emoteSprite").objectReferenceValue = HorusArtSetup.Art(Bubble);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static HorusHiddenGallery Gallery(Transform statue, GameObject crack, Sprite falcon)
    {
        var rootGo = new GameObject("HorusGallery");
        GameObject contents = GalleryContents(rootGo.transform);
        HorusHiddenGallery gallery = rootGo.AddComponent<HorusHiddenGallery>();
        var so = new SerializedObject(gallery);
        HorusArtSetup.Prop(so, "ground").objectReferenceValue = TilemapNamed("Ground");
        HorusArtSetup.Prop(so, "wall").objectReferenceValue = TilemapNamed("Wall");
        HorusArtSetup.Prop(so, "wallTop").objectReferenceValue = HorusRoomMap.WallTop();
        HorusArtSetup.Prop(so, "wallFace").objectReferenceValue = HorusRoomMap.WallFace();
        TileBase[] floors = HorusRoomMap.FloorTiles();
        SerializedProperty list = HorusArtSetup.Prop(so, "floors");
        list.arraySize = floors.Length;
        for (int i = 0; i < floors.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = floors[i];
        HorusArtSetup.Prop(so, "contents").objectReferenceValue = contents;
        HorusArtSetup.Prop(so, "wallCrack").objectReferenceValue = crack;
        HorusArtSetup.Prop(so, "statue").objectReferenceValue = statue;
        HorusArtSetup.Prop(so, "falconSprite").objectReferenceValue = falcon;
        HorusArtSetup.Prop(so, "emoteSprite").objectReferenceValue = HorusArtSetup.Art(Bubble);
        so.ApplyModifiedPropertiesWithoutUndo();
        return gallery;
    }

    // 회랑 안: 매의 둥지 + 히든 자리(금색 원) + 파피루스 조각 자리(종이색). 열리기 전엔 꺼둠
    private static GameObject GalleryContents(Transform parent)
    {
        var contents = new GameObject("HorusGallery_Contents");
        contents.transform.SetParent(parent, false);
        Transform t = contents.transform;
        HorusRoomObjects.Shape(t, "OBJ_HG1_Nest", new Vector2(-5f, 9.8f), HorusRoomObjects.Circle, new Vector2(1.2f, 0.7f), new Color(0.42f, 0.3f, 0.18f));
        HorusRoomObjects.Shape(t, "HiddenItemSpot_Horus", new Vector2(-5f, 10f), HorusRoomObjects.Circle, new Vector2(0.5f, 0.5f), new Color(1f, 0.85f, 0.35f, 0.8f));
        HorusRoomObjects.Shape(t, "PapyrusSpot_Horus_2", new Vector2(-7f, 6f), HorusRoomObjects.Square, new Vector2(0.4f, 0.3f), new Color(0.93f, 0.87f, 0.72f));
        contents.SetActive(false);
        return contents;
    }

    private static Tilemap TilemapNamed(string objName)
    {
        GameObject go = HorusArtSetup.Find(objName);
        return go != null ? go.GetComponent<Tilemap>() : null;
    }

    // ================= 연출 감독 + 목표 표시 =================
    private static void Director(Sprite falcon)
    {
        HorusDirector d = new GameObject("HorusDirector").AddComponent<HorusDirector>();
        d.gameObject.AddComponent<HorusTestKit>(); // 에디터에서 호루스 씬을 바로 Play할 때만 돋보기/붓/횃불 지급
        Volume dream = DreamVolume(d.transform);
        var so = new SerializedObject(d);
        HorusArtSetup.Prop(so, "horusPortrait").objectReferenceValue = HorusArtSetup.Art("H5/H5_06"); // [임시] 호루스 초상화가 오면 바꾸기
        HorusArtSetup.Prop(so, "falconSprite").objectReferenceValue = falcon;
        HorusArtSetup.Prop(so, "plateSprite").objectReferenceValue = HorusArtSetup.Art("H5/H5_08");
        HorusArtSetup.Prop(so, "emoteSprite").objectReferenceValue = HorusArtSetup.Art(Bubble);
        HorusArtSetup.Prop(so, "dreamVolume").objectReferenceValue = dream;
        HorusArtSetup.Prop(so, "isisSprite").objectReferenceValue = HorusArtSetup.Pixel(ObjectArt + "HO_Isis_Ghost.png");
        HorusArtSetup.Prop(so, "isisWing").objectReferenceValue = HorusArtSetup.Pixel(ObjectArt + "HO_Isis_Wing.png");
        HorusArtSetup.Prop(so, "featherSprite").objectReferenceValue = HorusArtSetup.Pixel(ObjectArt + "HO_Feather.png");
        HorusArtSetup.Prop(so, "rippleSprite").objectReferenceValue = HorusArtSetup.Pixel(ObjectArt + "HO_Ripple.png");
        SerializedProperty list = HorusArtSetup.Prop(so, "overlays");
        list.arraySize = Overlays.Length;
        for (int i = 0; i < Overlays.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = HorusArtSetup.Find(Overlays[i]);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // 스핑크스 꿈과 같은 후처리 Volume. 평소엔 무게 0 (안 보임) -> 이시스가 말할 때만 켜짐
    private static Volume DreamVolume(Transform parent)
    {
        DreamVolumeBuilder.Create(parent);
        Camera cam = Object.FindFirstObjectByType<Camera>();
        if (cam != null) DreamVolumeBuilder.EnablePostProcessing(cam);
        Volume v = parent.GetComponentInChildren<Volume>(true);
        if (v != null) v.weight = 0f;
        return v;
    }

    // 매 길잡이 (지금 목표 위에 앉아 있고, 목표가 바뀌면 날아감)
    private static void FalconGuide(Sprite falcon)
    {
        HorusFalconGuide g = new GameObject("HorusFalconGuide").AddComponent<HorusFalconGuide>();
        var so = new SerializedObject(g);
        HorusArtSetup.Prop(so, "guide").objectReferenceValue = Object.FindFirstObjectByType<HorusObjectiveGuide>(FindObjectsInactive.Include);
        HorusArtSetup.Prop(so, "falconSprite").objectReferenceValue = falcon;
        so.ApplyModifiedPropertiesWithoutUndo();
        HorusDirector d = Object.FindFirstObjectByType<HorusDirector>();
        if (d == null) return;
        var dso = new SerializedObject(d);
        HorusArtSetup.Prop(dso, "falcon").objectReferenceValue = g;
        dso.ApplyModifiedPropertiesWithoutUndo();
    }
}
