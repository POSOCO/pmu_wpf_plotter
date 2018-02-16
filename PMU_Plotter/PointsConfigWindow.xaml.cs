using System;
using System.Collections.Generic;
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
        public PointsConfigWindow()
        {
            InitializeComponent();
            ConfigForm.DataContext = new MeasurementsVM();
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

        public class MeasurementsVM
        {
            public string dataSetName { get; set; } = "Plot Title";
            public int dataRate { get; set; } = 25;
            public List<int> measIds { get; set; } = new List<int> { 1, 2, 3 };
            public List<String> measurementNames { get; set; } = new List<string> { "One", "Two", "Three" };
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
    }
}
