using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// [임시] 중앙 홀 물건 + 진행 (CentralRoomBuilder가 부름)
// 큐브 (9, 5) / 횃불 걸이 (4.5, 4.5) / 화로 (13.5, 4.5) / 북쪽 문 (9, 10) / 기둥 2개 / 맵 경계
public static class CentralRoomObjects
{
    private static string layer;
    private static int order;

    public static void Build(int mapH, string sortingLayer, int sortingOrder)
    {
        layer = sortingLayer;
        order = sortingOrder;
        var root = new GameObject("CentralRoom");

        Bounds(root.transform, 18f, mapH);
        Sprite("Pillar_L", "Hall_Obj_Pillar", new Vector3(6.5f, 8.2f), root.transform, 0);
        Sprite("Pillar_R", "Hall_Obj_Pillar", new Vector3(11.5f, 8.2f), root.transform, 0);

        HallFlame cube = Flame(root.transform, "Cube", "Hall_Obj_Cube_Off", "Hall_Obj_Cube_On", new Vector3(9f, 5f), new Color(1f, 0.85f, 0.5f), 4f, 0.7f, true);
        Box(cube.gameObject, new Vector2(2.6f, 1.2f), new Vector2(0f, 0.6f), false);
        HallFlame brazier = Flame(root.transform, "Brazier", "Hall_Obj_Brazier_Off", "Hall_Obj_Brazier_On", new Vector3(13.5f, 4.5f), new Color(1f, 0.7f, 0.35f), 6f, 1.6f, false);
        Box(brazier.gameObject, new Vector2(0.9f, 0.6f), new Vector2(0f, 0.3f), false);

        Inspectable rack = Rack(root.transform);
        Inspectable brazierInspect = BrazierInspect(brazier.gameObject);
        Transform door = Door(root.transform);
        Director(root.transform, cube, brazier, rack, brazierInspect, door);
    }

    // ================= 물건 =================
    private static Inspectable Rack(Transform root)
    {
        GameObject go = Sprite("TorchRack", "Hall_Obj_TorchRack_Full", new Vector3(4.5f, 4.5f), root, 0);
        Box(go, new Vector2(0.9f, 0.6f), new Vector2(0f, 0.3f), false);
        Inspectable insp = go.AddComponent<Inspectable>();
        Edit(insp, so =>
        {
            so.FindProperty("objectId").stringValue = "HALL_OBJ_TORCH_RACK";
            so.FindProperty("promptText").stringValue = "횃불 집기";
            so.FindProperty("targetRenderer").objectReferenceValue = go.GetComponent<SpriteRenderer>();
            so.FindProperty("doneSprite").objectReferenceValue = PrologueAssetFinder.PixelSprite("Hall_Obj_TorchRack_Taken", true);
            Lines(so, "lines", "나", "불 꺼진 횃불을 하나 집었다. 화로에 대 보자.");
            Lines(so, "afterDoneLines", "나", "나머지는 두고 가자.");
        });
        return insp;
    }

    // 화로: 횃불을 집은 뒤에만, 길게 눌러 불 붙이기 -> ITEM_TORCH (PlayerTorchTemp가 바로 켬)
    private static Inspectable BrazierInspect(GameObject brazier)
    {
        Inspectable insp = brazier.AddComponent<Inspectable>();
        Edit(insp, so =>
        {
            so.FindProperty("objectId").stringValue = "HALL_OBJ_BRAZIER";
            so.FindProperty("promptText").stringValue = "화로";
            so.FindProperty("holdPromptText").stringValue = "길게 눌러 횃불에 불 붙이기";
            so.FindProperty("requiredFlag").stringValue = "F_HALL_OBJ_TORCH_RACK_DONE";
            Lines(so, "lockedLines", "나", "불길이 뜨겁다. 불을 옮겨 담을 게 필요해.");
            so.FindProperty("holdSeconds").floatValue = 1.5f;
            SerializedProperty items = so.FindProperty("grantItems");
            items.arraySize = 1;
            items.GetArrayElementAtIndex(0).stringValue = "ITEM_TORCH";
            Lines(so, "lines", "나", "횃불에 불이 옮겨붙었다!");
            Lines(so, "afterDoneLines", "나", "꺼지면 여기서 다시 붙이면 되겠어.");
        });
        return insp;
    }

    private static Transform Door(Transform root)
    {
        GameObject go = Sprite("Door_Ra", "Hall_Obj_Door_Ra", new Vector3(9f, 10f), root, -1);
        Box(go, new Vector2(1.8f, 0.8f), new Vector2(0f, -0.2f), true);
        SceneDoor door = go.AddComponent<SceneDoor>();
        Edit(door, so =>
        {
            so.FindProperty("promptText").stringValue = "라의 신전으로";
            so.FindProperty("requiredItem").stringValue = "ITEM_TORCH";
            Lines(so, "lockedLines", "나", "문 너머가 새까맣다. 불 없이는 못 들어가겠어.");
            so.FindProperty("walkInto").vector2Value = new Vector2(9f, 10.6f);
            so.FindProperty("nextScene").stringValue = "Puzzle_Test_Ra_Tilemap";
        });
        return go.transform;
    }

    private static void Director(Transform root, HallFlame cube, HallFlame brazier, Inspectable rack, Inspectable brazierInspect, Transform door)
    {
        var go = new GameObject("HallSystems");
        go.transform.SetParent(root, false);
        HallTempDirector d = go.AddComponent<HallTempDirector>();
        Edit(d, so =>
        {
            so.FindProperty("cube").objectReferenceValue = cube;
            so.FindProperty("brazier").objectReferenceValue = brazier;
            so.FindProperty("rack").objectReferenceValue = rack;
            so.FindProperty("brazierInspect").objectReferenceValue = brazierInspect;
            so.FindProperty("door").objectReferenceValue = door;
        });
    }

    // ================= 도우미 =================
    private static HallFlame Flame(Transform root, string name, string off, string on, Vector3 pos, Color color, float radius, float intensity, bool startLit)
    {
        GameObject go = Sprite(name, off, pos, root, 0);
        var lightGo = new GameObject("Light");
        lightGo.transform.SetParent(go.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 1f, 0f);
        Light2D light = lightGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.pointLightOuterRadius = radius;

        HallFlame flame = go.AddComponent<HallFlame>();
        Edit(flame, so =>
        {
            so.FindProperty("target").objectReferenceValue = go.GetComponent<SpriteRenderer>();
            so.FindProperty("offSprite").objectReferenceValue = PrologueAssetFinder.PixelSprite(off, true);
            so.FindProperty("onSprite").objectReferenceValue = PrologueAssetFinder.PixelSprite(on, true);
            so.FindProperty("lightSource").objectReferenceValue = light;
            so.FindProperty("litIntensity").floatValue = intensity;
            so.FindProperty("startLit").boolValue = startLit;
        });
        return flame;
    }

    private static GameObject Sprite(string name, string file, Vector3 pos, Transform parent, int orderOffset)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PrologueAssetFinder.PixelSprite(file, true);
        sr.sortingLayerName = layer;
        sr.sortingOrder = order + orderOffset;
        return go;
    }

    private static void Bounds(Transform root, float w, float h)
    {
        var go = new GameObject("MapBounds");
        go.transform.SetParent(root, false);
        Box(go, new Vector2(w, 0.5f), new Vector2(w * 0.5f, -0.25f), false);
        Box(go, new Vector2(0.5f, h), new Vector2(-0.25f, h * 0.5f), false);
        Box(go, new Vector2(0.5f, h), new Vector2(w + 0.25f, h * 0.5f), false);
    }

    private static BoxCollider2D Box(GameObject go, Vector2 size, Vector2 offset, bool trigger)
    {
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = size;
        box.offset = offset;
        box.isTrigger = trigger;
        return box;
    }

    private static void Lines(SerializedObject so, string field, string speaker, string text)
    {
        SerializedProperty arr = so.FindProperty(field);
        arr.arraySize = 1;
        arr.GetArrayElementAtIndex(0).FindPropertyRelative("speaker").stringValue = speaker;
        arr.GetArrayElementAtIndex(0).FindPropertyRelative("text").stringValue = text;
    }

    private static void Edit(Object target, Action<SerializedObject> change)
    {
        var so = new SerializedObject(target);
        change(so);
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
