using System;
using System.Collections;
using UnityEngine;

// 퍼즐을 다 풀었을 때의 공통 반응: 맑은 종소리 + 금빛 번쩍 (그 뒤 퍼즐 판은 각 Station이 스스로 닫음)
// - PuzzleSolvedFx.Play();
// - PuzzleCloseKey.ExtraPressed : 퍼즐 판을 닫는 추가 키. ESC는 설정 창 키라서 뺐고, 닫기는 E 하나로 통일 (지금은 항상 false)
public static class PuzzleSolvedFx
{
    public const float HoldSeconds = 1.8f; // 다 푼 모습을 보여주고 닫을 때까지

    public static void Play()
    {
        try
        {
            GameAudio.Play("sfx_puzzle_solved", 0.8f);
            Cine.Run(ScreenTintUI.Flash(new Color(1f, 0.86f, 0.45f, 0.35f), 0.08f, 0.1f, 0.6f));
        }
        catch (Exception e)
        {
            Debug.LogError($"[PuzzleSolvedFx] 완료 효과 오류: {e.Message}");
        }
    }
}

public static class PuzzleCloseKey
{
    public static bool ExtraPressed => false;
}
