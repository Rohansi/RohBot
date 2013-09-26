using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SteamMobile
{
    public class TaskScheduler
    {
        private class Task
        {
            public double Delay;
            public double Accumulator;
            public Action Callback;

            public Task(double delay, Action callback)
            {
                Delay = delay;
                Accumulator = 0;
                Callback = callback;
            }
        }

        private List<Task> _tasks;
        private Stopwatch _timer;

        public TaskScheduler()
        {
            _tasks = new List<Task>();
            _timer = Stopwatch.StartNew();
        }

        public void Add(TimeSpan delay, Action callback)
        {
            lock (_tasks)
                _tasks.Add(new Task(delay.TotalSeconds, callback));
        }

        public void Run()
        {
            lock (_tasks)
            {
                var timeOffset = _timer.Elapsed.TotalSeconds;
                _timer.Restart();

                foreach (var task in _tasks)
                {
                    task.Accumulator += timeOffset;

                    if (task.Accumulator < task.Delay)
                        continue;

                    task.Callback();
                    task.Accumulator -= task.Delay;
                }
            }
        }
    }
}
