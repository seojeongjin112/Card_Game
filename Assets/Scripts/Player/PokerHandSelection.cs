using System;
using System.Collections.Generic;

namespace Poker_Card
{
    public class PokerHandSelection
    {
        private readonly HashSet<int> _selected = new HashSet<int>();
        private readonly int _maxCount;

        public event Action SelectionChanged;

        public int Count => _selected.Count;
        public IReadOnlyCollection<int> SelectedIndices => _selected;

        public PokerHandSelection(int maxCount)
        {
            _maxCount = maxCount;
        }

        public bool IsSelected(int index)
        {
            return _selected.Contains(index);
        }

        // 가득 찬 상태에서 새 카드를 누르면 아무 변화 없이 false를 돌려준다.
        public bool TryToggle(int index)
        {
            if (!_selected.Remove(index))
            {
                if (_selected.Count >= _maxCount)
                {
                    return false;
                }

                _selected.Add(index);
            }

            SelectionChanged?.Invoke();
            return true;
        }

        // 리롤로 카드가 바뀌면 그 자리의 선택만 풀어야 하므로 토글과 별도로 둔다.
        public bool Remove(int index)
        {
            if (!_selected.Remove(index))
            {
                return false;
            }

            SelectionChanged?.Invoke();
            return true;
        }

        public void Clear()
        {
            if (_selected.Count == 0)
            {
                return;
            }

            _selected.Clear();
            SelectionChanged?.Invoke();
        }
    }
}
