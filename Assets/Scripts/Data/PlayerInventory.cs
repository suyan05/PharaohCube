using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    // 0: 라, 1: 아누비스, 2: 바스테트, 3: 소베크, 5: 호루스
    public bool[] hasPlate = new bool[6];

    public void AddPlate(int faceIndex)
    {
        if (faceIndex < 0 || faceIndex >= hasPlate.Length)
        {
            Debug.LogWarning($"잘못된 faceIndex: {{faceIndex}}\"");
            return;
        }

        hasPlate[faceIndex] = true;
        Debug.Log($"{faceIndex}번 형판 획득! 현재 인벤토리: {string.Join(",", hasPlate)}");
    }

    [ContextMenu("테스트: 0번 형판 획득")]
    void TestAddPlate0() => AddPlate(0);

    public void LoadPlateData(bool[] data)
    {
        hasPlate = data;
        Debug.Log($"[PlayerInventory] 형판 데이터 불러옴: {string.Join(",", hasPlate)}");
    }

}
