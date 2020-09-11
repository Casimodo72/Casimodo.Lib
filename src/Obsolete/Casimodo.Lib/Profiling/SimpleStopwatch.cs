using System;
using System.Diagnostics;

namespace Casimodo.Lib
{
    public class SimpleStopwatch : Stopwatch
    {
        public SimpleStopwatch()
        { }

        public long TicksAtStart;

        public new SimpleStopwatch Start()
        {
            base.Start();
            TicksAtStart = ElapsedTicks;

            return this;
        }

        public double CurMs
        {
            get { return ToMs(ElapsedTicks - TicksAtStart); }
        }

        public double ToMs(long ticks)
        {
            return Math.Round((double)ticks / Stopwatch.Frequency * 1000, 2);
        }

        public SimpleStopwatch O(string text)
        {
            Debug.WriteLine(text + " : " + ToMs(ElapsedTicks - TicksAtStart) + " ms");
            return this;
        }

        public SimpleStopwatch O(string text, long ticks)
        {
            Debug.WriteLine(text + " : " + Math.Round((double)ticks / Stopwatch.Frequency * 1000, 2) + " ms");

            return this;
        }
    }
}