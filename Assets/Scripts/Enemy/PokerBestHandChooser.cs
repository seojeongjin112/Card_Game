using System;
using System.Collections.Generic;

namespace Poker_Card
{
    public class PokerBestHandChooser
    {
        private readonly PokerHandEvaluator _evaluator;

        public PokerBestHandChooser(PokerHandEvaluator evaluator)
        {
            _evaluator = evaluator;
        }

        // 점수가 같은 조합은 먼저 찾은 것을 유지한다. 정산은 점수 차이만 쓰므로 어느 쪽이든 결과가 같다.
        public PokerBestHand Choose(IReadOnlyList<PokerCardData> hand)
        {
            if (hand == null || hand.Count < PokerHandEvaluator.HandSize)
            {
                throw new ArgumentException($"최고 족보를 고르려면 최소 {PokerHandEvaluator.HandSize}장이 필요하다.", nameof(hand));
            }

            var picked = new List<PokerCardData>(PokerHandEvaluator.HandSize);
            PokerBestHand? best = null;
            Search(hand, 0, picked, ref best);
            return best.Value;
        }

        private void Search(IReadOnlyList<PokerCardData> hand, int start, List<PokerCardData> picked, ref PokerBestHand? best)
        {
            if (picked.Count == PokerHandEvaluator.HandSize)
            {
                var result = _evaluator.Evaluate(picked);
                if (best == null || result.Score > best.Value.Result.Score)
                {
                    best = new PokerBestHand(new List<PokerCardData>(picked), result);
                }

                return;
            }

            int remainingNeeded = PokerHandEvaluator.HandSize - picked.Count;
            for (int i = start; i <= hand.Count - remainingNeeded; i++)
            {
                picked.Add(hand[i]);
                Search(hand, i + 1, picked, ref best);
                picked.RemoveAt(picked.Count - 1);
            }
        }
    }
}
