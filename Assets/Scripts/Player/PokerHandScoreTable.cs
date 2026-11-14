using System;
using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Poker_Card
{
    [CreateAssetMenu(fileName = "PokerHandScoreTable", menuName = "CardGame/Poker Hand Score Table")]
    public class PokerHandScoreTable : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [SerializeField] private HandRank _handRank;
            [SerializeField] private string _displayName;
            [SerializeField] private int _score;

            public HandRank HandRank => _handRank;
            public string DisplayName => _displayName;
            public int Score => _score;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();
        [SerializeField] private bool _allowAceLowStraight = true;

        public bool AllowAceLowStraight => _allowAceLowStraight;

        public Entry GetEntry(HandRank handRank)
        {
            foreach (var entry in _entries)
            {
                if (entry.HandRank == handRank)
                {
                    return entry;
                }
            }

            return null;
        }
    }
}
