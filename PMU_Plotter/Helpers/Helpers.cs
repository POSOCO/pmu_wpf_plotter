using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PMU_Plotter.Helpers
{
    class Helpers
    {
        public static Color IdealTextColor(Color bg)
        {
            int nThreshold = 105;
            int bgDelta = Convert.ToInt32((bg.R * 0.299) + (bg.G * 0.587) +
                                          (bg.B * 0.114));

            Color foreColor = (255 - bgDelta < nThreshold) ? Color.FromRgb(0, 0, 0) : Color.FromRgb(255, 255, 255);
            return foreColor;
        }

        public static string ColorToHexString(Color c)
        {
            string str = "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
            return str;
        }

        public static DateTime GetTimeFromTemplate(string mode, VariableTime variableTime, DateTime dateTime)
        {
            DateTime resultTime = DateTime.Now;
            if (mode == "variable")
            {
                resultTime = resultTime.AddHours(variableTime.hours * -1);
                resultTime = resultTime.AddMinutes(variableTime.mins * -1);
                resultTime = resultTime.AddSeconds(variableTime.secs * -1);
            }
            else { resultTime = dateTime; }
            return resultTime;
        }
    }
}
