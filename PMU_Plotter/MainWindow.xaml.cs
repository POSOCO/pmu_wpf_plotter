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

namespace PMU_Plotter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public SeriesCollection SeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public long Step { get; set; }
        public Func<double, string> YFormatter { get; set; }
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

            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Series 1",
                    Values = new ChartValues<float> { 4, 6, 5, 2 ,4 }
                },
                new LineSeries
                {
                    Title = "Series 2",
                    Values = new ChartValues<float> { 6, 7, 3, 4 ,6 },
                    PointGeometry = null
                },
                new LineSeries
                {
                    Title = "Series 3",
                    Values = new ChartValues<float> { 4,2,7,2,7 },
                    PointGeometry = DefaultGeometries.Square,
                    PointGeometrySize = 15
                }
            };

            Labels = new[] { "Jan", "Feb", "Mar", "Apr", "May" };
            Step = 1;
            YFormatter = value => value.ToString("C");

            //modifying the series collection will animate and update the chart
            SeriesCollection.Add(new LineSeries
            {
                Title = "Series 4",
                Values = new ChartValues<float> { 5, 3, 2, 4 },
                LineSmoothness = 0, //0: straight lines, 1: really smooth lines
                //PointGeometry = Geometry.Parse("m 25 70.36218 20 -28 -20 22 -8 -6 z"),
                PointGeometry = DefaultGeometries.Diamond,
                //PointGeometrySize = 50,
                PointForeground = Brushes.Gray
            });

            //modifying any series values will also animate and update the chart
            SeriesCollection[3].Values.Add(5f);
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
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
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
                string[] lines = System.IO.File.ReadAllLines(str);
                // Display the file contents by using a foreach loop.
                WelcomeText.Text = String.Join("\n", lines);
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Show();
        }

        public void plotMeasIds(DateTime startTime, DateTime endTime, List<int> measurementIDs, int dataRate)
        {
            object payLoad = new { startTime = startTime, endTime = endTime, dataRate = dataRate, measurementIDs = measurementIDs };
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.RunWorkerAsync(payLoad);
        }

        private void TestBtn_Click(object sender, RoutedEventArgs e)
        {
            DateTime startTime = DateTime.Now.AddMinutes(-5).AddSeconds(-2);
            DateTime endTime = DateTime.Now.AddSeconds(-2);
            int dataRate = 25;
            List<int> measurementIDs = new List<int>() { 4924, 4929 };
            plotMeasIds(startTime, endTime, measurementIDs, dataRate);
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
            List<int> measurementIDs = (List < int >)argument.GetType().GetProperty("measurementIDs").GetValue(argument, null);
            Dictionary<object, List<PMUDataStructure>> parsedData = _historyAdapter.GetData(startTime, endTime, measurementIDs, true, false, dataRate);
            e.Result = new { parsedData = parsedData, startTime=startTime, endTime=endTime, dataRate=dataRate,measurementIDs=measurementIDs };
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
            Dictionary<object, List<PMUDataStructure>> parsedData = (Dictionary < object, List< PMUDataStructure >>)res.GetType().GetProperty("parsedData").GetValue(res, null);
            List<int> measurementIDs = (List < int >)res.GetType().GetProperty("measurementIDs").GetValue(res, null);
            int dataRate = (int)res.GetType().GetProperty("dataRate").GetValue(res, null);
            SeriesCollection = new SeriesCollection();
            PMUMeasDataLists lists;
            // check if we have atleast one measurement
            if (measurementIDs.Count > 1 && parsedData != null)
            {
                // lets keep step as 1 minute. Todo change step as per the plot preferences.
                Step = dataRate * 60;
                // get 1st list and add to SeriesCollection
                lists = _historyAdapter.getDataOfMeasId(parsedData, (uint)measurementIDs.ElementAt(0), true);
                SeriesCollection.Add(new LineSeries() { Title = measurementIDs.ElementAt(0).ToString(), Values = new ChartValues<float>(lists.pmuVals) });
                // update the labels from first measurement lists.pmuTimeStamps
                Labels = new string[lists.pmuTimeStamps.Count];
                for (int i = 0; i < lists.pmuTimeStamps.Count; i++)
                {
                    DateTime timeStamp = lists.pmuTimeStamps.ElementAt(i);
                    if (timeStamp.Minute == 0 && timeStamp.Hour == 0 && timeStamp.Second == 0 && timeStamp.Millisecond == 0)
                    {
                        Labels[i] = timeStamp.ToString("dd-MM-yyyy");
                    }
                    else
                    {
                        Labels[i] = timeStamp.ToString("HH:mm:ss.fff");
                    }
                }
                // get the data of remaining measurements and add to SeriesCollection
                for (int i = 1; i < measurementIDs.Count; i++)
                {
                    lists = _historyAdapter.getDataOfMeasId(parsedData, (uint)measurementIDs.ElementAt(i), true);
                    SeriesCollection.Add(new LineSeries() { Title = measurementIDs.ElementAt(i).ToString(), Values = new ChartValues<float>(lists.pmuVals) });
                }
            }
            System.Console.WriteLine("Viola! Finished plotting...");            
        }

    }
}
