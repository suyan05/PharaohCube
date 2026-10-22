using UnityEngine;

// 연출 도구가 코루틴/소리를 돌릴 때 쓰는 숨은 오브젝트 (씬에 놓을 필요 없음)
// 씬이 바뀌면 같이 사라지고 다음 씬에서 필요할 때 다시 생김
public class CineRunner : MonoBehaviour
{
    private static CineRunner instance;

    public static CineRunner Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<CineRunner>();
            if (instance == null) instance = new GameObject("CineRunner").AddComponent<CineRunner>();
            return instance;
        }
    }
}
