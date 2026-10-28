using UnityEngine;

// 앞뒤 자동 정렬(YSortSystem)에서 이 오브젝트만 따로 정하고 싶을 때 붙이는 표시
// - Auto: 자동 (기본. 바닥처럼 큰 그림 = 항상 아래, 정렬 순서 -5 이하 = 항상 아래, 20 이상 = 항상 위, 나머지 = 발밑 높이로)
// - Sort: 무조건 발밑 높이로 앞뒤 정렬
// - AlwaysBelow: 플레이어보다 항상 아래 (바닥 무늬, 깔개, 발자국 판)
// - AlwaysAbove: 플레이어보다 항상 위 (천장, 나뭇잎, 머리 위 장식)
// - Ignore: 자동 정렬이 아예 손대지 않음 (예전처럼 직접 정한 순서 그대로)
// 붙이는 곳: 그림(SpriteRenderer)이 있는 가장 바깥 오브젝트
[DisallowMultipleComponent]
public class YSortHint : MonoBehaviour
{
    public enum Mode { Auto, Sort, AlwaysBelow, AlwaysAbove, Ignore }

    public Mode mode = Mode.Auto;

    [Tooltip("발밑 높이를 이만큼 올리거나 내림 (월드 단위). 예: 그림 아래에 그림자가 길게 붙어 있으면 + 값")]
    public float footOffset = 0f;
}
