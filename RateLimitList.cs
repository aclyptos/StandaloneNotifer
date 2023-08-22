namespace StandaloneNotifier
{
    internal class RateLimitList
    {
        private readonly int _maxRequests;
        private readonly int _rateLimitTimeFrameSeconds;
        private readonly List<DateTime> _list;

        public RateLimitList(int maxRequests, int rateLimitTimeFrameSeconds) 
        {
            _list = new List<DateTime>();
            _maxRequests = maxRequests;
            _rateLimitTimeFrameSeconds = rateLimitTimeFrameSeconds;
        }

        public void RequestMade()
        {
            _list.Add(DateTime.UtcNow);
            if(_list.Count > (_maxRequests + 25)) _list.RemoveAt(0);
        }

        public bool IsRateLimitExceeded()
        {
            return _list.Count(x => DateTime.UtcNow.Subtract(x).TotalSeconds <= _rateLimitTimeFrameSeconds) >= _maxRequests;
        }
    }
}
