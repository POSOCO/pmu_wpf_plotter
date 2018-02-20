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
        public event EventHandler<ConfigMessageEventArgs> NewMessage;
        public MeasurementsVM measurementsVM = new MeasurementsVM();
        public PointsConfigWindow()
        {
            InitializeComponent();
            ConfigForm.DataContext = measurementsVM;
        }

        public void setDataTemplate(PlotDataTemplate template)
        {
            measurementsVM.PlotTemplate = template;
        }

        private void PreviewIntegerTextInput(object sender, TextCompositionEventArgs e)
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

            public void NotifyPropertyChanged(string propertyName)
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
                PlotTemplate = plotTemplate_;
            }

            public PlotDataTemplate plotTemplate_ { get; set; }
            public PlotDataTemplate PlotTemplate
            {
                get { return plotTemplate_; }
                set
                {
                    plotTemplate_ = value;
                    NotifyPropertyChanged("dataSetName");
                    NotifyPropertyChanged("dataRate");
                    NotifyPropertyChanged("StartDateMode");
                    NotifyPropertyChanged("StartDateModeStr");
                    NotifyPropertyChanged("StartDate");
                    NotifyPropertyChanged("StartTimeHoursIndex");
                    NotifyPropertyChanged("StartTimeMinsIndex");
                    NotifyPropertyChanged("StartTimeSecsIndex");
                    NotifyPropertyChanged("StartHoursVariable");
                    NotifyPropertyChanged("StartMinsVariable");
                    NotifyPropertyChanged("StartSecsVariable");
                    NotifyPropertyChanged("EndDateMode");
                    NotifyPropertyChanged("EndDateModeStr");
                    NotifyPropertyChanged("EndDate");
                    NotifyPropertyChanged("EndTimeHoursIndex");
                    NotifyPropertyChanged("EndTimeMinsIndex");
                    NotifyPropertyChanged("EndTimeSecsIndex");
                    NotifyPropertyChanged("EndHoursVariable");
                    NotifyPropertyChanged("EndMinsVariable");
                    NotifyPropertyChanged("EndSecsVariable");
                    NotifyPropertyChanged("measurements");
                }
            }
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

            public int StartDateMode { get { return dateLimitsModes.IndexOf(plotTemplate_.startDateMode); } set { plotTemplate_.startDateMode = dateLimitsModes.ElementAt(value); NotifyPropertyChanged("StartDateModeStr"); } }
            public string StartDateModeStr
            {
                get { return plotTemplate_.startDateMode; }
                set
                {
                    int modeInt = dateLimitsModes.IndexOf(value);
                    if (modeInt != -1)
                    {
                        plotTemplate_.startDateMode = value;
                    }
                }
            }

            public DateTime StartDate { get { return plotTemplate_.startDateTime; } set { plotTemplate_.startDateTime = value; } }
            public int StartTimeHoursIndex { get { return plotTemplate_.startDateTime.Hour; } set { plotTemplate_.startDateTime = new DateTime(plotTemplate_.startDateTime.Year, plotTemplate_.startDateTime.Month, plotTemplate_.startDateTime.Day, value, plotTemplate_.startDateTime.Minute, plotTemplate_.startDateTime.Second); } }
            public int StartTimeMinsIndex { get { return plotTemplate_.startDateTime.Minute; } set { plotTemplate_.startDateTime = new DateTime(plotTemplate_.startDateTime.Year, plotTemplate_.startDateTime.Month, plotTemplate_.startDateTime.Day, plotTemplate_.startDateTime.Hour, value, plotTemplate_.startDateTime.Second); } }
            public int StartTimeSecsIndex { get { return plotTemplate_.startDateTime.Second; } set { plotTemplate_.startDateTime = new DateTime(plotTemplate_.startDateTime.Year, plotTemplate_.startDateTime.Month, plotTemplate_.startDateTime.Day, plotTemplate_.startDateTime.Hour, plotTemplate_.startDateTime.Minute, value); } }

            public string StartHoursVariable
            {
                get { return plotTemplate_.startDateVariable.hours.ToString(); }
                set
                {
                    double doubleVal = plotTemplate_.startDateVariable.hours;
                    if (double.TryParse(value, out doubleVal))
                    {
                        plotTemplate_.startDateVariable.hours = doubleVal;
                    }
                }
            }

            public string StartMinsVariable
            {
                get { return plotTemplate_.startDateVariable.mins.ToString(); }
                set
                {
                    double doubleVal = plotTemplate_.startDateVariable.mins;
                    if (double.TryParse(value, out doubleVal))
                    {
                        plotTemplate_.startDateVariable.mins = doubleVal;
                    }
                }
            }

            public string StartSecsVariable
            {
                get { return plotTemplate_.startDateVariable.secs.ToString(); }
                set
                {
                    double doubleVal = plotTemplate_.startDateVariable.secs;
                    if (double.TryParse(value, out doubleVal))
                    {
                        plotTemplate_.startDateVariable.secs = doubleVal;
                    }
                }
            }

            public string EndHoursVariable
            {
                get { return plotTemplate_.endDateVariable.hours.ToString(); }
                set
                {
                    double doubleVal = plotTemplate_.endDateVariable.hours;
                    if (double.TryParse(value, out doubleVal))
                    {
                        plotTemplate_.endDateVariable.hours = doubleVal;
                    }
                }
            }

            public string EndMinsVariable
            {
                get { return plotTemplate_.endDateVariable.mins.ToString(); }
                set
                {
                    double doubleVal = plotTemplate_.endDateVariable.mins;
                    if (double.TryParse(value, out doubleVal))
                    {
                        plotTemplate_.endDateVariable.mins = doubleVal;
                    }
                }
            }

            public string EndSecsVariable
            {
                get { return plotTemplate_.endDateVariable.secs.ToString(); }
                set
                {
                    double doubleVal = plotTemplate_.endDateVariable.secs;
                    if (double.TryParse(value, out doubleVal))
                    {
                        plotTemplate_.endDateVariable.secs = doubleVal;
                    }
                }
            }

            public int EndDateMode { get { return dateLimitsModes.IndexOf(plotTemplate_.endDateMode); } set { plotTemplate_.endDateMode = dateLimitsModes.ElementAt(value); NotifyPropertyChanged("EndDateModeStr"); } }
            public string EndDateModeStr
            {
                get { return plotTemplate_.endDateMode; }
                set
                {
                    int modeInt = dateLimitsModes.IndexOf(value);
                    if (modeInt != -1)
                    {
                        plotTemplate_.endDateMode = value;
                    }

                }
            }

            public DateTime EndDate { get { return plotTemplate_.endDateTime; } set { plotTemplate_.endDateTime = value; } }
            public int EndTimeHoursIndex { get { return plotTemplate_.endDateTime.Hour; } set { plotTemplate_.endDateTime = new DateTime(plotTemplate_.endDateTime.Year, plotTemplate_.endDateTime.Month, plotTemplate_.endDateTime.Day, value, plotTemplate_.endDateTime.Minute, plotTemplate_.endDateTime.Second); } }
            public int EndTimeMinsIndex { get { return plotTemplate_.endDateTime.Minute; } set { plotTemplate_.endDateTime = new DateTime(plotTemplate_.endDateTime.Year, plotTemplate_.endDateTime.Month, plotTemplate_.endDateTime.Day, plotTemplate_.endDateTime.Hour, value, plotTemplate_.endDateTime.Second); } }
            public int EndTimeSecsIndex { get { return plotTemplate_.endDateTime.Second; } set { plotTemplate_.endDateTime = new DateTime(plotTemplate_.endDateTime.Year, plotTemplate_.endDateTime.Month, plotTemplate_.endDateTime.Day, plotTemplate_.endDateTime.Hour, plotTemplate_.endDateTime.Minute, value); } }

            public string FetchWindowHrs
            {
                get { return plotTemplate_.fetchWindow.hours.ToString(); }
                set
                {
                    double doubleVal = plotTemplate_.fetchWindow.hours;
                    if (double.TryParse(value, out doubleVal))
                    {
                        plotTemplate_.fetchWindow.hours = doubleVal;
                    }
                }
            }

            public string FetchWindowMins
            {
                get { return plotTemplate_.fetchWindow.mins.ToString(); }
                set
                {
                    double doubleVal = plotTemplate_.fetchWindow.mins;
                    if (double.TryParse(value, out doubleVal))
                    {
                        plotTemplate_.fetchWindow.mins = doubleVal;
                    }
                }
            }

            public string FetchWindowSecs
            {
                get { return plotTemplate_.fetchWindow.secs.ToString(); }
                set
                {
                    double doubleVal = plotTemplate_.fetchWindow.secs;
                    if (double.TryParse(value, out doubleVal))
                    {
                        plotTemplate_.fetchWindow.secs = doubleVal;
                    }
                }
            }

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

        private void ExportTemplate(object sender, RoutedEventArgs e)
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

        private void SaveTemplate(object sender, RoutedEventArgs e)
        {
            // todo save the underlying file also
            if (NewMessage != null)
            {
                NewMessage(this, new ConfigMessageEventArgs(measurementsVM.plotTemplate_));
            }
        }
    }

    public class ConfigMessageEventArgs : EventArgs
    {
        public ConfigMessageEventArgs(PlotDataTemplate dataTemplate)
        {
            this.dataTemplate = dataTemplate;
        }
        public PlotDataTemplate dataTemplate { get; set; }
    }

    public class IsVariableDateVisibleConverter : IValueConverter
    {
        public IsVariableDateVisibleConverter() { }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string modeString = (string)value;
            if (modeString == "variable")
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;

            if (visibility == Visibility.Visible)
                return "variable";
            else
                return "absolute";
        }
    }

    public class IsAbsoluteDateVisibleConverter : IValueConverter
    {
        public IsAbsoluteDateVisibleConverter() { }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string modeString = (string)value;
            if (modeString == "absolute")
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;

            if (visibility == Visibility.Visible)
                return "absolute";
            else
                return "variable";
        }
    }
}
