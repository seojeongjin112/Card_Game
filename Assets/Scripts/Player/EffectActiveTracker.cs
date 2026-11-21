using System;
using System.Collections.Generic;

namespace Effect_Card
{
    public class EffectActiveTracker
    {
        private class ActiveState
        {
            public int Remaining;
            public bool UsedThisRound;
        }

        private readonly EffectInventory _inventory;
        private readonly EffectRuleSet _ruleSet;
        private readonly Dictionary<EffectCardData, ActiveState> _states = new Dictionary<EffectCardData, ActiveState>();

        // 마법 거울이 복제할 대상. 거울 자신은 기록하지 않아 거울을 연달아 써도 원본 효과가 유지된다.
        public EffectCardData LastUsed { get; private set; }

        public IReadOnlyCollection<EffectCardData> StartedCards => _states.Keys;

        public event Action<EffectCardData> Used;
        public event Action<EffectCardData> Expired;

        public EffectActiveTracker(EffectInventory inventory, EffectRuleSet ruleSet)
        {
            _inventory = inventory;
            _ruleSet = ruleSet;
        }

        // 첫 사용 전에는 카운트다운이 시작되지 않으므로 사용 안 한 액티브는 슬롯에 계속 남는다.
        public bool HasStarted(EffectCardData card)
        {
            return _states.ContainsKey(card);
        }

        public int GetRemainingRounds(EffectCardData card)
        {
            return _states.TryGetValue(card, out var state) ? state.Remaining : _ruleSet.ActiveUsableRounds;
        }

        public bool CanUse(EffectCardData card)
        {
            if (!card.IsActive || !_inventory.IsEquipped(card))
            {
                return false;
            }

            return !_states.TryGetValue(card, out var state) || !state.UsedThisRound;
        }

        public bool TryUse(EffectCardData card)
        {
            if (!CanUse(card))
            {
                return false;
            }

            if (!_states.TryGetValue(card, out var state))
            {
                state = new ActiveState { Remaining = _ruleSet.ActiveUsableRounds };
                _states.Add(card, state);
            }

            state.UsedThisRound = true;
            if (!card.HasEffect(Core.EffectKind.Mirror))
            {
                LastUsed = card;
            }

            Used?.Invoke(card);
            return true;
        }

        // 순환 패시브가 발동하면 같은 라운드에 한 번 더 쓸 수 있게 사용 표시만 되돌린다. 남은 라운드는 그대로다.
        public void ResetUse(EffectCardData card)
        {
            if (_states.TryGetValue(card, out var state))
            {
                state.UsedThisRound = false;
            }
        }

        public void StartRound()
        {
            foreach (var state in _states.Values)
            {
                state.UsedThisRound = false;
            }
        }

        // 사용한 라운드도 한 라운드로 세므로 라운드가 끝날 때 줄이고, 0이 되면 지운다.
        public void EndRound()
        {
            foreach (var card in new List<EffectCardData>(_states.Keys))
            {
                _states[card].Remaining--;
                ExpireIfDone(card);
            }
        }

        // 재정비·신속처럼 남은 라운드를 직접 늘이거나 줄이는 효과가 쓴다. 아직 쓰지 않은 카드는 대상이 아니다.
        public bool AdjustRemainingRounds(EffectCardData card, int delta)
        {
            if (!_states.TryGetValue(card, out var state))
            {
                return false;
            }

            state.Remaining += delta;
            ExpireIfDone(card);
            return true;
        }

        private void ExpireIfDone(EffectCardData card)
        {
            if (_states[card].Remaining > 0)
            {
                return;
            }

            _states.Remove(card);
            _inventory.TryRemove(card);
            Expired?.Invoke(card);
        }
    }
}
