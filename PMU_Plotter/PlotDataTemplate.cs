using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMU_Plotter
{
    class PlotDataTemplate
    {
        public List<int> measIds { get; set; }
        public List<String> measurementNames { get; set; }
        public string dataSetName { get; set; }
        public string dateLimitsMode { get; set; }
        public int dataRate { get; set; }

        public PlotDataTemplate()
        {
            measIds = new List<int>();
            measurementNames = new List<string>();
            dataSetName = "Plot Title";
            dateLimitsMode = "variable";
            dataRate = 25;
        }
    }
}
