using System.Collections.Generic;
using System.Text;
using Core;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 적과 플레이어 사이의 가운데 상자(프로토타입의 evbox)를 단계마다 다른 구성으로 맞춘다.
    //  02 패 구성: 단계 제목 + 선택 N/5 + 큰 제목 + 설명 + 힌트 줄 + 상자 안의 버튼(리롤 모드, 최적 5장 자동 선택, 패 확정)
    //  03 쇼다운 준비: 족보 이름 + 제출 예정 카드 + 큰 점수 + 구분선 + 안내 글 + 버튼(패 다시 고르기, 제출 확정)
    //  04 정산: 점수판이 위쪽을 덮고, 결과 문구와 다음 버튼이 아래에 놓인다.
    // 이미 씬에 있는 글자와 버튼은 위치·색·문구만 바꿔 쓰고, 모자란 요소(알약, 힌트 줄, 자동 선택 버튼 등)는 코드가 만든다.
    public class BattleEventBoxView : MonoBehaviour
    {
        private const string SuitGlyphs = "♠♥♦♣";

        [Header("씬에 있는 요소")]
        [SerializeField] private RectTransform _box;
        [SerializeField] private Text _stepTitle;
        [SerializeField] private Text _heading;
        [SerializeField] private Text _description;
        [SerializeField] private Text _message;

        // 점수 계산 내역 글자. 점수판이 점수와 족보 이름을 보여 주므로 상자에서는 숨긴다(내용은 계속 갱신된다).
        [SerializeField] private Text _settleSummaryText;
        [SerializeField] private Button _submitButton;
        [SerializeField] private Button _rerollButton;
        [SerializeField] private Button _settleButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private PokerHandView _handView;
        [SerializeField] private PokerHandScoreTable _scoreTable;
        [SerializeField] private BattleProgressView _progressView;

        [Header("문구")]
        [SerializeField] private string[] _stepNumbers = { "01", "02", "03", "04" };
        [SerializeField] private string[] _stepLabels = { "드래프트", "전투 패 구성", "쇼다운 준비", "피해 정산" };
        [SerializeField] private string _composePrompt = "공격할 5장을 고르세요";
        [SerializeField] private string _draftHeading = "이번 라운드에 더할 효과 카드를 고르세요.";
        [SerializeField] private string _composeDescription = "포커 카드 7장 중 5장을 골라 족보를 만드세요. 액티브 효과 카드도 지금 사용할 수 있습니다.";
        [SerializeField] private string _rerollDescription = "리롤 모드 · 바꿀 카드를 눌러 교체하세요";
        [SerializeField] private string _draftDescription = "3장 중 1장만 획득합니다. 효과 카드가 가득 차면 버릴 카드를 함께 고릅니다.";
        [SerializeField] private string _selectedPillFormat = "선택 {0} / {1}";
        [SerializeField] private string _readyPill = "5 / 5 제출 가능";
        [SerializeField] private string _readyNote = "내 패 공개 전 · 상대도 제출 대기";
        [SerializeField] private string _baseScoreFormat = "기본 {0}점";
        [SerializeField] private string _rerollModeFormat = "리롤 모드 ({0})";
        [SerializeField] private string _rerollCancelLabel = "취소";
        [SerializeField] private string _autoLabel = "최적 5장 자동 선택";
        [SerializeField] private string _needFiveLabel = "5장 선택 필요";
        [SerializeField] private string _confirmHandLabel = "패 확정 → 03";
        [SerializeField] private string _backLabel = "← 패 다시 고르기";
        [SerializeField] private string _settleLabel = "제출 확정 → 04";
        [SerializeField] private string _pickedFormat = "제출 예정: {0}";
        [SerializeField] private string _basisFormat = "족보 {0}";
        [SerializeField] private string _useCardsHint = "아래 액티브 효과 카드를 눌러 사용할 수 있습니다";
        [SerializeField] private string _flushHintFormat = "{0} 문양이 4장 — {0} 1장만 더 오면 <b>플러시({1}점)</b>. 다른 문양 카드를 리롤해 보세요.";
        [SerializeField] private string _straightHintFormat = "숫자 {0} 1장만 더 오면 <b>스트레이트({1}점)</b>. 연속에 안 쓰이는 카드를 리롤해 보세요.";
        [SerializeField] private string _tripsHint = "트리플 완성! 같은 숫자 1장이 더 오면 <b>포카드</b>, 다른 페어가 생기면 <b>풀 하우스</b>입니다.";
        [SerializeField] private string _twoPairHintFormat = "투 페어 — 두 쌍 중 하나가 1장 더 맞으면 <b>풀 하우스({0}점)</b>입니다.";
        [SerializeField] private string _onePairHint = "원 페어 — 페어는 남기고 나머지를 리롤하면 <b>트리플</b>·<b>투 페어</b>를 노릴 수 있습니다.";
        [SerializeField] private string _highCardHintFormat = "족보 없음 — 같은 숫자 2장을 모으면 <b>원 페어({0}점)</b>. 낮은 카드를 리롤해 보세요.";
        [SerializeField] private string _hintBullet = "◆";

        [Header("배치 (상자 왼쪽 위 기준, 1920x1080 값)")]
        [SerializeField] private float _padding = 40f;
        [SerializeField] private float _stepTop = 30f;
        [SerializeField] private float _stepHeight = 36f;
        [SerializeField] private float _headingTop = 76f;
        [SerializeField] private float _headingHeight = 62f;
        [SerializeField] private float _descriptionTop = 146f;
        [SerializeField] private float _hintTop = 184f;
        [SerializeField] private float _hintHeight = 38f;
        [SerializeField] private float _pickedTop = 140f;
        [SerializeField] private float _dividerTop = 232f;
        [SerializeField] private float _buttonTop = 246f;
        [SerializeField] private float _buttonHeight = 48f;
        [SerializeField] private float _buttonGap = 14f;
        [SerializeField] private float _buttonPaddingX = 40f;
        [SerializeField] private float _buttonMinWidth = 140f;
        [SerializeField] private float _pillGap = 16f;

        [Header("글자 크기와 색")]
        [SerializeField] private int _stepFontSize = 24;
        [SerializeField] private int _pillFontSize = 24;
        [SerializeField] private int _headingFontSize = 46;
        [SerializeField] private int _smallFontSize = 26;
        [SerializeField] private int _bodyFontSize = 22;
        [SerializeField] private int _hintFontSize = 21;
        [SerializeField] private int _buttonFontSize = 22;
        [SerializeField] private int _bigScoreFontSize = 72;
        [SerializeField] private Color _textColor = new Color(0.957f, 0.945f, 0.91f, 1f);
        [SerializeField] private Color _bodyColor = new Color(0.812f, 0.902f, 0.863f, 1f);
        [SerializeField] private Color _mutedColor = new Color(0.62f, 0.796f, 0.722f, 1f);
        [SerializeField] private Color _goldColor = new Color(1f, 0.878f, 0.541f, 1f);
        [SerializeField] private Color _inkColor = new Color(0.067f, 0.067f, 0.067f, 1f);
        [SerializeField] private Color _redCardColor = new Color(1f, 0.45f, 0.4f, 1f);
        [SerializeField] private Color _hintBackColor = new Color(0.902f, 0.639f, 0.235f, 0.12f);
        [SerializeField] private Color _lineColor = new Color(0.25f, 0.4f, 0.35f, 1f);
        [SerializeField] private Color _grayButtonColor = new Color(0.247f, 0.298f, 0.278f, 1f);
        [SerializeField] private Color _primaryButtonColor = new Color(1f, 0.353f, 0.235f, 1f);
        [SerializeField] private Color _disabledButtonColor = new Color(0.32f, 0.4f, 0.37f, 1f);

        private PokerHandEvaluator _evaluator;
        private PokerBestHandChooser _chooser;
        private PokerDrawHint _drawHint;
        private Font _font;
        private BattlePhase _phase = BattlePhase.Compose;
        private float _left;
        private float _top;
        private float _width;

        private Text _stepNumber;
        private Text _stepLabel;
        private Image _pill;
        private Text _pillText;
        private Text _note;
        private Image _hintBar;
        private Text _hintText;
        private Button _autoButton;
        private Text _picked;
        private Text _bigScore;
        private Text _basis;
        private Image _divider;
        private Text _chip;
        private bool _built;

        private void Awake()
        {
            _evaluator = new PokerHandEvaluator(_scoreTable);
            _chooser = new PokerBestHandChooser(_evaluator);
            _drawHint = new PokerDrawHint(_chooser);
            _font = _heading.font;
        }

        private void OnEnable()
        {
            if (_progressView != null)
            {
                _progressView.PhaseShown -= OnPhaseShown;
                _progressView.PhaseShown += OnPhaseShown;
            }

            if (_handView != null)
            {
                _handView.Changed -= Refresh;
                _handView.Changed += Refresh;
            }
        }

        private void OnDisable()
        {
            if (_progressView != null)
            {
                _progressView.PhaseShown -= OnPhaseShown;
            }

            if (_handView != null)
            {
                _handView.Changed -= Refresh;
            }
        }

        // 다른 화면들이 Awake에서 자리를 잡은 뒤에 우리 배치를 덮어쓰도록 Start에서 만든다.
        private void Start()
        {
            Build();
            Refresh();
        }

        private void OnPhaseShown(int round, BattlePhase phase)
        {
            _phase = phase;
            if (_built)
            {
                Refresh();
            }
        }

        private void Build()
        {
            var corners = new Vector3[4];
            _box.GetWorldCorners(corners);
            var parent = (RectTransform)_box.parent;
            Vector3 topLeft = parent.InverseTransformPoint(corners[1]);
            Vector3 topRight = parent.InverseTransformPoint(corners[2]);
            _left = topLeft.x;
            _top = topLeft.y;
            _width = topRight.x - topLeft.x;

            _stepTitle.enabled = false;
            if (_settleSummaryText != null)
            {
                _settleSummaryText.enabled = false;
            }

            _stepNumber = AddText("StepNumber", parent, _stepFontSize, _goldColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            _stepLabel = AddText("StepLabel", parent, _stepFontSize, _textColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            _pill = AddImage("Pill", parent, Color.white);
            _pillText = AddText("PillText", _pill.rectTransform, _pillFontSize, _inkColor, FontStyle.Bold, TextAnchor.MiddleCenter);
            _note = AddText("StepNote", parent, 20, _mutedColor, FontStyle.Normal, TextAnchor.MiddleRight);
            _hintBar = AddImage("HintBar", parent, _hintBackColor);
            _hintText = AddText("HintText", _hintBar.rectTransform, _hintFontSize, _goldColor, FontStyle.Normal, TextAnchor.MiddleLeft);
            _picked = AddText("Picked", parent, _bodyFontSize, _bodyColor, FontStyle.Normal, TextAnchor.MiddleLeft);
            _bigScore = AddText("BigScore", parent, _bigScoreFontSize, _goldColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            _basis = AddText("Basis", parent, 20, _bodyColor, FontStyle.Normal, TextAnchor.MiddleLeft);
            _divider = AddImage("Divider", parent, _lineColor);
            _chip = AddText("Chip", parent, 20, _mutedColor, FontStyle.Normal, TextAnchor.MiddleLeft);

            _autoButton = BuildAutoButton(parent);

            StyleHeading(_heading);
            StyleBody(_description);
            _built = true;
        }

        private Button BuildAutoButton(RectTransform parent)
        {
            var go = new GameObject("AutoButton", typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            var label = AddText("Label", (RectTransform)go.transform, _buttonFontSize, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            go.AddComponent<UiButtonFx>();
            button.onClick.AddListener(OnAutoClicked);
            return button;
        }

        private void OnAutoClicked()
        {
            _handView.SelectBest(_chooser);
        }

        // 지금 상태에서 모든 요소의 문구·위치·보임을 다시 정한다. 단계와 손패가 바뀔 때마다 부른다.
        private void Refresh()
        {
            if (!_built)
            {
                return;
            }

            bool compose = _phase == BattlePhase.Compose;
            bool ready = _phase == BattlePhase.Ready;
            bool settled = _phase == BattlePhase.Settled;
            bool draft = _phase == BattlePhase.Draft;

            int index = (int)_phase;
            bool showHeader = compose || ready || draft;
            _stepNumber.gameObject.SetActive(showHeader);
            _stepLabel.gameObject.SetActive(showHeader);
            _stepNumber.text = _stepNumbers[index];
            _stepLabel.text = _stepLabels[index];
            LayoutStepRow();

            bool fiveSelected = _handView.SelectedCount == _handView.MaxSelectCount;
            PokerHandResult? current = null;
            if (fiveSelected)
            {
                current = _evaluator.Evaluate(_handView.GetSelectedCards());
            }

            RefreshPill(compose, ready);
            RefreshHeading(compose, ready, draft, current);
            RefreshDescription(compose, draft);
            RefreshHint(compose);
            RefreshReadyColumn(ready, current);
            RefreshButtons(compose, ready, settled, fiveSelected);

            // 결과 문구 글자가 붙은 오브젝트에는 결과 화면 컴포넌트도 있어서, 오브젝트를 끄면 버튼 연결이 끊긴다. 글자만 끈다.
            _message.enabled = settled;
            if (settled)
            {
                Place(_message.rectTransform, _padding, _buttonTop, _width - _padding * 2f - 320f, _buttonHeight);
                _message.alignment = TextAnchor.MiddleLeft;
                _message.fontSize = _bodyFontSize;
                _message.color = _textColor;
            }
        }

        private void LayoutStepRow()
        {
            Place(_stepNumber.rectTransform, _padding, _stepTop, 46f, _stepHeight);
            float labelLeft = _padding + 52f;
            float labelWidth = _stepLabel.preferredWidth + 4f;
            Place(_stepLabel.rectTransform, labelLeft, _stepTop, labelWidth, _stepHeight);
            PlaceRight(_note.rectTransform, _padding, _stepTop, 560f, _stepHeight);
        }

        private void RefreshPill(bool compose, bool ready)
        {
            bool show = compose || ready;
            _pill.gameObject.SetActive(show);
            _note.gameObject.SetActive(ready);
            if (!show)
            {
                return;
            }

            _pillText.text = compose
                ? string.Format(_selectedPillFormat, _handView.SelectedCount, _handView.MaxSelectCount)
                : _readyPill;
            float width = _pillText.preferredWidth + 28f;
            float left = _padding + 52f + _stepLabel.preferredWidth + _pillGap;
            Place(_pill.rectTransform, left, _stepTop, width, _stepHeight);
            Stretch(_pillText.rectTransform);
            _note.text = _readyNote;
        }

        private void RefreshHeading(bool compose, bool ready, bool draft, PokerHandResult? current)
        {
            _heading.gameObject.SetActive(true);
            _heading.horizontalOverflow = HorizontalWrapMode.Overflow;
            float width = ready ? (_width - _padding * 2f) * 0.5f : _width - _padding * 2f;
            Place(_heading.rectTransform, _padding, _headingTop, width, _headingHeight);

            if (draft)
            {
                _heading.text = _draftHeading;
            }
            else if (compose && current.HasValue)
            {
                string small = "<size=" + _smallFontSize + "><color=#" + ColorUtility.ToHtmlStringRGB(_bodyColor) + ">  "
                    + string.Format(_baseScoreFormat, current.Value.Score) + "</color></size>";
                _heading.text = current.Value.Name + small;
            }
            else if (ready && current.HasValue)
            {
                _heading.text = current.Value.Name;
            }
            else
            {
                _heading.text = _composePrompt;
            }
        }

        private void RefreshDescription(bool compose, bool draft)
        {
            bool show = compose || draft;
            _description.gameObject.SetActive(show);
            if (!show)
            {
                return;
            }

            Place(_description.rectTransform, _padding, _descriptionTop, _width - _padding * 2f, 32f);
            _description.text = draft ? _draftDescription : _handView.IsRerollMode ? _rerollDescription : _composeDescription;
        }

        private void RefreshHint(bool compose)
        {
            string hint = string.Empty;
            if (compose && !_handView.IsRerollMode && _handView.RerollsRemaining > 0)
            {
                hint = BuildHint(_drawHint.Evaluate(_handView.Hand));
            }

            _hintBar.gameObject.SetActive(hint.Length > 0);
            if (hint.Length == 0)
            {
                return;
            }

            Place(_hintBar.rectTransform, _padding, _hintTop, _width - _padding * 2f, _hintHeight);
            Stretch(_hintText.rectTransform);
            _hintText.rectTransform.offsetMin = new Vector2(16f, 0f);
            _hintText.rectTransform.offsetMax = new Vector2(-16f, 0f);
            _hintText.text = _hintBullet + "  " + hint.Replace("<b>", "<b><color=#ffffff>").Replace("</b>", "</color></b>");
        }

        private string BuildHint(DrawHint hint)
        {
            switch (hint.Kind)
            {
                case DrawHintKind.FlushDraw:
                    return string.Format(_flushHintFormat, SuitGlyphs[(int)hint.Suit], GetScore(HandRank.Flush));
                case DrawHintKind.StraightDraw:
                    return string.Format(_straightHintFormat, GetRankText(hint.MissingRank), GetScore(HandRank.Straight));
                case DrawHintKind.Trips:
                    return _tripsHint;
                case DrawHintKind.TwoPair:
                    return string.Format(_twoPairHintFormat, GetScore(HandRank.FullHouse));
                case DrawHintKind.OnePair:
                    return _onePairHint;
                case DrawHintKind.HighCard:
                    return string.Format(_highCardHintFormat, GetScore(HandRank.OnePair));
                default:
                    return string.Empty;
            }
        }

        private void RefreshReadyColumn(bool ready, PokerHandResult? current)
        {
            _picked.gameObject.SetActive(ready);
            _bigScore.gameObject.SetActive(ready);
            _basis.gameObject.SetActive(ready);
            if (!ready || !current.HasValue)
            {
                return;
            }

            float half = (_width - _padding * 2f) * 0.5f;
            Place(_picked.rectTransform, _padding, _pickedTop, half, 32f);
            _picked.text = string.Format(_pickedFormat, BuildPickedList());

            float rightLeft = _padding + half + 20f;
            Place(_bigScore.rectTransform, rightLeft, _headingTop - 6f, half, 78f);
            _bigScore.text = current.Value.Score.ToString();
            Place(_basis.rectTransform, rightLeft, _pickedTop, half, 32f);
            _basis.text = string.Format(_basisFormat, current.Value.Score);
        }

        private string BuildPickedList()
        {
            var cards = _handView.GetSelectedCards();
            var builder = new StringBuilder();
            string red = "#" + ColorUtility.ToHtmlStringRGB(_redCardColor);
            for (int i = 0; i < cards.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" · ");
                }

                bool isRed = cards[i].Suit == Suit.Heart || cards[i].Suit == Suit.Diamond;
                string code = SuitGlyphs[(int)cards[i].Suit] + GetRankText((int)cards[i].Rank);
                builder.Append(isRed ? "<color=" + red + "><b>" + code + "</b></color>" : "<b>" + code + "</b>");
            }

            return builder.ToString();
        }

        // 단계마다 보이는 버튼과 그 문구·색·자리를 정한다. 같은 제출 버튼이 02에서는 "패 확정", 03에서는 "패 다시 고르기"가 된다.
        private void RefreshButtons(bool compose, bool ready, bool settled, bool fiveSelected)
        {
            bool rerollMode = _handView.IsRerollMode;
            _divider.gameObject.SetActive(ready || settled);
            if (ready || settled)
            {
                Place(_divider.rectTransform, _padding, _dividerTop, _width - _padding * 2f, 3f);
            }

            _chip.gameObject.SetActive(ready);
            if (ready)
            {
                _chip.text = _useCardsHint;
            }

            _rerollButton.gameObject.SetActive(compose);
            _autoButton.gameObject.SetActive(compose && !rerollMode);
            _submitButton.gameObject.SetActive(compose || ready);
            _settleButton.gameObject.SetActive(ready);
            _nextButton.gameObject.SetActive(settled);

            float cursor = _padding;
            if (compose)
            {
                SetButton(_rerollButton, rerollMode ? _rerollCancelLabel : string.Format(_rerollModeFormat, _handView.RerollsRemaining), _grayButtonColor, ref cursor, false);
                if (!rerollMode)
                {
                    SetButton(_autoButton, _autoLabel, _grayButtonColor, ref cursor, false);
                }

                string submitLabel = fiveSelected && !rerollMode ? _confirmHandLabel : _needFiveLabel;
                SetButton(_submitButton, submitLabel, _primaryButtonColor, ref cursor, true);
            }
            else if (ready)
            {
                // 안내 글 뒤에 "패 다시 고르기"가 이어지고, 오른쪽 끝에 "제출 확정"이 놓인다.
                float chipWidth = _chip.preferredWidth + 8f;
                Place(_chip.rectTransform, _padding, _buttonTop, chipWidth, _buttonHeight);
                cursor = _padding + chipWidth + _buttonGap;
                SetButton(_submitButton, _backLabel, _grayButtonColor, ref cursor, false);
                SetButton(_settleButton, _settleLabel, _primaryButtonColor, ref cursor, true);
            }
            else if (settled)
            {
                SetButton(_nextButton, _nextButton.GetComponentInChildren<Text>().text, _primaryButtonColor, ref cursor, true);
            }
        }

        // 버튼을 문구 폭에 맞춰 크기를 정하고, 왼쪽 버튼은 앞에서부터 이어 붙이고 주요 버튼은 오른쪽 끝에 붙인다.
        private void SetButton(Button button, string label, Color color, ref float cursor, bool alignRight)
        {
            var text = button.GetComponentInChildren<Text>(true);
            text.text = label;
            text.fontSize = _buttonFontSize;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.alignment = TextAnchor.MiddleCenter;

            float width = Mathf.Max(_buttonMinWidth, text.preferredWidth + _buttonPaddingX);
            var rect = (RectTransform)button.transform;
            if (alignRight)
            {
                PlaceRight(rect, _padding, _buttonTop, width, _buttonHeight);
            }
            else
            {
                Place(rect, cursor, _buttonTop, width, _buttonHeight);
                cursor += width + _buttonGap;
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = Color.white;
            }

            var colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
            colors.selectedColor = color;
            colors.disabledColor = _disabledButtonColor;
            button.colors = colors;
        }

        private int GetScore(HandRank rank)
        {
            var entry = _scoreTable.GetEntry(rank);
            return entry != null ? entry.Score : 0;
        }

        private static string GetRankText(int rank)
        {
            switch (rank)
            {
                case 11: return "J";
                case 12: return "Q";
                case 13: return "K";
                case 14: return "A";
                default: return rank.ToString();
            }
        }

        private void StyleHeading(Text text)
        {
            text.fontSize = _headingFontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = _textColor;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private void StyleBody(Text text)
        {
            text.fontSize = _bodyFontSize;
            text.fontStyle = FontStyle.Normal;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = _bodyColor;
        }

        // 상자 왼쪽 위를 기준으로 자리를 잡는다. 모든 요소가 같은 부모(Stage)에 있어서 로컬 좌표를 그대로 쓴다.
        private void Place(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 1f);
            ApplyBoxOrigin(rect, left, top);
            rect.sizeDelta = new Vector2(width, height);
        }

        private void PlaceRight(RectTransform rect, float right, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(1f, 1f);
            ApplyBoxOrigin(rect, _width - right, top);
            rect.sizeDelta = new Vector2(width, height);
        }

        // 상자 기준 좌표(왼쪽 위가 원점, 아래로 갈수록 top이 커짐)를 Stage 좌표로 바꾼다.
        private void ApplyBoxOrigin(RectTransform rect, float left, float top)
        {
            rect.anchoredPosition = new Vector2(_left + left, _top - top);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private Text AddText(string name, RectTransform parent, int fontSize, Color color, FontStyle style, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            text.raycastTarget = false;
            return text;
        }

        private Image AddImage(string name, RectTransform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
    }
}
