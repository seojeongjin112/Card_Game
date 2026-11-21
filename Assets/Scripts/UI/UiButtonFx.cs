using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Poker_Card
{
    // 프로토타입 버튼의 두 효과: 주요 버튼이 천천히 떠올랐다 내려오는 것(btnIdle)과, 마우스를 올릴 때 빛이 한 번 지나가는 것(shine).
    // 버튼 자체는 건드리지 않고 이 컴포넌트만 붙이면 되며, 빛 띠는 코드가 자식으로 만든다.
    [RequireComponent(typeof(Button))]
    public class UiButtonFx : MonoBehaviour, IPointerEnterHandler
    {
        private const int ShineTextureWidth = 64;

        [SerializeField] private bool _idle = true;
        [SerializeField] private bool _shine = true;
        [SerializeField] private float _idleAmplitude = 4f;
        [SerializeField] private float _idlePeriod = 2.2f;
        [SerializeField] private float _shineSeconds = 0.55f;
        [SerializeField] private float _shineWidthFactor = 0.6f;
        [SerializeField] private float _shineTiltDegrees = 20f;
        [SerializeField] private float _shineTravelFactor = 1.3f;
        [SerializeField, Range(0f, 1f)] private float _shineAlpha = 0.55f;

        private Button _button;
        private RectTransform _band;
        private float _idleClock;
        private float _shineClock = -1f;
        private Vector3 _restPosition;
        private Vector3 _lastApplied;
        private bool _hasApplied;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_shine)
            {
                BuildShine();
            }
        }

        private void OnDisable()
        {
            // 꺼지는 동안 위치가 어긋난 채 남지 않도록 원래 자리로 돌린다.
            if (_hasApplied)
            {
                transform.localPosition = _restPosition;
                _hasApplied = false;
            }

            _shineClock = -1f;
            if (_band != null)
            {
                _band.gameObject.SetActive(false);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_shine && _button.interactable)
            {
                _shineClock = 0f;
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // 테스트가 시간을 직접 넘길 수 있게 Tick을 연다.
        public void Tick(float dt)
        {
            ApplyIdle(dt);
            ApplyShine(dt);
        }

        // 눌러서 쓸 수 있을 때만 떠오른다(프로토타입: :not([disabled])). 막히면 제자리로 내려온다.
        private void ApplyIdle(float dt)
        {
            float lift = 0f;
            if (_idle && _button.interactable)
            {
                _idleClock += dt;
                lift = _idleAmplitude * (1f - Mathf.Cos(_idleClock / _idlePeriod * Mathf.PI * 2f)) * 0.5f;
            }

            // 레이아웃이 자리를 다시 잡으면 그 자리를 새 기준으로 삼고, 아니면 지난번 기준에 오프셋만 더한다.
            if (!_hasApplied || (transform.localPosition - _lastApplied).sqrMagnitude > 0.0001f)
            {
                _restPosition = transform.localPosition;
            }

            transform.localPosition = _restPosition + Vector3.up * lift;
            _lastApplied = transform.localPosition;
            _hasApplied = true;
        }

        private void ApplyShine(float dt)
        {
            if (_band == null)
            {
                return;
            }

            if (_shineClock < 0f)
            {
                _band.gameObject.SetActive(false);
                return;
            }

            _shineClock += dt;
            float k = Mathf.Clamp01(_shineClock / _shineSeconds);
            if (k >= 1f)
            {
                _shineClock = -1f;
                _band.gameObject.SetActive(false);
                return;
            }

            // ease-out으로 오른쪽 바깥에서 왼쪽 바깥까지 지나간다(프로토타입 shine: 130% → -130%).
            float eased = 1f - (1f - k) * (1f - k);
            float width = ((RectTransform)transform).rect.width;
            _band.gameObject.SetActive(true);
            _band.anchoredPosition = new Vector2(Mathf.Lerp(width * _shineTravelFactor, -width * _shineTravelFactor, eased), 0f);
        }

        // 버튼 안쪽에서만 보이도록 마스크 안에 비스듬한 흰 띠를 둔다.
        private void BuildShine()
        {
            var maskGo = new GameObject("Shine", typeof(RectTransform));
            maskGo.layer = gameObject.layer;
            maskGo.transform.SetParent(transform, false);
            var maskRect = (RectTransform)maskGo.transform;
            maskRect.anchorMin = Vector2.zero;
            maskRect.anchorMax = Vector2.one;
            maskRect.offsetMin = maskRect.offsetMax = Vector2.zero;
            maskGo.AddComponent<RectMask2D>();

            var bandGo = new GameObject("Band", typeof(RectTransform));
            bandGo.layer = gameObject.layer;
            bandGo.transform.SetParent(maskRect, false);
            _band = (RectTransform)bandGo.transform;
            _band.anchorMin = _band.anchorMax = _band.pivot = new Vector2(0.5f, 0.5f);
            var rect = ((RectTransform)transform).rect;
            _band.sizeDelta = new Vector2(rect.width * _shineWidthFactor, rect.height * 2f);
            _band.localRotation = Quaternion.Euler(0f, 0f, _shineTiltDegrees);

            var image = bandGo.AddComponent<Image>();
            image.sprite = CreateShineSprite();
            image.raycastTarget = false;
            maskGo.SetActive(true);
            bandGo.SetActive(false);
        }

        // 가운데가 가장 밝고 양옆으로 투명해지는 띠.
        private Sprite CreateShineSprite()
        {
            var texture = new Texture2D(ShineTextureWidth, 2, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int x = 0; x < ShineTextureWidth; x++)
            {
                float t = (float)x / (ShineTextureWidth - 1);
                float alpha = Mathf.Clamp01(1f - Mathf.Abs(t - 0.5f) * 2f) * _shineAlpha;
                var color = new Color(1f, 1f, 1f, alpha);
                texture.SetPixel(x, 0, color);
                texture.SetPixel(x, 1, color);
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, ShineTextureWidth, 2), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
