using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Nefarius.ViGEm.Client;
using neptune_hidapi.net;

namespace sd_drivers
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static readonly ViGEmClient client = new();
        static readonly IVirtualGamepad virtualGamepad = client.CreateXbox360Controller();
        public static readonly NeptuneController neptune = new();
        
        public App()
        {
            neptune.OnControllerInputReceived += NeptuneInputInterpreter.Neptune_OnControllerInputReceived;
            neptune.LizardButtonsEnabled = false;
            neptune.LizardMouseEnabled = true; //Keep the trackpad as a real mouse
        }
    }
}
