using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Nefarius.ViGEm.Client;
using neptune_hidapi.net;

namespace sd_drivers
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static readonly ViGEmClient client = new();
        static readonly IVirtualGamepad virtualGamepad = client.CreateXbox360Controller();
        static readonly NeptuneController neptune = new();
        public MainWindow()
        {
            InitializeComponent();
            //virtualGamepad.Connect();
            neptune.OnControllerInputReceived += neptune_OnControllerInputReceived;
            neptune.LizardButtonsEnabled = false; //Mouse and Keyboard emulation enabled.
            neptune.LizardMouseEnabled = true;
            //neptune.Open();
        }

        private async Task neptune_OnControllerInputReceived(NeptuneControllerInputEventArgs arg)
        {
            foreach (var btn in arg.State.ButtonState.Buttons)
            {
                Console.WriteLine($"{btn}: {arg.State.ButtonState[btn]}      ");
            }
            foreach (var axis in arg.State.AxesState.Axes)
            {
                Console.WriteLine($"{axis}: {arg.State.AxesState[axis]}      ");
            }
        }

        private void btn_ActivateDriver_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

            if (neptune.isActive())
            {
                neptune.Close();
                button.Content = "Activate Driver";
            }
            else
            {
                neptune.Open();
                button.Content = "Deactivate Driver";
            }
        }
    }
}
