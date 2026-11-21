using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Effect_Card
{
    [CreateAssetMenu(fileName = "EffectCardDatabase", menuName = "CardGame/Effect Card Database")]
    public class EffectCardDatabase : ScriptableObject
    {
        [SerializeField] private List<EffectCardData> _cards = new List<EffectCardData>();

        public IReadOnlyList<EffectCardData> Cards => _cards;

        public EffectCardData Find(Suit suit, Rank rank)
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i].Matches(suit, rank))
                {
                    return _cards[i];
                }
            }

            return null;
        }
    }
}
