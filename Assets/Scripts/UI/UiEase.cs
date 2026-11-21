using UnityEngine;

namespace Poker_Card
{
    // 프로토타입 CSS의 cubic-bezier(x1, y1, x2, y2) 이징을 그대로 옮긴 것. 1을 넘는 y 값으로 살짝 튕기는 느낌을 낸다.
    public static class UiEase
    {
        private const int SolveIterations = 12;

        public static float CubicBezier(float x1, float y1, float x2, float y2, float t)
        {
            t = Mathf.Clamp01(t);

            // x(s) = t가 되는 곡선 매개변수 s를 이분법으로 찾고, 그 s의 y 값을 돌려준다.
            float low = 0f;
            float high = 1f;
            float s = t;
            for (int i = 0; i < SolveIterations; i++)
            {
                s = (low + high) * 0.5f;
                if (Sample(x1, x2, s) < t)
                {
                    low = s;
                }
                else
                {
                    high = s;
                }
            }

            return Sample(y1, y2, s);
        }

        private static float Sample(float a, float b, float s)
        {
            float inverse = 1f - s;
            return 3f * inverse * inverse * s * a + 3f * inverse * s * s * b + s * s * s;
        }
    }
}
