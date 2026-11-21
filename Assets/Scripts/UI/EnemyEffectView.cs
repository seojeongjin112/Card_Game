using Effect_Card;
using UnityEngine;

namespace Poker_Card
{
    // 상대가 장착한 효과 카드 행. 패시브는 공개하고, 액티브는 한 번 쓰기 전까지 숨긴다.
    public class EnemyEffectView : MonoBehaviour
    {
        [SerializeField] private EffectCardSlotView[] _slots;
        [SerializeField] private string _passiveText = "패시브";
        [SerializeField] private string _passiveSealedText = "패시브 (봉인됨)";
        [SerializeField] private string _hiddenActiveText = "액티브 (비공개)";
        [SerializeField] private string _revealedActiveFormat = "액티브 · {0}라운드 남음";
        [SerializeField] private string _emptyText = "빈 슬롯";

        private EffectBattleCoordinator _coordinator;

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        // BattleController가 조정자를 만든 뒤에 한 번 연결해 준다.
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

        private void Refresh()
        {
            if (_coordinator == null)
            {
                return;
            }

            var side = _coordinator.Enemy;
            var equipped = side.Inventory.Equipped;
            int slotCount = side.Inventory.SlotCount;

            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].gameObject.SetActive(i < slotCount);
                if (i >= slotCount)
                {
                    continue;
                }

                // 화면은 보기만 하는 용도라 상대 카드는 어떤 슬롯도 누를 수 없다.
                _slots[i].SetInteractable(false);

                if (i >= equipped.Count)
                {
                    _slots[i].ShowHidden(_emptyText);
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
                return;
            }

            if (!side.RevealedActives.Contains(card))
            {
                slot.ShowHidden(_hiddenActiveText);
                return;
            }

            slot.Show(card, string.Format(_revealedActiveFormat, side.Tracker.GetRemainingRounds(card)), false);
        }
    }
}
