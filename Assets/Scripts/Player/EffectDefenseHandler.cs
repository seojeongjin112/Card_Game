using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Effect_Card
{
    // 방어·회복 계열(실드, 감면, 무적, 회복, 불사)을 처리한다. 공격 점수 계산은 EffectScoreCalculator가 맡는다.
    public class EffectDefenseHandler
    {
        public readonly struct DamageResult
        {
            public int HpLost { get; }
            public int ShieldAbsorbed { get; }
            public bool WasImmune { get; }
            public bool ShieldBroken { get; }
            public float ReductionPercent { get; }

            public DamageResult(int hpLost, int shieldAbsorbed, bool wasImmune, bool shieldBroken, float reductionPercent)
            {
                HpLost = hpLost;
                ShieldAbsorbed = shieldAbsorbed;
                WasImmune = wasImmune;
                ShieldBroken = shieldBroken;
                ReductionPercent = reductionPercent;
            }
        }

        private readonly EffectRuleSet _ruleSet;

        public EffectDefenseHandler(EffectRuleSet ruleSet)
        {
            _ruleSet = ruleSet;
        }

        // 액티브를 쓴 즉시 적용되는 효과만 다룬다. 감면은 피해를 받을 때 계산하므로 여기서는 건드리지 않는다.
        public void ApplyActive(EffectCardData card, CombatantState self)
        {
            foreach (var effect in card.Effects)
            {
                switch (effect.Kind)
                {
                    case EffectKind.Heal:
                        self.Heal((int)effect.Value);
                        break;
                    case EffectKind.Shield:
                        self.AddShield((int)effect.Value);
                        break;
                    case EffectKind.SturdyShield:
                        self.AddShield(GetSturdyShield(effect, self));
                        break;
                    case EffectKind.Immune:
                        self.SetImmune(true);
                        break;
                }
            }
        }

        // 무적 → 감면 → 실드 → HP 순서. 꿰뚫기(pierce)는 실드와 감면만 무시하고 무적은 무시하지 못한다.
        public DamageResult ResolveDamage(CombatantState target, int damage,
            IReadOnlyList<EffectCardData> targetUsedActives, bool pierce, float breakShieldChance)
        {
            if (target.IsImmune)
            {
                target.SetImmune(false);
                return new DamageResult(0, 0, true, false, 0f);
            }

            float reduction = 0f;
            if (!pierce)
            {
                reduction = Mathf.Min(SumReduction(targetUsedActives), _ruleSet.DamageReductionCap);
                damage = Mathf.FloorToInt(damage * (1f - reduction / 100f) + 0.5f);
            }

            int absorbed = 0;
            bool broken = false;
            if (target.Shield > 0 && !pierce)
            {
                if (breakShieldChance > 0f && Random.Range(0f, 100f) < breakShieldChance)
                {
                    target.ClearShield();
                    broken = true;
                }
                else
                {
                    absorbed = target.AbsorbWithShield(damage);
                    damage -= absorbed;
                }
            }

            target.LoseHp(damage);
            return new DamageResult(damage, absorbed, false, broken, reduction);
        }

        // 한 판에 한 번만 발동하므로 UndyingUsed로 막는다.
        public bool TryUndying(CombatantState target, IReadOnlyList<EffectCardData> activePassives)
        {
            if (target.Hp > 0 || target.UndyingUsed)
            {
                return false;
            }

            foreach (var card in activePassives)
            {
                if (card.HasEffect(EffectKind.Undying))
                {
                    target.SurviveWithOneHp();
                    return true;
                }
            }

            return false;
        }

        private static int GetSturdyShield(EffectEntry effect, CombatantState self)
        {
            int shield = Mathf.FloorToInt((self.MaxHp - self.Hp) * effect.Value / 100f);
            return effect.Limit > 0f ? Mathf.Min(shield, (int)effect.Limit) : shield;
        }

        private static float SumReduction(IReadOnlyList<EffectCardData> usedActives)
        {
            float sum = 0f;
            foreach (var card in usedActives)
            {
                foreach (var effect in card.Effects)
                {
                    if (effect.Kind == EffectKind.DamageReduction)
                    {
                        sum += effect.Value;
                    }
                }
            }

            return sum;
        }
    }
}
