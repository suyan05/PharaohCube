// 문제 세트(변형)를 여러 개 가진 퍼즐 (실전 서브 퍼즐 4개)
// AnubisVariantManager가 새 게임마다 무작위로 고르고, 메인 저울 실패 때마다 다음 변형으로 바꿈
public interface IAnubisVariant
{
    // 변형 개수 (0번 = 기획서 기본 문제)
    int VariantCount { get; }

    // 지금 적용된 변형 번호
    int CurrentVariant { get; }

    // 변형 적용: 문제 값 교체 + 판 초기화 + 이 방 양피지 글 교체
    void ApplyVariant(int index);
}
