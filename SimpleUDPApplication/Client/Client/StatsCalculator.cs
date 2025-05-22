namespace Client
{
    public class StatsCalculator
    {
        private long _count = 0;
        private double _mean = 0;
        private double _m2 = 0;
        private readonly object _lock = new();

        private readonly List<double> _values = new();
        private readonly Dictionary<double, int> _frequencies = new();
        private double _mode = 0;
        private int _modeFrequency = 0;

        public void Add(double value)
        {
            lock (_lock)
            {
                _values.Add(value);
                _count++;

                double delta = value - _mean;
                _mean += delta / _count;
                _m2 += delta * (value - _mean);

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
                double median = 0;
                if (_count > 0)
                {
                    var sorted = _values.OrderBy(x => x).ToList();
                    median = (_count % 2 == 0) ?
                        (sorted[(int)(_count / 2)] + sorted[(int)(_count / 2) - 1]) / 2 :
                        sorted[(int)(_count / 2)];
                }
                return (_count, _mean, stddev, median, _mode);
            }
        }
    }
}
