using System.Collections.Generic;
using Core;

namespace Poker_Card
{
    public enum DrawHintKind
    {
        None,
        FlushDraw,
        StraightDraw,
        Trips,
        TwoPair,
        OnePair,
        HighCard
    }

    public readonly struct DrawHint
    {
        public DrawHintKind Kind { get; }

        // FlushDraw면 4장 모인 문양, StraightDraw면 모자란 숫자(A는 14)다.
        public Suit Suit { get; }
        public int MissingRank { get; }

        public DrawHint(DrawHintKind kind, Suit suit = Suit.Spade, int missingRank = 0)
        {
            Kind = kind;
            Suit = suit;
            MissingRank = missingRank;
        }
    }

    // 패 7장을 보고 "다음에 무엇을 노리면 좋은지"를 한 가지 골라 준다(프로토타입의 drawHint). 우선순위는 플러시 → 스트레이트 → 현재 족보 순이다.
    public class PokerDrawHint
    {
        private const int FlushDrawCount = 4;
        private const int StraightLength = 5;
        private const int HighestStraightLow = 10;
        private const int AceHigh = 14;
        private const int AceLow = 1;

        private readonly PokerBestHandChooser _chooser;

        public PokerDrawHint(PokerBestHandChooser chooser)
        {
            _chooser = chooser;
        }

        public DrawHint Evaluate(IReadOnlyList<PokerCardData> hand)
        {
            if (hand == null || hand.Count < PokerHandEvaluator.HandSize)
            {
                return new DrawHint(DrawHintKind.None);
            }

            var bestRank = _chooser.Choose(hand).Result.HandRank;

            if (bestRank < HandRank.Flush && TryFindFlushDraw(hand, out Suit suit))
            {
                return new DrawHint(DrawHintKind.FlushDraw, suit);
            }

            if (bestRank < HandRank.Straight && TryFindStraightDraw(hand, out int missing))
            {
                return new DrawHint(DrawHintKind.StraightDraw, Suit.Spade, missing);
            }

            switch (bestRank)
            {
                case HandRank.ThreeOfAKind: return new DrawHint(DrawHintKind.Trips);
                case HandRank.TwoPair: return new DrawHint(DrawHintKind.TwoPair);
                case HandRank.OnePair: return new DrawHint(DrawHintKind.OnePair);
                case HandRank.HighCard: return new DrawHint(DrawHintKind.HighCard);
                default: return new DrawHint(DrawHintKind.None);
            }
        }

        private static bool TryFindFlushDraw(IReadOnlyList<PokerCardData> hand, out Suit suit)
        {
            var counts = new Dictionary<Suit, int>();
            foreach (var card in hand)
            {
                counts[card.Suit] = counts.TryGetValue(card.Suit, out int count) ? count + 1 : 1;
            }

            foreach (var pair in counts)
            {
                if (pair.Value == FlushDrawCount)
                {
                    suit = pair.Key;
                    return true;
                }
            }

            suit = Suit.Spade;
            return false;
        }

        // 연속 5개 중 딱 1개만 모자란 구간이 있으면 그 숫자를 알려 준다. A는 14와 1 양쪽으로 센다.
        private static bool TryFindStraightDraw(IReadOnlyList<PokerCardData> hand, out int missingRank)
        {
            var values = new HashSet<int>();
            foreach (var card in hand)
            {
                values.Add((int)card.Rank);
            }

            if (values.Contains(AceHigh))
            {
                values.Add(AceLow);
            }

            for (int low = HighestStraightLow; low >= AceLow; low--)
            {
                int missingCount = 0;
                int missing = 0;
                for (int offset = 0; offset < StraightLength; offset++)
                {
                    int value = low + offset;
                    if (!values.Contains(value))
                    {
                        missingCount++;
                        missing = value;
                    }
                }

                if (missingCount == 1)
                {
                    missingRank = missing == AceLow ? AceHigh : missing;
                    return true;
                }
            }

            missingRank = 0;
            return false;
        }
    }
}
