using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMU_Plotter
{
    public class PlotDataTemplate
    {
        public List<int> measIds { get; set; }
        public List<String> measurementNames { get; set; }
        public string dataSetName { get; set; }
        public string startDateMode { get; set; }
        public DateTime startDateTime { get; set; }
        public VariableTime startDateVariable { get; set; }
        public string endDateMode { get; set; }
        public DateTime endDateTime { get; set; }
        public VariableTime endDateVariable { get; set; }
        public int dataRate { get; set; }
        public VariableTime fetchWindow { get; set; }

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
