using System;
using System.Collections.Generic;
using System.Text;
using Core;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 족보 점수표를 점수가 높은 순으로 보여 준다. 에셋 값을 읽어 채우고, 고른 5장이 만든 족보 줄을 노랗게 밝히며,
    // 줄 위에 마우스를 올리면 만드는 법과 예시 카드를 설명 상자로 보여 준다(프로토타입의 htable).
    public class HandScoreTableView : MonoBehaviour
    {
        [Serializable]
        private sealed class HandGuide
        {
            [SerializeField] private HandRank _rank;
            [SerializeField] private string _how;
            [SerializeField] private string[] _cards;
            [SerializeField] private int _hitCount;

            public HandGuide(HandRank rank, string how, int hitCount, params string[] cards)
            {
                _rank = rank;
                _how = how;
                _hitCount = hitCount;
                _cards = cards;
            }

            public HandRank Rank => _rank;
            public string How => _how;
            public string[] Cards => _cards;
            public int HitCount => _hitCount;
        }

        [SerializeField] private PokerHandScoreTable _table;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _scoreText;
        [SerializeField] private PokerHandView _handView;

        [Header("현재 족보 줄 강조 (프로토타입 .htable .on)")]
        [SerializeField] private Color _currentBarColor = new Color(1f, 0.84f, 0.35f, 1f);
        [SerializeField] private Color _currentTextColor = new Color(0.1f, 0.12f, 0.12f, 1f);
        [SerializeField] private float _currentScale = 1.04f;
        [SerializeField] private float _transitionSeconds = 0.2f;
        [SerializeField] private float _barPaddingX = 6f;

        [Header("설명 상자")]
        [SerializeField] private string _tipTitleFormat = "{0} · {1}점";

        // 프로토타입 HAND_GUIDE의 내용. 예시 카드는 족보를 이루는 카드가 앞에 오고 나머지는 키커다.
        [SerializeField]
        private HandGuide[] _guides =
        {
            new HandGuide(HandRank.RoyalStraightFlush, "같은 문양의 10·J·Q·K·A", 5, "A♦", "K♦", "Q♦", "J♦", "10♦"),
            new HandGuide(HandRank.StraightFlush, "같은 문양 + 숫자 5개 연속", 5, "9♠", "8♠", "7♠", "6♠", "5♠"),
            new HandGuide(HandRank.FourOfAKind, "같은 숫자 4장", 4, "7♠", "7♥", "7♣", "7♦", "Q♠"),
            new HandGuide(HandRank.FullHouse, "트리플 + 원 페어", 5, "10♠", "10♥", "10♦", "3♣", "3♠"),
            new HandGuide(HandRank.Flush, "같은 문양 5장 (숫자 상관없음)", 5, "K♥", "10♥", "7♥", "4♥", "2♥"),
            new HandGuide(HandRank.Straight, "숫자 5개 연속 (문양 상관없음)", 5, "9♣", "8♦", "7♠", "6♥", "5♣"),
            new HandGuide(HandRank.ThreeOfAKind, "같은 숫자 3장", 3, "8♠", "8♥", "8♦", "K♣", "4♠"),
            new HandGuide(HandRank.TwoPair, "같은 숫자 2장이 두 쌍", 4, "J♠", "J♦", "6♥", "6♣", "A♠"),
            new HandGuide(HandRank.OnePair, "같은 숫자 2장", 2, "Q♥", "Q♣", "9♠", "5♦", "3♥"),
            new HandGuide(HandRank.HighCard, "아무 족보도 없을 때", 1, "K♠", "9♥", "7♣", "4♦", "2♠")
        };

        private readonly List<HandRank> _rows = new List<HandRank>();
        private readonly List<string> _rowNames = new List<string>();
        private readonly List<int> _rowScores = new List<int>();
        private readonly List<Image> _bars = new List<Image>();
        private readonly List<float> _barLevels = new List<float>();
        private PokerHandEvaluator _evaluator;
        private UiHandTip _tip;
        private int _currentRow = -1;
        private bool _built;

        private void Awake()
        {
            _evaluator = new PokerHandEvaluator(_table);
        }

        private void OnEnable()
        {
            Fill();
            if (_handView != null)
            {
                _handView.Changed -= RefreshCurrent;
                _handView.Changed += RefreshCurrent;
                RefreshCurrent();
            }
        }

        private void OnDisable()
        {
            if (_handView != null)
            {
                _handView.Changed -= RefreshCurrent;
            }

            if (_tip != null)
            {
                _tip.Hide();
            }
        }

        private void Fill()
        {
            _rows.Clear();
            _rowNames.Clear();
            _rowScores.Clear();
            var ranks = (HandRank[])Enum.GetValues(typeof(HandRank));

            // enum 순서가 곧 족보 강약이라 뒤에서부터 돌면 높은 족보가 위에 온다.
            for (int i = ranks.Length - 1; i >= 0; i--)
            {
                var entry = _table.GetEntry(ranks[i]);
                if (entry == null)
                {
                    continue;
                }

                _rows.Add(ranks[i]);
                _rowNames.Add(entry.DisplayName);
                _rowScores.Add(entry.Score);
            }

            RebuildTexts();
            if (!_built)
            {
                BuildBars();
                _built = true;
            }
        }

        // 현재 족보 줄만 글자 색을 진하게 바꿔서, 노란 바 위에서 잘 읽히게 한다.
        private void RebuildTexts()
        {
            var names = new StringBuilder();
            var scores = new StringBuilder();
            string current = "#" + ColorUtility.ToHtmlStringRGB(_currentTextColor);
            for (int i = 0; i < _rows.Count; i++)
            {
                if (i > 0)
                {
                    names.Append('\n');
                    scores.Append('\n');
                }

                bool isCurrent = i == _currentRow;
                names.Append(isCurrent ? "<color=" + current + ">" + _rowNames[i] + "</color>" : _rowNames[i]);
                scores.Append(isCurrent ? "<color=" + current + ">" + _rowScores[i] + "</color>" : _rowScores[i].ToString());
            }

            _nameText.supportRichText = true;
            _scoreText.supportRichText = true;
            _nameText.text = names.ToString();
            _scoreText.text = scores.ToString();
        }

        // 글자 줄마다 같은 높이의 바를 글자 뒤에 깐다. 바는 평소엔 투명하고, 현재 족보와 마우스 올림 판정에 쓴다.
        private void BuildBars()
        {
            // 글자가 마우스를 가로채면 아래 바가 올림을 못 받으므로, 글자는 판정에서 뺀다.
            _nameText.raycastTarget = false;
            _scoreText.raycastTarget = false;

            Canvas.ForceUpdateCanvases();
            var panel = (RectTransform)transform;
            float lineHeight = _nameText.preferredHeight / Mathf.Max(1, _rows.Count);
            var corners = new Vector3[4];
            _nameText.rectTransform.GetWorldCorners(corners);
            Vector3 topLeft = panel.InverseTransformPoint(corners[1]);
            Vector3 topRight = panel.InverseTransformPoint(corners[2]);
            float width = (topRight.x - topLeft.x) + _barPaddingX * 2f;

            for (int i = 0; i < _rows.Count; i++)
            {
                var go = new GameObject("RowBar" + i, typeof(RectTransform));
                go.layer = gameObject.layer;
                go.transform.SetParent(panel, false);
                go.transform.SetSiblingIndex(i);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(width, lineHeight);
                rect.anchoredPosition = new Vector2((topLeft.x + topRight.x) / 2f, topLeft.y - lineHeight * (i + 0.5f));

                var image = go.AddComponent<Image>();
                image.color = Color.clear;
                image.raycastTarget = true;
                var hover = go.AddComponent<UiHandRowHover>();
                hover.Initialize(i, ShowTip, HideTip);
                _bars.Add(image);
                _barLevels.Add(0f);
            }

            var stage = (RectTransform)panel.parent;
            _tip = UiHandTip.Create(stage, _nameText.font);
        }

        // 5장을 다 골랐을 때만 족보가 정해지므로, 그 전에는 모든 줄이 꺼진 상태다.
        private void RefreshCurrent()
        {
            int row = -1;
            if (_handView.SelectedCount == _handView.MaxSelectCount)
            {
                var result = _evaluator.Evaluate(_handView.GetSelectedCards());
                row = _rows.IndexOf(result.HandRank);
            }

            if (row == _currentRow)
            {
                return;
            }

            _currentRow = row;
            RebuildTexts();
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
        }

        // 테스트가 시간을 직접 넘길 수 있게 Tick을 연다.
        public void Tick(float dt)
        {
            for (int i = 0; i < _bars.Count; i++)
            {
                float target = i == _currentRow ? 1f : 0f;
                _barLevels[i] = Mathf.MoveTowards(_barLevels[i], target, dt / _transitionSeconds);
                float level = Mathf.SmoothStep(0f, 1f, _barLevels[i]);
                _bars[i].color = new Color(_currentBarColor.r, _currentBarColor.g, _currentBarColor.b, _currentBarColor.a * level);
                _bars[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, _currentScale, level);
            }
        }

        private void ShowTip(int row)
        {
            var guide = FindGuide(_rows[row]);
            if (guide == null || _tip == null)
            {
                return;
            }

            string title = string.Format(_tipTitleFormat, _rowNames[row], _rowScores[row]);
            _tip.Show(_bars[row].rectTransform, title, guide.How, guide.Cards, guide.HitCount);
        }

        private void HideTip(int row)
        {
            if (_tip != null)
            {
                _tip.Hide();
            }
        }

        private HandGuide FindGuide(HandRank rank)
        {
            for (int i = 0; i < _guides.Length; i++)
            {
                if (_guides[i].Rank == rank)
                {
                    return _guides[i];
                }
            }

            return null;
        }
    }
}
