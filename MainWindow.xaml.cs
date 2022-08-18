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
        private static readonly NeptuneController neptune = Program.neptune;
        public MainWindow()
        {
            InitializeComponent();
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
