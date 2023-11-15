using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunningTracker
{
    public class PersonalBest
    {
        public int Age { get; set; }
        public Sex Sex { get; set; }
        
        public DateTime Date { get; set; }
        public DistanceInfo DistanceInfo { get; set; }
        public TimeSpan Duration { get; set; }
        
        public double Distance
        {
            get
            {
                return DistanceInfo.Distance;
            }
        }
        public double Speed { get { return DistanceInfo.Distance / Duration.TotalHours; } }
        public double Score
        {
            get
            {
                return DistanceInfo.GetScore(Duration, Age, Sex);
            }
        }
    }
}
