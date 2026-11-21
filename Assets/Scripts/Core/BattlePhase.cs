namespace Core
{
    // 화면 왼쪽의 단계 목록(드래프트 → 전투 패 구성 → 쇼다운 준비 → 피해 정산) 순서와 같다.
    public enum BattlePhase
    {
        Draft,
        Compose,
        Ready,
        Settled
    }
}
