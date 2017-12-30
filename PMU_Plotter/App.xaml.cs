﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PMU_Plotter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // https://stackoverflow.com/questions/26845815/how-to-pass-file-as-parameter-to-your-program-wpf-c-sharp
            this.Properties["ArbitraryArgName"] = null;
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                var fileName = args[1];
                if (File.Exists(fileName))
                {
                    /*
                    var extension = Path.GetExtension(fileName);
                    if (extension == ".sudhir")
                    {
                        this.Properties["ArbitraryArgName"] = fileName;
                    }
                    */
                    this.Properties["ArbitraryArgName"] = fileName;
                }
            }
            base.OnStartup(e);
        }
    }
}