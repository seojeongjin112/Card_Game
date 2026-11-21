using System;
using Core;
using Effect_Card;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 라운드 시작에 효과 카드 3장 중 1장을 고르는 화면. 슬롯이 가득 차면 버릴 카드도 함께 고른다.
    public class EffectDraftView : MonoBehaviour
    {
        // 슬롯 하나가 프로토타입처럼 보이게 하는 부품들(선택 후광, 뒷면, 카드 얼굴, 위·아래 라벨).
        private sealed class OfferVisual
        {
            public Image Glow;
            public GameObject Back;
            public EffectCardFace Face;
            public Text TopTag;
            public Text BottomTag;
        }

        [SerializeField] private GameObject _panel;
        [SerializeField] private EffectCardSlotView[] _offerSlots;
        [SerializeField] private GameObject _equippedRoot;
        [SerializeField] private EffectCardSlotView[] _equippedSlots;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _skipButton;
        [SerializeField] private Text _hintText;
        [SerializeField] private string _offerFormat = "{0} · {1}";
        [SerializeField] private string _activeLabel = "액티브";
        [SerializeField] private string _passiveLabel = "패시브";
        [SerializeField] private string _pickOfferHint = "받을 카드 1장을 고르세요.";
        [SerializeField] private string _pickDiscardHint = "슬롯이 가득 찼습니다. 버릴 카드를 고르세요.";
        [SerializeField] private string _cannotDiscardHint = "이 카드를 버리면 슬롯이 줄어 다른 카드가 들어갈 수 없습니다.";
        [SerializeField] private string _readyHint = "확정하면 카드를 획득합니다.";
        [SerializeField] private Text _previewText;
        [SerializeField] private string _previewHeader = "장착 후 예상 상태";
        [SerializeField] private string _previewEmptyText = "카드를 고르면 장착 후 상태를 미리 볼 수 있습니다.";
        [SerializeField] private string _previewInventoryFormat = "효과 인벤토리   {0} / {1} → {2} / {3}";
        [SerializeField] private string _previewNeedsReplaceText = "가득 참 · 교체 필요";
        [SerializeField] private string _previewTypeFormat = "액티브 / 패시브   {0} / {1}";
        [SerializeField] private string _activeInfoFormat = "액티브 · 첫 사용부터 {0}라운드 사용 가능";
        [SerializeField] private string _passiveInfoText = "패시브 · 장착 즉시 상시 적용 (상대에게 공개)";

        [Header("카드 공개 연출")]
        [SerializeField] private string _hiddenOfferText = "?";
        [SerializeField] private string _revealingHint = "카드 공개 중… 클릭하면 바로 공개됩니다.";
        [SerializeField] private float _revealStartDelay = 0.42f;
        [SerializeField] private float _commonPace = 0.26f;
        [SerializeField] private float _epicPace = 0.48f;
        [SerializeField] private float _legendPace = 0.65f;
        [SerializeField] private float _epicCharge = 0.6f;
        [SerializeField] private float _legendCharge = 0.95f;
        [SerializeField] private float _skipPace = 0.06f;
        [SerializeField] private float _chargeScaleAmount = 0.05f;
        [SerializeField] private float _chargeRotationDegrees = 3f;
        [SerializeField] private float _chargeShakeSpeed = 45f;

        [Header("프로토타입식 카드 표시")]
        [SerializeField] private Font _font;
        [SerializeField] private Vector2 _offerCardSize = new Vector2(200f, 267f);
        [SerializeField] private float _offerTagSpace = 45f;
        [SerializeField] private float _slotSidePadding = 0f;
        [SerializeField] private float _glowPadding = 14f;
        [SerializeField] private Color _glowColor = new Color(1f, 0.82f, 0.25f, 1f);
        [SerializeField] private Color _cardBackColor = new Color(0.1f, 0.18f, 0.2f, 1f);
        [SerializeField] private string _selectableTag = "선택 가능";
        [SerializeField] private string _selectedTag = "✓ 선택됨";
        [SerializeField] private string _epicTag = "◆ EPIC";
        [SerializeField] private string _legendTag = "★ LEGENDARY";
        [SerializeField] private EffectDraftLayoutSettings _layout = new EffectDraftLayoutSettings();
        [SerializeField] private string _previewInventoryLabel = "효과 인벤토리";
        [SerializeField] private string _previewTypeLabel = "액티브 / 패시브";
        [SerializeField] private string _previewInventoryValueFormat = "{0}/{1} → {2}/{3}";
        [SerializeField] private string _previewPairFormat = "{0} / {1}";
        [SerializeField] private string _cardActiveFooterFormat = "액티브 · {0}라운드";
        [SerializeField] private string _cardPassiveFooterText = "패시브 · 상시";
        [SerializeField] private string _confirmFormat = "선택 확정 · {0} 획득";
        [SerializeField] private string _confirmEmptyText = "카드를 선택하세요";
        [SerializeField] private string[] _gradeLetters = { "C", "R", "E", "L" };

        private EffectBattleCoordinator _coordinator;
        private int _selectedOffer = -1;
        private int _selectedDiscard = -1;
        private bool _wasOpen;
        private OfferVisual[] _offerVisuals;
        private DraftPreviewParts _previewParts;
        private Text _confirmLabel;
        private bool _revealing;
        private bool _charging;
        private bool _skipRequested;
        private int _revealedCount;
        private float _revealTimer;
        private float _chargeClock;

        // 카드가 공개되는 순간을 알린다. 연출 쪽이 그 자리에서 번쩍임·링을 터뜨린다.
        public event Action<RectTransform, EffectCardData> CardRevealed;

        private void Awake()
        {
            for (int i = 0; i < _offerSlots.Length; i++)
            {
                _offerSlots[i].Initialize(i);
            }

            _confirmLabel = _confirmButton.GetComponentInChildren<Text>();
            BuildOfferVisuals();
            ApplyPrototypeLayout();

            for (int i = 0; i < _equippedSlots.Length; i++)
            {
                _equippedSlots[i].Initialize(i);
            }
        }

        private void OnEnable()
        {
            foreach (var slot in _offerSlots)
            {
                slot.Clicked += OnOfferClicked;
            }

            foreach (var slot in _equippedSlots)
            {
                slot.Clicked += OnDiscardClicked;
            }

            _confirmButton.onClick.AddListener(OnConfirmClicked);
            _skipButton.onClick.AddListener(OnSkipClicked);
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            foreach (var slot in _offerSlots)
            {
                slot.Clicked -= OnOfferClicked;
            }

            foreach (var slot in _equippedSlots)
            {
                slot.Clicked -= OnDiscardClicked;
            }

            _confirmButton.onClick.RemoveListener(OnConfirmClicked);
            _skipButton.onClick.RemoveListener(OnSkipClicked);
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

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // 공개 연출을 시간에 따라 진행한다. 연출 중이 아닐 때는 아무것도 하지 않는다.
        public void Tick(float dt)
        {
            if (!_revealing || _coordinator == null)
            {
                return;
            }

            var offer = _coordinator.Player.Draft.Offer;
            if (_charging && _revealedCount < _offerSlots.Length)
            {
                AnimateCharge(_revealedCount, dt);
            }

            _revealTimer -= dt;
            if (_revealTimer > 0f)
            {
                return;
            }

            if (_revealedCount >= offer.Count)
            {
                EndReveal();
                return;
            }

            var card = offer[_revealedCount];
            float charge = _skipRequested ? 0f : GetCharge(card.Grade);

            // 영웅·전설은 공개 전에 한 번 떨며 충전하는 시간을 둔다.
            if (charge > 0f && !_charging)
            {
                _charging = true;
                _chargeClock = 0f;
                _revealTimer = charge;
                return;
            }

            _charging = false;
            int index = _revealedCount;
            ResetSlotTransform(index);
            _revealedCount++;
            _revealTimer = _skipRequested ? _skipPace : GetPace(card.Grade);
            Refresh();
            CardRevealed?.Invoke((RectTransform)_offerSlots[index].transform, card);

            if (_revealedCount >= offer.Count)
            {
                EndReveal();
            }
        }

        private void StartReveal()
        {
            _revealing = true;
            _charging = false;
            _skipRequested = false;
            _revealedCount = 0;
            _revealTimer = _revealStartDelay;

            // 앞선 공개가 중간에 끊겼더라도 슬롯이 흔들린 채로 남지 않게 처음 상태로 돌린다.
            for (int i = 0; i < _offerSlots.Length; i++)
            {
                ResetSlotTransform(i);
            }
        }

        private void EndReveal()
        {
            _revealing = false;
            _charging = false;
            Refresh();
        }

        private float GetCharge(EffectGrade grade)
        {
            return grade == EffectGrade.Legend ? _legendCharge : grade == EffectGrade.Epic ? _epicCharge : 0f;
        }

        private float GetPace(EffectGrade grade)
        {
            return grade == EffectGrade.Legend ? _legendPace : grade == EffectGrade.Epic ? _epicPace : _commonPace;
        }

        // 배치 그룹은 크기·회전을 무시하므로, 슬롯 위치를 건드리지 않고 크기와 기울기만 흔든다.
        private void AnimateCharge(int index, float dt)
        {
            _chargeClock += dt;
            var slot = _offerSlots[index].transform;
            slot.localScale = Vector3.one * (1f + Mathf.Sin(_chargeClock * _chargeShakeSpeed) * _chargeScaleAmount);
            slot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_chargeClock * _chargeShakeSpeed * 1.3f) * _chargeRotationDegrees);
        }

        private void ResetSlotTransform(int index)
        {
            _offerSlots[index].transform.localScale = Vector3.one;
            _offerSlots[index].transform.localRotation = Quaternion.identity;
        }

        private void OnOfferClicked(int index)
        {
            // 공개 중에는 선택 대신 "바로 공개"로 쓴다.
            if (_revealing)
            {
                _skipRequested = true;
                return;
            }

            _selectedOffer = index;
            Refresh();
        }

        private void OnDiscardClicked(int index)
        {
            _selectedDiscard = index;
            Refresh();
        }

        private void OnConfirmClicked()
        {
            var draft = _coordinator.Player.Draft;
            if (_revealing || _selectedOffer < 0 || _selectedOffer >= draft.Offer.Count)
            {
                return;
            }

            int discard = draft.NeedsReplace ? _selectedDiscard : -1;
            draft.TryPick(draft.Offer[_selectedOffer], discard);

            // 장착 변경 알림은 제안이 비워지기 전에 오므로, 끝난 뒤 한 번 더 그려야 화면이 닫힌다.
            Refresh();
        }

        private void OnSkipClicked()
        {
            if (_revealing)
            {
                return;
            }

            _coordinator.Player.Draft.Skip();
            Refresh();
        }

        private void Refresh()
        {
            if (_coordinator == null || !_coordinator.Player.Draft.IsOpen)
            {
                _wasOpen = false;
                _revealing = false;
                _panel.SetActive(false);
                return;
            }

            if (!_wasOpen)
            {
                _selectedOffer = -1;
                _selectedDiscard = -1;
                _wasOpen = true;
                StartReveal();
            }

            _panel.SetActive(true);
            var side = _coordinator.Player;
            var draft = side.Draft;

            ShowOffers(draft);
            ShowEquipped(side, draft.NeedsReplace);

            // 프로토타입의 드래프트에는 포기가 없고, 슬롯이 가득 차 교체할 때만 "새 카드 포기"를 쓴다.
            _skipButton.gameObject.SetActive(draft.NeedsReplace);
            UpdateHintAndConfirm(draft);
        }

        // 창의 틀·제목·미리보기·버튼을 프로토타입의 배치로 맞춘다. 장면에 있는 오브젝트를 재배치하는 코드다.
        private void ApplyPrototypeLayout()
        {
            var font = _font != null ? _font : _hintText.font;
            var title = _panel.transform.Find("Title").GetComponent<Text>();
            _previewParts = EffectDraftLayout.Apply(_layout, font, (RectTransform)_panel.transform, title,
                (RectTransform)_offerSlots[0].transform.parent, (RectTransform)_previewText.transform.parent, _previewText,
                (RectTransform)_equippedRoot.transform, _equippedSlots, _confirmButton, _skipButton, _hintText);
        }

        // 슬롯마다 후광, 뒷면, 카드 얼굴, 위·아래 라벨을 코드로 만든다. 슬롯 자체는 클릭 영역으로만 쓴다.
        private void BuildOfferVisuals()
        {
            var font = _font != null ? _font : _hintText.font;
            _offerVisuals = new OfferVisual[_offerSlots.Length];

            for (int i = 0; i < _offerSlots.Length; i++)
            {
                var slotRect = (RectTransform)_offerSlots[i].transform;
                slotRect.sizeDelta = new Vector2(_offerCardSize.x + _slotSidePadding, _offerCardSize.y + _offerTagSpace * 2f);

                var visual = new OfferVisual();
                visual.Glow = CreateChildImage("Glow", slotRect, _offerCardSize + new Vector2(_glowPadding, _glowPadding) * 2f, Color.clear);
                visual.Back = CreateChildImage("Back", slotRect, _offerCardSize, _cardBackColor).gameObject;
                CreateChildText("BackMark", visual.Back.GetComponent<RectTransform>(), font, _offerCardSize, Mathf.RoundToInt(_offerCardSize.y * 0.3f),
                    Color.white, TextAnchor.MiddleCenter).text = _hiddenOfferText;
                visual.Face = EffectCardFace.Create(slotRect, font, _offerCardSize);

                float tagOffset = _offerCardSize.y / 2f + _offerTagSpace / 2f;
                visual.TopTag = CreateChildText("TopTag", slotRect, font, new Vector2(_offerCardSize.x, _offerTagSpace), 22, Color.white, TextAnchor.MiddleCenter);
                visual.TopTag.rectTransform.anchoredPosition = new Vector2(0f, tagOffset);
                visual.BottomTag = CreateChildText("BottomTag", slotRect, font, new Vector2(_offerCardSize.x, _offerTagSpace), 24, Color.white, TextAnchor.MiddleCenter);
                visual.BottomTag.rectTransform.anchoredPosition = new Vector2(0f, -tagOffset);
                _offerVisuals[i] = visual;
            }

            // 카드가 커졌으니 한 줄 전체가 그 높이를 갖게 해, 아래 줄과 겹치지 않게 한다.
            var row = (RectTransform)_offerSlots[0].transform.parent;
            row.sizeDelta = new Vector2(row.sizeDelta.x, _offerCardSize.y + _offerTagSpace * 2f);
        }

        private static Image CreateChildImage(string name, RectTransform parent, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateChildText(string name, RectTransform parent, Font font, Vector2 size, int fontSize, Color color, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private void ShowOffers(EffectDraft draft)
        {
            for (int i = 0; i < _offerSlots.Length; i++)
            {
                bool exists = i < draft.Offer.Count;
                _offerSlots[i].gameObject.SetActive(exists);
                if (!exists)
                {
                    continue;
                }

                var visual = _offerVisuals[i];

                // 아직 공개되지 않은 카드는 뒷면("?")으로 두고, 눌러도 선택되지 않고 바로 공개하는 용도로 쓴다.
                if (_revealing && i >= _revealedCount)
                {
                    _offerSlots[i].ShowHidden(_hiddenOfferText);
                    _offerSlots[i].HideVisuals();
                    _offerSlots[i].SetInteractable(true);
                    _offerSlots[i].SetSelected(false);
                    visual.Back.SetActive(true);
                    visual.Face.gameObject.SetActive(false);
                    visual.Glow.color = Color.clear;
                    visual.TopTag.text = string.Empty;
                    visual.BottomTag.text = string.Empty;
                    continue;
                }

                var card = draft.Offer[i];
                bool selected = i == _selectedOffer;
                string type = card.IsActive ? _activeLabel : _passiveLabel;
                _offerSlots[i].Show(card, string.Format(_offerFormat, type, card.Description), false);
                _offerSlots[i].HideVisuals();
                _offerSlots[i].SetInteractable(true);
                _offerSlots[i].SetSelected(selected);

                var gradeColor = EffectCardFace.GetGradeColor(card.Grade);
                string footer = card.IsActive ? string.Format(_cardActiveFooterFormat, _coordinator.ActiveUsableRounds) : _cardPassiveFooterText;
                visual.Back.SetActive(false);
                visual.Face.gameObject.SetActive(true);
                visual.Face.Show(card, _activeLabel, _passiveLabel, footer, gradeColor, _gradeLetters[(int)card.Grade]);

                // 고른 카드는 금색 후광과 "✓ 선택됨"으로 보여 주고, 영웅·전설은 카드 아래에 등급 라벨을 단다.
                visual.Glow.color = selected ? _glowColor : Color.clear;
                visual.TopTag.text = selected ? _selectedTag : _selectableTag;
                visual.TopTag.color = selected ? _glowColor : Color.white;
                visual.BottomTag.text = card.Grade == EffectGrade.Legend ? _legendTag : card.Grade == EffectGrade.Epic ? _epicTag : string.Empty;
                visual.BottomTag.color = gradeColor;
            }
        }

        private void ShowEquipped(EffectSideContext side, bool needsReplace)
        {
            _equippedRoot.SetActive(needsReplace);
            if (!needsReplace)
            {
                return;
            }

            var equipped = side.Inventory.Equipped;
            for (int i = 0; i < _equippedSlots.Length; i++)
            {
                bool exists = i < equipped.Count;
                _equippedSlots[i].gameObject.SetActive(exists);
                if (!exists)
                {
                    continue;
                }

                var card = equipped[i];
                _equippedSlots[i].Show(card, card.IsActive ? _activeLabel : _passiveLabel, false);
                _equippedSlots[i].SetInteractable(true);
                _equippedSlots[i].SetSelected(i == _selectedDiscard);
            }
        }

        private void UpdateHintAndConfirm(EffectDraft draft)
        {
            // 공개 중에는 확정·포기를 막아 안 보이는 카드를 고르는 일이 없게 한다.
            _skipButton.interactable = !_revealing;
            if (_revealing)
            {
                _hintText.text = _revealingHint;
                _confirmButton.interactable = false;
                SetConfirmLabel(_confirmEmptyText);
                UpdatePreview(draft);
                return;
            }

            bool offerPicked = _selectedOffer >= 0;
            bool needsReplace = draft.NeedsReplace;
            bool canConfirm = offerPicked;
            string hint = _pickOfferHint;

            if (offerPicked && needsReplace)
            {
                if (_selectedDiscard < 0)
                {
                    hint = _pickDiscardHint;
                    canConfirm = false;
                }
                else if (!draft.Inventory.CanReplace(_selectedDiscard, draft.Offer[_selectedOffer]))
                {
                    hint = _cannotDiscardHint;
                    canConfirm = false;
                }
                else
                {
                    hint = _readyHint;
                }
            }
            else if (offerPicked)
            {
                hint = _readyHint;
            }

            _hintText.text = hint;
            _confirmButton.interactable = canConfirm;

            // 확정 버튼에는 무엇을 얻는지 적어 준다(프로토타입의 "선택 확정 · 6♠ 획득").
            SetConfirmLabel(canConfirm ? string.Format(_confirmFormat, EffectCardFace.GetCodeText(draft.Offer[_selectedOffer])) : _confirmEmptyText);
            UpdatePreview(draft);
        }

        private void SetConfirmLabel(string text)
        {
            if (_confirmLabel != null)
            {
                _confirmLabel.text = text;
            }
        }

        // 고른 카드를 장착했을 때의 인벤토리 장수, 액티브·패시브 수와 카드 설명을 보여 준다.
        private void UpdatePreview(EffectDraft draft)
        {
            if (_previewText == null)
            {
                return;
            }

            if (_selectedOffer < 0 || _selectedOffer >= draft.Offer.Count)
            {
                _previewText.text = _previewHeader + "\n\n" + _previewEmptyText;
                if (_previewParts != null)
                {
                    _previewParts.Row1Label.text = _previewEmptyText;
                    _previewParts.Row1Value.text = string.Empty;
                    _previewParts.Row2Label.text = string.Empty;
                    _previewParts.Row2Value.text = string.Empty;
                    _previewParts.CardBox.SetActive(false);
                }

                return;
            }

            var card = draft.Offer[_selectedOffer];
            var inventory = draft.Inventory;
            int replaceIndex = draft.NeedsReplace ? _selectedDiscard : -1;
            var after = inventory.Preview(card, replaceIndex);

            // 가득 찬 상태에서 바꿀 카드를 아직 안 골랐다면 장착되지 않으므로 그 사실을 먼저 알려 준다.
            string inventoryLine = draft.NeedsReplace && replaceIndex < 0
                ? _previewNeedsReplaceText
                : string.Format(_previewInventoryFormat, inventory.Equipped.Count, inventory.SlotCount, after.Count, after.SlotCount);

            string info = card.IsActive
                ? string.Format(_activeInfoFormat, _coordinator.ActiveUsableRounds)
                : _passiveInfoText;

            if (_previewParts != null)
            {
                bool needsDiscard = draft.NeedsReplace && replaceIndex < 0;
                _previewParts.Row1Label.text = _previewInventoryLabel;
                _previewParts.Row1Value.text = needsDiscard
                    ? _previewNeedsReplaceText
                    : string.Format(_previewInventoryValueFormat, inventory.Equipped.Count, inventory.SlotCount, after.Count, after.SlotCount);
                _previewParts.Row2Label.text = _previewTypeLabel;
                _previewParts.Row2Value.text = string.Format(_previewPairFormat, after.ActiveCount, after.PassiveCount);
                _previewParts.CardBox.SetActive(true);
                _previewParts.CardName.text = EffectCardFace.GetCodeText(card) + " " + card.DisplayName;
                _previewParts.CardDescription.text = card.Description;
                _previewParts.CardInfo.text = info;
            }

            _previewText.text = _previewHeader + "\n\n"
                + inventoryLine + "\n"
                + string.Format(_previewTypeFormat, after.ActiveCount, after.PassiveCount) + "\n\n"
                + EffectCardFace.GetCodeText(card) + " " + card.DisplayName + "\n" + card.Description + "\n" + info;
        }
    }
}
