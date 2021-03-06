using LiveCharts.Defaults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMU_Plotter
{
    [Serializable]
    public class PMUMeasDataLists
    {
        public List<float> pmuVals { get; set; }
        public List<string> pmuQualities { get; set; }
        public List<DateTime> pmuTimeStamps { get; set; }

        public PMUMeasDataLists()
        {
            pmuVals = new List<float>();
            pmuQualities = new List<string>();
            pmuTimeStamps = new List<DateTime>();
        }

        public PMUMeasDataLists(List<float> pmuVals, List<string> pmuQualities,List<DateTime> pmuTimeStamps)
        {
            this.pmuVals = pmuVals;
            this.pmuQualities = pmuQualities;
            this.pmuTimeStamps = pmuTimeStamps;
        }

        public List<DateTimePoint> getDateTimePoints()
        {
            List<DateTimePoint> points = new List<DateTimePoint>();
            for (int i=0;i<pmuTimeStamps.Count;i++)
            {
                points.Add(new DateTimePoint(pmuTimeStamps.ElementAt(i), pmuVals.ElementAt(i)));
            }
            return points;
        }
    }
}
