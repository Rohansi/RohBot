using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RohBot
{
    public class DelayManager
    {
        private const double DelayThreshold = 100;
        private const double DelayLimit = 500;
        private const double DecayRate = 3;

        public const double Authenticate = DecayRate * 6;
        public const double Database = DecayRate * 4;
        public const double Message = DecayRate * 2;

        private Dictionary<string, double> _delays;
        private Stopwatch _timer;

        public DelayManager()
        {
            _delays = new Dictionary<string, double>();
            _timer = Stopwatch.StartNew();
        }

        public void Update()
        {
            lock (_delays)
            {
                var delta = _timer.Elapsed.TotalSeconds;
                _timer.Restart();

                foreach (var k in _delays.Keys.ToList())
                {
                    _delays[k] -= DecayRate * delta;
                }

                _delays.RemoveAll(kv => kv.Value <= 0);
            }
        }

        public bool AddAndCheck(Connection connection, double cost)
        {
            lock (_delays)
            {
                double delay;
                if (!_delays.TryGetValue(connection.Address, out delay))
                {
                    if (cost > 0)
                        _delays.Add(connection.Address, Math.Min(cost, DelayLimit));
                }
                else
                {
                    _delays[connection.Address] = Math.Min(delay + cost, DelayLimit);
                }

                var shouldDelay = (delay + cost) >= DelayThreshold;

                if (shouldDelay)
                    connection.SendSysMessage("Too many requests are coming from your location and your request has been canceled. Please wait and try again in a few minutes.");

                return shouldDelay;
            }
        }

        public bool Check(Connection connection)
        {
            return AddAndCheck(connection, 0);
        }
    }
}
