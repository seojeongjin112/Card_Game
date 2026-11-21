using System;
using Core;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 라운드 번호와 현재 단계를 보여 준다. 상태를 들고 있지 않고 BattleController가 알려 줄 때만 바꾼다.
    public class BattleProgressView : MonoBehaviour
    {
        [SerializeField] private Text _roundText;
        [SerializeField] private Text _stepTitleText;
        [SerializeField] private Image[] _stepRows;
        [SerializeField] private Color _activeColor = new Color(0.95f, 0.35f, 0.25f, 1f);
        [SerializeField] private Color _idleColor = new Color(0f, 0f, 0f, 0f);
        [SerializeField] private string _roundFormat = "ROUND {0}";
        [SerializeField] private string _stepTitleFormat = "{0:00} {1}";
        [SerializeField] private string[] _stepNames = { "드래프트", "전투 패 구성", "쇼다운 준비", "피해 정산" };
        [SerializeField] private Text _headingText;
        [SerializeField] private Text _descriptionText;
        [SerializeField] private string[] _headings =
        {
            "이번 라운드에 더할 효과 카드를 고르세요.",
            "공격할 5장을 고르세요.",
            "쇼다운을 준비하세요.",
            "피해 정산 결과"
        };

        // 정산 단계의 설명은 비워 둔다. 같은 자리에 결과 문구(BattleResultView)가 나오기 때문이다.
        [SerializeField] private string[] _descriptions =
        {
            "3장 중 1장만 획득합니다. 효과 카드가 가득 차면 버릴 카드를 함께 고릅니다.",
            "포커 카드 7장 중 5장을 골라 족보를 만드세요. 액티브 효과 카드도 지금 사용할 수 있습니다.",
            "액티브 효과 카드를 쓰고 피해 정산을 누르세요. 제출은 취소할 수 있습니다.",
            string.Empty
        };

        // 단계가 바뀌었음을 알린다. 가운데 상자처럼 단계마다 구성이 달라지는 쪽이 자기 내용을 다시 그리는 데 쓴다.
        public event Action<int, BattlePhase> PhaseShown;

        public void Show(int round, BattlePhase phase)
        {
            _roundText.text = string.Format(_roundFormat, round);

            int active = (int)phase;
            for (int i = 0; i < _stepRows.Length; i++)
            {
                _stepRows[i].color = i == active ? _activeColor : _idleColor;
            }

            if (_stepTitleText != null)
            {
                _stepTitleText.text = string.Format(_stepTitleFormat, active + 1, _stepNames[active]);
            }

            if (_headingText != null)
            {
                _headingText.text = _headings[active];
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = _descriptions[active];
            }

            PhaseShown?.Invoke(round, phase);
        }
    }
}
