namespace Core
{
    // 시작 화면에서 고른 값을 전투 씬으로 넘기는 통로. 씬이 바뀌어도 static 값은 남는다.
    // 시작 화면을 거치지 않고 전투 씬만 바로 실행하면 값이 없으므로, 그때는 전투 쪽 인스펙터 값을 그대로 쓴다.
    public static class GameSession
    {
        private static EnemyDifficulty _difficulty = EnemyDifficulty.Normal;

        public static bool HasDifficulty { get; private set; }

        public static EnemyDifficulty Difficulty
        {
            get => _difficulty;
            set
            {
                _difficulty = value;
                HasDifficulty = true;
            }
        }
    }
}
