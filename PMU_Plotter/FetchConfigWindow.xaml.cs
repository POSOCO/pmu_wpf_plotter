using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for FetchConfigWindow.xaml
    /// </summary>
    public partial class FetchConfigWindow : Window
    {
        private FetchConfigVM fetchConfigVM;
        public event EventHandler<AutoFetchConfigMessageEventArgs> NewMessage;
        public FetchConfigWindow()
        {
            InitializeComponent();
            fetchConfigVM = new FetchConfigVM();
            AutoFetchConfigForm.DataContext = fetchConfigVM;
        }

        public void SetAutoFetchConfig(AutoFetchConfig autoFetchConfig)
        {
            fetchConfigVM.AutoFetchConfig = autoFetchConfig;
        }

        private void UpdateChanges_Click(object sender, RoutedEventArgs e)
        {
            if (NewMessage != null)
            {
                if (MessageBox.Show("Update Auto Fetch Configuration ?", "Update Configurtion", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    //do no stuff
                }
                else
                {
                    NewMessage(this, new AutoFetchConfigMessageEventArgs(fetchConfigVM.AutoFetchConfig));
                    this.Close();
                }
            }
        }
    }

    public class AutoFetchConfigMessageEventArgs : EventArgs
    {
        public AutoFetchConfigMessageEventArgs(AutoFetchConfig autoFetchAonfig)
        {
            this.AutoFetchConfig_ = autoFetchAonfig;
        }
        public AutoFetchConfig AutoFetchConfig_ { get; set; }
    }

    public class FetchConfigVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private AutoFetchConfig autoFetchConfig;

        public AutoFetchConfig AutoFetchConfig
        {
            get { return autoFetchConfig; }
            set
            {
                autoFetchConfig = value;
                NotifyPropertyChanged("FetchWindowHrs");
                NotifyPropertyChanged("FetchWindowMins");
                NotifyPropertyChanged("FetchWindowSecs");
            }
        }

        public FetchConfigVM()
        {
            this.autoFetchConfig = new AutoFetchConfig();
        }

        public string FetchWindowHrs
        {
            get { return autoFetchConfig.TimePeriod_.hours.ToString(); }
            set
            {
                double doubleVal = autoFetchConfig.TimePeriod_.hours;
                if (double.TryParse(value, out doubleVal))
                {
                    autoFetchConfig.TimePeriod_.hours = doubleVal;
                }
            }
        }

        public string FetchWindowMins
        {
            get { return autoFetchConfig.TimePeriod_.mins.ToString(); }
            set
            {
                double doubleVal = autoFetchConfig.TimePeriod_.mins;
                if (double.TryParse(value, out doubleVal))
                {
                    autoFetchConfig.TimePeriod_.mins = doubleVal;
                }
            }
        }

        public string FetchWindowSecs
        {
            get { return autoFetchConfig.TimePeriod_.secs.ToString(); }
            set
            {
                double doubleVal = autoFetchConfig.TimePeriod_.secs;
                if (double.TryParse(value, out doubleVal))
                {
                    autoFetchConfig.TimePeriod_.secs = doubleVal;
                }
            }
        }
    }
}
