namespace Client
{
    public class StatsCalculator
    {
        private long _count;
        private double _mean;
        private double _m2;
        private readonly object _lock = new();

        private readonly Dictionary<double, long> _frequencies = new();
        private double _mode;
        private long _modeFrequency;
        private readonly QuantileEstimator _quantile = new();

        public void Add(double value)
        {
            lock (_lock)
            {
                _count++;
                double delta = value - _mean;
                _mean += delta / _count;
                _m2 += delta * (value - _mean);

                _quantile.Add(value);

                if (_frequencies.TryGetValue(value, out var freq))
                    _frequencies[value] = ++freq;
                else
                    _frequencies[value] = 1;

                if (_frequencies[value] > _modeFrequency)
                {
                    _modeFrequency = _frequencies[value];
                    _mode = value;
                }
            }
        }

        public (long Count, double Mean, double StdDev, double Median, double Mode) GetStats()
        {
            lock (_lock)
            {
                double stddev = _count > 1 ? Math.Sqrt(_m2 / (_count - 1)) : 0.0;
                double median = _quantile.GetQuantile(0.5);
                return (_count, _mean, stddev, median, _mode);
            }
        }
    }
}
