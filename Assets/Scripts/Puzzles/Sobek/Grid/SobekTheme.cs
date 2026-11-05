using UnityEngine;

// 소베크 칸 퍼즐 그림 묶음 (모든 판이 같이 씀)
// Tools/Sobek 메뉴가 임시 그림(Assets/Art/Sobek/Placeholder)으로 채움 -> 아트가 나오면 여기 칸만 바꾸면 전체 반영
[CreateAssetMenu(fileName = "SobekTheme", menuName = "PharaohCube/Sobek/Theme")]
public class SobekTheme : ScriptableObject
{
    [Header("바닥 타일 (32x32)")]
    public Sprite floor;
    public Sprite wall;
    public Sprite water;
    public Sprite pit;
    public Sprite stone;
    public Sprite stairLow;   // 계단 0-1
    public Sprite stairHigh;  // 계단 1-2
    public Sprite raft;
    public Sprite island;
    public Sprite bridgeUp;
    public Sprite bridgeDown;

    [Header("발판 (일반 / 눌림은 색으로 표시)")]
    public Sprite plateRotate;
    public Sprite plateLift;
    public Sprite plateWater;
    public Sprite plateMirror;

    [Header("오브젝트")]
    public Sprite egg;
    public Sprite goal;
    public Sprite gate;
    public Sprite altar;

    [Header("색 (그림이 흰색 계열이면 이 색이 곱해짐)")]
    public Color floorTop = new Color(0.92f, 0.84f, 0.66f);
    public Color floorLow = new Color(0.78f, 0.66f, 0.45f);
    public Color waterColor = new Color(0.45f, 0.65f, 0.80f);
    public Color submergedTint = new Color(0.55f, 0.72f, 0.88f);
    [Tooltip("승강 발판이 눌렸을 때 밝기")]
    [Range(1f, 2f)] public float pressedBrightness = 1.35f;

    [Header("그리기 순서")]
    public string sortingLayer = "Ground";
    public int baseOrder = -20;
}
