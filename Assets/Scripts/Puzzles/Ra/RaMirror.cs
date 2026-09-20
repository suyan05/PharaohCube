using UnityEngine;

public class RaMirror : MonoBehaviour
{
    [Header("회전 설정")]
    [SerializeField] private float rotateStep = 45f;
    [SerializeField] private bool isRepaired = true;

    public bool IsRepaired => isRepaired;

    private void OnMouseDown()
    {
        if (!isRepaired)
        {
            RepairMirror();
            return;
        }

        RotateRight();
    }

    public void RotateLeft()
    {
        transform.Rotate(0f, -rotateStep, 0f, Space.World);
    }

    public void RotateRight()
    {
        transform.Rotate(0f, rotateStep, 0f, Space.World);
    }

    public void RepairMirror()
    {
        isRepaired = true;
        Debug.Log($"<color=cyan>[서브 퍼즐 클리어]</color> {gameObject.name}에 파편을 끼워 복구했습니다!");
    }
}