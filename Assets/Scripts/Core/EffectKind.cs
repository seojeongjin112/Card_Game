namespace Core
{
    // 프로토타입의 fx 키를 그대로 종류로 옮긴 목록. 처리기가 분류별로 나뉘므로 같은 처리기가 맡는 종류끼리 모아 둔다.
    public enum EffectKind
    {
        // 점수·피해 보정
        BonusPercent,
        CritChance,
        Multiplier,
        Smelt,
        Revenge,
        Link,
        ShieldBreakerBonus,
        Awaken,
        JokerPercent,
        Counter,
        Chain,
        Again,
        DamageOverTime,
        BreakShield,
        Pierce,
        AvatarDamageOverTime,
        Contagion,

        // 방어·회복
        Immune,
        DamageReduction,
        Shield,
        Heal,
        SturdyShield,
        Undying,

        // 방해·함정·유틸
        Storm,
        Confuse,
        Cleanse,
        TrapNullifyDisrupt,
        TrapReflectDisrupt,
        TrapPunishUtility,
        Refit,
        Haste,
        Scheme,
        Recycle,
        Mirror,

        // 드로우·리롤·슬롯
        Fate,
        ExtraRerollEachTurn,
        LuckRerollChance,
        SlotBonus,
        DrawWeightSpade,
        DrawWeightHeart,
        DrawWeightClover,
        DrawWeightDiamond
    }
}
