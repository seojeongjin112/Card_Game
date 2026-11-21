using System.Collections.Generic;
using Core;

namespace Effect_Card
{
    public class EffectDeckRules
    {
        private readonly EffectRuleSet _ruleSet;

        public EffectDeckRules(EffectRuleSet ruleSet)
        {
            _ruleSet = ruleSet;
        }

        // 화면에 그대로 보여 줄 수 있도록 불가 사유를 문자열로 돌려주고, 추가해도 되면 null을 돌려준다.
        public string GetRejectReason(IReadOnlyList<EffectCardData> deck, EffectCardData card)
        {
            if (Contains(deck, card))
            {
                return "이미 덱에 있는 카드입니다";
            }

            if (deck.Count >= _ruleSet.DeckMax)
            {
                return $"덱은 최대 {_ruleSet.DeckMax}장입니다";
            }

            if (card.Grade == EffectGrade.Epic && Count(deck, EffectGrade.Epic) >= _ruleSet.EpicMax)
            {
                return $"영웅 등급은 최대 {_ruleSet.EpicMax}장입니다";
            }

            if (card.Grade == EffectGrade.Legend && Count(deck, EffectGrade.Legend) >= _ruleSet.LegendMax)
            {
                return $"전설 등급은 최대 {_ruleSet.LegendMax}장입니다";
            }

            return null;
        }

        public bool CanAdd(IReadOnlyList<EffectCardData> deck, EffectCardData card)
        {
            return GetRejectReason(deck, card) == null;
        }

        // 짐꾼처럼 슬롯을 늘리는 패시브는 장착 중일 때만 반영해야 해서 장착 목록을 받는다.
        public int GetSlotCount(IReadOnlyList<EffectCardData> equipped)
        {
            int slots = _ruleSet.BaseSlots;
            for (int i = 0; i < equipped.Count; i++)
            {
                var effects = equipped[i].Effects;
                for (int j = 0; j < effects.Count; j++)
                {
                    if (effects[j].Kind == EffectKind.SlotBonus)
                    {
                        slots += (int)effects[j].Value;
                    }
                }
            }

            return slots;
        }

        private static bool Contains(IReadOnlyList<EffectCardData> deck, EffectCardData card)
        {
            for (int i = 0; i < deck.Count; i++)
            {
                if (deck[i].Matches(card.Suit, card.Rank))
                {
                    return true;
                }
            }

            return false;
        }

        private static int Count(IReadOnlyList<EffectCardData> deck, EffectGrade grade)
        {
            int count = 0;
            for (int i = 0; i < deck.Count; i++)
            {
                if (deck[i].Grade == grade)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
