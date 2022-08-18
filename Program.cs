using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nefarius.ViGEm.Client;
using neptune_hidapi.net;

namespace sd_drivers
{
    class Program
    {
        static readonly ViGEmClient client = new();
        static readonly IVirtualGamepad virtual360Gamepad = client.CreateXbox360Controller();
        static readonly IVirtualGamepad virtualDS4Gamepad = client.CreateDualShock4Controller();
        public static readonly NeptuneController neptune = new();
        
        [STAThread]
        static void Main()
        {
            App application = new();
            application.InitializeComponent();
            application.Run();

            neptune.OnControllerInputReceived += NeptuneInputInterpreter.Neptune_OnControllerInputReceived;
            neptune.LizardButtonsEnabled = false;
            neptune.LizardMouseEnabled = true; //Keep the trackpad as a real mouse
        }
    }
}
