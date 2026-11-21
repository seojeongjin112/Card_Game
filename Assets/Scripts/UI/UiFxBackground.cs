using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 배경에 떠다니는 카드 문양. 전투가 뜨거워지면(Pulse) 더 빨라지고 진해진다.
    public class UiFxBackground : MonoBehaviour
    {
        [SerializeField] private RectTransform _layer;
        [SerializeField] private Font _font;
        [SerializeField] private int _count = 26;
        [SerializeField] private string[] _glyphs = { "♠", "♥", "♣", "♦" };
        [SerializeField] private string _redGlyphs = "♥♦";
        [SerializeField] private Color _redColor = new Color(1f, 0.54f, 0.42f, 1f);
        [SerializeField] private Color _lightColor = new Color(0.85f, 0.96f, 0.91f, 1f);
        [SerializeField] private Vector2 _sizeRange = new Vector2(24f, 84f);
        [SerializeField] private Vector2 _speedRange = new Vector2(12f, 48f);
        [SerializeField] private Vector2 _alphaRange = new Vector2(0.04f, 0.12f);
        [SerializeField] private float _swayAmplitude = 19f;
        [SerializeField] private float _rotationSpeedDegrees = 17f;
        [SerializeField] private float _heatSpeedFactor = 3f;
        [SerializeField] private float _heatDecayPerSecond = 0.6f;
        [SerializeField] private float _heatMax = 1.5f;
        [SerializeField] private float _respawnMargin = 80f;

        private Text[] _texts;
        private float[] _x;
        private float[] _y;
        private float[] _speed;
        private float[] _rotation;
        private float[] _rotationSpeed;
        private float[] _alpha;
        private float[] _sway;
        private float _heat;
        private float _clock;

        public float Heat => _heat;

        // 코드로 만들 때는 Awake가 돌기 전에 레이어와 글꼴을 정해야 하므로, 꺼 둔 채로 만들고 값을 넣은 뒤 켠다.
        public static UiFxBackground Create(RectTransform parent, Font font)
        {
            var go = new GameObject("BackgroundSuits", typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            go.SetActive(false);
            var background = go.AddComponent<UiFxBackground>();
            background._layer = rect;
            background._font = font;
            go.SetActive(true);
            return background;
        }

        private void Awake()
        {
            _texts = new Text[_count];
            _x = new float[_count];
            _y = new float[_count];
            _speed = new float[_count];
            _rotation = new float[_count];
            _rotationSpeed = new float[_count];
            _alpha = new float[_count];
            _sway = new float[_count];

            for (int i = 0; i < _count; i++)
            {
                _texts[i] = CreateText();
                Respawn(i, true);
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // 전투가 격해지는 사건(피해, 발동, 컷인)마다 불러 문양이 한동안 빨라지게 한다.
        public void Pulse(float amount)
        {
            _heat = Mathf.Min(_heatMax, _heat + amount);
        }

        public void Tick(float dt)
        {
            _clock += dt;
            _heat = Mathf.Max(0f, _heat - _heatDecayPerSecond * dt);

            float halfHeight = _layer.rect.height / 2f;
            float speedFactor = 1f + _heat * _heatSpeedFactor;
            for (int i = 0; i < _count; i++)
            {
                _y[i] += _speed[i] * speedFactor * dt;
                _rotation[i] += _rotationSpeed[i] * dt;
                if (_y[i] > halfHeight + _respawnMargin)
                {
                    Respawn(i, false);
                }

                var rect = _texts[i].rectTransform;
                rect.anchoredPosition = new Vector2(_x[i] + Mathf.Sin(_clock + _sway[i]) * _swayAmplitude, _y[i]);
                rect.localRotation = Quaternion.Euler(0f, 0f, _rotation[i]);

                var color = _texts[i].color;
                color.a = Mathf.Min(1f, _alpha[i] * (1f + _heat));
                _texts[i].color = color;
            }
        }

        private Text CreateText()
        {
            var go = new GameObject("BgSuit", typeof(RectTransform));
            go.layer = _layer.gameObject.layer;
            go.transform.SetParent(_layer, false);
            var text = go.AddComponent<Text>();
            text.font = _font;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        // 처음에는 화면 어디에나 흩뿌리고, 위로 사라진 문양은 아래에서 다시 올라오게 한다.
        private void Respawn(int index, bool anywhere)
        {
            float halfWidth = _layer.rect.width / 2f;
            float halfHeight = _layer.rect.height / 2f;
            string glyph = _glyphs[Random.Range(0, _glyphs.Length)];

            _x[index] = Random.Range(-halfWidth, halfWidth);
            _y[index] = anywhere ? Random.Range(-halfHeight, halfHeight) : -halfHeight - _respawnMargin;
            _speed[index] = Random.Range(_speedRange.x, _speedRange.y);
            _rotation[index] = Random.Range(0f, 360f);
            _rotationSpeed[index] = Random.Range(-_rotationSpeedDegrees, _rotationSpeedDegrees);
            _alpha[index] = Random.Range(_alphaRange.x, _alphaRange.y);
            _sway[index] = Random.Range(0f, 6f);

            var text = _texts[index];
            text.text = glyph;
            text.fontSize = Mathf.RoundToInt(Random.Range(_sizeRange.x, _sizeRange.y));
            text.rectTransform.sizeDelta = new Vector2(text.fontSize * 2f, text.fontSize * 2f);
            text.color = _redGlyphs.Contains(glyph) ? _redColor : _lightColor;
        }
    }
}
