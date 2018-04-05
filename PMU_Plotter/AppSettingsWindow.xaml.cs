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
    /// Interaction logic for AppSettingsWindow.xaml
    /// </summary>
    public partial class AppSettingsWindow : Window
    {
        AppSettingsVM appSettingsVM;
        public AppSettingsWindow()
        {
            InitializeComponent();
            appSettingsVM = new AppSettingsVM();
            AppSettingsConfigForm.DataContext = appSettingsVM;
        }

        private void UpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Update Application Settings ?", "Update Application Settings", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                //do no stuff
            }
            else
            {
                appSettingsVM.SaveXml();
                DialogResult = true;
            }            
        }
    }

    public class AppSettingsVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string HostString { get { return configurationManager.Host; } set { configurationManager.Host = value; } }
        public string PathString { get { return configurationManager.Path; } set { configurationManager.Path = value; } }
        public string UsernameString { get { return configurationManager.UserName; } set { configurationManager.UserName = value; } }
        public string PasswordString { get { return configurationManager.Password; } set { configurationManager.Password = value; } }

        public ConfigurationManager configurationManager;

        public AppSettingsVM()
        {
            configurationManager = new ConfigurationManager();
            // update the settings from xml file
            UpdateFromXml();
        }

        public void UpdateFromXml()
        {
            configurationManager.Initialize();
        }

        public void SaveXml()
        {
            configurationManager.Save();
        }
    }
}
