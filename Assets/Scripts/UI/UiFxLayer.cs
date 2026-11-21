using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 화면 전체에 걸리는 연출(번쩍임, 흔들림, 확대 펀치, 배너). 모든 움직임을 Tick 하나로 돌려서 시험 때 시간을 직접 흘릴 수 있다.
    public class UiFxLayer : MonoBehaviour
    {
        [SerializeField] private Image _flashImage;
        [SerializeField] private RectTransform _shakeTarget;
        [SerializeField] private RectTransform _bannerRoot;
        [SerializeField] private Image _bannerBand;
        [SerializeField] private Text _bannerText;
        [SerializeField] private Text _bannerSub;

        // 프로토타입의 '동작 줄이기' 옵션: 0이면 흔들림을 끄고, 1.7이면 강하게 흔든다.
        [SerializeField, Range(0f, 2f)] private float _shakeScale = 1f;
        [SerializeField] private bool _flashLow;
        [SerializeField] private float _lowFlashOpacityLimit = 0.12f;
        [SerializeField] private float _lowFlashOpacityFactor = 0.25f;
        [SerializeField] private float _lowFlashSlowFactor = 1.6f;
        [SerializeField] private float _shakeAmplitude = 10f;
        [SerializeField] private float _shakeRotationDegrees = 0.6f;
        [SerializeField] private float _shakeBaseDuration = 0.38f;
        [SerializeField] private float _shakeDurationPerPower = 0.12f;
        [SerializeField] private float _punchDuration = 0.26f;
        [SerializeField] private float _bannerSlideIn = 0.22f;
        [SerializeField] private float _bannerHold = 0.6f;
        [SerializeField] private float _bannerSlideOut = 0.22f;
        [SerializeField] private float _bannerSlideDistance = 2200f;
        [SerializeField] private float _bannerTextStartScale = 2.2f;
        [SerializeField] private float _rainbowSpeed = 1.5f;

        private Color _flashColor;
        private float _flashOpacity;
        private float _flashTime;
        private float _flashDuration;

        private float _shakePower;
        private float _shakeTime;
        private float _shakeDuration;

        private float _punchScale = 1f;
        private float _punchTime;

        private float _bannerTime;
        private float _bannerTotal;
        private bool _bannerRainbow;
        private Color _bannerColor;

        private bool IsFlashing => _flashTime < _flashDuration;
        private bool IsShaking => _shakeTime < _shakeDuration;
        private bool IsPunching => _punchTime < _punchDuration;
        private bool IsBannerShowing => _bannerTime < _bannerTotal;

        private void Awake()
        {
            _flashTime = _flashDuration = 0f;
            _shakeTime = _shakeDuration = 0f;
            _punchTime = _punchDuration;
            _bannerTime = _bannerTotal = 0f;
            _flashImage.color = Color.clear;
            _bannerRoot.gameObject.SetActive(false);
            enabled = false;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Flash(Color color, float opacity, float duration)
        {
            // 섬광 줄이기: 약하고 느리게 보여 눈이 덜 부시게 한다.
            if (_flashLow)
            {
                opacity = Mathf.Min(opacity * _lowFlashOpacityFactor, _lowFlashOpacityLimit);
                duration *= _lowFlashSlowFactor;
            }

            _flashColor = color;
            _flashOpacity = opacity;
            _flashDuration = duration;
            _flashTime = 0f;
            enabled = true;
        }

        public void Shake(float power)
        {
            if (_shakeScale <= 0f)
            {
                return;
            }

            _shakePower = power;
            _shakeDuration = _shakeBaseDuration + power * _shakeDurationPerPower;
            _shakeTime = 0f;
            enabled = true;
        }

        public void Punch(float scale)
        {
            if (_shakeScale <= 0f)
            {
                return;
            }

            _punchScale = scale;
            _punchTime = 0f;
            enabled = true;
        }

        public void ShowBanner(string text, string sub, Color color, bool rainbow)
        {
            _bannerText.text = text;
            _bannerSub.text = sub;
            _bannerSub.gameObject.SetActive(!string.IsNullOrEmpty(sub));
            _bannerColor = color;
            _bannerRainbow = rainbow;
            _bannerTotal = _bannerSlideIn + _bannerHold + _bannerSlideOut;
            _bannerTime = 0f;
            _bannerRoot.gameObject.SetActive(true);
            enabled = true;
        }

        public void Tick(float dt)
        {
            TickFlash(dt);
            TickShake(dt);
            TickPunch(dt);
            TickBanner(dt);
            enabled = IsFlashing || IsShaking || IsPunching || IsBannerShowing;
        }

        private void TickFlash(float dt)
        {
            if (!IsFlashing)
            {
                _flashImage.color = Color.clear;
                return;
            }

            _flashTime += dt;
            float fade = 1f - Mathf.Clamp01(_flashTime / _flashDuration);
            var color = _flashColor;
            color.a = _flashOpacity * fade;
            _flashImage.color = color;
        }

        private void TickShake(float dt)
        {
            if (_shakeTarget == null)
            {
                return;
            }

            if (!IsShaking)
            {
                _shakeTarget.anchoredPosition = Vector2.zero;
                _shakeTarget.localRotation = Quaternion.identity;
                return;
            }

            _shakeTime += dt;
            float decay = 1f - Mathf.Clamp01(_shakeTime / _shakeDuration);
            float amplitude = _shakeAmplitude * _shakePower * _shakeScale * decay;
            float rotation = _shakeRotationDegrees * _shakePower * _shakeScale * decay;
            _shakeTarget.anchoredPosition = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * amplitude;
            _shakeTarget.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-1f, 1f) * rotation);
        }

        private void TickPunch(float dt)
        {
            if (_shakeTarget == null)
            {
                return;
            }

            if (!IsPunching)
            {
                _shakeTarget.localScale = Vector3.one;
                return;
            }

            _punchTime += dt;
            float wave = Mathf.Sin(Mathf.Clamp01(_punchTime / _punchDuration) * Mathf.PI);
            _shakeTarget.localScale = Vector3.one * (1f + (_punchScale - 1f) * wave);
        }

        private void TickBanner(float dt)
        {
            if (!IsBannerShowing)
            {
                _bannerRoot.gameObject.SetActive(false);
                return;
            }

            _bannerTime += dt;
            float outStart = _bannerSlideIn + _bannerHold;

            // 들어올 때는 왼쪽에서, 나갈 때는 오른쪽으로 빠진다.
            float x = 0f;
            if (_bannerTime < _bannerSlideIn)
            {
                x = -_bannerSlideDistance * (1f - EaseOut(_bannerTime / _bannerSlideIn));
            }
            else if (_bannerTime > outStart)
            {
                x = _bannerSlideDistance * EaseOut((_bannerTime - outStart) / _bannerSlideOut);
            }

            ((RectTransform)_bannerBand.transform).anchoredPosition = new Vector2(x, 0f);

            // 글자는 크게 시작해 제자리로 줄어들며 찍히는 느낌을 준다.
            float pop = EaseOut(Mathf.Clamp01(_bannerTime / _bannerSlideIn));
            _bannerText.transform.localScale = Vector3.one * Mathf.Lerp(_bannerTextStartScale, 1f, pop);
            _bannerText.transform.localPosition = new Vector3(x, _bannerText.transform.localPosition.y, 0f);
            _bannerSub.transform.localPosition = new Vector3(x, _bannerSub.transform.localPosition.y, 0f);

            var band = _bannerRainbow ? Color.HSVToRGB(Mathf.Repeat(_bannerTime * _rainbowSpeed, 1f), 0.7f, 1f) : _bannerColor;
            band.a = _bannerBand.color.a;
            _bannerBand.color = band;
        }

        private static float EaseOut(float t)
        {
            float inverse = 1f - Mathf.Clamp01(t);
            return 1f - inverse * inverse * inverse;
        }
    }
}
