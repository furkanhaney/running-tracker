using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;

namespace RunningTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Handle text field inputs

        RunnerProfile Profile;
        readonly string SaveFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Running Tracker\";

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
            datePicker.SelectedDate = DateTime.Now;

            if (!TryLoad())
                NewProfile();

            UpdateUI();
            ResetProfileForm();
            ResetAddForm();
        }

        void UpdateUI()
        {
            UpdateDataGrid();
            UpdateLabels();
            UpdatePlot();
        }
        void UpdateLabels()
        {
            var furthestRun = Profile.FurthestRun;
            var longestRun = Profile.LongestRun;

            if (furthestRun == null)
                lblFurthestRun.Content = "Furthest Run: You need to run first.";
            else
                lblFurthestRun.Content = "Furthest Run: " + furthestRun.Distance.ToString("#0.0") + " km (" + furthestRun.Date.ToString(@"MMM dd yyyy") + ")";

            if (longestRun == null)
                lblLongestRun.Content = "Longest Run: You need to run first.";
            else
                lblLongestRun.Content = "Longest Run: " + longestRun.Duration + " (" + longestRun.Date.ToString(@"MMM dd yyyy") + ")";

            lblName.Content = Profile.Name + " , " + Profile.Age;
            lblTotalDistance.Content = "Total Distance: " + Profile.TotalDistance.ToString("#0.0") + " km";
            lblTotalDuration.Content = "Total Duration: " + Profile.TotalDuration.ToString();
            lblAvgDistance.Content = "Avg. Distance: " + Profile.AverageDistance.ToString("#0.0") + " km";
            lblAvgDuration.Content = "Avg. Duration: " + Profile.AverageDuration.ToString(@"hh\:mm\:ss");
            lblTotalRuns.Content = "Total Runs: " + Profile.TotalWorkouts.ToString("###,##0");

            lblFatBurned.Content = "Fat Burned: " + (Profile.RunRecords.Sum(r => r.Calories) / 7.7).ToString("###,##0") + " g";
            lblCurrentBMI.Content = "Current BMI: " + Profile.BMI.ToString("0.00");
            lblMileage.Content = "Mileage: " + Profile.Mileage.ToString("0.00") + " mi";
            lblRequiredDistance.Content = Profile.CurrentDistance.ToString("#0.0") + " km / " + Profile.RequiredDistance.ToString("#0.0") + " km";
            xpBar.Value = (Profile.CurrentDistance / Profile.RequiredDistance);
            lblRank.Content = Profile.Rank;
        }
        void UpdateDataGrid()
        {
            dataGridRuns.ItemsSource = Profile.RunRecords.OrderByDescending(o => o.Date);
            dataGridRecords.ItemsSource = Profile.GetPersonalBests();
            listBoxTags.ItemsSource = Profile.AvailableTags;
            listBoxPlaces.ItemsSource = Profile.AvailablePlaces;
            cmbTags.ItemsSource = Profile.AvailableTags;
            cmbPlaces.ItemsSource = Profile.AvailablePlaces;
            UpdateReportsGrid();
        }
        void UpdateReportsGrid()
        {
            if (Profile == null)
                return;
            if (cmbTimeSpan.SelectedIndex == 0)
                dataGridReports.ItemsSource = Profile.GetWeeklyReports().OrderByDescending(o => o.StartDate);
            else
                dataGridReports.ItemsSource = Profile.GetMonthlyReports().OrderByDescending(o => o.StartDate);
        }
        void UpdatePlot()
        {
            PlotType plotType = (PlotType)cmbPlotType.SelectedIndex;
            List<DataPoint> dataPoints = GetData(plotType);
            PlotModel model = GetModel(plotType);
            OxyColor lineColor = OxyColor.FromRgb(0, 0, 100);
            string trackerFormat = GetTrackerFormat(plotType);
            LineSeries line = new LineSeries() { Color = lineColor, TrackerFormatString = trackerFormat };
            foreach (var data in dataPoints)
                line.Points.Add(data);
            model.Series.Add(line);
            plot.Model = model;
        }

        List<DataPoint> GetData(PlotType plotType)
        {
            var Reports = cmbPlotTimeSpan.SelectedIndex == 0 ? Profile.GetWeeklyReports() : Profile.GetMonthlyReports();

            if (plotType == PlotType.Distance)
                return Reports.Select(o => new DataPoint(DateTimeAxis.ToDouble(o.StartDate), o.Distance)).ToList();
            else if (plotType == PlotType.Speed)
                return Reports.Select(o => new DataPoint(DateTimeAxis.ToDouble(o.StartDate), o.Speed)).ToList();
            else
                return Reports.Select(o => new DataPoint(DateTimeAxis.ToDouble(o.StartDate), o.Duration.TotalHours)).ToList();
        }
        string GetTrackerFormat(PlotType plotType)
        {
            string format = "Date: {2:dd/MM/yyyy}" + Environment.NewLine;
            if (plotType == PlotType.Distance)
                return format + "Distance: {4:#0.0 km}";
            else if (plotType == PlotType.Speed)
                return format + "Speed: {4:#0.00 kmh}";
            else
                return format + "Duration: {4:#0.0 h}";
        }
        PlotModel GetModel(PlotType plotType)
        {
            string stringFormat = "0.0 km";
            if (plotType == PlotType.Speed)
                stringFormat = "0.00 kmh";
            else if (plotType == PlotType.Time)
                stringFormat = "0.0 h";

            var Model = new PlotModel();
            var dateAxis = new DateTimeAxis() { MajorGridlineStyle = LineStyle.Solid, StringFormat = "dd/MM/yyyy", MinorGridlineStyle = LineStyle.Dot, IntervalLength = 80 };
            Model.Axes.Add(dateAxis);
            var valueAxis = new LinearAxis() { MajorGridlineStyle = LineStyle.Solid, StringFormat = stringFormat, MinorGridlineStyle = LineStyle.Dot };
            Model.Axes.Add(valueAxis);
            return Model;
        }

        private void Save()
        {
            Directory.CreateDirectory(SaveFolder);
            Directory.CreateDirectory(SaveFolder + @"Backups\");
            Save(@"\profile.xml");
            Save(@"\Backups\" + DateTime.UtcNow.ToString("MMM-dd-yyyy_h-m-s") + ".xml");
        }
        private void Save(string savePath)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(RunnerProfile), new XmlRootAttribute("RunnerProfile"));
                string path = SaveFolder + savePath;
                File.Delete(path);
                using (var stream = File.OpenWrite(path))
                    serializer.Serialize(stream, Profile);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error while trying to save: " + e.Message);
            }
        }
        private bool TryLoad()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(RunnerProfile), new XmlRootAttribute("RunnerProfile"));
                Directory.CreateDirectory(SaveFolder);
                string path = SaveFolder + @"\profile.xml";
                using (var stream = File.OpenRead(path))
                    Profile = (RunnerProfile)(serializer.Deserialize(stream));
                Profile.Compute();
                return true;
            }
            catch
            {
                MessageBox.Show("No profile was found. A new profile will be created!");
                return false;
            }
        }
        private void AddRun()
        {
            DateTime date = (DateTime)datePicker.SelectedDate;
            double distance = (double)numDistance.Value;
            double duration = (double)numDuration.Value;
            double incline = (double)numIncline.Value;

            var newRun = new RunRecord()
            {
                Date = date,
                Place = cmbPlaces.Text,
                Tag = cmbTags.Text,
                Distance = distance,
                Hours = duration / 60,
                Weight = Profile.Weight,
                Incline = incline * 100,
            };

            Profile.AddRun(newRun);
        }
        private void ResetAddForm()
        {
            datePicker.SelectedDate = DateTime.Now;
            numDistance.Value = 0.1m;
            numDuration.Value = 1;
            numIncline.Value = 0;
        }
        private double GetLastWeight()
        {
            if (Profile.RunRecords.Count == 0)
                return 0;
            return Profile.RunRecords.OrderByDescending(o => o.Date).ToArray()[0].Weight;
        }
        private void ResetProfileForm()
        {
            txtName.Text = Profile.Name;
            birthdayPicker.SelectedDate = Profile.Birthday;
            numWeight.Value = (Decimal)Profile.Weight;
            numHeight.Value = (Decimal)Profile.Height;
            cmbSex.SelectedIndex = Profile.Sex == Sex.Male ? 0 : 1;
        }
        private void NewProfile()
        {
            Profile = new RunnerProfile();
            Profile.Name = "John Doe";
            Profile.Height = 170;
            Profile.Weight = 70;
            Profile.Birthday = DateTime.UtcNow - TimeSpan.FromDays(365 * 30);
            Profile.Initialize();
            Profile.Compute();
            Save();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            AddRun();
            UpdateUI();
            Save();
        }
        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            ResetAddForm();
        }
        private void txtName_LostFocus(object sender, RoutedEventArgs e)
        {
            Profile.Name = txtName.Text;
            UpdateUI();
            Save();
        }
        private void birthdayPicker_LostFocus(object sender, RoutedEventArgs e)
        {
            if (birthdayPicker.SelectedDate != null)
            {
                Profile.Birthday = (DateTime)birthdayPicker.SelectedDate;
                Profile.Compute();
                UpdateUI();
                Save();
            }
        }
        private void btnAddTag_Click(object sender, RoutedEventArgs e)
        {
            if (!txtNewTag.Text.Equals(""))
            {
                Profile.AvailableTags.Add(txtNewTag.Text);
                txtNewTag.Text = "";
                Save();
            }
        }
        private void txtWeight_LostFocus(object sender, RoutedEventArgs e)
        {

        }
        private void txtHeight_LostFocus(object sender, RoutedEventArgs e)
        {

        }
        private void listBoxTags_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                var tag = listBoxTags.SelectedItem;
                Profile.AvailableTags.Remove((string)tag);
                Save();
            }
        }
        private void dataGridRuns_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete)
                return;

            string sMessageBoxText = "Do you want to delete this run?";
            string sCaption = "Warning";

            MessageBoxButton btnMessageBox = MessageBoxButton.YesNo;
            MessageBoxImage icnMessageBox = MessageBoxImage.Warning;

            MessageBoxResult rsltMessageBox = MessageBox.Show(sMessageBoxText, sCaption, btnMessageBox, icnMessageBox);

            if (rsltMessageBox == MessageBoxResult.Yes)
            {
                Profile.DeleteRun((RunRecord)dataGridRuns.SelectedItem);
                Save();
                UpdateUI();
            }
        }
        private void cmbPlotType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Profile != null)
                UpdatePlot();
        }
        private void cmbSex_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Profile == null)
                return;
            Profile.Sex = cmbSex.SelectedIndex == 0 ? Sex.Male : Sex.Female;
            Profile.Compute();
            UpdateUI();
            Save();
        }
        private void cmbTimeSpan_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateReportsGrid();
        }
        private void cmbPlotTimeSpan_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Profile != null)
                UpdatePlot();
        }
        private void numWeight_LostFocus(object sender, RoutedEventArgs e)
        {
            Profile.Weight = (double)numWeight.Value;
            Save();
            UpdateUI();
        }
        private void numHeight_LostFocus(object sender, RoutedEventArgs e)
        {
            Profile.Height = (double)numHeight.Value;
            Save();
            UpdateUI();
        }
        private void btnAddPlace_Click(object sender, RoutedEventArgs e)
        {
            if (!txtNewPlace.Text.Equals(""))
            {
                Profile.AvailablePlaces.Add(txtNewPlace.Text);
                txtNewPlace.Text = "";
                Save();
            }
        }
        private void listBoxPlaces_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                var place = listBoxPlaces.SelectedItem;
                Profile.AvailablePlaces.Remove((string)place);
                Save();
            }
        }
    }

    enum PlotType { Distance, Speed, Time }
}
