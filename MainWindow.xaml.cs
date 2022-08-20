using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Keyboard;
using Hardcodet.Wpf.TaskbarNotification;
using neptune_hidapi.net;
using static System.Windows.Forms.AxHost;

namespace sd_drivers
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly TaskbarIcon tbi = new();
        readonly NeptuneController neptune = new();

        List<ButtonState> buttons = new();

        static Dictionary<NeptuneControllerButton, VirtualKeyCode> ButtonsToKeyCodes = new();
        static List<NeptuneControllerButton> SpammableButtons = new();

        public MainWindow()
        {
            InitializeComponent();
            SetTaskbarIcon();
            InitUI();
            InitDictionary();
            InitSpammableButtons();
            
            InitButtons();

            neptune.OnControllerInputReceived += Neptune_OnControllerInputReceived;
            neptune.LizardButtonsEnabled = false;
            neptune.LizardMouseEnabled = true; //Keep the trackpad as a real mouse
        }

        private void InitDictionary()
        {
            ButtonsToKeyCodes.Add(NeptuneControllerButton.BtnA, VirtualKeyCode.Space);
            ButtonsToKeyCodes.Add(NeptuneControllerButton.BtnB, VirtualKeyCode.LeftControl);
            ButtonsToKeyCodes.Add(NeptuneControllerButton.BtnX, VirtualKeyCode.KeyR);
            ButtonsToKeyCodes.Add(NeptuneControllerButton.BtnY, VirtualKeyCode.KeyQ);
            ButtonsToKeyCodes.Add(NeptuneControllerButton.BtnMenu, VirtualKeyCode.Tab);
            ButtonsToKeyCodes.Add(NeptuneControllerButton.BtnSteam, VirtualKeyCode.LeftWin);
            ButtonsToKeyCodes.Add(NeptuneControllerButton.BtnOptions, VirtualKeyCode.Escape);
            ButtonsToKeyCodes.Add(NeptuneControllerButton.BtnDpadLeft, VirtualKeyCode.Left);
            ButtonsToKeyCodes.Add(NeptuneControllerButton.BtnDpadDown, VirtualKeyCode.Down);
            ButtonsToKeyCodes.Add(NeptuneControllerButton.BtnDpadRight, VirtualKeyCode.Right);
            ButtonsToKeyCodes.Add(NeptuneControllerButton.BtnDpadUp, VirtualKeyCode.Up);
            ButtonsToKeyCodes.Add(NeptuneControllerButton.BtnLPadTouch, VirtualKeyCode.KeyW);
            ButtonsToKeyCodes.Add(NeptuneControllerButton.BtnR2, VirtualKeyCode.Lbutton);
        }

        private void InitSpammableButtons()
        {
            SpammableButtons.Add(NeptuneControllerButton.BtnLPadTouch);
            SpammableButtons.Add(NeptuneControllerButton.BtnR2);
        }

        private void InitButtons()
        {
            foreach(NeptuneControllerButton button in Enum.GetValues(typeof(NeptuneControllerButton)))
            {
                if (ButtonsToKeyCodes.ContainsKey(button))
                    buttons.Add(new ButtonState(button, ButtonsToKeyCodes[button], SpammableButtons.Contains(button)));
            }
        }

        private void InitUI()
        {
            var collection = DeckCanvas.Children.OfType<Ellipse>().ToList();
            collection.ForEach(x => x.Visibility = Visibility.Hidden);
        }

        private Task Neptune_OnControllerInputReceived(NeptuneControllerInputEventArgs arg)
        {
            TranslateInputs(arg.State);
            UpdateUI(arg.State);
            return Task.CompletedTask;
        }

        private void TranslateInputs(NeptuneControllerInputState state)
        {
            List<ButtonState> spammableButtonsToDeactivate = new();
            
            foreach (var button in buttons)
            {
                if (button.isPressed && state.ButtonState[button.Button] && !button.isSpammable)
                {
                    button.wasTriggeredAndIsStillHeld = true;
                }

                if(button.isPressed && !state.ButtonState[button.Button] && !button.isSpammable) 
                {
                    button.wasTriggeredAndIsStillHeld = false;
                }

                if (button.isPressed && !state.ButtonState[button.Button] && button.isSpammable)
                {
                    spammableButtonsToDeactivate.Add(button);
                }
            }
            
            buttons.ForEach(x => x.isPressed = state.ButtonState[x.Button]);

            KeyboardInputGenerator.KeyDown(buttons.Where(x => x.isPressed && !x.wasTriggeredAndIsStillHeld).Select(x => x.Key).ToArray());
            KeyboardInputGenerator.KeyUp(buttons.Where(x => x.isPressed && !x.isSpammable).Select(x => x.Key).ToArray());
            
            if (spammableButtonsToDeactivate.Count > 0)
            {
                KeyboardInputGenerator.KeyUp(spammableButtonsToDeactivate.Select(x => x.Key).ToArray());
                spammableButtonsToDeactivate.ForEach(x => x.isPressed = false);
            }
        }

        private void UpdateUI(NeptuneControllerInputState state)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                this.btn_a.Visibility = state.ButtonState[NeptuneControllerButton.BtnA] ? Visibility.Visible : Visibility.Hidden;
                this.btn_b.Visibility = state.ButtonState[NeptuneControllerButton.BtnB] ? Visibility.Visible : Visibility.Hidden;
                this.btn_x.Visibility = state.ButtonState[NeptuneControllerButton.BtnX] ? Visibility.Visible : Visibility.Hidden;
                this.btn_y.Visibility = state.ButtonState[NeptuneControllerButton.BtnY] ? Visibility.Visible : Visibility.Hidden;
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
            var button = (System.Windows.Controls.Button)sender;

            if (neptune.isActive())
            {
                neptune.Close();
                InitUI();
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
    public class ButtonState {
       
        public bool isSpammable;
        public bool wasTriggeredAndIsStillHeld;
        public bool isPressed;
        public ButtonState(NeptuneControllerButton button, VirtualKeyCode key, bool isSpammable = false)
        {
            this.Button = button;
            this.Key = key;
            this.isSpammable = isSpammable;
        }

        public NeptuneControllerButton Button { get; set; }
        public VirtualKeyCode Key { get; set; }
    }

    public enum State
    {
        Activated,
        Deactivated,
        ToActivate,
        ToDeactivate
    }
}
