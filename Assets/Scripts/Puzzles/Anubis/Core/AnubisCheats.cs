using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

// 아누비스 방 테스트용 치트키 (Managers에 부착, 에디터/개발 빌드에서만 동작)
// F1: 지금 열린 퍼즐 자동 풀기 / F2: 서브 퍼즐 보상 전부 지급 / F3: 양피지 조각 전부 줍기 / F4: 도움말
// 퍼즐 코드를 수정하지 않기 위해 private 함수는 리플렉션(이름으로 찾아 호출)으로 실행함
public class AnubisCheats : MonoBehaviour
{
    [Header("치트키")]
    [Tooltip("지금 열려 있는 퍼즐 자동 풀기")]
    [SerializeField] private Key solveOpenPuzzleKey = Key.F1;
    [Tooltip("서브 퍼즐 1~4 보상 전부 지급 (제단 해금 / 석문 열림)")]
    [SerializeField] private Key grantAllRewardsKey = Key.F2;
    [Tooltip("씬의 양피지 조각 전부 줍기")]
    [SerializeField] private Key collectAllScrollsKey = Key.F3;
    [Tooltip("도움말 표시")]
    [SerializeField] private Key helpKey = Key.F4;

    [Header("설정")]
    [Tooltip("체크하면 정식 빌드에서도 동작 (출시 전 반드시 끄기)")]
    [SerializeField] private bool enableInReleaseBuild = false;
    [Tooltip("F2로 보상을 줄 때 간격 (초). 제단 해금 메시지를 하나씩 보기 위함")]
    [Range(0f, 2f)]
    [SerializeField] private float rewardInterval = 0.6f;

    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    // 3x3 마방진 정답 8가지 (가운데 5 고정)
    private static readonly int[][] MagicSquares =
    {
        new[] { 2, 7, 6, 9, 5, 1, 4, 3, 8 },
        new[] { 2, 9, 4, 7, 5, 3, 6, 1, 8 },
        new[] { 4, 3, 8, 9, 5, 1, 2, 7, 6 },
        new[] { 4, 9, 2, 3, 5, 7, 8, 1, 6 },
        new[] { 6, 1, 8, 7, 5, 3, 2, 9, 4 },
        new[] { 6, 7, 2, 1, 5, 9, 8, 3, 4 },
        new[] { 8, 1, 6, 3, 5, 7, 4, 9, 2 },
        new[] { 8, 3, 4, 1, 5, 9, 6, 7, 2 },
    };

    private bool CheatsAllowed => Debug.isDebugBuild || enableInReleaseBuild;

    private void Update()
    {
        if (!CheatsAllowed) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb[solveOpenPuzzleKey].wasPressedThisFrame) SolveOpenPuzzle();
        else if (kb[grantAllRewardsKey].wasPressedThisFrame) StartCoroutine(GrantAllRewards());
        else if (kb[collectAllScrollsKey].wasPressedThisFrame) CollectAllScrolls();
        else if (kb[helpKey].wasPressedThisFrame) ShowHelp();
    }

    // ================= F1: 열린 퍼즐 풀기 =================
    // FindFirstObjectByType은 활성 오브젝트만 찾으므로 '지금 열린 패널'의 퍼즐만 잡힘
    private void SolveOpenPuzzle()
    {
        // 실전 퍼즐
        if (TrySolve<SandPuzzleReal>(p => Call(p, "TestSolve"))) return;
        if (TrySolve<GearPuzzleReal>(p => Call(p, "TestSetAnswer") && Call(p, "OperateCrank"))) return;
        if (TrySolve<LeverPuzzleReal>(p => Call(p, "TestSetAnswer") && Call(p, "CheckBalance"))) return;
        if (TrySolve<SymbolSquarePuzzleReal>(p => Call(p, "TestFillAnswer") && Call(p, "CheckSolution"))) return;

        // 튜토리얼 퍼즐
        if (TrySolve<LionSandDecanter>(SolveTutorialSand)) return;
        if (TrySolve<GearPulleyPuzzle>(SolveTutorialGear)) return;
        if (TrySolve<AnubisLeverPuzzle>(SolveTutorialLever)) return;
        if (TrySolve<MaatMagicSquarePuzzle>(SolveTutorialMagic)) return;

        // 메인 저울
        if (TrySolve<AnubisPuzzleManager2D>(SolveMainScale)) return;

        Notify("열려 있는 퍼즐이 없습니다. 제단에서 E로 퍼즐을 연 뒤 F1을 누르세요.");
    }

    private bool TrySolve<T>(Func<T, bool> solver) where T : MonoBehaviour
    {
        var puzzle = FindFirstObjectByType<T>();
        if (puzzle == null) return false;

        bool ok = solver(puzzle);
        Notify(ok ? $"[치트] {typeof(T).Name} 자동 풀이 실행" : $"[치트] {typeof(T).Name} 자동 풀이 실패 (Console 확인)");
        return true;
    }

    // 튜토리얼 모래: 8L 4 / 5L 4 / 3L 0
    private bool SolveTutorialSand(LionSandDecanter p)
    {
        return Set(p, "isPouring", false)
            && Set(p, "curA", 4) && Set(p, "curB", 4) && Set(p, "curC", 0)
            && Call(p, "UpdateVisualsInstant")
            && Call(p, "CheckClearCondition");
    }

    // 튜토리얼 기어: 12T → 24T (2배) x 움직 도르래 (2배) = 4배
    private bool SolveTutorialGear(GearPulleyPuzzle p)
    {
        return Set(p, "isOperating", false)
            && Set(p, "driverIndex", 0) && Set(p, "drivenIndex", 1) && Set(p, "pulleyMultiplier", 2)
            && Call(p, "UpdateVisuals")
            && Call(p, "OperateCrank");
    }

    // 튜토리얼 지렛대: 추 2개(2kg) x 2칸(150px) = 600 = 심장 4kg x 150px
    private bool SolveTutorialLever(AnubisLeverPuzzle p)
    {
        return Set(p, "isTesting", false)
            && Set(p, "currentDistance", 2) && Set(p, "currentWeightCount", 2)
            && Call(p, "UpdateVisuals")
            && Call(p, "CheckLeverBalance");
    }

    // 튜토리얼 마방진: 고정칸과 맞는 정답을 골라 채움
    private bool SolveTutorialMagic(MaatMagicSquarePuzzle p)
    {
        var board = Get<int[]>(p, "boardValues");
        var isFixed = Get<bool[]>(p, "isFixed");
        if (board == null || isFixed == null) return false;

        int[] answer = FindMagicSquare(board, isFixed);
        if (answer == null)
        {
            Debug.LogWarning("[AnubisCheats] 고정칸과 맞는 마방진 정답이 없습니다.");
            return false;
        }

        for (int i = 0; i < 9; i++) board[i] = answer[i];
        Set(p, "selectedSlotIndex", -1);
        return Call(p, "UpdateVisuals") && Call(p, "CheckSolution");
    }

    private static int[] FindMagicSquare(int[] board, bool[] isFixed)
    {
        foreach (var sq in MagicSquares)
        {
            bool match = true;
            for (int i = 0; i < 9 && match; i++)
                if (isFixed[i] && board[i] != sq[i]) match = false;
            if (match) return sq;
        }
        return null;
    }

    // 메인 저울: 왼쪽 접시를 비우고 심장 + 황금 단지만 올린 뒤 심판 레버
    private bool SolveMainScale(AnubisPuzzleManager2D p)
    {
        var leftPan = Get<AnubisUIScalePan>(p, "leftPan");
        if (leftPan == null) return false;

        AnubisUIDragItem heart = null, gold = null;
        foreach (var item in FindObjectsByType<AnubisUIDragItem>(FindObjectsSortMode.None))
        {
            if (item.isHeart) heart = item;
            else if (item.isGold) gold = item;
            else if (item.CurrentPan == leftPan) item.ReturnToOriginalSlot(); // 오답 유물 내리기
        }

        if (heart == null || gold == null)
        {
            Debug.LogWarning("[AnubisCheats] 심장(isHeart) 또는 황금 단지(isGold) 유물을 찾지 못했습니다.");
            return false;
        }

        PutOnPan(heart, leftPan);
        PutOnPan(gold, leftPan);
        p.OnPullJudgmentLever();
        return true;
    }

    private static void PutOnPan(AnubisUIDragItem item, AnubisUIScalePan pan)
    {
        if (item.CurrentPan == pan) return;
        if (item.CurrentPan != null) item.CurrentPan.RemoveItem(item);
        pan.AddItem(item);
    }

    // ================= F2: 서브 퍼즐 보상 전부 지급 =================
    private IEnumerator GrantAllRewards()
    {
        if (ItemInventory.Instance == null)
        {
            Notify("ItemInventory가 씬에 없습니다 (Managers 확인).");
            yield break;
        }

        string[] rewards = { AnubisItemIds.GoldJar, AnubisItemIds.ClayPot, AnubisItemIds.Obsidian, AnubisItemIds.BronzeBowl };
        Notify("[치트] 서브 퍼즐 보상 4개 지급");

        foreach (var id in rewards)
        {
            ItemInventory.Instance.Grant(id); // 이미 있으면 무시됨
            if (rewardInterval > 0f) yield return new WaitForSeconds(rewardInterval);
        }
    }

    // ================= F3: 양피지 조각 전부 줍기 =================
    private void CollectAllScrolls()
    {
        var pieces = FindObjectsByType<AnubisScrollPiece>(FindObjectsSortMode.None);
        if (pieces.Length == 0)
        {
            Notify("씬에 남은 양피지 조각이 없습니다.");
            return;
        }

        foreach (var piece in pieces) piece.Interact(null);
        Notify($"[치트] 양피지 조각 {pieces.Length}개 획득");
    }

    // ================= F4: 도움말 =================
    private void ShowHelp()
    {
        Notify($"[치트키]  {solveOpenPuzzleKey}: 열린 퍼즐 풀기   {grantAllRewardsKey}: 보상 전부 지급\n" +
               $"{collectAllScrollsKey}: 양피지 전부 줍기   {helpKey}: 도움말", 6f);
    }

    // ================= 공통 =================
    private static void Notify(string message, float duration = 3f)
    {
        Debug.Log($"<color=orange>{message}</color>");
        AnubisToast.Show(message, duration);
    }

    // private 함수 호출 (매개변수 없는 함수만)
    private static bool Call(object target, string methodName)
    {
        var m = target.GetType().GetMethod(methodName, Flags, null, Type.EmptyTypes, null);
        if (m == null)
        {
            Debug.LogWarning($"[AnubisCheats] {target.GetType().Name}에 '{methodName}()' 함수가 없습니다. 스크립트 버전을 확인하세요.");
            return false;
        }
        m.Invoke(target, null);
        return true;
    }

    // private 변수 값 바꾸기
    private static bool Set(object target, string fieldName, object value)
    {
        var f = target.GetType().GetField(fieldName, Flags);
        if (f == null)
        {
            Debug.LogWarning($"[AnubisCheats] {target.GetType().Name}에 '{fieldName}' 변수가 없습니다.");
            return false;
        }
        f.SetValue(target, value);
        return true;
    }

    // private 변수 값 읽기
    private static T Get<T>(object target, string fieldName) where T : class
    {
        var f = target.GetType().GetField(fieldName, Flags);
        if (f == null)
        {
            Debug.LogWarning($"[AnubisCheats] {target.GetType().Name}에 '{fieldName}' 변수가 없습니다.");
            return null;
        }
        return f.GetValue(target) as T;
    }
}