using UnityEngine;

public class CubeInteraction : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Camera mainCamera;

    private void Awake()
    {
        // Inspector에서 안 넣어놨으면 자동으로 메인 카메라를 찾음
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) // 마우스 왼쪽 클릭
        {
            TryClickCube();
        }
    }

    private void TryClickCube()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject == gameObject)
            {
                OnCubeClicked();
            }
        }
    }

    private void OnCubeClicked()
    {
        Debug.Log("큐브가 클릭됐습니다! UI 오픈 요청을 보냅니다.");
        CubeUIManager.Instance.OpenUI();
    }
}