using Effect_Card;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 라운드 시작에 효과 카드 3장 중 1장을 고르는 화면. 슬롯이 가득 차면 버릴 카드도 함께 고른다.
    public class EffectDraftView : MonoBehaviour
    {
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

        private EffectBattleCoordinator _coordinator;
        private int _selectedOffer = -1;
        private int _selectedDiscard = -1;
        private bool _wasOpen;

        private void Awake()
        {
            for (int i = 0; i < _offerSlots.Length; i++)
            {
                _offerSlots[i].Initialize(i);
            }

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

        private void OnOfferClicked(int index)
        {
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
            if (_selectedOffer < 0 || _selectedOffer >= draft.Offer.Count)
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
            _coordinator.Player.Draft.Skip();
            Refresh();
        }

        private void Refresh()
        {
            if (_coordinator == null || !_coordinator.Player.Draft.IsOpen)
            {
                _wasOpen = false;
                _panel.SetActive(false);
                return;
            }

            if (!_wasOpen)
            {
                _selectedOffer = -1;
                _selectedDiscard = -1;
                _wasOpen = true;
            }

            _panel.SetActive(true);
            var side = _coordinator.Player;
            var draft = side.Draft;

            ShowOffers(draft);
            ShowEquipped(side, draft.NeedsReplace);
            UpdateHintAndConfirm(draft);
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

                var card = draft.Offer[i];
                string type = card.IsActive ? _activeLabel : _passiveLabel;
                _offerSlots[i].Show(card, string.Format(_offerFormat, type, card.Description), false);
                _offerSlots[i].SetInteractable(true);
                _offerSlots[i].SetSelected(i == _selectedOffer);
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
            UpdatePreview(draft);
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

            _previewText.text = _previewHeader + "\n\n"
                + inventoryLine + "\n"
                + string.Format(_previewTypeFormat, after.ActiveCount, after.PassiveCount) + "\n\n"
                + card.DisplayName + "\n" + card.Description + "\n" + info;
        }
    }
}
