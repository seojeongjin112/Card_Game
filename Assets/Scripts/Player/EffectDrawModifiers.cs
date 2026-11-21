using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Effect_Card
{
    // 패시브가 포커 카드 드로우(문양 가중치)와 리롤 횟수에 주는 영향을 계산한다.
    public class EffectDrawModifiers
    {
        private readonly EffectRuleSet _ruleSet;

        public EffectDrawModifiers(EffectRuleSet ruleSet)
        {
            _ruleSet = ruleSet;
        }

        // 라운드 시작 때 한 번 부른다. 행운은 카드마다 확률로 +1이라 호출할 때마다 결과가 달라질 수 있다.
        public int GetExtraRerolls(IReadOnlyList<EffectCardData> passives)
        {
            int extra = 0;
            foreach (var passive in passives)
            {
                foreach (var effect in passive.Effects)
                {
                    if (effect.Kind == EffectKind.ExtraRerollEachTurn)
                    {
                        extra += (int)effect.Value;
                    }
                    else if (effect.Kind == EffectKind.LuckRerollChance && Roll(effect.Chance))
                    {
                        extra += (int)effect.Value;
                    }
                }
            }

            return extra;
        }

        // 기본 가중치는 1이고, 해당 문양 가중치 카드가 있을 때만 올라간다.
        public float GetSuitWeight(Suit suit, IReadOnlyList<EffectCardData> passives)
        {
            var kind = ToWeightKind(suit);
            float weight = 1f;
            foreach (var passive in passives)
            {
                foreach (var effect in passive.Effects)
                {
                    if (effect.Kind == kind)
                    {
                        weight += effect.Value * _ruleSet.DrawWeightFactor;
                    }
                }
            }

            return weight;
        }

        private static EffectKind ToWeightKind(Suit suit)
        {
            switch (suit)
            {
                case Suit.Spade: return EffectKind.DrawWeightSpade;
                case Suit.Heart: return EffectKind.DrawWeightHeart;
                case Suit.Diamond: return EffectKind.DrawWeightDiamond;
                default: return EffectKind.DrawWeightClover;
            }
        }

        private static bool Roll(float chancePercent)
        {
            if (chancePercent >= 100f)
            {
                return true;
            }

            return chancePercent > 0f && Random.Range(0f, 100f) < chancePercent;
        }
    }
}
