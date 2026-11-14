using Core;

namespace Poker_Card
{
    public readonly struct PokerHandResult
    {
        public HandRank HandRank { get; }
        public string Name { get; }
        public int Score { get; }

        public PokerHandResult(HandRank handRank, string name, int score)
        {
            HandRank = handRank;
            Name = name;
            Score = score;
        }
    }
}
