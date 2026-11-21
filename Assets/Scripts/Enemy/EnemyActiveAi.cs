using System.Collections.Generic;
using Core;
using Effect_Card;
using UnityEngine;

namespace Poker_Card
{
    // 상대가 이번 라운드에 어떤 액티브를 쓸지 정하고 조정자를 통해 실제로 사용한다.
    public class EnemyActiveAi
    {
        private const int PlayerPassivesToStorm = 2;

        private readonly EnemyAiSettings _settings;
        private readonly EnemyAiSettings.Profile _profile;

        public EnemyActiveAi(EnemyAiSettings settings, EnemyDifficulty difficulty)
        {
            _settings = settings;
            _profile = settings.GetProfile(difficulty);
        }

        // 내 패를 볼 수 없으므로 판단 재료는 상대 자신의 족보 점수와 공개된 상태(HP, 실드, 패시브 수)뿐이다.
        public int UseActives(EffectBattleCoordinator coordinator, PokerHandResult enemyHand)
        {
            var actives = new List<EffectCardData>();
            foreach (var card in coordinator.Enemy.Inventory.Equipped)
            {
                if (card.IsActive)
                {
                    actives.Add(card);
                }
            }

            Shuffle(actives);

            int used = 0;
            foreach (var card in actives)
            {
                if (!coordinator.Enemy.Tracker.CanUse(card))
                {
                    continue;
                }

                bool smart = Random.value < _profile.SmartRate;
                bool use = smart ? Wants(card, coordinator, enemyHand.Score) : Random.value < _profile.UseRate;
                if (use && coordinator.TryUseActive(false, card) == EffectBattleCoordinator.ActiveUseResult.Used)
                {
                    used++;
                }
            }

            return used;
        }

        // 위에서부터 처음 맞는 효과 묶음으로 판단하므로 검사 순서가 곧 우선순위다.
        public bool Wants(EffectCardData card, EffectBattleCoordinator coordinator, int baseScore)
        {
            var self = coordinator.Enemy;
            var player = coordinator.Player;
            bool weak = baseScore <= _settings.WeakScore;
            bool strong = baseScore >= _settings.StrongScore;
            float hpRatio = self.State.Hp / (float)self.State.MaxHp;

            if (card.HasEffect(EffectKind.Heal) && !card.HasEffect(EffectKind.Shield))
            {
                return self.State.MaxHp - self.State.Hp >= GetValue(card, EffectKind.Heal);
            }

            if (card.HasEffect(EffectKind.Shield) || card.HasEffect(EffectKind.SturdyShield)
                || card.HasEffect(EffectKind.DamageReduction) || card.HasEffect(EffectKind.Immune))
            {
                return weak || hpRatio <= _settings.LowHpRatio;
            }

            if (card.HasEffect(EffectKind.Counter))
            {
                return weak;
            }

            if (card.HasEffect(EffectKind.Multiplier))
            {
                return strong || player.State.Hp <= _settings.PlayerKillZoneHp;
            }

            if (card.HasEffect(EffectKind.BonusPercent) || card.HasEffect(EffectKind.CritChance)
                || card.HasEffect(EffectKind.Chain) || card.HasEffect(EffectKind.Again)
                || card.HasEffect(EffectKind.DamageOverTime) || card.HasEffect(EffectKind.Pierce))
            {
                return !weak || player.State.Hp <= _settings.PlayerFinishZoneHp;
            }

            if (card.HasEffect(EffectKind.BreakShield))
            {
                return player.State.Shield > 0;
            }

            if (card.HasEffect(EffectKind.Storm))
            {
                return player.GetActivePassives().Count >= PlayerPassivesToStorm;
            }

            if (card.HasEffect(EffectKind.Cleanse))
            {
                return self.State.Dots.Count > 0;
            }

            if (card.HasEffect(EffectKind.Refit))
            {
                return player.Tracker.StartedCards.Count > 0;
            }

            if (card.HasEffect(EffectKind.Confuse))
            {
                return !strong;
            }

            if (card.HasEffect(EffectKind.Mirror))
            {
                return self.Tracker.LastUsed != null;
            }

            // 운명은 AI가 원하는 포커 카드를 고를 방법이 아직 없어 쓰지 않는다.
            if (card.HasEffect(EffectKind.Fate))
            {
                return false;
            }

            return Random.value < _settings.FallbackUseRate;
        }

        private static float GetValue(EffectCardData card, EffectKind kind)
        {
            foreach (var effect in card.Effects)
            {
                if (effect.Kind == kind)
                {
                    return effect.Value;
                }
            }

            return 0f;
        }

        private static void Shuffle(List<EffectCardData> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
