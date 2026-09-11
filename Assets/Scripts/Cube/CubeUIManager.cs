using UnityEngine;

public class CubeUIManager : MonoBehaviour
{
    // 싱글톤
    public static CubeUIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //TODO(강태호) : 실제 면 확대 UI 여는 코드 작성 예정
    public void OpenUI()
    {
        Debug.Log("[CubeUIManager] OpenUI() 호출됨 - 여기서 실제 UI 패널을 열어야 함");
        // Ex: uiPanel.SetActive(true);
    }
}
