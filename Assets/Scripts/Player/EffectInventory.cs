using System;
using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Effect_Card
{
    public class EffectInventory
    {
        private readonly EffectDeckRules _rules;
        private readonly List<EffectCardData> _pool;
        private readonly List<EffectCardData> _equipped = new List<EffectCardData>();

        public IReadOnlyList<EffectCardData> Pool => _pool;
        public IReadOnlyList<EffectCardData> Equipped => _equipped;
        public int SlotCount => _rules.GetSlotCount(_equipped);
        public bool IsFull => _equipped.Count >= SlotCount;

        public event Action Changed;

        public EffectInventory(IEnumerable<EffectCardData> deck, EffectDeckRules rules)
        {
            _rules = rules;
            _pool = new List<EffectCardData>(deck);
        }

        // 시작부터 영웅·전설이 나오면 초반 균형이 무너지므로 일반·희귀를 먼저 뽑고 모자랄 때만 높은 등급을 쓴다.
        public void DealStarters(int count)
        {
            var low = new List<EffectCardData>();
            var high = new List<EffectCardData>();
            foreach (var card in _pool)
            {
                (card.Grade <= EffectGrade.Rare ? low : high).Add(card);
            }

            Shuffle(low);
            Shuffle(high);
            low.AddRange(high);

            for (int i = 0; i < count && i < low.Count; i++)
            {
                _pool.Remove(low[i]);
                _equipped.Add(low[i]);
            }

            Changed?.Invoke();
        }

        public bool IsEquipped(EffectCardData card)
        {
            return _equipped.Contains(card);
        }

        public bool TryEquip(EffectCardData card)
        {
            if (IsFull || !_pool.Contains(card))
            {
                return false;
            }

            _pool.Remove(card);
            _equipped.Add(card);
            Changed?.Invoke();
            return true;
        }

        // 슬롯을 늘려 주는 카드를 버리면 남은 카드가 칸에 못 들어가므로 교체 후 장착 수와 슬롯 수를 미리 비교한다.
        public bool CanReplace(int equippedIndex, EffectCardData card)
        {
            if (equippedIndex < 0 || equippedIndex >= _equipped.Count || !_pool.Contains(card))
            {
                return false;
            }

            var after = new List<EffectCardData>(_equipped) { [equippedIndex] = card };
            return after.Count <= _rules.GetSlotCount(after);
        }

        // 교체되어 나간 카드는 풀로 돌아가지 않고 이번 대전에서 사라진다.
        public bool TryReplace(int equippedIndex, EffectCardData card)
        {
            if (!CanReplace(equippedIndex, card))
            {
                return false;
            }

            _pool.Remove(card);
            _equipped[equippedIndex] = card;
            Changed?.Invoke();
            return true;
        }

        // 3라운드가 지난 액티브를 지우는 용도다. 빠진 카드는 풀로 돌아가지 않는다.
        public bool TryRemove(EffectCardData card)
        {
            if (!_equipped.Remove(card))
            {
                return false;
            }

            Changed?.Invoke();
            return true;
        }

        private static void Shuffle(List<EffectCardData> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
