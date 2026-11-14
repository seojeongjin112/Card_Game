using System.Collections.Generic;

namespace Poker_Card
{
    public readonly struct PokerBestHand
    {
        public IReadOnlyList<PokerCardData> Cards { get; }
        public PokerHandResult Result { get; }

        public PokerBestHand(IReadOnlyList<PokerCardData> cards, PokerHandResult result)
        {
            Cards = cards;
            Result = result;
        }
    }
}
