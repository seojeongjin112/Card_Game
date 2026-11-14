using System.Collections.Generic;
using UnityEngine;

namespace Poker_Card
{
    [CreateAssetMenu(fileName = "PokerCardDatabase", menuName = "CardGame/Poker Card Database")]
    public class PokerCardDatabase : ScriptableObject
    {
        [SerializeField] private List<PokerCardData> _cards = new List<PokerCardData>();

        public IReadOnlyList<PokerCardData> Cards => _cards;
    }
}
