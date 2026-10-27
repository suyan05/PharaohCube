using UnityEngine;
using UnityEngine.UI;

// 라 엔딩 임시 그림 (아트의 라 일러스트가 오기 전까지 도형으로)
// - Rays(): 해 뒤에서 도는 빛줄기 그림 (코드로 한 번 만들어 재사용)
// - Placeholder(): 매 머리 + 태양 원반 + 넓은 목걸이 + 지팡이 실루엣 (역광 느낌)
public static class RaFinaleShapes
{
    private const int RayCount = 16;
    private const int RaySize = 256;

    private static readonly Color Shadow = new Color(0.10f, 0.05f, 0.03f, 1f);
    private static readonly Color Gold = new Color(0.95f, 0.70f, 0.25f, 1f);
    private static readonly Color Lapis = new Color(0.12f, 0.23f, 0.37f, 1f);

    private static Sprite rays;

    // ================= 빛줄기 =================
    public static Sprite Rays()
    {
        if (rays != null) return rays;
        var pixels = new Color32[RaySize * RaySize];
        float half = RaySize * 0.5f;
        for (int i = 0; i < pixels.Length; i++)
        {
            float x = i % RaySize + 0.5f - half;
            float y = i / RaySize + 0.5f - half;
            pixels[i] = new Color32(255, 255, 255, (byte)(RayAlpha(x, y, half) * 255f));
        }
        var tex = new Texture2D(RaySize, RaySize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(pixels);
        tex.Apply();
        rays = Sprite.Create(tex, new Rect(0, 0, RaySize, RaySize), new Vector2(0.5f, 0.5f), 100f);
        return rays;
    }

    // 각도로 줄무늬(부채꼴) + 가운데서 멀어질수록 흐려짐
    private static float RayAlpha(float x, float y, float half)
    {
        float dist = Mathf.Sqrt(x * x + y * y) / half;
        if (dist > 1f) return 0f;
        float angle = Mathf.Atan2(y, x) * RayCount * 0.5f;
        float stripe = Mathf.Clamp01(Mathf.Cos(angle) * 2.2f - 0.6f);
        return stripe * (1f - dist) * Mathf.Clamp01(dist * 4f);
    }

    // ================= 임시 라 실루엣 =================
    public static void Placeholder(RectTransform parent, Font font)
    {
        RectTransform root = UIBuild.Stretch(parent, "RaPlaceholder");
        Body(root);
        Head(root);
        Text label = UIBuild.Text(root, "TempLabel", font, 22, new Vector2(700f, -505f), new Vector2(460f, 40f), new Color(1f, 1f, 1f, 0.45f));
        label.text = "[임시] 라 일러스트 자리 (1920x1080)";
    }

    // 어깨, 몸통, 넓은 목걸이(금 - 청금석 - 금), 지팡이
    private static void Body(RectTransform root)
    {
        Shape(root, "Staff", new Vector2(360f, -150f), new Vector2(22f, 760f), null, Gold, 0f);
        Shape(root, "StaffHead", new Vector2(345f, 235f), new Vector2(70f, 26f), null, Gold, 20f);
        Shape(root, "Torso", new Vector2(0f, -330f), new Vector2(720f, 480f), null, Shadow, 0f);
        Shape(root, "ShoulderL", new Vector2(-250f, -170f), new Vector2(300f, 300f), UIBuild.Circle(), Shadow, 0f);
        Shape(root, "ShoulderR", new Vector2(250f, -170f), new Vector2(300f, 300f), UIBuild.Circle(), Shadow, 0f);
        Shape(root, "CollarGold1", new Vector2(0f, -125f), new Vector2(560f, 64f), null, Gold, 0f);
        Shape(root, "CollarLapis", new Vector2(0f, -170f), new Vector2(520f, 36f), null, Lapis, 0f);
        Shape(root, "CollarGold2", new Vector2(0f, -205f), new Vector2(470f, 30f), null, Gold, 0f);
        Shape(root, "Neck", new Vector2(0f, -40f), new Vector2(170f, 150f), null, Shadow, 0f);
    }

    // 매 머리 (왼쪽을 봄) + 부리 + 눈 + 이마의 코브라(우라에우스)
    private static void Head(RectTransform root)
    {
        Shape(root, "Head", new Vector2(0f, 70f), new Vector2(250f, 250f), UIBuild.Circle(), Shadow, 0f);
        Shape(root, "Beak", new Vector2(-125f, 40f), new Vector2(150f, 64f), null, Shadow, -28f);
        Shape(root, "Eye", new Vector2(-45f, 95f), new Vector2(30f, 30f), UIBuild.Circle(), Gold, 0f);
        Shape(root, "EyeLine", new Vector2(-20f, 78f), new Vector2(80f, 8f), null, Gold, -10f);
        Shape(root, "Uraeus", new Vector2(0f, 215f), new Vector2(56f, 56f), UIBuild.Circle(), Gold, 0f);
    }

    private static Image Shape(RectTransform parent, string name, Vector2 pos, Vector2 size, Sprite sprite, Color color, float angle)
    {
        Image img = UIBuild.Image(parent, name, pos, size, sprite, color);
        img.color = color;
        img.preserveAspect = false;
        img.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
        return img;
    }
}
