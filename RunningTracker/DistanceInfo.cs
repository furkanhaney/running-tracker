using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunningTracker
{
    [Serializable]
    public class DistanceInfo
    {
        public string Name;
        public double Distance;
        public TimeSpan MaleWorldRecord;
        public TimeSpan FemaleWorldRecord;

        public double[] maleFactors = new double[96];
        public double[] femaleFactors = new double[96];

        public override int GetHashCode()
        {
            var hash = Name.GetHashCode();
            hash += MaleWorldRecord.GetHashCode();
            hash += FemaleWorldRecord.GetHashCode();
            foreach (var m in maleFactors)
                hash += m.GetHashCode();
            foreach (var f in femaleFactors)
                hash += f.GetHashCode();
            return hash;
        }

        double GetFactor(int age, Sex sex)
        {
            if (sex == Sex.Male){
                if (age < 5)
                    return maleFactors[0];
                if (age > 100)
                    return maleFactors[95];
                return maleFactors[age - 5];
            }
            else
            {
                if (age < 5)
                    return femaleFactors[0];
                if (age > 100)
                    return femaleFactors[95];
                return femaleFactors[age - 5];
            }
        }

        public double GetScore(TimeSpan duration, int age, Sex sex)
        {
            return 100 * (sex == Sex.Male ? MaleWorldRecord : FemaleWorldRecord).TotalHours / duration.TotalHours / GetFactor(age, sex);
        }
    }
}
