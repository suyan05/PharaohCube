using UnityEditor;
using UnityEngine;

// H3 우자트 복원 화면 그림 (HorusUiArt가 부름)
// 석판 H3_02(윤곽)를 720 x 540 크기로 (0, 80)에 놓았을 때, 각 부위가 그림에서 있는 자리와 크기
// 순서 = 퍼즐 조각 순서 (1/2, 1/4, 1/8, 1/16, 1/32, 1/64). 정답(어느 조각이 어느 자리) 데이터는 그대로
// [확인 필요] 자리는 그림을 눈으로 재서 넣은 값 -> Unity에서 조금 어긋나면 Inspector의 Piece Art 칸에서 다듬기
public static class HorusWedjatArt
{
    private struct PieceArt
    {
        public string art;
        public Vector2 pos;
        public Vector2 size;

        public PieceArt(string art, float x, float y, float w, float h)
        {
            this.art = art;
            pos = new Vector2(x, y);
            size = new Vector2(w, h);
        }
    }

    private static readonly PieceArt[] Pieces =
    {
        new PieceArt("H3/H3_08", 96f, 116f, 100f, 60f),     // 1/2  눈 앞쪽(코 쪽) 흰자 = 눈동자 오른쪽
        new PieceArt("H3/H3_06", 0f, 131f, 100f, 100f),     // 1/4  눈동자
        new PieceArt("H3/H3_03", 0f, 209f, 484f, 100f),     // 1/8  눈썹
        new PieceArt("H3/H3_07", -90f, 116f, 92f, 62f),     // 1/16 눈 뒤쪽(귀 쪽) 흰자 = 눈동자 왼쪽
        new PieceArt("H3/H3_05", -118f, -38f, 253f, 130f),  // 1/32 나선 꼬리
        new PieceArt("H3/H3_04", -2f, 46f, 478f, 270f)      // 1/64 눈물선 (눈 테두리 + 아래로 떨어지는 선)
    };

    public static int Apply()
    {
        WedjatRestoreView view = Object.FindFirstObjectByType<WedjatRestoreView>(FindObjectsInactive.Include);
        if (view == null) return 0;
        var so = new SerializedObject(view);
        HorusArtSetup.SetSprite(so, "tabletSprite", "H3/H3_02");
        HorusArtSetup.SetSprite(so, "tabletDoneSprite", "H3/H3_01");
        HorusArtSetup.SetVector(so, "tabletCenter", new Vector2(0f, 80f));
        HorusArtSetup.SetVector(so, "tabletSize", new Vector2(720f, 540f));
        HorusArtSetup.SetFloat(so, "trayScale", 0.45f);
        FillPieces(HorusArtSetup.Prop(so, "pieceArt"));
        so.ApplyModifiedPropertiesWithoutUndo();
        return 1;
    }

    private static void FillPieces(SerializedProperty list)
    {
        if (list == null) return;
        list.arraySize = Pieces.Length;
        for (int i = 0; i < Pieces.Length; i++)
        {
            SerializedProperty e = list.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("sprite").objectReferenceValue = HorusArtSetup.Art(Pieces[i].art);
            e.FindPropertyRelative("slotPos").vector2Value = Pieces[i].pos;
            e.FindPropertyRelative("size").vector2Value = Pieces[i].size;
        }
    }
}
