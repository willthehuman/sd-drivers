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
    public partial class MainWindow
    {
        private readonly TaskbarIcon _tbi = new();
        private readonly NeptuneController _neptune = new();

        private readonly List<State> _inputStates = new();

        private static Dictionary<NeptuneControllerButton, VirtualKeyCode> _buttonsToKeyCodes = new();
        private static Dictionary<NeptuneControllerAxis, Tuple<VirtualKeyCode, VirtualKeyCode>> _axisToKeyCodes = new();

        private static List<NeptuneControllerButton> _spammableButtons = new();
        private static List<NeptuneControllerAxis> _spammableAxis = new();
        private static Dictionary<NeptuneControllerAxis, float> _thresholds = new();

        public MainWindow()
        {
            InitializeComponent();
            SetTaskbarIcon();

            //GenerateJsonFromEnum();
            //GenerateJsonFromDict();

            InitUi();
            InitDictionary();
            InitSpammables();
            InitThresholds();

            InitButtons();
            InitAxis();

            _neptune.OnControllerInputReceived += Neptune_OnControllerInputReceived;
            _neptune.LizardButtonsEnabled = false;
            _neptune.LizardMouseEnabled = true; //Keep the trackpad as a real mouse
        }

        private static void InitDictionary()
        {
            //buttons
            _buttonsToKeyCodes.Clear();
            var fileName = "configs/config.json";
            var jsonString = File.ReadAllText(fileName);
            _buttonsToKeyCodes = JsonConvert.DeserializeObject<Dictionary<NeptuneControllerButton, VirtualKeyCode>>(jsonString)!;

            //axis (analog inputs like joysticks and triggers)
            _axisToKeyCodes.Clear();
            fileName = "configs/config_axis.json";
            jsonString = File.ReadAllText(fileName);
            _axisToKeyCodes = JsonConvert.DeserializeObject<Dictionary<NeptuneControllerAxis, Tuple<VirtualKeyCode, VirtualKeyCode> >>(jsonString)!;
        }

        private static void InitSpammables()
        {
            _spammableButtons.Clear();
            var fileName = "configs/spammables.json";
            var jsonString = File.ReadAllText(fileName);
            _spammableButtons = JsonConvert.DeserializeObject<List<NeptuneControllerButton>>(jsonString)!;

            _spammableAxis.Clear();
            fileName = "configs/spammable_axis.json";
            jsonString = File.ReadAllText(fileName);
            _spammableAxis = JsonConvert.DeserializeObject<List<NeptuneControllerAxis>>(jsonString)!;
        }

        private static void InitThresholds()
        {
            _thresholds.Clear();
            const string fileName = "configs/thresholds.json";
            var jsonString = File.ReadAllText(fileName);
            _thresholds = JsonConvert.DeserializeObject<Dictionary<NeptuneControllerAxis, float>>(jsonString)!;
        }

        private void InitButtons()
        {
            foreach (NeptuneControllerButton button in Enum.GetValues(typeof(NeptuneControllerButton)))
            {
                if (_buttonsToKeyCodes.ContainsKey(button))
                    _inputStates.Add(new ButtonState(button, _buttonsToKeyCodes[button], _spammableButtons.Contains(button)));
            }
        }

        private void InitAxis()
        {
            foreach (NeptuneControllerAxis axe in Enum.GetValues(typeof(NeptuneControllerAxis)))
            {
                if (_axisToKeyCodes.ContainsKey(axe))
                    _inputStates.Add(new AxisState(axe, _axisToKeyCodes[axe], _spammableAxis.Contains(axe)));
            }
        }

        private void InitUi()
        {
            var collection = DeckCanvas.Children.OfType<Ellipse>().ToList();
            collection.ForEach(x => x.Visibility = Visibility.Hidden);
        }

        private Task Neptune_OnControllerInputReceived(NeptuneControllerInputEventArgs arg)
        {
            TranslateInputs(arg.State);
            UpdateUi(arg.State);
            return Task.CompletedTask;
        }

        private void TranslateInputs(NeptuneControllerInputState state)
        {
            var spammableInputsToDeactivate = GenerateSpammableInputs(state);
            _inputStates.ForEach(x => x.UpdateState(state));

            KeyboardInputGenerator.KeyDown(_inputStates.Where(x => x.IsPressed && !x.WasTriggeredAndIsStillHeld).Select(x => x.Key).ToArray());
            KeyboardInputGenerator.KeyUp(_inputStates.Where(x => x.IsPressed && !x.IsSpammable).Select(x => x.Key).ToArray());

            if (spammableInputsToDeactivate.Count <= 0) return;
            KeyboardInputGenerator.KeyUp(spammableInputsToDeactivate.Select(x => x.Key).ToArray());

            foreach(var input in spammableInputsToDeactivate.Where(x => x.GetType() == typeof(AxisState)).Cast<AxisState>())
            {
                _inputStates.Single(x => x.GetType() == typeof(AxisState) && ((AxisState)x).Axis == input.Axis).IsPressed = false;
                if (input.NegativeKey != VirtualKeyCode.None)
                    _inputStates.Single(x => x.GetType() == typeof(AxisState) && ((AxisState)x).Axis == input.Axis).Key = VirtualKeyCode.None;
            }
            foreach (var input in spammableInputsToDeactivate.Where(x => x.GetType() == typeof(ButtonState)).Cast<ButtonState>())
            {
                _inputStates.Single(x => x.GetType() == typeof(ButtonState) && ((ButtonState)x).Button == input.Button).IsPressed = false;
            }
        }

        private List<State> GenerateSpammableInputs(NeptuneControllerInputState state)
        {
            List<State> spammableInputsToDeactivate = new();

            foreach (var input in _inputStates)
            {
                switch (input)
                {
                    case ButtonState buttonState:
                        {
                            if (buttonState.IsPressed && state.ButtonState[buttonState.Button] && !buttonState.IsSpammable)
                            {
                                buttonState.WasTriggeredAndIsStillHeld = true;
                            }

                            if (buttonState.IsPressed && !state.ButtonState[buttonState.Button] && !buttonState.IsSpammable)
                            {
                                buttonState.WasTriggeredAndIsStillHeld = false;
                            }

                            if (buttonState.IsPressed && !state.ButtonState[buttonState.Button] && buttonState.IsSpammable)
                            {
                                spammableInputsToDeactivate.Add(buttonState);
                            }

                            break;
                        }
                    case AxisState axisState:
                        {
                            if (axisState.IsPressed && Math.Abs(state.AxesState[axisState.Axis]) >= _thresholds[axisState.Axis] && !axisState.IsSpammable)
                            {
                                axisState.WasTriggeredAndIsStillHeld = true;
                            }

                            if (axisState.IsPressed && !(Math.Abs(state.AxesState[axisState.Axis]) >= _thresholds[axisState.Axis]) && !axisState.IsSpammable)
                            {
                                axisState.WasTriggeredAndIsStillHeld = false;
                            }

                            if (axisState.IsPressed && !(Math.Abs(state.AxesState[axisState.Axis]) >= _thresholds[axisState.Axis]) && axisState.IsSpammable)
                            {
                                spammableInputsToDeactivate.Add(axisState);
                            }

                            break;
                        }
                }
            }

            return spammableInputsToDeactivate;
        }

        private void UpdateUi(NeptuneControllerInputState state)
        {
            if (Application.Current == null)
                return;

            Application.Current.Dispatcher.Invoke(delegate
            {
                btn_a.Visibility = state.ButtonState[NeptuneControllerButton.BtnA] ? Visibility.Visible : Visibility.Hidden;
                btn_b.Visibility = state.ButtonState[NeptuneControllerButton.BtnB] ? Visibility.Visible : Visibility.Hidden;
                btn_x.Visibility = state.ButtonState[NeptuneControllerButton.BtnX] ? Visibility.Visible : Visibility.Hidden;
                btn_y.Visibility = state.ButtonState[NeptuneControllerButton.BtnY] ? Visibility.Visible : Visibility.Hidden;
            });
        }

        private void SetTaskbarIcon()
        {
            var icon = _neptune.isActive() ? "content\\on.ico" : "content\\off.ico";
            _tbi.Icon = new System.Drawing.Icon(icon, new System.Drawing.Size(96, 96));
            _tbi.ToolTipText = _neptune.isActive() ? "Active" : "Inactive";
        }

        private void btn_ActivateDriver_Click(object sender, RoutedEventArgs e)
        {
            var button = (System.Windows.Controls.Button)sender;

            if (_neptune.isActive())
            {
                _neptune.Close();
                InitUi();
                button.Content = "Activate Driver";
            }
            else
            {
                _neptune.Open();
                button.Content = "Deactivate Driver";
            }

            SetTaskbarIcon();
        }

        private void btn_ActivateDriver_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;
        }

        private static void GenerateJsonFromEnum()
        {
            Dictionary<string, string> keyValuePairs = new();

            var axisNames = Enum.GetNames(typeof(NeptuneControllerAxis)).ToList();
            var axisValues = (from object? axis in Enum.GetValues(typeof(NeptuneControllerAxis)) select axis.ToString()).ToList();

            for (var i = 0; i < axisNames.Count; i++)
            {
                keyValuePairs.Add(axisNames[i], axisValues[i] ?? string.Empty);
            }

            const string filename = "axis.json";
            var content = JsonConvert.SerializeObject(keyValuePairs, Formatting.Indented);
            File.WriteAllText(filename, content);
        }
        private static void GenerateJsonFromDict()
        {
            _axisToKeyCodes.Add(NeptuneControllerAxis.LeftStickX, new Tuple<VirtualKeyCode, VirtualKeyCode>(VirtualKeyCode.None, VirtualKeyCode.Numpad0));
            const string filename = "AxisToKeyCodes.json";
            var content = JsonConvert.SerializeObject(_axisToKeyCodes, Formatting.Indented);
            File.WriteAllText(filename, content);
        }
    }
    public class ButtonState : State 
    {
        public ButtonState(NeptuneControllerButton button, VirtualKeyCode key, bool isSpammable = false)
        {
            Button = button;
            Key = key;
            IsSpammable = isSpammable;
        }

        public NeptuneControllerButton Button { get; }

        public override void UpdateState(NeptuneControllerInputState state)
        {
            IsPressed = state.ButtonState[Button];
        }
    }

    public class AxisState : State
    {
        private readonly float threshold;
        public AxisState(NeptuneControllerAxis axis, Tuple<VirtualKeyCode, VirtualKeyCode> keys, float threshold, bool isSpammable = false)
        {
            Axis = axis;
            PositiveKey = keys.Item1;
            NegativeKey = keys.Item2;
            IsSpammable = isSpammable;
            this.threshold = threshold;
        }

        public NeptuneControllerAxis Axis { get; }
        public VirtualKeyCode PositiveKey { get; }
        public VirtualKeyCode NegativeKey { get; }

        public override void UpdateState(NeptuneControllerInputState state)
        {
            IsPressed = Math.Abs(state.AxesState[Axis]) >= threshold;

            if(IsPressed && NegativeKey != VirtualKeyCode.None)
            {
                if (Math.Sign(state.AxesState[Axis]) == -1)
                {
                    Key = NegativeKey;
                }
                else if (Math.Sign(state.AxesState[Axis]) == 1)
                {
                    Key = PositiveKey;
                }
            }
        }
    }

    public abstract class State {
        public bool IsSpammable;
        public bool WasTriggeredAndIsStillHeld;
        public bool IsPressed;

        public VirtualKeyCode Key { get; set; }
        public abstract void UpdateState(NeptuneControllerInputState state);
    }
}
