using UnityEngine;

public class TestPuzzle : PuzzleBase
{
    public override void Submit()
    {
        if (Validate())
        {
            OnSuccessInternal();
        }
        else
        {
            OnFailInternal();
        }
    }

    protected override bool Validate()
    {
        return true; // 테스트용: 항상 성공
    }

    [ContextMenu("테스트: Open 호출")]
    void TestOpen() => Open();

    [ContextMenu("테스트: Submit 호출")]
    void TestSubmit() => Submit();
}