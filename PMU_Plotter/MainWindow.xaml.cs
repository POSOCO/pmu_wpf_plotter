using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using LiveCharts;
using LiveCharts.Wpf;
using System.ComponentModel;
using LiveCharts.Defaults;
using Newtonsoft.Json;
using System.IO;

namespace PMU_Plotter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public SeriesCollection SeriesCollection { get; set; }
        public long Step { get; set; }
        public Func<double, string> YFormatter { get; set; }
        public Func<double, string> XFormatter { get; set; }
        public List<DateTime> timeStamps_ { get; set; }
        PlotDataTemplate plotTemplate_;
        ConfigurationManager _configManager;
        HistoryDataAdapter _historyAdapter;
        public MainWindow()
        {
            InitializeComponent();
            String str = (String)((App)Application.Current).Properties["ArbitraryArgName"];
            openFileName(str);

            _configManager = new ConfigurationManager();
            _configManager.Initialize();
            _historyAdapter = new HistoryDataAdapter();
            _historyAdapter.Initialize(_configManager);
            SeriesCollection = new SeriesCollection();
            timeStamps_ = new List<DateTime>();
            plotTemplate_ = new PlotDataTemplate();
            //Labels = new string[0];
            Step = 1;
            YFormatter = value => String.Format("{0:0.###}", value);
            XFormatter = delegate (double val)
            {
                if (timeStamps_.Count > 0)
                {
                    DateTime startTimeStamp = timeStamps_.ElementAt(0);
                    DateTime timeStamp;

                    if (val == 0 || val == (double)(timeStamps_.Count - 1))
                    {
                        timeStamp = timeStamps_.ElementAt((int)val);
                        return timeStamp.ToString("dd-MM-yyyy HH:mm:ss.fff");
                    }
                    else
                    {
                        timeStamp = startTimeStamp.AddSeconds(val / (double)plotTemplate_.dataRate);
                        if (timeStamp.Hour == 0 && timeStamp.Minute == 0 && timeStamp.Second == 0 && timeStamp.Millisecond == 0)
                        {
                            return timeStamp.ToString("dd-MM-yyyy");
                        }
                        return timeStamp.ToString("HH:mm:ss.fff");
                    }

                }
                return val.ToString();
            };
            MyChart.LegendLocation = LegendLocation.Top;
            DataContext = this;
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

            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            // MessageBox.Show("You clicked 'Exit...'");
            Application.Current.Shutdown();
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
            }
            else if (mItem.Tag.ToString() == "ZX")
            {
                MyChart.Zoom = ZoomingOptions.X;
            }
            else if (mItem.Tag.ToString() == "ZY")
            {
                MyChart.Zoom = ZoomingOptions.Y;
            }
            else if (mItem.Tag.ToString() == "ZOff")
            {
                MyChart.Zoom = ZoomingOptions.None;
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
            }
            else if (mItem.Tag.ToString() == "PX")
            {
                MyChart.Pan = PanningOptions.X;
            }
            else if (mItem.Tag.ToString() == "PY")
            {
                MyChart.Pan = PanningOptions.Y;
            }
            else if (mItem.Tag.ToString() == "POff")
            {
                MyChart.Pan = PanningOptions.None;
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            MyChart.AxisX[0].MinValue = double.NaN;
            MyChart.AxisX[0].MaxValue = double.NaN;
            MyChart.AxisY[0].MinValue = double.NaN;
            MyChart.AxisY[0].MaxValue = double.NaN;
            // todo make it more procedural by function
            WelcomeText.Text = "Reset Axis done...\n" + WelcomeText.Text;
        }

        public void plotMeasIds(DateTime startTime, DateTime endTime)
        {
            object payLoad = new { startTime = startTime, endTime = endTime, dataRate = plotTemplate_.dataRate, measurementIDs = plotTemplate_.measIds, measurementNames = plotTemplate_.measurementNames };
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.RunWorkerAsync(payLoad);
        }

        private void TestBtn_Click(object sender, RoutedEventArgs e)
        {
            DateTime startTime = DateTime.Now.AddDays(-1).AddSeconds(-3);
            DateTime endTime = DateTime.Now.AddDays(-1);
            List<int> measurementIDs = new List<int>() { 4924, 4929 };
            if (plotTemplate_.measIds.Count == 0)
            {
                plotTemplate_.measIds.AddRange(new List<int>() { 4924, 4929 });
                plotTemplate_.measurementNames.AddRange(new List<string>() { "meas1", "meas2" });
            }
            plotMeasIds(startTime, endTime);
        }

        // created by sudhir on 30.12.2017
        // worker thread background stuff
        // Todo stub implement piecewise fetching here like that of on demand fetch
        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            object argument = e.Argument;
            int dataRate = (int)argument.GetType().GetProperty("dataRate").GetValue(argument, null);
            DateTime startTime = (DateTime)argument.GetType().GetProperty("startTime").GetValue(argument, null);
            DateTime endTime = (DateTime)argument.GetType().GetProperty("endTime").GetValue(argument, null);
            List<int> measurementIDs = (List<int>)argument.GetType().GetProperty("measurementIDs").GetValue(argument, null);
            List<string> measurementNames = (List<string>)argument.GetType().GetProperty("measurementNames").GetValue(argument, null);
            ConfigurationManager _configManager = new ConfigurationManager();
            _configManager.Initialize();
            HistoryDataAdapter _historyAdapter = new HistoryDataAdapter();
            _historyAdapter.Initialize(_configManager);
            Dictionary<object, List<PMUDataStructure>> parsedData = _historyAdapter.GetData(startTime, endTime, measurementIDs, true, false, dataRate);
            e.Result = new { parsedData = parsedData, startTime = startTime, endTime = endTime, dataRate = dataRate, measurementIDs = measurementIDs, measurementNames = measurementNames };
        }

        // created by sudhir on 30.12.2017
        // worker thread ui update stuff
        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        // created by sudhir on 30.12.2017
        // worker thread completed stuff
        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
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
                SeriesCollection.Add(new LineSeries() { Title = measurementNames.ElementAt(0) + "_" + measurementIDs.ElementAt(0).ToString(), Values = new ChartValues<float>(lists.pmuVals), PointGeometry = null, Fill = Brushes.Transparent, StrokeThickness = 1 });

                // get the data of remaining measurements and add to SeriesCollection
                for (int i = 1; i < measurementIDs.Count; i++)
                {
                    lists = _historyAdapter.getDataOfMeasId(parsedData, (uint)measurementIDs.ElementAt(i), true);
                    SeriesCollection.Add(new LineSeries() { Title = measurementNames.ElementAt(i).ToString() + "_" + measurementIDs.ElementAt(i).ToString(), Values = new ChartValues<float>(lists.pmuVals), PointGeometry = null, Fill = Brushes.Transparent, StrokeThickness = 1 });
                }

            }
            System.Console.WriteLine("Viola! Finished plotting...");
        }

    }
}
