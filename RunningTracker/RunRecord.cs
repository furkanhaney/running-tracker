using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunningTracker
{
    public class RunRecord
    {
        public DateTime Date { get; set; }
        public double Hours { get; set; }
        public double Distance { get; set; }
        public double Incline { get; set; }
        public double Weight { get; set; }
        public string Place { get; set; }
        public string Tag { get; set; }

        public double Speed
        {
            get
            {
                return Distance / Hours;
            }
        }
        public TimeSpan Pace
        {
            get
            {
                return TimeSpan.FromHours(Hours / Distance);
            }
        }
        public double Calories
        {
            get
            {
                return 0.863 * Distance * Weight;
            }
        }
        public TimeSpan Duration
        {
            get
            {
                return TimeSpan.FromHours((double)Hours);
            }
        }
    }
}
