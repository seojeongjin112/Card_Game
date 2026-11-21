using Effect_Card;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 내가 장착한 효과 카드 행. 액티브를 누르면 사용하고, 조정자가 알려 줄 때만 다시 그린다.
    public class EffectHandView : MonoBehaviour
    {
        [SerializeField] private EffectCardSlotView[] _slots;
        [SerializeField] private Text _titleText;
        [SerializeField] private string _titleFormat = "효과 카드 (장착) {0} / {1}";
        [SerializeField] private string _passiveText = "패시브";
        [SerializeField] private string _passiveSealedText = "패시브 (봉인됨)";
        [SerializeField] private string _activeReadyText = "액티브 · 사용 전";
        [SerializeField] private string _activeRemainingFormat = "액티브 · {0}라운드 남음";
        [SerializeField] private string _activeUsedFormat = "사용함 · {0}라운드 남음";

        private EffectBattleCoordinator _coordinator;

        private void Awake()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].Initialize(i);
            }
        }

        private void OnEnable()
        {
            foreach (var slot in _slots)
            {
                slot.Clicked += OnSlotClicked;
            }

            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            foreach (var slot in _slots)
            {
                slot.Clicked -= OnSlotClicked;
            }

            Unsubscribe();
        }

        // lit이 null이면 모두 원래대로, 아니면 그 카드만 밝히고 나머지는 흐리게 한다.
        public void SetSpotlight(EffectCardData lit)
        {
            if (_coordinator == null)
            {
                return;
            }

            var equipped = _coordinator.Player.Inventory.Equipped;
            for (int i = 0; i < _slots.Length; i++)
            {
                bool hasCard = i < equipped.Count;
                var state = lit == null || !hasCard ? EffectCardSlotView.SpotlightState.Normal
                    : equipped[i] == lit ? EffectCardSlotView.SpotlightState.Lit : EffectCardSlotView.SpotlightState.Dimmed;
                _slots[i].SetSpotlight(state);
            }
        }

        // 연출이 카드가 있는 자리에서 터지도록, 장착 중인 카드의 슬롯 위치를 알려 준다. 없으면 null이다.
        public RectTransform GetSlotRect(EffectCardData card)
        {
            if (_coordinator == null)
            {
                return null;
            }

            var equipped = _coordinator.Player.Inventory.Equipped;
            for (int i = 0; i < equipped.Count && i < _slots.Length; i++)
            {
                if (equipped[i] == card)
                {
                    return (RectTransform)_slots[i].transform;
                }
            }

            return null;
        }

        // BattleController가 조정자를 만든 뒤에 한 번 연결해 준다. 효과를 끄면 호출되지 않는다.
        public void Bind(EffectBattleCoordinator coordinator)
        {
            Unsubscribe();
            _coordinator = coordinator;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }

            Refresh();
        }

        // Bind와 OnEnable 중 무엇이 먼저 불려도 한 번만 구독되도록 먼저 해제하고 구독한다.
        private void Subscribe()
        {
            if (_coordinator != null)
            {
                _coordinator.Changed -= Refresh;
                _coordinator.Changed += Refresh;
            }
        }

        private void Unsubscribe()
        {
            if (_coordinator != null)
            {
                _coordinator.Changed -= Refresh;
            }
        }

        private void OnSlotClicked(int index)
        {
            var equipped = _coordinator?.Player.Inventory.Equipped;
            if (equipped == null || index >= equipped.Count || !equipped[index].IsActive)
            {
                return;
            }

            _coordinator.TryUseActive(true, equipped[index]);
        }

        private void Refresh()
        {
            if (_coordinator == null)
            {
                return;
            }

            var side = _coordinator.Player;
            var equipped = side.Inventory.Equipped;
            int slotCount = side.Inventory.SlotCount;

            if (_titleText != null)
            {
                _titleText.text = string.Format(_titleFormat, equipped.Count, slotCount);
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                // 짐꾼이 없을 때는 기본 슬롯 수만큼만 보여 준다.
                _slots[i].gameObject.SetActive(i < slotCount);
                if (i >= slotCount)
                {
                    continue;
                }

                if (i >= equipped.Count)
                {
                    _slots[i].ShowEmpty();
                    _slots[i].SetInteractable(false);
                    continue;
                }

                ShowCard(_slots[i], equipped[i], side);
            }
        }

        private void ShowCard(EffectCardSlotView slot, EffectCardData card, EffectSideContext side)
        {
            if (!card.IsActive)
            {
                bool sealedByStorm = side.State.ArePassivesSealed;
                slot.Show(card, sealedByStorm ? _passiveSealedText : _passiveText, sealedByStorm);
                slot.SetInteractable(false);
                return;
            }

            bool started = side.Tracker.HasStarted(card);
            bool canUse = side.Tracker.CanUse(card) && IsRoundOpen();
            int remaining = side.Tracker.GetRemainingRounds(card);

            string status;
            if (canUse)
            {
                status = started ? string.Format(_activeRemainingFormat, remaining) : _activeReadyText;
            }
            else
            {
                status = string.Format(_activeUsedFormat, remaining);
            }

            slot.Show(card, status, !canUse);
            slot.SetInteractable(canUse);
        }

        // 정산 뒤에는 조정자가 사용을 막으므로, 화면도 같은 기준으로 어둡게 보여 준다.
        private bool IsRoundOpen()
        {
            return _coordinator.IsRoundOpen;
        }
    }
}
