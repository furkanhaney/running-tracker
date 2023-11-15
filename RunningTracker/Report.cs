using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunningTracker
{
    public class Report
    {
        TimePeriod TimePeriod { get; set; }
        List<RunRecord> Runs { get; set; }

        public Report(TimePeriod TimePeriod, List<RunRecord> runs)
        {
            this.TimePeriod = TimePeriod;
            Runs = runs;
        }

        public DateTime StartDate
        {
            get
            {
                if (TimePeriod == TimePeriod.Weekly)
                {
                    DateTime date = Runs[0].Date;
                    while (date.DayOfWeek != DayOfWeek.Monday)
                        date -= TimeSpan.FromDays(1);
                    return date;
                }
                else
                {
                    return new DateTime(Runs[0].Date.Year, Runs[0].Date.Month, 1);
                }
            }
        }
        public int Workouts
        {
            get
            {
                return Runs.Count;
            }
        }
        public double Distance
        {
            get
            {
                return Runs.Sum(o => o.Distance);
            }
        }
        public double Weight
        {
            get
            {
                return Runs.Average(o => o.Weight);
            }
        }
        public TimeSpan Duration
        {
            get
            {
                return TimeSpan.FromHours(Runs.Sum(o => o.Duration.TotalHours));
            }
        }
        public double Speed
        {
            get
            {
                return Distance / Duration.TotalHours;
            }
        }
    }

    public enum TimePeriod { Weekly, Monthly }
}
