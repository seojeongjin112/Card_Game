namespace Core
{
    // J/Q/K가 없는 덱이라 로열 스트레이트 플러시는 만들 수 없어 정의하지 않는다.
    public enum HandRank
    {
        HighCard,
        OnePair,
        TwoPair,
        ThreeOfAKind,
        Straight,
        Flush,
        FullHouse,
        FourOfAKind,
        StraightFlush
    }
}
