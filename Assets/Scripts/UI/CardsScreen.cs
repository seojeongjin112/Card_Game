using System;
using System.Collections.Generic;
using Core;
using Effect_Card;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Poker_Card
{
    // 카드 화면(프로토타입의 카드 탭). 왼쪽에 고른 효과 카드의 상세, 오른쪽에 검색·정렬·필터와 카드 격자가 있다.
    // 씬에는 이 컴포넌트가 붙은 빈 오브젝트와 카메라만 두고, 화면은 모두 코드가 만든다.
    public class CardsScreen : MonoBehaviour
    {
        private enum SortMode
        {
            Suit,
            Grade,
            Name
        }

        private enum FilterMode
        {
            All,
            Active,
            Passive,
            EpicPlus,
            MyDeck
        }

        private sealed class MiniCard
        {
            public EffectCardData Data;
            public RectTransform Rect;
            public Image Border;
            public CanvasGroup Group;
        }

        private const float GridLeft = 504f;
        private const float GridTop = 181f;
        private const float GridWidth = 1379f;
        private const float GridHeight = 780f;
        private const int GridColumns = 9;
        private static readonly Suit[] SuitOrder = { Suit.Spade, Suit.Heart, Suit.Clover, Suit.Diamond };

        [Header("데이터와 씬")]
        [SerializeField] private EffectCardDatabase _effectDatabase;
        [SerializeField] private EffectRuleSet _ruleSet;

        [Header("문구")]
        [SerializeField] private string _logoWord = "RUNE DUEL";
        [SerializeField] private string _logoSub = "룬 결투";
        [SerializeField] private string _pageLabel = "카드";
        [SerializeField] private string _searchPlaceholder = "카드 이름·효과 검색";
        [SerializeField] private string[] _sortNames = { "문양순", "등급순", "이름순" };
        [SerializeField] private string[] _filterNames = { "전체", "액티브", "패시브", "영웅+", "내 덱" };
        [SerializeField] private string[] _gradeNames = { "일반", "희귀", "영웅", "전설" };
        [SerializeField] private string _activeName = "액티브";
        [SerializeField] private string _passiveName = "패시브";
        [SerializeField] private string _countFormat = "{0}/{1}";
        [SerializeField] private string _emptyText = "조건에 맞는 카드가 없습니다";
        [SerializeField] private string _useLabel = "사용 기간";
        [SerializeField] private string _useFormat = "{0}라운드";
        [SerializeField] private string _passiveUseLabel = "발동";
        [SerializeField] private string _passiveUseValue = "상시";
        [SerializeField] private string _suitLabel = "문양";
        [SerializeField] private string _rankLabel = "숫자";
        [SerializeField] private string _activeFooterFormat = "액티브 · 첫 사용부터 {0}라운드";
        [SerializeField] private string _passiveFooter = "패시브 · 상시";
        [SerializeField] private string _removeLabel = "덱에서 빼기";
        [SerializeField] private string _addLabel = "덱에 넣기";
        [SerializeField] private string _deckCountFormat = "{0}/{1}";
        [SerializeField] private string _deckTag = "덱";
        [SerializeField] private string _addedToast = "덱에 넣었습니다";
        [SerializeField] private string _removedToast = "덱에서 뺐습니다";

        [Header("색")]
        [SerializeField] private Color _backInner = new Color(0.07f, 0.27f, 0.2f, 1f);
        [SerializeField] private Color _backOuter = new Color(0.01f, 0.06f, 0.045f, 1f);
        [SerializeField] private Color _asideColor = new Color(0.015f, 0.075f, 0.058f, 0.78f);
        [SerializeField] private Color _lineColor = new Color(0.18f, 0.34f, 0.29f, 1f);
        [SerializeField] private Color _fieldColor = new Color(0.05f, 0.17f, 0.13f, 1f);
        [SerializeField] private Color _textColor = new Color(0.957f, 0.945f, 0.91f, 1f);
        [SerializeField] private Color _bodyColor = new Color(0.812f, 0.902f, 0.863f, 1f);
        [SerializeField] private Color _mutedColor = new Color(0.62f, 0.76f, 0.7f, 1f);
        [SerializeField] private Color _goldColor = new Color(1f, 0.82f, 0.25f, 1f);
        [SerializeField] private Color _inkColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        [SerializeField] private Color[] _chipColors =
        {
            new Color(0.82f, 0.88f, 0.85f, 1f),
            new Color(1f, 0.47f, 0.27f, 1f),
            new Color(0.24f, 0.55f, 1f, 1f),
            new Color(0.69f, 0.42f, 1f, 1f),
            new Color(1f, 0.82f, 0.25f, 1f)
        };
        [SerializeField] private Color _grayButton = new Color(0.247f, 0.298f, 0.278f, 1f);

        [Header("움직임")]
        [SerializeField] private float _fadeSeconds = 0.5f;
        [SerializeField] private float _cardIntroSeconds = 0.3f;
        [SerializeField] private float _cardIntroStagger = 0.014f;
        [SerializeField] private float _cardIntroStartScale = 0.7f;
        [SerializeField] private float _selectedScale = 1.06f;

        private Font _font;
        private RectTransform _root;
        private RectTransform _stage;
        private UiScreenFade _fade;
        private EffectCardFace _detailFace;
        private Text _kindText;
        private Text _nameText;
        private Image _codeBadge;
        private Text _codeText;
        private Text _descriptionText;
        private Text _useLabelText;
        private Text _useValueText;
        private Text _suitValueText;
        private Text _rankValueText;
        private InputField _search;
        private Text _sortText;
        private GameObject _sortList;
        private Text _countText;
        private Text _emptyLabel;
        private RectTransform _gridContent;
        private ScrollRect _scroll;
        private readonly List<Image> _chipImages = new List<Image>();
        private readonly List<Text> _chipTexts = new List<Text>();
        private readonly List<MiniCard> _cards = new List<MiniCard>();
        private SortMode _sort = SortMode.Suit;
        private FilterMode _filter = FilterMode.All;
        private string _query = string.Empty;
        private EffectCardData _selected;
        private float _gridClock;
        private List<EffectCardData> _deck;
        private EffectDeckRules _deckRules;
        private Image _toggleButton;
        private Text _toggleLabel;
        private Text _toggleCount;
        private UiToast _toast;

        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            _deck = PlayerDeckStore.LoadEditable(_effectDatabase);
            _deckRules = new EffectDeckRules(_ruleSet);
            _root = UiBuild.CreateCanvas(transform, "CardsCanvas");
            UiBuild.BuildBackdrop(_root, _font, _backInner, _backOuter);

            var stageGo = new GameObject("Stage", typeof(RectTransform));
            stageGo.transform.SetParent(_root, false);
            _stage = (RectTransform)stageGo.transform;
            UiBuild.Center(_stage, new Vector2(UiBuild.StageWidth, UiBuild.StageHeight), Vector2.zero);

            BuildAside();
            BuildToolbar();
            BuildGrid();
            UiLobbyChrome.BuildTopBar(_stage, _font, _logoWord, _logoSub, _pageLabel, null, null);
            UiLobbyChrome.BuildTabBar(_stage, _font, "cards", OnTabSelected);
            BuildSortList();
            _toast = UiToast.Create(_stage, _font, 900f, 640f, 2.2f, 0.25f);

            _fade = UiScreenFade.Create(_root, _fadeSeconds);

            var first = _effectDatabase != null ? _effectDatabase.Find(Suit.Spade, Rank.Queen) : null;
            if (first == null && _effectDatabase != null && _effectDatabase.Cards.Count > 0)
            {
                first = _effectDatabase.Cards[0];
            }

            Select(first);
            RefreshChips();
            RebuildGrid();
        }

        private void Update()
        {
            // 화면 비율이 달라도 1920x1080 무대가 통째로 맞춰지도록 배율만 조절한다.
            float fit = Mathf.Min(_root.rect.width / UiBuild.StageWidth, _root.rect.height / UiBuild.StageHeight);
            _stage.localScale = Vector3.one * fit;

            _gridClock += Time.unscaledDeltaTime;
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                float k = Mathf.Clamp01((_gridClock - i * _cardIntroStagger) / _cardIntroSeconds);
                float eased = 1f - (1f - k) * (1f - k);
                float scale = Mathf.Lerp(_cardIntroStartScale, 1f, eased) * (card.Data == _selected ? _selectedScale : 1f);
                card.Rect.localScale = Vector3.one * scale;
                card.Group.alpha = eased;
            }
        }

        private void OnTabSelected(string id)
        {
            _fade.GoTo(UiLobbyChrome.GetSceneName(id));
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem", typeof(EventSystem));
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        // ---------- 왼쪽 상세 ----------

        private void BuildAside()
        {
            float top = UiLobbyChrome.TopBarHeight + 3f;
            float height = UiBuild.StageHeight - UiLobbyChrome.TabBarHeight - top;
            var aside = UiBuild.AddImage("Aside", _stage, _asideColor);
            UiBuild.Place(aside.rectTransform, 0f, top, 467f, height);
            aside.raycastTarget = false;
            var border = UiBuild.AddImage("AsideLine", _stage, _lineColor);
            UiBuild.Place(border.rectTransform, 467f, top, 3f, height);
            border.raycastTarget = false;

            var cardGo = new GameObject("DetailCard", typeof(RectTransform));
            cardGo.transform.SetParent(_stage, false);
            var cardRect = (RectTransform)cardGo.transform;
            UiBuild.Place(cardRect, 98f, 112f, 267f, 363f);
            var hit = cardGo.AddComponent<Image>();
            hit.color = Color.clear;
            cardGo.AddComponent<UiCardTilt>();
            _detailFace = EffectCardFace.Create(cardRect, _font, new Vector2(267f, 363f));

            _kindText = UiBuild.AddText("Kind", _stage, _font, 20, _mutedColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            UiBuild.Place(_kindText.rectTransform, 35f, 488f, 400f, 28f);

            _nameText = UiBuild.AddText("Name", _stage, _font, 40, _textColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            UiBuild.Place(_nameText.rectTransform, 35f, 522f, 300f, 52f);

            _codeBadge = UiBuild.AddImage("CodeBadge", _stage, _textColor);
            _codeBadge.raycastTarget = false;
            _codeText = UiBuild.AddText("Code", _codeBadge.rectTransform, _font, 20, _inkColor, FontStyle.Bold, TextAnchor.MiddleCenter);
            UiBuild.Stretch(_codeText.rectTransform);

            _descriptionText = UiBuild.AddText("Description", _stage, _font, 22, _bodyColor, FontStyle.Normal, TextAnchor.UpperLeft);
            _descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiBuild.Place(_descriptionText.rectTransform, 35f, 590f, 400f, 70f);

            BuildStat(35f, out _useLabelText, out _useValueText);
            BuildStat(169f, out Text suitLabel, out _suitValueText);
            BuildStat(304f, out Text rankLabel, out _rankValueText);
            suitLabel.text = _suitLabel;
            rankLabel.text = _rankLabel;

            // 덱에 넣고 빼는 큰 버튼. 아래에 지금 덱 장수가 작게 붙는다.
            _toggleButton = UiBuild.AddButton("DeckToggle", _stage, _font, _grayButton, string.Empty, 28, ToggleSelectedInDeck);
            UiBuild.Place(_toggleButton.rectTransform, 35f, 830f, 393f, 80f);
            _toggleLabel = _toggleButton.GetComponentInChildren<Text>();
            _toggleLabel.rectTransform.offsetMin = new Vector2(0f, 18f);
            _toggleCount = UiBuild.AddText("Count", _toggleButton.rectTransform, _font, 16, new Color(1f, 1f, 1f, 0.7f), FontStyle.Bold, TextAnchor.LowerCenter);
            UiBuild.Stretch(_toggleCount.rectTransform);
            _toggleCount.rectTransform.offsetMin = new Vector2(0f, 8f);
        }

        // 작은 이름 위에 큰 값이 놓인 상자 하나.
        private void BuildStat(float left, out Text label, out Text value)
        {
            var box = UiBuild.AddImage("Stat", _stage, _fieldColor);
            UiBuild.Place(box.rectTransform, left, 700f, 124f, 76f);
            box.raycastTarget = false;
            label = UiBuild.AddText("Label", box.rectTransform, _font, 15, _mutedColor, FontStyle.Bold, TextAnchor.MiddleCenter);
            UiBuild.Place(label.rectTransform, 0f, 8f, 124f, 24f);
            value = UiBuild.AddText("Value", box.rectTransform, _font, 28, _textColor, FontStyle.Bold, TextAnchor.MiddleCenter);
            UiBuild.Place(value.rectTransform, 0f, 32f, 124f, 38f);
        }

        // ---------- 오른쪽 도구줄 ----------

        private void BuildToolbar()
        {
            // 검색 칸
            var field = UiBuild.AddImage("Search", _stage, _fieldColor);
            UiBuild.Place(field.rectTransform, GridLeft, 107f, 312f, 56f);
            var line = UiBuild.AddImage("SearchLine", field.rectTransform, _lineColor);
            line.raycastTarget = false;
            UiBuild.Stretch(line.rectTransform);
            var inner = UiBuild.AddImage("SearchInner", field.rectTransform, _fieldColor);
            inner.raycastTarget = false;
            UiBuild.Stretch(inner.rectTransform);
            inner.rectTransform.offsetMin = new Vector2(3f, 3f);
            inner.rectTransform.offsetMax = new Vector2(-3f, -3f);

            var text = UiBuild.AddText("Text", field.rectTransform, _font, 22, _textColor, FontStyle.Normal, TextAnchor.MiddleLeft);
            UiBuild.Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(18f, 4f);
            text.rectTransform.offsetMax = new Vector2(-18f, -4f);
            text.supportRichText = false;
            var placeholder = UiBuild.AddText("Placeholder", field.rectTransform, _font, 22, _mutedColor, FontStyle.Normal, TextAnchor.MiddleLeft);
            UiBuild.Stretch(placeholder.rectTransform);
            placeholder.rectTransform.offsetMin = new Vector2(18f, 4f);
            placeholder.rectTransform.offsetMax = new Vector2(-18f, -4f);
            placeholder.text = _searchPlaceholder;

            _search = field.gameObject.AddComponent<InputField>();
            _search.targetGraphic = field;
            _search.textComponent = text;
            _search.placeholder = placeholder;
            _search.onValueChanged.AddListener(OnSearchChanged);

            // 정렬 버튼
            var sort = UiBuild.AddButton("Sort", _stage, _font, _fieldColor, string.Empty, 22, ToggleSortList);
            UiBuild.Place(sort.rectTransform, 829f, 107f, 200f, 56f);
            _sortText = sort.GetComponentInChildren<Text>();
            _sortText.alignment = TextAnchor.MiddleLeft;
            _sortText.rectTransform.offsetMin = new Vector2(18f, 0f);
            var arrow = UiBuild.AddText("Arrow", sort.rectTransform, _font, 18, _mutedColor, FontStyle.Bold, TextAnchor.MiddleRight);
            arrow.text = "▼";
            UiBuild.Stretch(arrow.rectTransform);
            arrow.rectTransform.offsetMax = new Vector2(-16f, 0f);
            RefreshSortLabel();

            // 필터 칩
            float x = 1043f;
            for (int i = 0; i < _filterNames.Length; i++)
            {
                int index = i;
                var chip = UiBuild.AddButton("Chip" + i, _stage, _font, Color.white, _filterNames[i], 20, () => SetFilter((FilterMode)index));
                var chipText = chip.GetComponentInChildren<Text>();
                float width = chipText.preferredWidth + 44f;
                UiBuild.Place(chip.rectTransform, x, 111f, width, 47f);
                _chipImages.Add(chip);
                _chipTexts.Add(chipText);
                x += width + 10f;
            }

            _countText = UiBuild.AddText("Count", _stage, _font, 24, _goldColor, FontStyle.Bold, TextAnchor.MiddleRight);
            UiBuild.Place(_countText.rectTransform, GridLeft + GridWidth - 160f, 117f, 160f, 34f);
        }

        // 정렬 목록: 버튼을 누르면 아래에 세 항목이 펼쳐진다. 다른 곳을 눌러 닫는 처리는 두지 않고 항목을 고르거나 버튼을 다시 누르면 닫힌다.
        private void BuildSortList()
        {
            var list = UiBuild.AddImage("SortList", _stage, _fieldColor);
            UiBuild.Place(list.rectTransform, 829f, 167f, 200f, 56f * _sortNames.Length);
            UiBuild.AddOutline(list, _lineColor, new Vector2(3f, 3f));
            for (int i = 0; i < _sortNames.Length; i++)
            {
                int index = i;
                var item = UiBuild.AddButton("Sort" + i, list.rectTransform, _font, _fieldColor, _sortNames[i], 22, () => SetSort((SortMode)index));
                UiBuild.Place(item.rectTransform, 0f, i * 56f, 200f, 56f);
                var label = item.GetComponentInChildren<Text>();
                label.alignment = TextAnchor.MiddleLeft;
                label.rectTransform.offsetMin = new Vector2(18f, 0f);
            }

            _sortList = list.gameObject;
            _sortList.SetActive(false);
        }

        private void ToggleSortList()
        {
            _sortList.SetActive(!_sortList.activeSelf);
            if (_sortList.activeSelf)
            {
                _sortList.transform.SetAsLastSibling();
            }
        }

        private void SetSort(SortMode mode)
        {
            _sort = mode;
            _sortList.SetActive(false);
            RefreshSortLabel();
            RebuildGrid();
        }

        private void RefreshSortLabel()
        {
            _sortText.text = _sortNames[(int)_sort];
        }

        private void SetFilter(FilterMode mode)
        {
            _filter = mode;
            RefreshChips();
            RebuildGrid();
        }

        // 켜진 칩은 색으로 채우고 글자는 어둡게, 꺼진 칩은 테두리 색의 글자만 보이게 한다.
        private void RefreshChips()
        {
            for (int i = 0; i < _chipImages.Count; i++)
            {
                bool on = i == (int)_filter;
                Color color = _chipColors[i];
                var button = _chipImages[i].GetComponent<Button>();
                UiBuild.SetButtonColors(button, on ? color : new Color(color.r, color.g, color.b, 0.2f));
                _chipTexts[i].color = on ? _inkColor : color;
            }
        }

        private void OnSearchChanged(string value)
        {
            _query = value == null ? string.Empty : value.Trim();
            RebuildGrid();
        }

        // ---------- 카드 격자 ----------

        private void BuildGrid()
        {
            var viewportGo = new GameObject("GridViewport", typeof(RectTransform));
            viewportGo.transform.SetParent(_stage, false);
            var viewport = (RectTransform)viewportGo.transform;
            UiBuild.Place(viewport, GridLeft, GridTop, GridWidth, GridHeight);
            viewportGo.AddComponent<RectMask2D>();
            var hit = viewportGo.AddComponent<Image>();
            hit.color = Color.clear;

            var contentGo = new GameObject("GridContent", typeof(RectTransform));
            contentGo.transform.SetParent(viewport, false);
            _gridContent = (RectTransform)contentGo.transform;
            _gridContent.anchorMin = new Vector2(0f, 1f);
            _gridContent.anchorMax = new Vector2(1f, 1f);
            _gridContent.pivot = new Vector2(0.5f, 1f);
            _gridContent.anchoredPosition = Vector2.zero;
            _gridContent.sizeDelta = Vector2.zero;

            var layout = contentGo.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(135f, 157f);
            layout.spacing = new Vector2(16f, 16f);
            layout.padding = new RectOffset(5, 5, 14, 12);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = GridColumns;
            layout.childAlignment = TextAnchor.UpperLeft;
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 스크롤 막대: 가는 길 위에 손잡이가 움직인다.
            var track = UiBuild.AddImage("ScrollTrack", _stage, new Color(0.04f, 0.13f, 0.1f, 0.9f));
            UiBuild.Place(track.rectTransform, GridLeft + GridWidth + 10f, GridTop, 12f, GridHeight);
            var handle = UiBuild.AddImage("ScrollHandle", track.rectTransform, new Color(0.62f, 0.76f, 0.7f, 0.75f));
            UiBuild.Stretch(handle.rectTransform);
            var scrollbar = track.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.targetGraphic = handle;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            _scroll = viewportGo.AddComponent<ScrollRect>();
            _scroll.viewport = viewport;
            _scroll.content = _gridContent;
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 40f;
            _scroll.verticalScrollbar = scrollbar;
            _scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            _emptyLabel = UiBuild.AddText("Empty", _stage, _font, 28, _mutedColor, FontStyle.Bold, TextAnchor.MiddleCenter);
            _emptyLabel.text = _emptyText;
            UiBuild.Place(_emptyLabel.rectTransform, GridLeft, GridTop + 300f, GridWidth, 60f);
            _emptyLabel.gameObject.SetActive(false);
        }

        // 필터·정렬·검색이 바뀔 때마다 격자를 처음부터 다시 만들고, 카드가 차례로 튀어나오게 시계를 되돌린다.
        private void RebuildGrid()
        {
            foreach (var card in _cards)
            {
                Destroy(card.Rect.gameObject);
            }

            _cards.Clear();
            if (_effectDatabase == null)
            {
                return;
            }

            var list = new List<EffectCardData>();
            foreach (var card in _effectDatabase.Cards)
            {
                if (Matches(card))
                {
                    list.Add(card);
                }
            }

            list.Sort(Compare);
            foreach (var card in list)
            {
                _cards.Add(BuildMiniCard(card));
            }

            _countText.text = string.Format(_countFormat, list.Count, _effectDatabase.Cards.Count);
            _emptyLabel.gameObject.SetActive(list.Count == 0);
            _gridClock = 0f;
            _scroll.verticalNormalizedPosition = 1f;
        }

        private bool Matches(EffectCardData card)
        {
            switch (_filter)
            {
                case FilterMode.Active:
                    if (!card.IsActive)
                    {
                        return false;
                    }

                    break;
                case FilterMode.Passive:
                    if (card.IsActive)
                    {
                        return false;
                    }

                    break;
                case FilterMode.EpicPlus:
                    if (card.Grade < EffectGrade.Epic)
                    {
                        return false;
                    }

                    break;
                case FilterMode.MyDeck:
                    if (!_deck.Contains(card))
                    {
                        return false;
                    }

                    break;
            }

            if (_query.Length == 0)
            {
                return true;
            }

            return card.DisplayName.IndexOf(_query, StringComparison.OrdinalIgnoreCase) >= 0
                || card.Description.IndexOf(_query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private int Compare(EffectCardData a, EffectCardData b)
        {
            if (_sort == SortMode.Name)
            {
                int byName = string.CompareOrdinal(a.DisplayName, b.DisplayName);
                if (byName != 0)
                {
                    return byName;
                }
            }

            if (_sort == SortMode.Grade && a.Grade != b.Grade)
            {
                return b.Grade.CompareTo(a.Grade);
            }

            int suit = Array.IndexOf(SuitOrder, a.Suit).CompareTo(Array.IndexOf(SuitOrder, b.Suit));
            if (suit != 0)
            {
                return suit;
            }

            return GetLowRank(a.Rank).CompareTo(GetLowRank(b.Rank));
        }

        // 작은 카드 한 장. 모양은 공통 도구가 그리고, 여기서는 눌림·기울기·튀어나오기만 붙인다.
        private MiniCard BuildMiniCard(EffectCardData data)
        {
            bool inDeck = _deck.Contains(data);
            var border = UiMiniCard.Create(_gridContent, _font, data, new Vector2(UiMiniCard.BaseWidth, UiMiniCard.BaseHeight), data == _selected, inDeck ? _deckTag : null);
            var button = border.gameObject.AddComponent<Button>();
            button.targetGraphic = border;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            var captured = data;
            button.onClick.AddListener(() => Select(captured));
            border.gameObject.AddComponent<UiCardTilt>();
            var group = border.gameObject.AddComponent<CanvasGroup>();

            return new MiniCard { Data = data, Rect = border.rectTransform, Border = border, Group = group };
        }

        private void RefreshBorder(MiniCard card)
        {
            bool selected = card.Data == _selected;
            card.Border.color = selected ? _goldColor : EffectCardFace.GetGradeColor(card.Data.Grade);
        }

        // ---------- 선택 ----------

        private void Select(EffectCardData data)
        {
            _selected = data;
            foreach (var card in _cards)
            {
                RefreshBorder(card);
            }

            if (data == null)
            {
                return;
            }

            int rounds = _ruleSet != null ? _ruleSet.ActiveUsableRounds : 3;
            string footer = data.IsActive ? string.Format(_activeFooterFormat, rounds) : _passiveFooter;
            _detailFace.Show(data, _activeName, _passiveName, footer, EffectCardFace.GetGradeColor(data.Grade), GetGradeLetter(data.Grade));

            string kind = data.IsActive ? _activeName : _passiveName;
            _kindText.text = _gradeNames[(int)data.Grade] + " · " + kind + " · " + EffectCardFace.GetCategoryLabel(data.Category);
            _nameText.text = data.DisplayName;
            _descriptionText.text = data.Description;

            // 이름 옆에 "Q♠" 같은 코드 상자를 붙인다.
            _codeText.text = EffectCardFace.GetCodeText(data);
            float badgeLeft = 35f + _nameText.preferredWidth + 14f;
            UiBuild.Place(_codeBadge.rectTransform, badgeLeft, 534f, _codeText.preferredWidth + 24f, 32f);

            _useLabelText.text = data.IsActive ? _useLabel : _passiveUseLabel;
            _useValueText.text = data.IsActive ? string.Format(_useFormat, rounds) : _passiveUseValue;
            _suitValueText.text = "♠♥♦♣"[(int)data.Suit].ToString();
            _suitValueText.color = data.Suit == Suit.Heart || data.Suit == Suit.Diamond ? new Color(1f, 0.45f, 0.4f, 1f) : _textColor;
            _rankValueText.text = GetRankText(data.Rank);
            RefreshToggle();
        }

        private void RefreshToggle()
        {
            if (_selected == null)
            {
                return;
            }

            bool inDeck = _deck.Contains(_selected);
            _toggleLabel.text = inDeck ? _removeLabel : _addLabel;
            _toggleCount.text = string.Format(_deckCountFormat, _deck.Count, _ruleSet.DeckMax);
        }

        // 고른 카드를 덱에 넣거나 뺀다. 넣을 수 없으면(장수·등급 제한) 이유를 안내 글로 보여 준다.
        private void ToggleSelectedInDeck()
        {
            if (_selected == null)
            {
                return;
            }

            if (_deck.Contains(_selected))
            {
                _deck.Remove(_selected);
                _toast.Show(_removedToast);
            }
            else
            {
                string reason = _deckRules.GetRejectReason(_deck, _selected);
                if (reason != null)
                {
                    _toast.Show(reason);
                    return;
                }

                _deck.Add(_selected);
                _toast.Show(_addedToast);
            }

            PlayerDeckStore.Save(_deck);
            RefreshToggle();
            RebuildGrid();
        }

        private string GetGradeLetter(EffectGrade grade)
        {
            return "CREL"[(int)grade].ToString();
        }

        private static int GetLowRank(Rank rank)
        {
            return rank == Rank.Ace ? 1 : (int)rank;
        }

        private static string GetRankText(Rank rank)
        {
            switch (rank)
            {
                case Rank.Jack: return "J";
                case Rank.Queen: return "Q";
                case Rank.King: return "K";
                case Rank.Ace: return "A";
                default: return ((int)rank).ToString();
            }
        }
    }
}
