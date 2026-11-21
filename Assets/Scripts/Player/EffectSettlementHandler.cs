using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Effect_Card
{
    // 점수 비교로 승자가 정해진 뒤에 일어나는 추가 효과(연격, 재결산, 지속 피해, 반격 준비, 화신, 전염)를 처리한다.
    public class EffectSettlementHandler
    {
        public readonly struct WinResult
        {
            public int FirstHit { get; }
            public int ChainDamage { get; }
            public int AgainDamage { get; }
            public bool DotApplied { get; }
            public bool AgainFizzled { get; }

            public WinResult(int firstHit, int chainDamage, int againDamage, bool dotApplied, bool againFizzled)
            {
                FirstHit = firstHit;
                ChainDamage = chainDamage;
                AgainDamage = againDamage;
                DotApplied = dotApplied;
                AgainFizzled = againFizzled;
            }

            public int TotalDamage => FirstHit + ChainDamage + AgainDamage;
        }

        private readonly EffectDefenseHandler _defense;

        public EffectSettlementHandler(EffectDefenseHandler defense)
        {
            _defense = defense;
        }

        // 이긴 쪽이 쓴 액티브(연격, 찬스 스텝, 열상, 매의 눈, 파괴)를 한 번에 반영한다.
        public WinResult ResolveWin(CombatantState loser, int damage, IReadOnlyList<EffectCardData> winnerUsedActives,
            IReadOnlyList<EffectCardData> loserUsedActives)
        {
            bool pierce = false;
            float breakChance = 0f;
            foreach (var card in winnerUsedActives)
            {
                foreach (var effect in card.Effects)
                {
                    if (effect.Kind == EffectKind.Pierce)
                    {
                        pierce = true;
                    }
                    else if (effect.Kind == EffectKind.BreakShield)
                    {
                        breakChance += effect.Chance;
                    }
                }
            }

            int first = _defense.ResolveDamage(loser, damage, loserUsedActives, pierce, breakChance).HpLost;

            int chain = 0;
            int again = 0;
            bool dotApplied = false;
            bool fizzled = false;

            foreach (var card in winnerUsedActives)
            {
                foreach (var effect in card.Effects)
                {
                    if (effect.Kind == EffectKind.Again && first <= 0)
                    {
                        fizzled = true;
                    }

                    if (first <= 0)
                    {
                        continue;
                    }

                    switch (effect.Kind)
                    {
                        case EffectKind.Chain:
                            // 연격은 이미 들어간 피해의 일부를 추가로 입히는 것이라 실드·감면을 다시 거치지 않는다.
                            chain += _defense.ResolveDamage(loser, Mathf.FloorToInt(first * effect.Value),
                                loserUsedActives, true, 0f).HpLost;
                            break;
                        case EffectKind.Again:
                            again += _defense.ResolveDamage(loser, damage, loserUsedActives, pierce, breakChance).HpLost;
                            break;
                        case EffectKind.DamageOverTime:
                            loser.AddDot((int)effect.Value, effect.Turns, card);
                            dotApplied = true;
                            break;
                    }
                }
            }

            return new WinResult(first, chain, again, dotApplied, fizzled);
        }

        // 반격: 이번 라운드에 피해를 입지 않았을 때만 다음 라운드 보너스가 생기고, 아니면 0으로 초기화된다.
        public int ResolveCounterBonus(IReadOnlyList<EffectCardData> usedActives, int damageTaken)
        {
            if (damageTaken > 0)
            {
                return 0;
            }

            foreach (var card in usedActives)
            {
                foreach (var effect in card.Effects)
                {
                    if (effect.Kind == EffectKind.Counter)
                    {
                        return (int)effect.Value;
                    }
                }
            }

            return 0;
        }

        // 화신은 매 라운드 끝에 상대에게, 전염은 내가 지속 피해 중일 때만 상대에게 지속 피해를 건다.
        public void ApplyPassiveDots(CombatantState owner, CombatantState opponent, IReadOnlyList<EffectCardData> ownerPassives)
        {
            bool ownerHadDot = owner.Dots.Count > 0;
            foreach (var passive in ownerPassives)
            {
                foreach (var effect in passive.Effects)
                {
                    if (effect.Kind == EffectKind.AvatarDamageOverTime
                        || (effect.Kind == EffectKind.Contagion && ownerHadDot))
                    {
                        opponent.AddDot((int)effect.Value, effect.Turns, passive);
                    }
                }
            }
        }
    }
}
