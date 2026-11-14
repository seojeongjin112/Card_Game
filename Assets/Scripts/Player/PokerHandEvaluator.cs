using System;
using System.Collections.Generic;
using System.Linq;
using Core;

namespace Poker_Card
{
    public class PokerHandEvaluator
    {
        public const int HandSize = 5;

        private const int AceLowStraightHighRank = (int)Rank.Five;
        private const int AceHighValue = (int)Rank.Ace;
        private const int RoyalLowValue = (int)Rank.Ten;
        private const int StraightSpan = HandSize - 1;

        private readonly PokerHandScoreTable _table;

        public PokerHandEvaluator(PokerHandScoreTable table)
        {
            _table = table;
        }

        public PokerHandResult Evaluate(IReadOnlyList<PokerCardData> cards)
        {
            if (cards == null || cards.Count != HandSize)
            {
                throw new ArgumentException($"족보 판정에는 정확히 {HandSize}장이 필요하다.", nameof(cards));
            }

            var groupSizes = cards
                .GroupBy(card => card.Rank)
                .Select(group => group.Count())
                .OrderByDescending(size => size)
                .ToList();

            bool isFlush = cards.All(card => card.Suit == cards[0].Suit);
            bool isStraight = groupSizes.Count == HandSize && IsStraight(cards);

            // 숫자가 모두 다르고 연속인데 가장 낮은 수가 10이면 10-J-Q-K-A뿐이다.
            bool isRoyal = isStraight && isFlush && cards.Min(card => (int)card.Rank) == RoyalLowValue;

            HandRank handRank = Classify(groupSizes, isFlush, isStraight, isRoyal);
            var entry = _table.GetEntry(handRank);
            if (entry == null)
            {
                throw new InvalidOperationException($"점수표에 {handRank} 항목이 없다.");
            }

            return new PokerHandResult(handRank, entry.DisplayName, entry.Score);
        }

        private static HandRank Classify(List<int> groupSizes, bool isFlush, bool isStraight, bool isRoyal)
        {
            if (isRoyal)
            {
                return HandRank.RoyalStraightFlush;
            }

            if (isStraight && isFlush)
            {
                return HandRank.StraightFlush;
            }

            if (groupSizes[0] == 4)
            {
                return HandRank.FourOfAKind;
            }

            if (groupSizes[0] == 3 && groupSizes[1] == 2)
            {
                return HandRank.FullHouse;
            }

            if (isFlush)
            {
                return HandRank.Flush;
            }

            if (isStraight)
            {
                return HandRank.Straight;
            }

            if (groupSizes[0] == 3)
            {
                return HandRank.ThreeOfAKind;
            }

            if (groupSizes[0] == 2 && groupSizes[1] == 2)
            {
                return HandRank.TwoPair;
            }

            if (groupSizes[0] == 2)
            {
                return HandRank.OnePair;
            }

            return HandRank.HighCard;
        }

        // 모든 숫자가 서로 다른 상태에서만 호출한다.
        private bool IsStraight(IReadOnlyList<PokerCardData> cards)
        {
            var values = cards.Select(card => (int)card.Rank).OrderBy(value => value).ToList();

            if (values[values.Count - 1] - values[0] == StraightSpan)
            {
                return true;
            }

            // A는 14로 정렬되므로 A-2-3-4-5는 마지막이 A이고 나머지가 2~5인 경우로 따로 확인한다.
            if (!_table.AllowAceLowStraight || values[values.Count - 1] != AceHighValue)
            {
                return false;
            }

            return values[values.Count - 2] == AceLowStraightHighRank
                && values[values.Count - 2] - values[0] == StraightSpan - 1;
        }
    }
}
