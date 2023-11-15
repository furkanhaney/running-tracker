using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RunningTracker
{
    public class RunnerProfile
    {
        // Stored Properties
        public string Name { get; set; }
        public Sex Sex { get; set; }
        public DateTime Birthday { get; set; }
        public DateTime DateCreated { get; set; }

        public double Weight { get; set; }
        public double Height { get; set; }
        public ObservableCollection<RunRecord> RunRecords { get; set; }
        public ObservableCollection<string> AvailableTags { get; set; }
        public ObservableCollection<string> AvailablePlaces { get; set; }

        public int Level { get; set; }
        public double CurrentDistance { get; set; }
        string[] Ranks = { "Beginner", "Amateur", "Intermediate", "Advanced", "Expert" };

        // Visual Properties
        public string Rank
        {
            get
            {
                return Ranks[Math.Min(Ranks.Length - 1, Level)];
            }
        }
        public double RequiredDistance
        {
            get
            {
                return 100 * (2 * Level + 1);
            }
        }
        public int TotalWorkouts
        {
            get
            {
                return RunRecords.Count;
            }
        }
        public double TotalDistance
        {
            get
            {
                return RunRecords.Sum(o => o.Distance);
            }
        }
        public TimeSpan TotalDuration
        {
            get
            {
                TimeSpan total = TimeSpan.FromHours(0);
                foreach (var r in RunRecords)
                    total += r.Duration;
                return total;
            }
        }
        public RunRecord LongestRun
        {
            get
            {
                if (RunRecords.Count == 0)
                    return null;
                return RunRecords.OrderByDescending(o => o.Duration).ToArray()[0];
            }
        }
        public RunRecord FurthestRun
        {
            get
            {
                if (RunRecords.Count == 0)
                    return null;
                return RunRecords.OrderByDescending(o => o.Distance).ToArray()[0];
            }
        }
        public double AverageDistance
        {
            get
            {
                if (RunRecords.Count == 0)
                    return 0;
                return RunRecords.Average(o => o.Distance);
            }
        }
        public TimeSpan AverageDuration
        {
            get
            {
                if (RunRecords.Count == 0)
                    return TimeSpan.Zero;
                return TimeSpan.FromHours((double)RunRecords.Average(o => o.Hours));
            }
        }
        public int Age
        {
            get
            {
                return (int)Math.Floor((DateTime.UtcNow - Birthday).TotalDays / 365);
            }
        }
        public double BMI
        {
            get
            {
                return Weight / Height / Height * 10000;
            }
        }
        public double Mileage
        {
            get
            {
                var reports = WeeklyReports.OrderByDescending(o => o.StartDate).ToList();
                if (reports.Count == 0)
                    return 0;
                int k = 0;
                var consideredReports = new List<Report>();
                while (k < reports.Count && k < 4)
                    consideredReports.Add(reports[k++]);
                return consideredReports.Average(o => o.Distance) / 1.609;
            }
        }

        List<PersonalBest> PersonalBests;
        List<Report> WeeklyReports;
        List<Report> MonthlyReports;

        public List<PersonalBest> GetPersonalBests()
        {
            return PersonalBests;
        }
        public List<Report> GetWeeklyReports()
        {
            return WeeklyReports;
        }
        public List<Report> GetMonthlyReports()
        {
            return MonthlyReports;
        }

        public RunnerProfile()
        {
            DateCreated = DateTime.UtcNow;
            RunRecords = new ObservableCollection<RunRecord>();
            AvailableTags = new ObservableCollection<string>();
            AvailablePlaces = new ObservableCollection<string>();
        }

        public void Compute()
        {
            GenerateWeeklyReports();
            GenerateMonthlyReports();
            CalculatePersonalBests();
        }
        public void AddRun(RunRecord record)
        {
            RunRecords.Add(record);
            RunRecords = new ObservableCollection<RunRecord>(RunRecords.OrderBy(o => o.Date));
            CurrentDistance += record.Distance;
            if (CurrentDistance > RequiredDistance)
            {
                CurrentDistance -= RequiredDistance;
                Level++;
            }
            Compute();
        }
        public void DeleteRun(RunRecord run)
        {
            RunRecords.Remove(run);
            CurrentDistance -= run.Distance;
        }
        public void Initialize()
        {
            // Default tags
            AvailableTags.Add("Walking");
            AvailableTags.Add("Running");
            AvailableTags.Add("Interval");
            AvailableTags.Add("Fartlek");
            AvailableTags.Add("Tempo");

            //Default places
            AvailablePlaces.Add("Outdoors");
            AvailablePlaces.Add("Treadmill");
            AvailablePlaces.Add("Track");
        }

        void GenerateWeeklyReports()
        {
            WeeklyReports = new List<Report>();

            if (RunRecords.Count == 0)
                return;

            int year = RunRecords[0].Date.Year;
            int week = Tools.GetIso8601WeekOfYear(RunRecords[0].Date);
            List<RunRecord> CurrentRuns = new List<RunRecord>();

            for (int i = 0; i < RunRecords.Count; i++)
            {
                if (year != RunRecords[i].Date.Year || week != Tools.GetIso8601WeekOfYear(RunRecords[i].Date))
                {
                    WeeklyReports.Add(new Report(TimePeriod.Weekly, CurrentRuns));
                    CurrentRuns = new List<RunRecord>();
                    year = RunRecords[i].Date.Year;
                    week = Tools.GetIso8601WeekOfYear(RunRecords[i].Date);
                }
                CurrentRuns.Add(RunRecords[i]);
            }
            WeeklyReports.Add(new Report(TimePeriod.Weekly, CurrentRuns));
        }
        void GenerateMonthlyReports()
        {
            MonthlyReports = new List<Report>();

            if (RunRecords.Count == 0)
                return;

            int year = RunRecords[0].Date.Year;
            int month = RunRecords[0].Date.Month;
            List<RunRecord> CurrentRuns = new List<RunRecord>();

            for (int i = 0; i < RunRecords.Count; i++)
            {
                if (year != RunRecords[i].Date.Year || month != RunRecords[i].Date.Month)
                {
                    MonthlyReports.Add(new Report(TimePeriod.Monthly, CurrentRuns));
                    CurrentRuns = new List<RunRecord>();
                    year = RunRecords[i].Date.Year;
                    month = RunRecords[i].Date.Month;
                }
                CurrentRuns.Add(RunRecords[i]);
            }
            MonthlyReports.Add(new Report(TimePeriod.Monthly, CurrentRuns));
        }
        void CalculatePersonalBests()
        {
            var list = new List<PersonalBest>();
            foreach (var d in Distances.DistanceInfos.OrderBy(d => d.Distance))
            {
                var possibleRuns = RunRecords.Where(r => r.Distance > d.Distance).OrderByDescending(r => r.Speed).ToArray();
                if (possibleRuns.Length != 0)
                {
                    RunRecord run = possibleRuns[0];
                    TimeSpan duration = TimeSpan.FromHours(run.Duration.TotalHours * (double)(d.Distance / run.Distance));
                    list.Add(new PersonalBest() { Age = Age, Sex = Sex, Duration = duration, DistanceInfo = d, Date = run.Date });
                }
            }
            PersonalBests = list;
        }
    }

    public enum Sex { Male, Female }
}
