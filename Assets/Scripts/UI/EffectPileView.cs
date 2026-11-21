using Effect_Card;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 효과 덱에 남은 카드 수를 보여 주는 더미. 드래프트로 뽑을 카드가 얼마나 남았는지 알려 준다.
    public class EffectPileView : MonoBehaviour
    {
        [SerializeField] private Text _text;
        [SerializeField] private string _format = "효과 덱\n{0}장";

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

            _text.text = string.Format(_format, _coordinator.Player.Inventory.Pool.Count);
        }
    }
}
