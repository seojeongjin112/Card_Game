using System;
using Core;
using UnityEngine;

namespace Poker_Card
{
    [Serializable]
    public class PokerCardData
    {
        [SerializeField] private Suit _suit;
        [SerializeField] private Rank _rank;
        [SerializeField] private Sprite _sprite;

        public Suit Suit => _suit;
        public Rank Rank => _rank;
        public Sprite Sprite => _sprite;

        public PokerCardData(Suit suit, Rank rank, Sprite sprite)
        {
            _suit = suit;
            _rank = rank;
            _sprite = sprite;
        }
    }
}
