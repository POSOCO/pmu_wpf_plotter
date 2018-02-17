using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace PMU_Plotter
{
    /// <summary>
    /// Interaction logic for PointsConfigWindow.xaml
    /// </summary>
    public partial class PointsConfigWindow : Window
    {
        public MeasurementsVM measurementsVM = new MeasurementsVM();
        public PointsConfigWindow()
        {
            InitializeComponent();
            ConfigForm.DataContext = measurementsVM;
        }

        private void PreviewNumericTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private static bool IsTextAllowed(string text)
        {
            Regex regex = new Regex("[^0-9]+"); //regex that matches disallowed text
            return !regex.IsMatch(text);
        }

        private void AddNewMeasurement(object sender, RoutedEventArgs e)
        {
            string newMeasIdStr = newMeasIdInput.Text.Trim();
            int newMeasId = 0;
            string newMeasName = newMeasNameInput.Text.Trim();
            if (newMeasIdStr == "" || !int.TryParse(newMeasIdStr, out newMeasId))
            {
                MessageBox.Show("Invalid New Meas. Id...");
                return;
            }
            if (newMeasName == "")
            {
                MessageBox.Show("Enter a non empty string for New Meas. Name...");
                return;
            }
            // check if meas ID already in the list
            int newMeasIdIndex = measurementsVM.measIds.IndexOf(newMeasId);
            if (newMeasIdIndex != -1)
            {
                MessageBox.Show(String.Format("Meas. Id already present in the list at position {0}...", (newMeasIdIndex + 1)));
                return;
            }
            measurementsVM.measIds.Add(newMeasId);
            measurementsVM.measurementNames.Add(newMeasName);
            measurementsVM.NotifyPropertyChanged("measurements");
        }

        private void DeleteMeasurement(object sender, RoutedEventArgs e)
        {
            if (MeasurementsListBox.SelectedIndex != -1)
            {
                int selectedIndex = MeasurementsListBox.SelectedIndex;
                measurementsVM.measIds.RemoveAt(selectedIndex);
                measurementsVM.measurementNames.RemoveAt(selectedIndex);
                measurementsVM.NotifyPropertyChanged("measurements");
            }
        }

        public class MeasurementsVM : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public void NotifyPropertyChanged(string propertyName = "")
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }

            public MeasurementsVM()
            {
                plotTemplate_ = new PlotDataTemplate();
                plotTemplate_.dataSetName = "Plot Title";
                plotTemplate_.dataRate = 25;
                plotTemplate_.measIds = new List<int> { 1, 2, 3 };
                plotTemplate_.measurementNames = new List<string> { "One", "Two", "Three" };
            }

            public PlotDataTemplate plotTemplate_ { get; set; }
            public string dataSetName
            {
                get
                {
                    return plotTemplate_.dataSetName;
                }
                set
                {
                    plotTemplate_.dataSetName = value;
                }
            }
            public int dataRate
            {
                get
                {
                    return plotTemplate_.dataRate;
                }
                set
                {
                    plotTemplate_.dataRate = value;
                }
            }
            public List<string> hourStrings { get; set; } = new List<string> { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23" };
            public List<string> minuteStrings { get; set; } = new List<string> { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31", "32", "33", "34", "35", "36", "37", "38", "39", "40", "41", "42", "43", "44", "45", "46", "47", "48", "49", "50", "51", "52", "53", "54", "55", "56", "57", "58", "59" };
            public List<string> dateLimitsModes { get; set; } = new List<string> { "variable", "absolute" };
            public int dateLimitsModeSelection = 0;
            public int DateLimitsModeSelection { get { return dateLimitsModeSelection; } set { dateLimitsModeSelection = value; plotTemplate_.dateLimitsMode = dateLimitsModes.ElementAt(dateLimitsModeSelection); } }
            public DateTime StartDate { get; set; } = DateTime.Now;
            public int StartTimeHoursIndex { get; set; } = 0;
            public int StartTimeMinsIndex { get; set; } = 0;
            public int StartTimeSecsIndex { get; set; } = 0;
            public int EndTimeHoursIndex { get; set; } = 0;
            public int EndTimeMinsIndex { get; set; } = 0;
            public int EndTimeSecsIndex { get; set; } = 0;
            public DateTime EndDate { get; set; } = DateTime.Now;
            public List<int> measIds { get { return plotTemplate_.measIds; } set { plotTemplate_.measIds = value; } }
            public List<String> measurementNames { get { return plotTemplate_.measurementNames; } set { plotTemplate_.measurementNames = value; } }
            public List<String> measurements
            {
                get
                {
                    List<string> viewStrings = new List<string>();
                    for (int i = 0; i < Math.Min(measIds.Count, measurementNames.Count); i++)
                    {
                        viewStrings.Add(measIds.ElementAt(i).ToString() + " - " + measurementNames.ElementAt(i));
                    }
                    return viewStrings;
                }
            }
        }

        private void SaveTemplate(object sender, RoutedEventArgs e)
        {
            string jsonText = JsonConvert.SerializeObject(measurementsVM.plotTemplate_, Formatting.Indented);
            SaveFileDialog savefileDialog = new SaveFileDialog();
            // set a default file name
            savefileDialog.FileName = String.Format("pmu_plot_template_{0}.json", DateTime.Now.ToString("dd.MM.yyyy_HH.mm.ss"));
            // set filters - this can be done in properties as well
            savefileDialog.Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*";

            if (savefileDialog.ShowDialog() == true)
            {
                File.WriteAllText(savefileDialog.FileName, jsonText);
                MessageBox.Show("Exported the template JSON File!!!");
            }
        }
    }
}
