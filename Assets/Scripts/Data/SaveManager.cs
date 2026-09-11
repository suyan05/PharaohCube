using UnityEngine;

public class SaveManager : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private CubeFaceState cubeFaceState;
    [SerializeField] private CubeSealManager cubeSealManager;

    private const string PlateKeyPrefix = "Plate_";
    private const string FaceKeyPrefix = "Face_";
    private const string SealKey = "FinalSealUnlocked";

    public void SaveGame()
    {
        for (int i = 0; i < playerInventory.hasPlate.Length; i++)
        {
            PlayerPrefs.SetInt(PlateKeyPrefix + i, playerInventory.hasPlate[i] ? 1 : 0);
        }

        for (int i = 0; i < cubeFaceState.faceActivated.Length; i++)
        {
            PlayerPrefs.SetInt(FaceKeyPrefix + i, cubeFaceState.faceActivated[i] ? 1 : 0);
        }

        PlayerPrefs.SetInt(SealKey, cubeSealManager.IsSealUnlocked ? 1 : 0);

        PlayerPrefs.Save();
        Debug.Log("[SaveManager] 게임 저장 완료");
    }

    public void LoadGame()
    {
        bool[] loadedPlates = new bool[6];
        bool[] loadedFaces = new bool[6];

        for (int i = 0; i < 6; i++)
        {
            loadedPlates[i] = PlayerPrefs.GetInt(PlateKeyPrefix + i, 0) == 1;
            loadedFaces[i] = PlayerPrefs.GetInt(FaceKeyPrefix + i, 0) == 1;
            /*키,0 에서 두 번째 0은 저장된 값이 없으면 0(false)을 쓴다는 뜻. 
            게임을 처음 실행했을 때(저장된 데이터 x)도 에러없이 전부 false로 알잘딱깔센 시작.*/
        }

        playerInventory.LoadPlateData(loadedPlates);
        cubeFaceState.LoadFaceData(loadedFaces);

        bool sealUnlocked = PlayerPrefs.GetInt(SealKey, 0) == 1;
        cubeSealManager.SetSealUnlocked(sealUnlocked);

        Debug.Log("[SaveManager] 게임 불러오기 완료");
    }

    [ContextMenu("테스트: 저장")]
    void TestSave() => SaveGame();

    [ContextMenu("테스트: 불러오기")]
    void TestLoad() => LoadGame();

    [ContextMenu("테스트: 저장 데이터 전체 삭제")]
    void TestClear()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("[SaveManager] 저장 데이터 전체 삭제됨");
    }
}