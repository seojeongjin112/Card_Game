using System.Collections.Generic;

namespace Poker_Card
{
    public class EnemyPokerHand
    {
        private readonly PokerDeck _deck;
        private readonly PokerBestHandChooser _chooser;
        private readonly int _handCount;

        public IReadOnlyList<PokerCardData> Hand { get; private set; } = new List<PokerCardData>();
        public PokerBestHand Best { get; private set; }

        // 상대는 플레이어와 별도의 덱을 쓰므로 같은 카드를 받을 수도 있다.
        public EnemyPokerHand(IReadOnlyList<PokerCardData> source, PokerHandEvaluator evaluator, int handCount)
        {
            _deck = new PokerDeck(source);
            _chooser = new PokerBestHandChooser(evaluator);
            _handCount = handCount;
        }

        public void Deal()
        {
            _deck.Reset();
            Hand = _deck.DrawHand(_handCount);
            Best = _chooser.Choose(Hand);
        }
    }
}
