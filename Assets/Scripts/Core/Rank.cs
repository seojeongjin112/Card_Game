namespace Core
{
    // 값이 곧 족보 비교용 숫자다. Ace는 가장 높은 14로 두고, A-2-3-4-5는 판정기에서 따로 처리한다.
    public enum Rank
    {
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
        Ace = 14
    }
}
