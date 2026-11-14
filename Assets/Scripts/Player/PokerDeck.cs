using System.Collections.Generic;
using UnityEngine;

namespace Poker_Card
{
    public class PokerDeck
    {
        private readonly IReadOnlyList<PokerCardData> _source;
        private readonly List<PokerCardData> _remaining;

        public int RemainingCount => _remaining.Count;

        public PokerDeck(IReadOnlyList<PokerCardData> source)
        {
            _source = source;
            _remaining = new List<PokerCardData>(source.Count);
            Reset();
        }

        // 라운드마다 전체 덱을 새로 섞는다. 이전 라운드에 뽑은 카드는 돌려놓는다.
        public void Reset()
        {
            _remaining.Clear();
            _remaining.AddRange(_source);
            Shuffle();
        }

        // 리롤로 버린 카드가 같은 라운드에 다시 나오지 않도록 뽑은 카드는 덱에서 제거한다.
        public bool TryDraw(out PokerCardData card)
        {
            if (_remaining.Count == 0)
            {
                card = null;
                return false;
            }

            int last = _remaining.Count - 1;
            card = _remaining[last];
            _remaining.RemoveAt(last);
            return true;
        }

        public List<PokerCardData> DrawHand(int count)
        {
            var hand = new List<PokerCardData>(count);
            for (int i = 0; i < count && TryDraw(out PokerCardData card); i++)
            {
                hand.Add(card);
            }

            return hand;
        }

        private void Shuffle()
        {
            for (int i = _remaining.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_remaining[i], _remaining[j]) = (_remaining[j], _remaining[i]);
            }
        }
    }
}
