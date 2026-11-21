using System;
using System.Collections.Generic;
using UnityEngine;

namespace Poker_Card
{
    public class PokerHandView : MonoBehaviour
    {
        [SerializeField] private PokerCardDatabase _database;
        [SerializeField] private PokerCardSlot[] _slots;
        [SerializeField] private PokerSubmitButton _submitButton;
        [SerializeField] private PokerRerollButton _rerollButton;
        [SerializeField] private int _maxSelectCount = 5;
        [SerializeField] private int _rerollsPerRound = 1;
        [SerializeField] private float _dealStagger = 0.07f;

        private PokerDeck _deck;
        private PokerHandSelection _selection;
        private PokerRerollCounter _rerollCounter;
        private List<PokerCardData> _hand = new List<PokerCardData>();
        private bool _isSubmitted;
        private bool _isRerollMode;
        private bool _isLocked;

        public event Action<IReadOnlyList<PokerCardData>> HandSubmitted;
        public event Action SubmitCancelled;

        // 선택·리롤·제출 등 화면에 보이는 상태가 바뀔 때마다 알린다. 도구 줄처럼 현재 상태를 따라 보여 주는 쪽이 쓴다.
        public event Action Changed;

        public int HandCount => _slots.Length;
        public int MaxSelectCount => _maxSelectCount;
        // 다른 화면의 OnEnable이 이 클래스의 Awake보다 먼저 불릴 수 있어서, 준비 전에는 0장으로 돌려준다.
        public int SelectedCount => _selection != null ? _selection.Count : 0;

        public IReadOnlyList<PokerCardData> GetSelectedCards()
        {
            return _selection != null ? CollectSelectedCards() : new List<PokerCardData>();
        }

        private void Awake()
        {
            _deck = new PokerDeck(_database.Cards);
            _selection = new PokerHandSelection(_maxSelectCount);
            _rerollCounter = new PokerRerollCounter(_rerollsPerRound);

            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].Initialize(i);
            }
        }

        private void OnEnable()
        {
            _selection.SelectionChanged += RefreshView;
            _submitButton.Clicked += OnSubmitButtonClicked;
            _rerollButton.Clicked += OnRerollButtonClicked;
            foreach (var slot in _slots)
            {
                slot.Clicked += OnSlotClicked;
            }
        }

        private void OnDisable()
        {
            _selection.SelectionChanged -= RefreshView;
            _submitButton.Clicked -= OnSubmitButtonClicked;
            _rerollButton.Clicked -= OnRerollButtonClicked;
            foreach (var slot in _slots)
            {
                slot.Clicked -= OnSlotClicked;
            }
        }

        private void Start()
        {
            Deal();
        }

        // 에디터에서 우클릭 메뉴로 다시 나눠 보며 그림과 문양·숫자가 맞는지 확인하기 위해 둔다. 플레이 중에만 동작한다.
        [ContextMenu("Deal")]
        public void Deal()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            bool wasSubmitted = _isSubmitted;

            _deck.Reset();
            _rerollCounter.Reset();
            _isSubmitted = false;
            _isRerollMode = false;
            _isLocked = false;
            _selection.Clear();

            _hand = _deck.DrawHand(_slots.Length);
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].SetSprite(_hand[i].Sprite);
                _slots[i].PlayDeal(i * _dealStagger);
            }

            RefreshView();

            // 제출 중에 다시 나누면 결과 표시도 지워야 하므로 취소와 같은 알림을 보낸다.
            if (wasSubmitted)
            {
                SubmitCancelled?.Invoke();
            }
        }

        // 패는 그대로 두고 나눠 주는 연출만 다시 보여 준다.
        public void ReplayDeal()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].PlayDeal(i * _dealStagger);
            }
        }

        private void OnSlotClicked(int index)
        {
            // 제출 중에는 선택을 바꾸지 못하게 하고, 바꾸려면 먼저 제출을 취소해야 한다.
            if (_isSubmitted)
            {
                return;
            }

            if (_isRerollMode)
            {
                Reroll(index);
                return;
            }

            _selection.TryToggle(index);
        }

        private void OnRerollButtonClicked()
        {
            if (_isSubmitted)
            {
                return;
            }

            if (!_isRerollMode && _rerollCounter.Remaining <= 0)
            {
                return;
            }

            _isRerollMode = !_isRerollMode;
            RefreshView();
        }

        private void Reroll(int index)
        {
            _isRerollMode = false;

            // 뽑기에 실패하면 횟수를 쓰지 않고 모드만 끝낸다.
            if (!_deck.TryDraw(out PokerCardData replacement) || !_rerollCounter.TryConsume())
            {
                RefreshView();
                return;
            }

            _hand[index] = replacement;
            _slots[index].SetSprite(replacement.Sprite);

            // 선택 해제가 이벤트로 화면을 갱신한다. 선택돼 있지 않았다면 직접 갱신해야 한다.
            if (!_selection.Remove(index))
            {
                RefreshView();
            }
        }

        // 정산이 끝난 뒤에는 제출을 취소할 수 없도록 막는다. 다음 라운드의 Deal에서 풀린다.
        public void SetLocked(bool locked)
        {
            _isLocked = locked;
            RefreshView();
        }

        private void OnSubmitButtonClicked()
        {
            if (_isLocked)
            {
                return;
            }

            if (_isSubmitted)
            {
                _isSubmitted = false;
                RefreshView();
                SubmitCancelled?.Invoke();
                return;
            }

            if (_selection.Count != _maxSelectCount)
            {
                return;
            }

            _isRerollMode = false;
            _isSubmitted = true;
            RefreshView();
            HandSubmitted?.Invoke(CollectSelectedCards());
        }

        private List<PokerCardData> CollectSelectedCards()
        {
            var selected = new List<PokerCardData>(_maxSelectCount);
            for (int i = 0; i < _hand.Count; i++)
            {
                if (_selection.IsSelected(i))
                {
                    selected.Add(_hand[i]);
                }
            }

            return selected;
        }

        private void RefreshView()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                bool selected = _selection.IsSelected(i);
                _slots[i].SetSelected(selected);
                // 제출 중에는 제출에서 빠진 카드를 어둡게 해서 잠긴 상태가 눈에 보이게 한다.
                _slots[i].SetDimmed(_isSubmitted && !selected);
            }

            _submitButton.SetState(_selection.Count == _maxSelectCount, _isSubmitted, _isLocked);
            _rerollButton.SetState(
                _rerollCounter.Remaining,
                _rerollCounter.MaxCount,
                _isRerollMode,
                !_isSubmitted && _rerollCounter.Remaining > 0);

            Changed?.Invoke();
        }
    }
}
