using System;
using System.Configuration;
using System.Data;
using System.Windows;
using AutoUpdaterDotNET;

namespace AQShuttle
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Point AutoUpdater directly to your GitHub update.xml file
            AutoUpdater.Start("https://raw.githubusercontent.com/Lifereaper/AQShuttle/master/update.xml");
        }
    }
}
