using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

// 매의 회랑 (호루스의 숨은 공간) - 같은 씬 안에서 열림
// - 처음엔 회랑 타일이 아예 없음 (탑다운에서 "방이 하나 더 있네"가 안 보이게). 서쪽 벽 틈(조사 대사)만 있음
// - 우자트 부적을 매 석상에 끼우면(HorusFalconStatue) 연출: 석상 눈 빛남 -> 매가 서쪽 벽으로 날아감
//   -> 쿠르릉 + 화면 흔들림 + 돌가루 -> 벽이 사라지고 회랑 타일이 생김 -> 카메라 범위도 넓어짐
// - 열린 상태는 플래그로 저장 -> 씬에 다시 들어오면 바로 열린 채로
// [확인 필요] 플래그 이름 F_HORUS_HIDDEN_OPEN 은 히든 아이템 담당자와 맞추기
public class HorusHiddenGallery : MonoBehaviour
{
    // 회랑 칸 (맨 윗줄 = y 13, 맨 왼칸 = x -9). '#' 벽 윗면, 'B' 벽 앞면, 'g' 바닥, 'H' 지금 벽 -> 바닥(통로), ' ' 그대로
    private const int Left = -9;
    private const int Top = 13;

    private static readonly string[] Rows =
    {
        "#########",
        "#BBBBBBB ",
        "#ggggggg ",
        "#ggggggg ",
        "#ggggggg ",
        "#gggggggH",
        "#gggggggH",
        "#ggggggg ",
        "#ggggggg ",
        "#########"
    };

    [Header("타일맵 (빌더가 채움)")]
    [SerializeField] private Tilemap ground;
    [SerializeField] private Tilemap wall;
    [SerializeField] private TileBase wallTop;
    [SerializeField] private TileBase wallFace;
    [SerializeField] private TileBase[] floors;

    [Header("열리면 켜질 것 / 사라질 것")]
    [SerializeField] private GameObject contents;
    [SerializeField] private GameObject wallCrack;

    [Header("연출")]
    [SerializeField] private Transform statue;
    [SerializeField] private Vector3 eyeOffset = new Vector3(0.12f, 0.25f, 0f);
    [SerializeField] private Vector2 passage = new Vector2(-0.5f, 8f);
    [SerializeField] private Sprite falconSprite;
    [SerializeField] private Sprite emoteSprite;

    [Header("열림 플래그")]
    [SerializeField] private string openFlag = "F_HORUS_HIDDEN_OPEN";

    public bool IsOpen => GameManager.Instance != null && GameManager.Instance.HasFlag(openFlag);

    private bool busy;
    private bool painted;
    private static readonly Color Gold = new Color(1f, 0.82f, 0.35f, 0.9f);

    private void Start()
    {
        try
        {
            if (IsOpen) OpenNow();
            else if (contents != null) contents.SetActive(false);
        }
        catch (Exception e)
        {
            Debug.LogError($"[매의 회랑] 준비 오류: {e}");
        }
    }

    // HorusFalconStatue가 부름 (부적은 석상이 이미 씀)
    public void PlayOpenCutscene()
    {
        if (busy || IsOpen) return;
        busy = true;
        Cine.Run(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        Cine.Begin(true);
        HorusFx.MatchPlayer();
        yield return Cine.Say("나", "비어 있는 눈에... 되찾은 호루스의 눈을 끼워 보자.");
        yield return EyeAwakens();
        Vector3 wallPos = new Vector3(passage.x, passage.y, 0f);
        CineCamera.Focus((statue.position + wallPos) * 0.5f);
        CineCamera.Zoom(5f);
        yield return HorusFx.Fly(falconSprite, statue.position + Vector3.up, wallPos + Vector3.up * 0.5f, 1.6f, 1.1f, Color.white, HorusFx.Above, 1.6f);
        CineCamera.Focus(wallPos);
        yield return WallBreaks(wallPos);
        yield return Cine.Say("나", "벽이... 무너졌어! 저 안에 방이 있었잖아?");
        CineCamera.Release();
        Cine.End();
        busy = false;
    }

    // 석상 눈이 금빛으로 번쩍 + 띵 소리
    private IEnumerator EyeAwakens()
    {
        GameAudio.Play("sfx_discover", 0.8f);
        Vector3 eye = statue.position + eyeOffset;
        StartCoroutine(HorusFx.Pulse(eye, 1.4f, Gold, 1.8f));
        HorusFx.StarBurst(eye, new Vector2(0.3f, 0.3f), 10, Gold);
        CineCamera.Focus(statue.position);
        CineCamera.Zoom(4f);
        yield return ScreenTintUI.Flash(new Color(1f, 0.85f, 0.5f, 0.45f), 0.06f, 0.1f, 0.5f);
        yield return Cine.Wait(0.6f);
    }

    // 쿠르릉 -> 돌가루 -> 벽 사라짐 + 회랑 생김 -> 하얗게 번쩍 + 내 머리 위 "!"
    private IEnumerator WallBreaks(Vector3 wallPos)
    {
        UnlockRumble.Play();
        yield return CineCamera.Rumble(1.3f, 0.04f, 0.22f);
        HorusFx.StarBurst(wallPos, new Vector2(0.6f, 1.2f), 28, new Color(0.85f, 0.75f, 0.6f, 1f));
        OpenNow();
        StartCoroutine(HorusFx.Emote(CineActor.Player(), emoteSprite, 1.2f));
        yield return ScreenTintUI.Flash(new Color(1f, 1f, 1f, 0.6f), 0.04f, 0.08f, 0.6f);
        yield return Cine.Wait(0.5f);
    }

    // 실제로 여는 부분 (연출 없이 바로 = 다시 들어왔을 때)
    public void OpenNow()
    {
        if (!painted) Paint();
        painted = true;
        if (contents != null) contents.SetActive(true);
        if (wallCrack != null) wallCrack.SetActive(false);
        GameManager gm = GameManager.Instance;
        if (gm != null && !gm.HasFlag(openFlag)) gm.SetFlag(openFlag);
        Camera cam = Camera.main;
        CameraFollow2D follow = cam != null ? cam.GetComponent<CameraFollow2D>() : null;
        if (follow != null) follow.RefreshBounds(); // 넓어진 바닥까지 카메라가 따라감
    }

    private void Paint()
    {
        if (ground == null || wall == null)
        {
            Debug.LogError("[매의 회랑] Ground / Wall 타일맵 연결 필요");
            return;
        }
        var rng = new System.Random(13);
        for (int r = 0; r < Rows.Length; r++)
        {
            for (int i = 0; i < Rows[r].Length; i++) PaintCell(new Vector3Int(Left + i, Top - r, 0), Rows[r][i], rng);
        }
    }

    private void PaintCell(Vector3Int cell, char c, System.Random rng)
    {
        if (c == '#') wall.SetTile(cell, wallTop);
        else if (c == 'B') wall.SetTile(cell, wallFace);
        else if (c == 'g' || c == 'H')
        {
            wall.SetTile(cell, null);
            if (floors != null && floors.Length > 0) ground.SetTile(cell, floors[rng.Next(floors.Length)]);
        }
    }
}
