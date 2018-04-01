using LiveCharts;
using LiveCharts.Geared;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading;
using System.Threading.Tasks;

namespace PMU_Plotter
{
    /// <summary>
    /// Interaction logic for GearedTest.xaml
    /// </summary>
    public partial class GearedPlotWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // todo stop fetching button that will stop the worker
        // save user preferences - https://blogs.msdn.microsoft.com/patrickdanino/2008/07/23/user-settings-in-wpf/

        // ***Declare a System.Threading.CancellationTokenSource.
        CancellationTokenSource cts;

        public SeriesCollection SeriesCollection { get; set; }
        public long Step { get; set; }
        public Func<double, string> YFormatter { get; set; }
        public Func<double, string> XFormatter { get; set; }

        public string PlotBackgroundColor
        {
            get
            {
                return plotTemplate_.BackgroundColor;
            }
            set
            {
                plotTemplate_.BackgroundColor = value;
            }
        }

        // not used
        public string LegendTextColor
        {
            get
            {
                //Color c = Helpers.Helpers.IdealTextColor((Color)ColorConverter.ConvertFromString("#000000"));
                //string hexString = Helpers.Helpers.ColorToHexString(c);
                //return hexString;
                return plotTemplate_.TextColor;
            }
        }

        public List<DateTime> timeStamps_ { get; set; }
        PlotDataTemplate plotTemplate_;
        ConfigurationManager _configManager;
        HistoryDataAdapter _historyAdapter;
        PointsConfigWindow configWindow;

        public GearedPlotWindow()
        {
            InitializeComponent();
            AddLinesToConsole("Welcome User!");
            plotTemplate_ = new PlotDataTemplate();
            _configManager = new ConfigurationManager();
            _configManager.Initialize();
            _historyAdapter = new HistoryDataAdapter();
            _historyAdapter.Initialize(_configManager);

            String str = (String)((App)Application.Current).Properties["FilePathArgName"];
            OpenFileName(str);
            SeriesCollection = new SeriesCollection();
            timeStamps_ = new List<DateTime> { DateTime.Now };
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
                    return timeStamp.ToString("HH:mm:ss.fff\ndd-MMM-yy");
                }
                return val.ToString();
            };
            MyChart.LegendLocation = LegendLocation.Top;
            DataContext = this;
        }

        private async void OpenFileName(string str)
        {
            if (str != null)
            {
                plotTemplate_ = JsonConvert.DeserializeObject<PlotDataTemplate>(File.ReadAllText(str));
                // Display the file contents by using a foreach loop.
                WelcomeText.Text = JsonConvert.SerializeObject(plotTemplate_, Formatting.Indented);
                await FetchAndPlotData();
                ResetAxes();
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
                OpenFileName(filename);
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
                    AddLinesToConsole("Saved the updated template file!!!");
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
                AddLinesToConsole("Saved the updated template file!!!");
                if (savefileDialog.FileName != null)
                {
                    ((App)Application.Current).Properties["FilePathArgName"] = savefileDialog.FileName;
                }
            }
        }

        private void TestBtn_Click(object sender, RoutedEventArgs e)
        {
            AddLinesToConsole("Started Plotting...");
            SeriesCollection.Clear();
            List<float> pmuVals = new List<float>();
            Random randNumGenerator = new Random();
            float val = 0;
            for (int i = 0; i < 100000; i++)
            {
                val = val + 0.001f * randNumGenerator.Next(-100, 100);
                pmuVals.Add(val);
            }
            SeriesCollection.Add(new GLineSeries() { Title = "Geared Values Testing", Values = new GearedValues<float>(pmuVals), PointGeometry = null, Fill = Brushes.Transparent, StrokeThickness = 2, LineSmoothness = 0 });
            AddLinesToConsole("Finished Plotting!!!");
            ResetAxes();
        }

        private async void FetchBtn_Click(object sender, RoutedEventArgs e)
        {
            await FetchAndPlotData();
            ResetAxes();
        }

        private void FetchStopBtn_Click(object sender, RoutedEventArgs e)
        {
            // stop running fetch tasks
            if (cts != null)
            {
                cts.Cancel();                
            }
        }

        private async Task FetchAndPlotData()
        {
            // stop running fetch tasks
            if (cts != null)
            {
                //cts.Cancel();
                return;
            }
            // ***Instantiate the CancellationTokenSource.
            cts = new CancellationTokenSource();
            try
            {
                // fetch the points data from plotTemplate_ and plot them
                DateTime startTime = Helpers.Helpers.GetTimeFromTemplate(plotTemplate_.startDateMode, plotTemplate_.startDateVariable, plotTemplate_.startDateTime);
                DateTime endTime = Helpers.Helpers.GetTimeFromTemplate(plotTemplate_.endDateMode, plotTemplate_.endDateVariable, plotTemplate_.endDateTime);
                if (plotTemplate_.measIds.Count == 0)
                {
                    plotTemplate_.measIds.AddRange(new List<int>() { 4924, 4929 });
                    plotTemplate_.measurementNames.AddRange(new List<string>() { "meas1", "meas2" });
                }
                //PlotMeasIds(startTime, endTime);
                await PlotMeasIdsAsync(startTime, endTime, cts.Token);
            }
            // *** If cancellation is requested, an OperationCanceledException results.
            catch (OperationCanceledException)
            {
                AddLinesToConsole("Existing Fetch task cancelled");
            }
            catch (Exception ex)
            {
                AddLinesToConsole($"Error in running fetch task: {ex.Message}");
            }
            // ***Set the CancellationTokenSource to null when the download is complete.
            cts = null;
        }

        public void PlotMeasIds(DateTime startTime, DateTime endTime)
        {
            object payLoad = new { startTime = startTime, endTime = endTime, dataRate = plotTemplate_.dataRate, measurementIDs = plotTemplate_.measIds, measurementNames = plotTemplate_.measurementNames, fetchWindow = plotTemplate_.fetchWindow };
            AddLinesToConsole("Started fetching data");
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.RunWorkerAsync(payLoad);
        }

        // worker thread background stuff
        // Todo update immediately on the chart after the piecewise fetch
        void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            // todo refer template manager for for doing the piecewise fetch
            object argument = e.Argument;
            int dataRate = (int)argument.GetType().GetProperty("dataRate").GetValue(argument, null);
            DateTime startTime = (DateTime)argument.GetType().GetProperty("startTime").GetValue(argument, null);
            DateTime endTime = (DateTime)argument.GetType().GetProperty("endTime").GetValue(argument, null);
            List<int> measurementIDs = (List<int>)argument.GetType().GetProperty("measurementIDs").GetValue(argument, null);
            List<string> measurementNames = (List<string>)argument.GetType().GetProperty("measurementNames").GetValue(argument, null);
            VariableTime fetchWindow = (VariableTime)argument.GetType().GetProperty("fetchWindow").GetValue(argument, null);
            if (measurementIDs.Count == 0)
            {
                object nullVal = null;
                e.Result = new { measurementsData = nullVal, startTime = startTime, endTime = endTime, dataRate = dataRate, measurementIDs = measurementIDs, measurementNames = measurementNames, isSuccess = false, errorMsg = "Number of measurements are zero..." };
                return;
            }

            ConfigurationManager _configManager = new ConfigurationManager();
            _configManager.Initialize();
            HistoryDataAdapter _historyAdapter = new HistoryDataAdapter();
            _historyAdapter.Initialize(_configManager);
            if (dataRate <= 0 || dataRate > 25)
            {
                // default dataRate of PMU
                dataRate = 25;
            }
            int numWindows = 1;
            // find the number of seconds in a fetch window
            //stub
            int reportFetchWindowSecs = (int)(fetchWindow.hours * 60 * 60 + fetchWindow.mins * 60 + fetchWindow.secs);
            if (reportFetchWindowSecs <= 0)
            {
                numWindows = 1;
                reportFetchWindowSecs = Convert.ToInt32(Math.Ceiling((endTime - startTime).TotalSeconds));
            }
            else
            {
                // find the number of fetch windows as Ceil(Fetchspan/windowspan)
                int reportfetchSpanSecs = Convert.ToInt32(Math.Ceiling((endTime - startTime).TotalSeconds));
                numWindows = reportfetchSpanSecs / reportFetchWindowSecs;
            }
            DateTime fetchEndTime = startTime;
            Dictionary<object, List<PMUDataStructure>> parsedData;
            List<PMUMeasDataLists> measurementsData = new List<PMUMeasDataLists>();

            for (int window = 0; window < numWindows; window++)
            {
                // fetch and update for ith window
                DateTime fetchStartTime = fetchEndTime;
                fetchEndTime = fetchStartTime.AddSeconds(reportFetchWindowSecs);
                if (fetchEndTime > endTime)
                {
                    fetchEndTime = endTime;
                }
                // get the data of all measurementIds for the window
                parsedData = _historyAdapter.GetData(fetchStartTime, fetchEndTime, measurementIDs, true, false, dataRate);
                // check if we have atleast one measurement
                if (measurementIDs.Count > 0 && parsedData != null)
                {
                    // get the data of measurements and add to List
                    for (int i = 0; i < measurementIDs.Count; i++)
                    {
                        PMUMeasDataLists measurementData;
                        measurementData = _historyAdapter.getDataOfMeasId(parsedData, (uint)measurementIDs.ElementAt(i), true);
                        if (window == 0)
                        {
                            measurementsData.Add(measurementData);
                        }
                        else
                        {
                            measurementsData.ElementAt(i).pmuQualities.AddRange(measurementData.pmuQualities);
                            measurementsData.ElementAt(i).pmuTimeStamps.AddRange(measurementData.pmuTimeStamps);
                            measurementsData.ElementAt(i).pmuVals.AddRange(measurementData.pmuVals);
                        }
                    }
                }
                else
                {
                    // we didnot get the required result
                    e.Result = new { measurementsData = measurementsData, startTime = startTime, endTime = endTime, dataRate = dataRate, measurementIDs = measurementIDs, measurementNames = measurementNames, isSuccess = false, errorMsg = "Unable to parse data..." };
                    return;
                }
                (sender as BackgroundWorker).ReportProgress(window, new { numWindows = numWindows });
            }

            e.Result = new { measurementsData = measurementsData, startTime = startTime, endTime = endTime, dataRate = dataRate, measurementIDs = measurementIDs, measurementNames = measurementNames, isSuccess = true, errorMsg = "" };
        }

        // worker thread ui update stuff
        void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            object res = e.UserState;
            int numWindows = (int)res.GetType().GetProperty("numWindows").GetValue(res, null);
            AddLinesToConsole("Completed fetching of " + (e.ProgressPercentage + 1).ToString() + " of " + numWindows + " windows");
        }

        // worker thread completed stuff
        void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            AddLinesToConsole("Finished fetching data");
            object res = e.Result;

            bool isSuccess = (bool)res.GetType().GetProperty("isSuccess").GetValue(res, null);
            string errorMsg = (string)res.GetType().GetProperty("errorMsg").GetValue(res, null);

            if (isSuccess == false)
            {
                AddLinesToConsole(errorMsg);
                return;
            }

            List<PMUMeasDataLists> measurementsData = (List<PMUMeasDataLists>)res.GetType().GetProperty("measurementsData").GetValue(res, null);
            List<int> measurementIDs = (List<int>)res.GetType().GetProperty("measurementIDs").GetValue(res, null);
            List<string> measurementNames = (List<string>)res.GetType().GetProperty("measurementNames").GetValue(res, null);
            int dataRate = (int)res.GetType().GetProperty("dataRate").GetValue(res, null);

            // SeriesCollection = new SeriesCollection();
            SeriesCollection.Clear();

            // Todo change step as per the plot preferences.
            Step = dataRate;

            // get 1st measurement Data and add to SeriesCollection, also update the timestamps and dataRate
            PMUMeasDataLists lists = measurementsData.ElementAt(0);
            timeStamps_ = new List<DateTime>(lists.pmuTimeStamps);
            SeriesCollection.Add(new GLineSeries() { Title = measurementNames.ElementAt(0) + "_" + measurementIDs.ElementAt(0).ToString(), Values = new GearedValues<float>(lists.pmuVals), PointGeometry = null, Fill = Brushes.Transparent, StrokeThickness = 2, LineSmoothness = 0 });

            // get the data of remaining measurements and add to SeriesCollection
            for (int i = 1; i < measurementIDs.Count; i++)
            {
                lists = measurementsData.ElementAt(i);
                SeriesCollection.Add(new GLineSeries() { Title = measurementNames.ElementAt(i).ToString() + "_" + measurementIDs.ElementAt(i).ToString(), Values = new GearedValues<float>(lists.pmuVals), PointGeometry = null, Fill = Brushes.Transparent, StrokeThickness = 2, LineSmoothness = 0 });
            }
            ResetAxes();
        }

        public async Task PlotMeasIdsAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken)
        {
            await Task.Delay(1); // 0 millisecond delay
            AddLinesToConsole("Started fetching async data");
            int dataRate = plotTemplate_.dataRate;
            List<int> measurementIDs = plotTemplate_.measIds;
            List<string> measurementNames = plotTemplate_.measurementNames;
            VariableTime fetchWindow = plotTemplate_.fetchWindow;
            if (measurementIDs.Count == 0)
            {
                return;
            }
            //ConfigurationManager _configManager = new ConfigurationManager();
            //_configManager.Initialize();
            //HistoryDataAdapter _historyAdapter = new HistoryDataAdapter();
            //_historyAdapter.Initialize(_configManager);
            if (dataRate <= 0 || dataRate > 25)
            {
                // default dataRate of PMU
                dataRate = 25;
            }
            int numWindows = 1;
            // find the number of seconds in a fetch window
            //stub
            int reportFetchWindowSecs = (int)(fetchWindow.hours * 60 * 60 + fetchWindow.mins * 60 + fetchWindow.secs);
            if (reportFetchWindowSecs <= 0)
            {
                numWindows = 1;
                reportFetchWindowSecs = Convert.ToInt32(Math.Ceiling((endTime - startTime).TotalSeconds));
            }
            else
            {
                // find the number of fetch windows as Ceil(Fetchspan/windowspan)
                int reportfetchSpanSecs = Convert.ToInt32(Math.Ceiling((endTime - startTime).TotalSeconds));
                numWindows = reportfetchSpanSecs / reportFetchWindowSecs;
            }
            DateTime fetchEndTime = startTime;
            Dictionary<object, List<PMUDataStructure>> parsedData;
            List<PMUMeasDataLists> measurementsData = new List<PMUMeasDataLists>();

            for (int window = 0; window < numWindows; window++)
            {
                // fetch and update for ith window
                DateTime fetchStartTime = fetchEndTime;
                fetchEndTime = fetchStartTime.AddSeconds(reportFetchWindowSecs);
                if (fetchEndTime > endTime)
                {
                    fetchEndTime = endTime;
                }
                // get the data of all measurementIds for the window
                parsedData = await _historyAdapter.GetDataAsync(fetchStartTime, fetchEndTime, measurementIDs, true, false, dataRate);
                cancellationToken.ThrowIfCancellationRequested();
                // check if we have atleast one measurement
                if (parsedData != null)
                {
                    // get the data of measurements and add to List
                    for (int i = 0; i < measurementIDs.Count; i++)
                    {
                        PMUMeasDataLists measurementData;
                        measurementData = await _historyAdapter.getDataOfMeasIdAsync(parsedData, (uint)measurementIDs.ElementAt(i), true);
                        cancellationToken.ThrowIfCancellationRequested();
                        if (window == 0)
                        {
                            measurementsData.Add(measurementData);
                        }
                        else
                        {
                            measurementsData.ElementAt(i).pmuQualities.AddRange(measurementData.pmuQualities);
                            measurementsData.ElementAt(i).pmuTimeStamps.AddRange(measurementData.pmuTimeStamps);
                            measurementsData.ElementAt(i).pmuVals.AddRange(measurementData.pmuVals);
                        }
                    }
                }
                else
                {
                    // we didnot get the required result
                    return;
                }
                // todo update plot
                AddLinesToConsole($"Completed async fetching of {window + 1} of {numWindows} windows");
            }
            // fetch completed, now update plot
            AddLinesToConsole("Finished async fetching data");
            SeriesCollection.Clear();

            // Todo change step as per the plot preferences.
            Step = dataRate;

            // get 1st measurement Data and add to SeriesCollection, also update the timestamps and dataRate
            PMUMeasDataLists lists = measurementsData.ElementAt(0);
            timeStamps_ = new List<DateTime>(lists.pmuTimeStamps);
            SeriesCollection.Add(new GLineSeries() { Title = measurementNames.ElementAt(0) + "_" + measurementIDs.ElementAt(0).ToString(), Values = new GearedValues<float>(lists.pmuVals), PointGeometry = null, Fill = Brushes.Transparent, StrokeThickness = 2, LineSmoothness = 0 });

            // get the data of remaining measurements and add to SeriesCollection
            for (int i = 1; i < measurementIDs.Count; i++)
            {
                lists = measurementsData.ElementAt(i);
                SeriesCollection.Add(new GLineSeries() { Title = measurementNames.ElementAt(i).ToString() + "_" + measurementIDs.ElementAt(i).ToString(), Values = new GearedValues<float>(lists.pmuVals), PointGeometry = null, Fill = Brushes.Transparent, StrokeThickness = 2, LineSmoothness = 0 });
            }
        }

        public void AddLinesToConsole(string str)
        {
            string consoleTxt = WelcomeText.Text;
            // todo limit number of lines to 10
            WelcomeText.Text = DateTime.Now.ToString() + ": " + str + "\n" + consoleTxt;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            //Application.Current.Shutdown();
            this.Close();
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
                AddLinesToConsole("Zoom mode set to XY");
            }
            else if (mItem.Tag.ToString() == "ZX")
            {
                MyChart.Zoom = ZoomingOptions.X;
                AddLinesToConsole("Zoom mode set to X");
            }
            else if (mItem.Tag.ToString() == "ZY")
            {
                MyChart.Zoom = ZoomingOptions.Y;
                AddLinesToConsole("Zoom mode set to Y");
            }
            else if (mItem.Tag.ToString() == "ZOff")
            {
                MyChart.Zoom = ZoomingOptions.None;
                AddLinesToConsole("Zoom mode set to Off");
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
                AddLinesToConsole("Pan mode set to XY");
            }
            else if (mItem.Tag.ToString() == "PX")
            {
                MyChart.Pan = PanningOptions.X;
                AddLinesToConsole("Pan mode set to X");
            }
            else if (mItem.Tag.ToString() == "PY")
            {
                MyChart.Pan = PanningOptions.Y;
                AddLinesToConsole("Pan mode set to Y");
            }
            else if (mItem.Tag.ToString() == "POff")
            {
                MyChart.Pan = PanningOptions.None;
                AddLinesToConsole("Pan mode set to None");
            }

        }

        private void ResetAxes()
        {
            // If no line series are present, then use Double.NaN for resetting the axis
            if (SeriesCollection == null || SeriesCollection.Count == 0)
            {
                MyChart.AxisX[0].MinValue = double.NaN;
                MyChart.AxisX[0].MaxValue = double.NaN;
                MyChart.AxisY[0].MinValue = double.NaN;
                MyChart.AxisY[0].MaxValue = double.NaN;
            }
            else
            {
                // get the first sample of all the lineseries, add +-10 for Y max/min and , lineSeries length for X axis max/min
                double maxYVal = double.NaN;
                double minYVal = double.NaN;
                double numXSamples = 100;
                // find the number of samples present
                numXSamples = SeriesCollection.ElementAt(0).Values.Count;
                for (int i = 0; i < SeriesCollection.Count; i++)
                {
                    //double maxSeriesVal = ((LiveCharts.Geared.GearedValues<float>)SeriesCollection.ElementAt(i).Values).Max();
                    //double minSeriesVal = ((LiveCharts.Geared.GearedValues<float>)SeriesCollection.ElementAt(i).Values).Min();
                    LiveCharts.Geared.GearedValues<float> SeriesValues = ((LiveCharts.Geared.GearedValues<float>)SeriesCollection.ElementAt(i).Values);
                    if (SeriesValues.Count < 1)
                    {
                        continue;
                    }
                    double maxSeriesVal = SeriesValues.Max();
                    if (double.IsNaN(maxSeriesVal))
                    {
                        maxSeriesVal = SeriesValues.First();
                    }

                    double minSeriesVal = SeriesValues.Min();
                    if (double.IsNaN(minSeriesVal))
                    {
                        minSeriesVal = SeriesValues.First();
                    }

                    if (double.IsNaN(minYVal))
                    {
                        minYVal = minSeriesVal;
                    }

                    if (double.IsNaN(maxYVal))
                    {
                        maxYVal = maxSeriesVal;
                    }

                    if (!double.IsNaN(minSeriesVal) && minYVal > minSeriesVal)
                    {
                        minYVal = minSeriesVal;
                    }
                    if (!double.IsNaN(maxSeriesVal) && maxYVal < maxSeriesVal)
                    {
                        maxYVal = maxSeriesVal;
                    }
                }
                double valDiff = 0;
                if (!double.IsNaN(maxYVal) && !double.IsNaN(minYVal))
                {
                    valDiff = maxYVal - minYVal;
                }
                MyChart.AxisX[0].SetRange(-numXSamples * 0.1, numXSamples * 1.1);
                MyChart.AxisY[0].SetRange(minYVal - valDiff * 0.1, maxYVal + valDiff * 0.1);
            }

        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            ResetAxes();
            AddLinesToConsole("Reset Axis done...");
        }

        private void NewWindow_Click(object sender, RoutedEventArgs e)
        {
            GearedPlotWindow win = new GearedPlotWindow();
            win.Show();
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
            if (sender is PointsConfigWindow configWin)
            {
                // change the plot Data Template as per the window message
                this.plotTemplate_ = e.dataTemplate;
                //todo use notifypropertychanged with a vm to notify the background and text colors
                NotifyPropertyChanged("PlotBackgroundColor");
            }
        }
    }
}
