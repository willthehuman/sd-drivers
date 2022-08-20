using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Keyboard;
using Hardcodet.Wpf.TaskbarNotification;
using neptune_hidapi.net;
using Newtonsoft.Json;

namespace sd_drivers
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly TaskbarIcon tbi = new();
        readonly NeptuneController neptune = new();

        List<State> inputStates = new();

        static Dictionary<NeptuneControllerButton, VirtualKeyCode> ButtonsToKeyCodes = new();
        static Dictionary<NeptuneControllerAxis, VirtualKeyCode> AxisToKeyCodes = new();

        static List<NeptuneControllerButton> SpammableButtons = new();
        static List<NeptuneControllerAxis> SpammableAxis = new();
        static Dictionary<NeptuneControllerAxis, float> Thresholds = new();

        public MainWindow()
        {
            InitializeComponent();
            SetTaskbarIcon();

            //GenerateJson();

            InitUI();
            InitDictionary();
            InitSpammables();
            InitThresholds();

            InitButtons();
            InitAxis();

            neptune.OnControllerInputReceived += Neptune_OnControllerInputReceived;
            neptune.LizardButtonsEnabled = false;
            neptune.LizardMouseEnabled = true; //Keep the trackpad as a real mouse
        }

        private void InitDictionary()
        {
            //buttons
            ButtonsToKeyCodes.Clear();
            string fileName = "configs/config.json";
            string jsonString = File.ReadAllText(fileName);
            ButtonsToKeyCodes = JsonConvert.DeserializeObject<Dictionary<NeptuneControllerButton, VirtualKeyCode>>(jsonString)!;

            //axis (analog inputs like joysticks and triggers)
            AxisToKeyCodes.Clear();
            fileName = "configs/config_axis.json";
            jsonString = File.ReadAllText(fileName);
            AxisToKeyCodes = JsonConvert.DeserializeObject<Dictionary<NeptuneControllerAxis, VirtualKeyCode>>(jsonString)!;
        }

        private void InitSpammables()
        {
            SpammableButtons.Clear();
            string fileName = "configs/spammables.json";
            string jsonString = File.ReadAllText(fileName);
            SpammableButtons = JsonConvert.DeserializeObject<List<NeptuneControllerButton>>(jsonString)!;

            SpammableAxis.Clear();
            fileName = "configs/spammable_axis.json";
            jsonString = File.ReadAllText(fileName);
            SpammableAxis = JsonConvert.DeserializeObject<List<NeptuneControllerAxis>>(jsonString)!;
        }

        private void InitThresholds()
        {
            Thresholds.Clear();
            string fileName = "configs/thresholds.json";
            string jsonString = File.ReadAllText(fileName);
            Thresholds = JsonConvert.DeserializeObject<Dictionary<NeptuneControllerAxis, float>>(jsonString)!;
        }

        private void InitButtons()
        {
            foreach (NeptuneControllerButton button in Enum.GetValues(typeof(NeptuneControllerButton)))
            {
                if (ButtonsToKeyCodes.ContainsKey(button))
                    inputStates.Add(new ButtonState(button, ButtonsToKeyCodes[button], SpammableButtons.Contains(button)));
            }
        }

        private void InitAxis()
        {
            foreach (NeptuneControllerAxis axe in Enum.GetValues(typeof(NeptuneControllerAxis)))
            {
                if (AxisToKeyCodes.ContainsKey(axe))
                    inputStates.Add(new AxisState(axe, AxisToKeyCodes[axe], Thresholds[axe], SpammableAxis.Contains(axe)));
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
            List<State> spammableInputsToDeactivate = new();

            foreach (var input in inputStates)
            {
                if (input is ButtonState buttonState)
                {
                    var button = (ButtonState)input;
                    if (button.isPressed && state.ButtonState[button.Button] && !button.isSpammable)
                    {
                        button.wasTriggeredAndIsStillHeld = true;
                    }

                    if (button.isPressed && !state.ButtonState[button.Button] && !button.isSpammable)
                    {
                        button.wasTriggeredAndIsStillHeld = false;
                    }

                    if (button.isPressed && !state.ButtonState[button.Button] && button.isSpammable)
                    {
                        spammableInputsToDeactivate.Add(button);
                    }
                }
                
                if (input is AxisState axisState)
                {
                    var axe = (AxisState)input;
                    if (axe.isPressed && state.AxesState[axe.Axis] > Thresholds[axe.Axis] && !axe.isSpammable)
                    {
                        axe.wasTriggeredAndIsStillHeld = true;
                    }

                    if (axe.isPressed && !(state.AxesState[axe.Axis] > Thresholds[axe.Axis]) && !axe.isSpammable)
                    {
                        axe.wasTriggeredAndIsStillHeld = false;
                    }

                    if (axe.isPressed && !(state.AxesState[axe.Axis] > Thresholds[axe.Axis]) && axe.isSpammable)
                    {
                        spammableInputsToDeactivate.Add(axe);
                    }
                }
            }

            inputStates.Where(x => x.GetType() == typeof(ButtonState)).ToList().ForEach(x => x.isPressed = state.ButtonState[((ButtonState)x).Button]);
            inputStates.Where(x => x.GetType() == typeof(AxisState)).ToList().ForEach(x => x.isPressed = state.AxesState[((AxisState)x).Axis] > Thresholds[((AxisState)x).Axis]);

            KeyboardInputGenerator.KeyDown(inputStates.Where(x => x.isPressed && !x.wasTriggeredAndIsStillHeld).Select(x => x.Key).ToArray());
            KeyboardInputGenerator.KeyUp(inputStates.Where(x => x.isPressed && !x.isSpammable).Select(x => x.Key).ToArray());

            if (spammableInputsToDeactivate.Count > 0)
            {
                KeyboardInputGenerator.KeyUp(spammableInputsToDeactivate.Select(x => x.Key).ToArray());
                spammableInputsToDeactivate.ForEach(x => x.isPressed = false);
            }
        }

        private void UpdateUI(NeptuneControllerInputState state)
        {
            if (Application.Current == null)
                return;

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

        private void btn_ActivateDriver_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;
        }

        private void GenerateJson()
        {
            List<string> axisNames = new();
            List<string> axisValues = new();
            Dictionary<string, string> keyValuePairs = new();

            foreach (var axis in Enum.GetNames(typeof(NeptuneControllerAxis)))
            {
                axisNames.Add(axis);
            }

            foreach (var axis in Enum.GetValues(typeof(NeptuneControllerAxis)))
            {
                axisValues.Add(axis.ToString());
            }

            for (int i = 0; i < axisNames.Count; i++)
            {
                keyValuePairs.Add(axisNames[i], axisValues[i]);
            }

            var filename = "axis.json";
            var content = JsonConvert.SerializeObject(keyValuePairs, Formatting.Indented);
            File.WriteAllText(filename, content);
        }
    }
    public class ButtonState : State 
    {
        public ButtonState(NeptuneControllerButton button, VirtualKeyCode key, bool isSpammable = false)
        {
            this.Button = button;
            this.Key = key;
            this.isSpammable = isSpammable;
        }

        public NeptuneControllerButton Button { get; set; }
    }

    public class AxisState : State
    {
        public float activationThreshold;
        public float currentValue;
        public AxisState(NeptuneControllerAxis axis, VirtualKeyCode key, float activationThreshold, bool isSpammable = false)
        {
            this.Axis = axis;
            this.Key = key;
            this.activationThreshold = activationThreshold;
            this.isSpammable = isSpammable;
        }

        public NeptuneControllerAxis Axis { get; set; }
    }

    public abstract class State {
        public bool isSpammable;
        public bool wasTriggeredAndIsStillHeld;
        public bool isPressed;

        public VirtualKeyCode Key { get; set; }
    }
}
