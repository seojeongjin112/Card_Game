using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 16:9로 배치한 화면이 창 모양과 상관없이 잘리지 않고 통째로 보이게 스케일 기준을 맞춘다.
    [RequireComponent(typeof(CanvasScaler))]
    public class StageFit : MonoBehaviour
    {
        private CanvasScaler _scaler;

        private void Awake()
        {
            _scaler = GetComponent<CanvasScaler>();
        }

        private void OnEnable()
        {
            Fit();
        }

        // 캔버스 크기가 바뀔 때(창 크기 변경)만 불리므로 매 프레임 확인하지 않는다.
        private void OnRectTransformDimensionsChange()
        {
            if (_scaler != null)
            {
                Fit();
            }
        }

        private void Fit()
        {
            float stageAspect = _scaler.referenceResolution.x / _scaler.referenceResolution.y;
            float screenAspect = Screen.width / (float)Mathf.Max(1, Screen.height);

            // 창이 16:9보다 가로로 길면 높이에, 세로로 길면 너비에 맞춰야 양쪽 모두 잘리지 않는다.
            _scaler.matchWidthOrHeight = screenAspect > stageAspect ? 1f : 0f;
        }
    }
}
