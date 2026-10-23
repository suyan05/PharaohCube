using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// 꿈 후처리 Volume 만들기 (PrologueSphinxBuilder가 부름)
// - 프로필 에셋: Assets/Settings/Prologue/DreamVolume.asset (있으면 그대로 다시 씀 -> 손으로 고친 값 유지)
//   Bloom(번짐), Vignette(보라 테두리), Chromatic Aberration(색 갈라짐), Lens Distortion(일렁임),
//   Film Grain(아주 약한 입자), Color Adjustments(살짝 보라 쪽으로)
// - 씬에 Global Volume + DreamFX를 붙임
// - Main Camera의 Post Processing을 켬 (꺼져 있으면 효과가 하나도 안 보임)
public static class DreamVolumeBuilder
{
    private const string ProfilePath = "Assets/Settings/Prologue/DreamVolume.asset";

    public static void Create(Transform parent)
    {
        try
        {
            var go = new GameObject("DreamFX");
            go.transform.SetParent(parent, false);

            Volume volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = LoadOrCreateProfile();
            go.AddComponent<DreamFX>();
        }
        catch (Exception e)
        {
            Debug.LogError($"[DreamVolumeBuilder] 꿈 후처리 만들기 오류: {e}");
        }
    }

    public static void EnablePostProcessing(Camera cam)
    {
        UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
        if (data == null) return;

        Undo.RecordObject(data, "카메라 후처리");
        data.renderPostProcessing = true;
        EditorUtility.SetDirty(data);
    }

    // ================= 프로필 =================
    private static VolumeProfile LoadOrCreateProfile()
    {
        VolumeProfile existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (existing != null) return existing;

        PrologueAssetFinder.EnsureFolder("Assets/Settings/Prologue");
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, ProfilePath);

        AddBloom(profile);
        AddVignette(profile);
        AddLens(profile);
        AddColor(profile);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    private static void AddBloom(VolumeProfile profile)
    {
        Bloom bloom = Add<Bloom>(profile);
        bloom.threshold.Override(0.75f);
        bloom.intensity.Override(1.1f);
        bloom.scatter.Override(0.75f);
        bloom.tint.Override(new Color(0.85f, 0.78f, 1f));
    }

    private static void AddVignette(VolumeProfile profile)
    {
        Vignette vignette = Add<Vignette>(profile);
        vignette.color.Override(new Color(0.16f, 0.09f, 0.28f));
        vignette.intensity.Override(0.4f);
        vignette.smoothness.Override(0.55f);
    }

    private static void AddLens(VolumeProfile profile)
    {
        ChromaticAberration chroma = Add<ChromaticAberration>(profile);
        chroma.intensity.Override(0.12f);

        LensDistortion lens = Add<LensDistortion>(profile);
        lens.intensity.Override(-0.06f);
        lens.scale.Override(1.02f); // 일렁일 때 화면 가장자리가 비어 보이지 않게 살짝 확대

        FilmGrain grain = Add<FilmGrain>(profile);
        grain.type.Override(FilmGrainLookup.Thin1);
        grain.intensity.Override(0.15f);
    }

    private static void AddColor(VolumeProfile profile)
    {
        ColorAdjustments color = Add<ColorAdjustments>(profile);
        color.saturation.Override(-10f);
        color.colorFilter.Override(new Color(0.95f, 0.92f, 1f));
    }

    // 프로필 에셋 안에 효과를 하위 에셋으로 저장 (이렇게 해야 씬을 다시 열어도 남아 있음)
    private static T Add<T>(VolumeProfile profile) where T : VolumeComponent
    {
        T component = profile.Add<T>(true);
        component.name = typeof(T).Name;
        component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(component, profile);
        return component;
    }
}
