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

        private List<Task> tasks;
        private Stopwatch timer;

        public TaskScheduler()
        {
            tasks = new List<Task>();
            timer = Stopwatch.StartNew();
        }

        public void Add(TimeSpan delay, Action callback)
        {
            lock (tasks)
                tasks.Add(new Task(delay.TotalSeconds, callback));
        }

        public void Run()
        {
            lock (tasks)
            {
                var timeOffset = timer.Elapsed.TotalSeconds;
                timer.Restart();

                foreach (var task in tasks)
                {
                    task.Accumulator += timeOffset;
                    if (task.Accumulator > task.Delay)
                    {
                        task.Callback();
                        task.Accumulator -= task.Delay;
                    }
                }
            }
        }
    }
}