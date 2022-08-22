using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Keyboard;
using Device.Net;
using Hardcodet.Wpf.TaskbarNotification;
using Hid.Net.Windows;
using hidapi;
using Microsoft.Extensions.Logging;
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

        private static HidDevice _hidDevice = new(10462, 4613, 64);
        private static bool _isSteamDeckDeviceDetected;

        public MainWindow()
        {
            InitializeComponent();

            SetTaskbarIcon();
            _tbi.LeftClickCommand = new ToggleDriverCommand(this);

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

        private static void CheckForDeck()
        {
            var loggerFactory = LoggerFactory.Create((builder) =>
            {
                _ = builder.SetMinimumLevel(LogLevel.Debug);
            });

            //Register the factory for creating Hid devices. 
            var hidFactory =
                new FilterDeviceDefinition()
                .CreateWindowsHidDeviceFactory(loggerFactory);

            var deviceDefinitions = (hidFactory.GetConnectedDeviceDefinitionsAsync().Result).ToList();
            _isSteamDeckDeviceDetected = deviceDefinitions.Any(x => x.VendorId == 10462 && x.ProductId == 4613);
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
                    _inputStates.Add(new AxisState(axe, _axisToKeyCodes[axe], _thresholds[axe], _spammableAxis.Contains(axe)));
            }
        }

        private void InitUi()
        {
            foreach (FrameworkElement el in DeckCanvas.Children)
            {
                if (el.Name.StartsWith("Btn"))
                {
                    el.Visibility = Visibility.Hidden;
                }
            }
        }

        private Task Neptune_OnControllerInputReceived(NeptuneControllerInputEventArgs arg)
        {
            TranslateInputs(arg.State);
            UpdateUi(arg.State);
            return Task.CompletedTask;
        }

        private void TranslateInputs(NeptuneControllerInputState state)
        {
            var spammableInputsToDeactivate = GenerateSpammableInputsToDeactivate(ref state);
            _inputStates.ForEach(x => x.UpdateState(ref state));

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

        private List<State> GenerateSpammableInputsToDeactivate(ref NeptuneControllerInputState state)
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
                foreach(FrameworkElement el in DeckCanvas.Children)
                {
                    if (el.Name.StartsWith("Btn"))
                    {
                        el.Visibility = state.ButtonState[(NeptuneControllerButton)Enum.Parse(typeof(NeptuneControllerButton), el.Name)] ? Visibility.Visible : Visibility.Hidden;
                    }
                }
            });
        }

        private void SetTaskbarIcon(bool showBalloon = false)
        {
            var icon = _neptune.isActive() ? "content\\on.ico" : "content\\off.ico";
            _tbi.Icon = new System.Drawing.Icon(icon, new System.Drawing.Size(96, 96));
            _tbi.ToolTipText = _neptune.isActive() ? "Active" : "Inactive";

            if (showBalloon)
                _tbi.ShowBalloonTip("sd-driver", "Driver is now " + _tbi.ToolTipText, BalloonIcon.Info);
        }

        public void btn_ActivateDriver_Click(object sender, RoutedEventArgs e)
        {
            var button = (System.Windows.Controls.Button)sender;

            if (!_isSteamDeckDeviceDetected)
                throw new Exception("This software only works on a Steam Deck!");

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

            SetTaskbarIcon(true);
        }

        private void btn_ActivateDriver_PreviewKeyDown(object sender, KeyEventArgs e)
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

        private void btn_HideWindow_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btn_HideWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
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

        public override void UpdateState(ref NeptuneControllerInputState state)
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

        public override void UpdateState(ref NeptuneControllerInputState state)
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
        public abstract void UpdateState(ref NeptuneControllerInputState state);
    }

    public class ToggleDriverCommand : ICommand
    {
        readonly MainWindow mainWindow;
        public ToggleDriverCommand(MainWindow mw) => mainWindow = mw;
        public void Execute(object parameter) => mainWindow.btn_ActivateDriver_Click(mainWindow.btn_ActivateDriver, null);

        public bool CanExecute(object parameter) => true;

        public event EventHandler? CanExecuteChanged;
    }
}
