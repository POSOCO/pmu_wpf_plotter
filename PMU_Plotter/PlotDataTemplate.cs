using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PMU_Plotter
{
    public class PlotDataTemplate
    {
        public List<int> measIds { get; set; } = new List<int>();
        public List<String> measurementNames { get; set; }
        public string dataSetName { get; set; } = "Plot Title";
        public string startDateMode { get; set; } = "variable";
        public DateTime startDateTime { get; set; } = DateTime.Now;
        public VariableTime startDateVariable { get; set; } = new VariableTime();
        public string endDateMode { get; set; } = "variable";
        public DateTime endDateTime { get; set; } = DateTime.Now;
        public VariableTime endDateVariable { get; set; } = new VariableTime();
        public int dataRate { get; set; } = 25;
        public VariableTime fetchWindow { get; set; } = new VariableTime();
        public string BackgroundColor { get; set; } = "#171717";
        //public string TextColor { get; set; } = Helpers.Helpers.ColorToHexString(Helpers.Helpers.IdealTextColor((Color)ColorConverter.ConvertFromString("#171717")));
        public string TextColor { get; set; } = "#FF0000";
        public PlotDataTemplate()
        {
            measIds = new List<int>();
            measurementNames = new List<string>();
            dataSetName = "Plot Title";
            startDateMode = "variable";
            startDateTime = DateTime.Now;
            startDateVariable = new VariableTime();
            endDateMode = "variable";
            endDateTime = DateTime.Now;
            endDateVariable = new VariableTime();
            dataRate = 25;
            fetchWindow = new VariableTime();
        }
    }

    public class VariableTime
    {
        public double hours { get; set; } = 0;
        public double mins { get; set; } = 0;
        public double secs { get; set; } = 0;
    }
}
