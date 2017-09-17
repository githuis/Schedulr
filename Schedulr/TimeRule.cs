using System;
using System.Collections.Generic;
using System.Text;

namespace Schedulr
{
    class TimeRule
    {
    }

    public class TimeSpacCalculator
    {
        public static TimeSpan GetTimeSpanIntersect(TimeShift input, TimeSpan start, TimeSpan end)
        {
            // Loopsback input from 23:59 - 00:00
            if (input.Start > input.End)
                return GetTimeSpanIntersect(new TimeShift(input.Start, TimeSpan.FromHours(24)), start, end) +
                       GetTimeSpanIntersect(new TimeShift(TimeSpan.FromHours(0), input.End), start, end);

            // Loopsback Shift from 23:59 - 00:00
            if (start > end)
                return GetTimeSpanIntersect(input, new TimeSpan(), end) +
                       GetTimeSpanIntersect(input, start, TimeSpan.FromHours(24));
            if (input.End < start)
                return new TimeSpan();

            if (input.Start > end)
                return new TimeSpan();

            var actualStart = input.Start < start
                ? start
                : input.Start;

            var actualEnd = input.End > end
                ? end
                : input.End;

            return actualEnd - actualStart;
        }
    }
    public class TimeRange : TimeShift
    {
        public TimeRange(string name, TimeSpan start, TimeSpan end) : base(start, end)
        {
            Name = name;
        }

        public string Name { get; set; }
    }

    public class TimeShift
    {
        public TimeShift(TimeSpan start, TimeSpan end)
        {
            Start = start;
            End = end;
        }

        public virtual TimeSpan Start { get; set; }
        public virtual TimeSpan End { get; set; }
    }
}
