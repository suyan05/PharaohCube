using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// 매 석상 + 매의 회랑(같은 씬 안에서 열리는 숨은 공간) + 연출 감독 + 목표 표시 (HorusRoomObjects가 부름)
// - 회랑 타일은 처음엔 없음. 서쪽 벽 틈(조사 대사)만 보임 -> 우자트 부적을 왼쪽 매 석상에 끼우면 열림 (HorusHiddenGallery)
// - 회랑 안 히든 자리 표시는 열리기 전엔 꺼져 있음 (히든 아이템은 다른 개발자가 연결)
public static class HorusGalleryBuild
{
    private const string Bubble = "Assets/image/ui/UI_Prompt_New.png";
    private static readonly Color Stone = new Color(0.85f, 0.78f, 0.62f);
    private static readonly string[] Overlays = { "H1_WingOverlay", "H2_ChartOverlay", "H3_WedjatOverlay", "H4_PathOverlay", "H5_SkyOverlay" };

    public static void Build(Transform root)
    {
        Sprite falcon = HorusArtSetup.Art("H1/H1_03");
        GameObject left = Statue(root, "OBJ_H12_FalconStatue_L", new Vector2(9.5f, 12.8f), falcon);
        Statue(root, "OBJ_H12_FalconStatue_R", new Vector2(14.5f, 12.8f), falcon);
        GameObject crack = WallCrack(root);
        HorusHiddenGallery gallery = Gallery(left.transform, crack, falcon);
        HorusFalconStatue statue = left.AddComponent<HorusFalconStatue>();
        var so = new SerializedObject(statue);
        HorusArtSetup.Prop(so, "gallery").objectReferenceValue = gallery;
        so.ApplyModifiedPropertiesWithoutUndo();
        Director(falcon);
        GoalMarker();
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
    private static GameObject WallCrack(Transform root)
    {
        Tile cracked = HorusRoomMap.Load("Title_11_0") as Tile;
        Sprite s = cracked != null ? cracked.sprite : HorusRoomObjects.Square;
        GameObject go = HorusRoomObjects.Shape(root, "OBJ_HG0_WallCrack", new Vector2(-0.5f, 8f), s, Vector2.one, new Color(0.45f, 0.38f, 0.32f));
        Vector2 b = s.bounds.size;
        go.transform.localScale = new Vector3(1f / b.x, 2f / b.y, 1f);
        go.GetComponent<SpriteRenderer>().sortingOrder = -8;
        go.AddComponent<BoxCollider2D>().isTrigger = true; // 벽 타일이 막고 있으니 이건 조사용
        HorusRoomObjects.Inspect(go, "HO_WALL_CRACK", "벽 틈 살펴보기", new[,]
        {
            { "나", "벽 틈으로 차가운 바람이 새어 나온다... 이 너머에 뭔가 있는 것 같아." },
            { "나", "그러고 보니 저 매 석상, 아까부터 이쪽 벽을 보고 있는 것 같은데?" }
        });
        return go;
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
        var so = new SerializedObject(d);
        HorusArtSetup.Prop(so, "horusPortrait").objectReferenceValue = HorusArtSetup.Art("H5/H5_06"); // [임시] 호루스 초상화가 오면 바꾸기
        HorusArtSetup.Prop(so, "falconSprite").objectReferenceValue = falcon;
        HorusArtSetup.Prop(so, "plateSprite").objectReferenceValue = HorusArtSetup.Art("H5/H5_08");
        HorusArtSetup.Prop(so, "emoteSprite").objectReferenceValue = HorusArtSetup.Art(Bubble);
        SerializedProperty list = HorusArtSetup.Prop(so, "overlays");
        list.arraySize = Overlays.Length;
        for (int i = 0; i < Overlays.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = HorusArtSetup.Find(Overlays[i]);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void GoalMarker()
    {
        HorusGoalMarker m = new GameObject("HorusGoalMarker").AddComponent<HorusGoalMarker>();
        var so = new SerializedObject(m);
        HorusArtSetup.Prop(so, "guide").objectReferenceValue = Object.FindFirstObjectByType<HorusObjectiveGuide>(FindObjectsInactive.Include);
        HorusArtSetup.Prop(so, "markerSprite").objectReferenceValue = HorusArtSetup.Art("H2/H2_star_on");
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
