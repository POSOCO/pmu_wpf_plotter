using LiveCharts;
using LiveCharts.Geared;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
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
    /// Interaction logic for GearedTest.xaml
    /// </summary>
    public partial class GearedTest : Window
    {
        public SeriesCollection SeriesCollection { get; set; }
        public long Step { get; set; }
        public Func<double, string> YFormatter { get; set; }
        public Func<double, string> XFormatter { get; set; }
        public List<DateTime> timeStamps_ { get; set; }
        PlotDataTemplate plotTemplate_;
        ConfigurationManager _configManager;
        HistoryDataAdapter _historyAdapter;
        PointsConfigWindow configWindow;

        public GearedTest()
        {
            InitializeComponent();
            addLinesToConsole("Welcome User!");

            plotTemplate_ = new PlotDataTemplate();

            _configManager = new ConfigurationManager();
            _configManager.Initialize();
            _historyAdapter = new HistoryDataAdapter();
            _historyAdapter.Initialize(_configManager);

            String str = (String)((App)Application.Current).Properties["FilePathArgName"];
            openFileName(str);
            SeriesCollection = new SeriesCollection();
            timeStamps_ = new List<DateTime>();
            //Labels = new string[0];
            Step = 1;
            YFormatter = value => String.Format("{0:0.###}", value);
            XFormatter = delegate (double val)
            {
                if (timeStamps_.Count > 0)
                {
                    DateTime startTimeStamp = timeStamps_.ElementAt(0);
                    DateTime timeStamp;
                    timeStamp = startTimeStamp.AddSeconds(val / 25.0);
                    return timeStamp.ToString("HH:mm:ss.fff");
                }
                return val.ToString();
            };
            MyChart.LegendLocation = LegendLocation.Top;
            DataContext = this;
        }

        private void openFileName(string str)
        {
            if (str != null)
            {
                plotTemplate_ = JsonConvert.DeserializeObject<PlotDataTemplate>(File.ReadAllText(str));
                // Display the file contents by using a foreach loop.
                WelcomeText.Text = JsonConvert.SerializeObject(plotTemplate_, Formatting.Indented);
            }
        }

        private void OpenCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // MessageBox.Show("You clicked 'Open...'");
            OpenFileDialog openFileDialog = new OpenFileDialog();
            // openFileDialog.Multiselect = true;
            openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
            if (openFileDialog.ShowDialog() == true)
            {
                string filename = openFileDialog.FileNames[0];
                openFileName(filename);
                if (filename != null)
                {
                    ((App)Application.Current).Properties["FilePathArgName"] = filename;
                }
            }
        }

        private void SaveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // get the filename
            if (MessageBox.Show("Save this Template?", "Save", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                string filename = (String)((App)Application.Current).Properties["FilePathArgName"];
                if (filename != null)
                {
                    string jsonText = JsonConvert.SerializeObject(plotTemplate_, Formatting.Indented);
                    File.WriteAllText(filename, jsonText);
                    addLinesToConsole("Saved the updated template file!!!");
                }
                else
                {
                    // open save as window
                    SaveAs_Click(this, null);
                }
            }
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            string filename = (String)((App)Application.Current).Properties["FilePathArgName"];
            if (filename == null)
            {
                filename = String.Format("pmu_plot_template_{0}.json", DateTime.Now.ToString("dd.MM.yyyy_HH.mm.ss"));
            }
            string jsonText = JsonConvert.SerializeObject(plotTemplate_, Formatting.Indented);
            SaveFileDialog savefileDialog = new SaveFileDialog();
            // set a default file name
            savefileDialog.FileName = filename;
            // set filters - this can be done in properties as well
            savefileDialog.Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*";
            if (savefileDialog.ShowDialog() == true)
            {
                File.WriteAllText(savefileDialog.FileName, jsonText);
                addLinesToConsole("Saved the updated template file!!!");
                if (savefileDialog.FileName != null)
                {
                    ((App)Application.Current).Properties["FilePathArgName"] = savefileDialog.FileName;
                }
            }
        }

        private void TestBtn_Click(object sender, RoutedEventArgs e)
        {
            addLinesToConsole("Started Plotting...");
            SeriesCollection.Clear();
            List<double> pmuVals = new List<double>();
            Random randNumGenerator = new Random();
            for (int i = 0; i < 100000; i++)
            {
                pmuVals.Add(randNumGenerator.Next(0, 10));
            }
            SeriesCollection.Add(new GLineSeries() { Title = "Geared Values Testing", Values = new GearedValues<double>(pmuVals), PointGeometry = null, Fill = Brushes.Transparent, StrokeThickness = 1, LineSmoothness = 0 });
            MyChart.AxisX[0].MinValue = -100;
            MyChart.AxisX[0].MaxValue = 100100;
            MyChart.AxisY[0].MinValue = 0;
            MyChart.AxisY[0].MaxValue = 15;
            addLinesToConsole("Finished Plotting!!!");
        }

        private void FetchBtn_Click(object sender, RoutedEventArgs e)
        {
            // fetch the points data from plotTemplate_ and plot them
            DateTime startTime = DateTime.Now;
            DateTime endTime = DateTime.Now;
            if (plotTemplate_.startDateMode == "variable")
            {
                startTime = startTime.AddHours(plotTemplate_.startDateVariable.hours * -1);
                startTime = startTime.AddMinutes(plotTemplate_.startDateVariable.mins * -1);
                startTime = startTime.AddSeconds(plotTemplate_.startDateVariable.secs * -1);
            }
            else { startTime = plotTemplate_.startDateTime; }

            if (plotTemplate_.endDateMode == "variable")
            {
                endTime = endTime.AddHours(plotTemplate_.endDateVariable.hours * -1);
                endTime = endTime.AddMinutes(plotTemplate_.endDateVariable.mins * -1);
                endTime = endTime.AddSeconds(plotTemplate_.endDateVariable.secs * -1);
            }
            else { endTime = plotTemplate_.endDateTime; }

            if (plotTemplate_.measIds.Count == 0)
            {
                plotTemplate_.measIds.AddRange(new List<int>() { 4924, 4929 });
                plotTemplate_.measurementNames.AddRange(new List<string>() { "meas1", "meas2" });
            }
            plotMeasIds(startTime, endTime);
        }

        public void plotMeasIds(DateTime startTime, DateTime endTime)
        {
            object payLoad = new { startTime = startTime, endTime = endTime, dataRate = plotTemplate_.dataRate, measurementIDs = plotTemplate_.measIds, measurementNames = plotTemplate_.measurementNames, fetchWindow = plotTemplate_.fetchWindow };
            addLinesToConsole("Started fetching data");
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.RunWorkerAsync(payLoad);
        }

        // worker thread background stuff
        // Todo stub implement piecewise fetching here like that of on demand fetch
        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            // todo refer template manager for for doing the piecewise fetch
            object argument = e.Argument;
            int dataRate = (int)argument.GetType().GetProperty("dataRate").GetValue(argument, null);
            DateTime startTime = (DateTime)argument.GetType().GetProperty("startTime").GetValue(argument, null);
            DateTime endTime = (DateTime)argument.GetType().GetProperty("endTime").GetValue(argument, null);
            List<int> measurementIDs = (List<int>)argument.GetType().GetProperty("measurementIDs").GetValue(argument, null);
            List<string> measurementNames = (List<string>)argument.GetType().GetProperty("measurementNames").GetValue(argument, null);
            VariableTime fetchWindow = (VariableTime)argument.GetType().GetProperty("fetchWindow").GetValue(argument, null);
            ConfigurationManager _configManager = new ConfigurationManager();
            _configManager.Initialize();
            HistoryDataAdapter _historyAdapter = new HistoryDataAdapter();
            _historyAdapter.Initialize(_configManager);
            int numWindows = 1;
            // find the number of seconds in a fetch window
            //stub
            int reportFetchWindowSecs = (int)(fetchWindow.hours * 60 * 60 + fetchWindow.mins * 60 + fetchWindow.secs);
            if (reportFetchWindowSecs <= 0)
            {
                numWindows = 1;
            }
            else
            {
                // find the number of fetch windows as Ceil(Fetchspan/windowspan)
                int reportfetchSpanSecs = Convert.ToInt32(Math.Ceiling((endTime - startTime).TotalSeconds));
                numWindows = reportfetchSpanSecs / reportFetchWindowSecs;
            }

            // fetch and update the 1st window
            DateTime fetchStartTime = startTime;
            DateTime fetchEndTime = fetchStartTime.AddSeconds(reportFetchWindowSecs);
            if (fetchEndTime > endTime)
            {
                fetchEndTime = endTime;
            }
            if (dataRate <= 0 || dataRate > 25)
            {
                // default dataRate of PMU
                dataRate = 25;
            }
            Dictionary<object, List<PMUDataStructure>> parsedData = _historyAdapter.GetData(startTime, endTime, measurementIDs, true, false, dataRate);
            e.Result = new { parsedData = parsedData, startTime = startTime, endTime = endTime, dataRate = dataRate, measurementIDs = measurementIDs, measurementNames = measurementNames };
        }

        // worker thread ui update stuff
        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        // worker thread completed stuff
        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            addLinesToConsole("Finished fetching data");
            object res = e.Result;
            Dictionary<object, List<PMUDataStructure>> parsedData = (Dictionary<object, List<PMUDataStructure>>)res.GetType().GetProperty("parsedData").GetValue(res, null);
            List<int> measurementIDs = (List<int>)res.GetType().GetProperty("measurementIDs").GetValue(res, null);
            List<string> measurementNames = (List<string>)res.GetType().GetProperty("measurementNames").GetValue(res, null);
            int dataRate = (int)res.GetType().GetProperty("dataRate").GetValue(res, null);
            // SeriesCollection = new SeriesCollection();
            SeriesCollection.Clear();
            PMUMeasDataLists lists;
            // check if we have atleast one measurement
            if (measurementIDs.Count > 0 && parsedData != null)
            {
                // lets keep step as 1 minute. Todo change step as per the plot preferences.
                Step = dataRate;
                // get 1st list and add to SeriesCollection
                lists = _historyAdapter.getDataOfMeasId(parsedData, (uint)measurementIDs.ElementAt(0), true);
                timeStamps_ = new List<DateTime>(lists.pmuTimeStamps);
                SeriesCollection.Add(new GLineSeries() { Title = measurementNames.ElementAt(0) + "_" + measurementIDs.ElementAt(0).ToString(), Values = new GearedValues<float>(lists.pmuVals), PointGeometry = null, Fill = Brushes.Transparent, StrokeThickness = 1, LineSmoothness = 0 });

                // get the data of remaining measurements and add to SeriesCollection
                for (int i = 1; i < measurementIDs.Count; i++)
                {
                    lists = _historyAdapter.getDataOfMeasId(parsedData, (uint)measurementIDs.ElementAt(i), true);
                    SeriesCollection.Add(new GLineSeries() { Title = measurementNames.ElementAt(i).ToString() + "_" + measurementIDs.ElementAt(i).ToString(), Values = new GearedValues<float>(lists.pmuVals), PointGeometry = null, Fill = Brushes.Transparent, StrokeThickness = 1, LineSmoothness = 0 });
                }
                addLinesToConsole("Viola! Finished plotting");
            }
            else if (parsedData == null)
            {
                addLinesToConsole("Unable to parse the fetched data...");
            }
            else if (measurementIDs.Count == 0)
            {
                addLinesToConsole("No measurement Ids to plot...");
            }

        }

        public void addLinesToConsole(string str)
        {
            string consoleTxt = WelcomeText.Text;
            // todo limit number of lines to 10
            WelcomeText.Text = DateTime.Now.ToString() + ": " + str + "\n" + consoleTxt;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Show();
        }

        private void Zoom_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mItem = sender as MenuItem;
            mItem.IsChecked = true;
            foreach (MenuItem item in (mItem.Parent as MenuItem).Items)
            {
                if (item.Tag != mItem.Tag)
                {
                    item.IsChecked = false;
                }
            }
            if (mItem.Tag.ToString() == "ZXY")
            {
                MyChart.Zoom = ZoomingOptions.Xy;
                addLinesToConsole("Zoom mode set to XY");
            }
            else if (mItem.Tag.ToString() == "ZX")
            {
                MyChart.Zoom = ZoomingOptions.X;
                addLinesToConsole("Zoom mode set to X");
            }
            else if (mItem.Tag.ToString() == "ZY")
            {
                MyChart.Zoom = ZoomingOptions.Y;
                addLinesToConsole("Zoom mode set to Y");
            }
            else if (mItem.Tag.ToString() == "ZOff")
            {
                MyChart.Zoom = ZoomingOptions.None;
                addLinesToConsole("Zoom mode set to Off");
            }
        }

        private void Pan_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mItem = sender as MenuItem;
            mItem.IsChecked = true;
            foreach (MenuItem item in (mItem.Parent as MenuItem).Items)
            {
                if (item.Tag != mItem.Tag)
                {
                    item.IsChecked = false;
                }
            }
            if (mItem.Tag.ToString() == "PXY")
            {
                MyChart.Pan = PanningOptions.Xy;
                addLinesToConsole("Pan mode set to XY");
            }
            else if (mItem.Tag.ToString() == "PX")
            {
                MyChart.Pan = PanningOptions.X;
                addLinesToConsole("Pan mode set to X");
            }
            else if (mItem.Tag.ToString() == "PY")
            {
                MyChart.Pan = PanningOptions.Y;
                addLinesToConsole("Pan mode set to Y");
            }
            else if (mItem.Tag.ToString() == "POff")
            {
                MyChart.Pan = PanningOptions.None;
                addLinesToConsole("Pan mode set to None");
            }

        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            MyChart.AxisX[0].MinValue = double.NaN;
            MyChart.AxisX[0].MaxValue = double.NaN;
            MyChart.AxisY[0].MinValue = double.NaN;
            MyChart.AxisY[0].MaxValue = double.NaN;
            addLinesToConsole("Reset Axis done...");
        }

        private void mainWindow_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private void ConfigBtn_Click(object sender, RoutedEventArgs e)
        {
            if (configWindow == null)
            {
                configWindow = new PointsConfigWindow();
            }
            else if (configWindow.IsLoaded == false)
            {
                // https://social.msdn.microsoft.com/Forums/vstudio/en-US/c521ac47-7326-483a-be60-42de0e355711/how-can-a-method-of-a-wpf-window-find-out-if-the-window-is-closed-?forum=wpf
                configWindow.Close();
                configWindow = new PointsConfigWindow();
            }
            configWindow.NewMessage += MessageReceived;
            configWindow.setDataTemplate(plotTemplate_);
            configWindow.Show();
            configWindow.Activate();
        }
        void MessageReceived(object sender, ConfigMessageEventArgs e)
        {
            PointsConfigWindow configWin = sender as PointsConfigWindow;
            if (configWin != null)
            {
                // change the plot Data Template as per the window message
                this.plotTemplate_ = e.dataTemplate;
            }
        }
    }
}
