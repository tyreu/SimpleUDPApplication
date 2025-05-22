namespace Client
{
    public class QuantileEstimator
    {
        private readonly List<double> _samples = new();
        private readonly object _lock = new();

        public void Add(double value)
        {
            lock (_lock) _samples.Add(value);
        }

        public double GetQuantile(double q)
        {
            lock (_lock)
            {
                if (_samples.Count == 0) return 0.0;
                var sorted = _samples.OrderBy(x => x).ToArray();
                int index = (int)Math.Floor(q * (sorted.Length - 1));
                return sorted[index];
            }
        }
    }
}
