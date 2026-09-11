using UnityEngine;

public class CubeSealManager : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private CubeFaceState cubeFaceState;

    /*OnEnable/OnDisable을 쓴 이유
     Awake or Start 을 안 쓴건, 이벤트 구독을 켜질 때 연경하고 꺼질 때 해제하는 습관 때문.
    나중에 오브젝트가 비활 도ㅒㅆ다가 다시 켜질 때 이벤트가 중복으로 연결되거나, 오브젝트가 파괴되도
    이벤트가 계속 남아서 에러나는 경우 방지.*/

    public bool IsSealUnlocked { get; private set; }


    private void OnEnable()
    {
        cubeFaceState.OnAllFacesActivated += HandleAllFacesActivated;
    }

    private void OnDisable()
    {
        cubeFaceState.OnAllFacesActivated -= HandleAllFacesActivated;
    }

    private void HandleAllFacesActivated()
    {
        SetSealUnlocked(true);
        Debug.Log("[CubeSealManager] 6개 면 전부 활성화 확인 → 최종 봉인 퍼즐 오픈 요청");
        // TODO(강태호)작업 완료: 최종 봉인 UI 오픈 연결
        if (FinalPuzzleUI.Instance != null)
        {
            FinalPuzzleUI.Instance.Open();
        }
        else
        {
            Debug.LogWarning("[CubeSealManager] FinalPuzzleUI 인스턴스를 찾을 수 없습니다!");
        }
    }

    // 저장된 데이터를 불러올 때 사용 (이벤트 재발생 없이 값만 설정)
    public void SetSealUnlocked(bool unlocked)
    {
        IsSealUnlocked = unlocked;
    }

    [ContextMenu("테스트: 최종 봉인 체크 수동 실행")]
    void TestCheck()
    {
        HandleAllFacesActivated();
    }
}