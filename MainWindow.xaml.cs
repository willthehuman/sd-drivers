using System;
using System.Windows;
using Nefarius.ViGEm.Client;
namespace sd_drivers
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static readonly ViGEmClient client = new();
        static readonly IVirtualGamepad virtualGamepad = client.CreateXbox360Controller();
        public MainWindow()
        {
            InitializeComponent();
            virtualGamepad.Connect();
        }
    }
}
