using System;
using System.Collections.Generic;
using Dapplo.Windows.Input.Keyboard;
using System.Runtime.InteropServices;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Structs;

namespace sd_drivers
{
    internal static class InputTools
    {
        public static void ButtonPress(List<ButtonState> buttonStates)
        {
            //List<VirtualKeyCode> keysDown = new();
            //List<VirtualKeyCode> keysUp = new();

            //foreach (var state in buttonStates)
            //{
            //    if (state.State == State.ToActivate)
            //    {
            //        keysDown.Add(state.Key);
            //    }
            //    else if (state.State == State.ToDeactivate)
            //    {
            //        keysUp.Add(state.Key);
            //    }
            //}

            //KeyboardInputGenerator.KeyDown(keysDown.ToArray());
            //KeyboardInputGenerator.KeyUp(keysUp.ToArray());
        }
    }
}