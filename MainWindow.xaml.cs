using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using Nefarius.ViGEm.Client;
using neptune_hidapi.net;

namespace sd_drivers
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly TaskbarIcon tbi = new();
        readonly NeptuneController neptune = new();
        public MainWindow()
        {
            InitializeComponent();
            SetTaskbarIcon();
            neptune.OnControllerInputReceived += Neptune_OnControllerInputReceived;
            neptune.LizardButtonsEnabled = false;
            neptune.LizardMouseEnabled = true; //Keep the trackpad as a real mouse
        }

        private Task Neptune_OnControllerInputReceived(NeptuneControllerInputEventArgs arg)
        {
            UpdateUI(arg.State);
            return Task.CompletedTask;
        }

        private void UpdateUI(NeptuneControllerInputState state)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                y_button_state.Content = state.ButtonState[NeptuneControllerButton.BtnY];
            });
        }
        
        private void SetTaskbarIcon()
        {
            var icon = neptune.isActive() ? "content\\on.ico" : "content\\off.ico";
            tbi.Icon = new System.Drawing.Icon(icon, new System.Drawing.Size(96, 96));
            tbi.ToolTipText = neptune.isActive() ? "Active" : "Unactive";
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

            SetTaskbarIcon();
        }
    }
}
