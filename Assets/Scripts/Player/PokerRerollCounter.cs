namespace Poker_Card
{
    public class PokerRerollCounter
    {
        private readonly int _maxCount;
        private int _remaining;

        public int Remaining => _remaining;
        public int MaxCount => _maxCount;

        public PokerRerollCounter(int maxCount)
        {
            _maxCount = maxCount;
            _remaining = maxCount;
        }

        public bool TryConsume()
        {
            if (_remaining <= 0)
            {
                return false;
            }

            _remaining--;
            return true;
        }

        public void Reset()
        {
            _remaining = _maxCount;
        }
    }
}
