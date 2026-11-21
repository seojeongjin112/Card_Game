using Core;
using Effect_Card;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 영웅·전설을 얻었을 때 화면 가운데에 카드가 뒤집히며 커졌다가 사라지고, 뒤에서 광선이 도는 연출(프로토타입의 획득 화면).
    public class UiFxGetCard : MonoBehaviour
    {
        [SerializeField] private Font _font;
        [SerializeField] private Vector2 _cardSize = new Vector2(266f, 362f);
        [SerializeField] private float _raySize = 1200f;
        [SerializeField] private float _duration = 1.8f;
        [SerializeField] private float _rayTurnSeconds = 7f;
        [SerializeField] private float _popEnd = 0.3f;
        [SerializeField] private float _settleEnd = 0.4f;
        [SerializeField] private float _holdEnd = 0.85f;
        [SerializeField] private float _startScale = 0.2f;
        [SerializeField] private float _popScale = 1.25f;
        [SerializeField] private float _holdScale = 1.1f;
        [SerializeField] private float _endScale = 0.6f;
        [SerializeField] private float _startRotationY = 540f;
        [SerializeField] private float _exitDropDistance = 400f;
        [SerializeField] private float _rayStartScale = 0.3f;
        [SerializeField] private float _rayFadeInEnd = 0.25f;
        [SerializeField] private float _rayWedgeDegrees = 8f;
        [SerializeField] private float _rayPeriodDegrees = 18f;
        [SerializeField] private int _rayTextureSize = 256;
        [SerializeField] private Color _commonColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        [SerializeField] private Color _rareColor = new Color(0.3f, 0.55f, 0.95f, 1f);
        [SerializeField] private Color _epicColor = new Color(0.69f, 0.42f, 1f, 1f);
        [SerializeField] private Color _legendColor = new Color(1f, 0.82f, 0.25f, 1f);
        [SerializeField] private float _epicRayAlpha = 0.5f;
        [SerializeField] private float _legendRayAlpha = 0.6f;
        [SerializeField] private string _activeLabel = "액티브";
        [SerializeField] private string _passiveLabel = "패시브";
        [SerializeField] private string _activeFooterFormat = "액티브 · 첫 사용부터 {0}라운드";
        [SerializeField] private string _passiveFooterText = "패시브 · 장착 즉시 상시";
        [SerializeField] private string[] _gradeLetters = { "C", "R", "E", "L" };

        private RectTransform _root;
        private RectTransform _rayRect;
        private Image _rays;
        private CanvasGroup _cardGroup;
        private EffectCardFace _face;
        private float _age;
        private bool _active;

        private void Awake()
        {
            _root = (RectTransform)transform;

            var rayGo = new GameObject("Rays", typeof(RectTransform));
            rayGo.layer = gameObject.layer;
            rayGo.transform.SetParent(_root, false);
            _rayRect = (RectTransform)rayGo.transform;
            _rayRect.sizeDelta = new Vector2(_raySize, _raySize);
            _rays = rayGo.AddComponent<Image>();
            _rays.sprite = CreateRaySprite();
            _rays.raycastTarget = false;

            _face = EffectCardFace.Create(_root, _font, _cardSize);
            _cardGroup = _face.gameObject.AddComponent<CanvasGroup>();
            _cardGroup.blocksRaycasts = false;

            gameObject.SetActive(false);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Show(EffectCardData card, int activeRounds)
        {
            Color grade = GetGradeColor(card.Grade);
            string footer = card.IsActive ? string.Format(_activeFooterFormat, activeRounds) : _passiveFooterText;
            _face.Show(card, _activeLabel, _passiveLabel, footer, grade, _gradeLetters[(int)card.Grade]);

            float rayAlpha = card.Grade == EffectGrade.Legend ? _legendRayAlpha : _epicRayAlpha;
            var rayColor = grade;
            rayColor.a = rayAlpha;
            _rays.color = rayColor;

            _age = 0f;
            _active = true;
            gameObject.SetActive(true);
            Tick(0f);
        }

        public void Tick(float dt)
        {
            if (!_active)
            {
                return;
            }

            _age += dt;
            float t = _age / _duration;
            if (t >= 1f)
            {
                _active = false;
                gameObject.SetActive(false);
                return;
            }

            TickCard(t);
            TickRays(t);
        }

        // 뒤집히며 커지고(0~30%), 살짝 줄었다가(~40%), 머문 뒤(~85%), 작아지며 아래로 빠진다.
        private void TickCard(float t)
        {
            float scale;
            float rotationY = 0f;
            float drop = 0f;
            float alpha = 1f;

            if (t < _popEnd)
            {
                float k = t / _popEnd;
                scale = Mathf.LerpUnclamped(_startScale, _popScale, EaseOutBack(k));
                rotationY = Mathf.Lerp(_startRotationY, 0f, EaseOut(k));
                alpha = Mathf.Clamp01(k * 2f);
            }
            else if (t < _settleEnd)
            {
                scale = Mathf.Lerp(_popScale, _holdScale, (t - _popEnd) / (_settleEnd - _popEnd));
            }
            else if (t < _holdEnd)
            {
                scale = _holdScale;
            }
            else
            {
                float k = (t - _holdEnd) / (1f - _holdEnd);
                scale = Mathf.Lerp(_holdScale, _endScale, k);
                drop = _exitDropDistance * k;
                alpha = 1f - k;
            }

            var rect = _face.Rect;
            rect.localScale = Vector3.one * scale;
            rect.localRotation = Quaternion.Euler(0f, rotationY, 0f);
            rect.anchoredPosition = new Vector2(0f, -drop);
            _cardGroup.alpha = alpha;
        }

        private void TickRays(float t)
        {
            float alpha;
            float scale = 1f;
            if (t < _rayFadeInEnd)
            {
                float k = t / _rayFadeInEnd;
                alpha = k;
                scale = Mathf.Lerp(_rayStartScale, 1f, k);
            }
            else if (t < _holdEnd)
            {
                alpha = 1f;
            }
            else
            {
                alpha = 1f - (t - _holdEnd) / (1f - _holdEnd);
            }

            _rayRect.localScale = Vector3.one * scale;
            _rayRect.localRotation = Quaternion.Euler(0f, 0f, -_age / _rayTurnSeconds * 360f);
            _rays.canvasRenderer.SetAlpha(alpha);
        }

        private Color GetGradeColor(EffectGrade grade)
        {
            switch (grade)
            {
                case EffectGrade.Legend: return _legendColor;
                case EffectGrade.Epic: return _epicColor;
                case EffectGrade.Rare: return _rareColor;
                default: return _commonColor;
            }
        }

        // 각도마다 번갈아 켜지는 광선 무늬에 중심이 진하고 바깥이 옅은 마스크를 곱한 흰색 이미지. 색은 Image가 입힌다.
        private Sprite CreateRaySprite()
        {
            int size = _rayTextureSize;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) / 2f;
            float corner = center * Mathf.Sqrt(2f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float angle = (Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 360f) % _rayPeriodDegrees;
                    float wedge = angle < _rayWedgeDegrees ? 1f : 0f;
                    float radial = 1f - Mathf.SmoothStep(0.15f, 0.65f, Mathf.Sqrt(dx * dx + dy * dy) / corner);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, wedge * radial));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static float EaseOut(float t)
        {
            float inverse = 1f - Mathf.Clamp01(t);
            return 1f - inverse * inverse * inverse;
        }

        // 목표를 살짝 넘었다가 돌아오는 이징. 프로토타입의 cubic-bezier(.2,1.4,.4,1)처럼 통통 튀게 한다.
        private static float EaseOutBack(float t)
        {
            const float overshoot = 1.4f;
            float k = Mathf.Clamp01(t) - 1f;
            return 1f + (overshoot + 1f) * k * k * k + overshoot * k * k;
        }
    }
}
