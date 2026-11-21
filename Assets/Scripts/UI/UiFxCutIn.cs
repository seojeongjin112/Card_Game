using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 영웅·전설 액티브를 쓸 때 화면을 어둡게 하고 가로 띠로 카드 이름과 설명을 보여 주는 필살기 컷인.
    public class UiFxCutIn : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Image _dim;
        [SerializeField] private RectTransform _band;
        [SerializeField] private Image _bandImage;
        [SerializeField] private Text _gradeText;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _descriptionText;
        [SerializeField] private float _dimOpacity = 0.7f;
        [SerializeField] private float _slideIn = 0.18f;
        [SerializeField] private float _slideOut = 0.22f;
        [SerializeField] private float _slideDistance = 2200f;
        [SerializeField] private float _bandOpacity = 0.95f;

        private float _time;
        private float _total;

        private void Awake()
        {
            _root.SetActive(false);
            enabled = false;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Show(string grade, string cardName, string description, Color color, float duration)
        {
            _gradeText.text = grade;
            _nameText.text = cardName;
            _descriptionText.text = description;
            color.a = _bandOpacity;
            _bandImage.color = color;
            _total = duration;
            _time = 0f;
            _root.SetActive(true);
            enabled = true;
        }

        public void Tick(float dt)
        {
            if (_time >= _total)
            {
                _root.SetActive(false);
                enabled = false;
                return;
            }

            _time += dt;
            float outStart = _total - _slideOut;

            // 어두운 배경은 들어올 때 서서히 켜지고 나갈 때 꺼지며, 띠는 왼쪽에서 들어와 오른쪽으로 빠진다.
            float x = 0f;
            float dim = _dimOpacity;
            if (_time < _slideIn)
            {
                float t = _time / _slideIn;
                x = -_slideDistance * (1f - EaseOut(t));
                dim *= t;
            }
            else if (_time > outStart)
            {
                float t = Mathf.Clamp01((_time - outStart) / _slideOut);
                x = _slideDistance * EaseOut(t);
                dim *= 1f - t;
            }

            _band.anchoredPosition = new Vector2(x, 0f);
            _dim.color = new Color(0f, 0f, 0f, dim);
        }

        private static float EaseOut(float t)
        {
            float inverse = 1f - Mathf.Clamp01(t);
            return 1f - inverse * inverse * inverse;
        }
    }
}
