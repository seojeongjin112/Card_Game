using System;
using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Effect_Card
{
    [Serializable]
    public class EffectCardData
    {
        [SerializeField] private Suit _suit;
        [SerializeField] private Rank _rank;
        [SerializeField] private string _displayName;
        [SerializeField] private EffectGrade _grade;
        [SerializeField] private EffectCategory _category;
        [SerializeField] private bool _isActive;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private List<EffectEntry> _effects = new List<EffectEntry>();

        public Suit Suit => _suit;
        public Rank Rank => _rank;
        public string DisplayName => _displayName;
        public EffectGrade Grade => _grade;
        public EffectCategory Category => _category;
        public bool IsActive => _isActive;
        public string Description => _description;
        public IReadOnlyList<EffectEntry> Effects => _effects;

        // 같은 이름 카드가 여러 장(방패 ♥7·♥8)이라 문양+숫자 쌍으로만 카드를 구별한다.
        public bool Matches(Suit suit, Rank rank)
        {
            return _suit == suit && _rank == rank;
        }

        public bool HasEffect(EffectKind kind)
        {
            for (int i = 0; i < _effects.Count; i++)
            {
                if (_effects[i].Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        public EffectCardData(Suit suit, Rank rank, string displayName, EffectGrade grade,
            EffectCategory category, bool isActive, string description, List<EffectEntry> effects)
        {
            _suit = suit;
            _rank = rank;
            _displayName = displayName;
            _grade = grade;
            _category = category;
            _isActive = isActive;
            _description = description;
            _effects = effects;
        }
    }
}
