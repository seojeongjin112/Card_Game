using System;
using System.Text;
using Core;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 족보 점수표를 점수가 높은 순으로 보여 준다. 에셋 값만 읽으므로 한 번 채우면 갱신할 일이 없다.
    public class HandScoreTableView : MonoBehaviour
    {
        [SerializeField] private PokerHandScoreTable _table;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _scoreText;

        private void OnEnable()
        {
            Fill();
        }

        private void Fill()
        {
            var names = new StringBuilder();
            var scores = new StringBuilder();
            var ranks = (HandRank[])Enum.GetValues(typeof(HandRank));

            // enum 순서가 곧 족보 강약이라 뒤에서부터 돌면 높은 족보가 위에 온다.
            for (int i = ranks.Length - 1; i >= 0; i--)
            {
                var entry = _table.GetEntry(ranks[i]);
                if (entry == null)
                {
                    continue;
                }

                if (names.Length > 0)
                {
                    names.Append('\n');
                    scores.Append('\n');
                }

                names.Append(entry.DisplayName);
                scores.Append(entry.Score);
            }

            _nameText.text = names.ToString();
            _scoreText.text = scores.ToString();
        }
    }
}
