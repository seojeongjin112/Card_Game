using System;
using System.Collections.Generic;
using UnityEngine;

namespace Effect_Card
{
    public class EffectDraft
    {
        private readonly EffectInventory _inventory;
        private readonly EffectRuleSet _ruleSet;
        private readonly List<EffectCardData> _offer = new List<EffectCardData>();

        public IReadOnlyList<EffectCardData> Offer => _offer;
        public EffectInventory Inventory => _inventory;
        public bool IsOpen => _offer.Count > 0;

        // 슬롯이 가득 차 있으면 고른 카드로 바꿀 기존 카드도 함께 골라야 한다.
        public bool NeedsReplace => _inventory.IsFull;

        public event Action<EffectCardData> Picked;

        // 제안이 비워진 뒤에 알린다. 장착 변경 알림은 비우기 전에 와서, 화면이 "아직 열려 있다"고 잘못 읽는 것을 막는다.
        public event Action Closed;

        public EffectDraft(EffectInventory inventory, EffectRuleSet ruleSet)
        {
            _inventory = inventory;
            _ruleSet = ruleSet;
        }

        // 효과 덱이 비면 제안할 카드가 없으므로 false를 돌려 호출한 쪽이 드래프트를 건너뛰게 한다.
        public bool Open()
        {
            _offer.Clear();

            var candidates = new List<EffectCardData>(_inventory.Pool);
            int count = Mathf.Min(_ruleSet.DraftOfferCount, candidates.Count);
            for (int i = 0; i < count; i++)
            {
                int j = UnityEngine.Random.Range(i, candidates.Count);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
                _offer.Add(candidates[i]);
            }

            return IsOpen;
        }

        public bool TryPick(EffectCardData card, int replaceIndex = -1)
        {
            if (!_offer.Contains(card))
            {
                return false;
            }

            bool success = _inventory.IsFull
                ? _inventory.TryReplace(replaceIndex, card)
                : _inventory.TryEquip(card);
            if (!success)
            {
                return false;
            }

            _offer.Clear();
            Picked?.Invoke(card);
            Closed?.Invoke();
            return true;
        }

        // 새 카드 포기. 제안된 카드는 풀에 그대로 남아 이후 라운드에 다시 나올 수 있다.
        public void Skip()
        {
            _offer.Clear();
            Closed?.Invoke();
        }
    }
}
