using System;
using System.ComponentModel;
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
        TaskbarIcon tbi = new();
        public MainWindow()
        {
            InitializeComponent();
            SetTaskbarIcon();
        }

        private void SetTaskbarIcon()
        {
            var icon = Program.neptune.isActive() ? "content\\on.ico" : "content\\off.ico";
            tbi.Icon = new System.Drawing.Icon(icon, new System.Drawing.Size(96, 96));
            tbi.ToolTipText = Program.neptune.isActive() ? "Active" : "Unactive";
        }

        private void btn_ActivateDriver_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

            if (Program.neptune.isActive())
            {
                Program.neptune.Close();
                button.Content = "Activate Driver";
            }
            else
            {
                Program.neptune.Open();
                button.Content = "Deactivate Driver";
            }

            SetTaskbarIcon();
        }
    }
}
