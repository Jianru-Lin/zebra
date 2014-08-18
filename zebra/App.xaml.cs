using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace zebra
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Global g;
        private ViewManager viewManager;

        private void onStartup(object sender, StartupEventArgs e)
        {
            viewManager = new ViewManager();
        }
    }

}
