using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using neptune_hidapi.net;

namespace sd_drivers
{
    internal static class NeptuneInputInterpreter
    {
        public static Task Neptune_OnControllerInputReceived(NeptuneControllerInputEventArgs arg)
        {
            //foreach (var btn in arg.State.ButtonState.Buttons)
            //{
            //    Debug.WriteLine($"{btn}: {arg.State.ButtonState[btn]}      ");
            //}
            //foreach (var axis in arg.State.AxesState.Axes)
            //{
            //    Debug.WriteLine($"{axis}: {arg.State.AxesState[axis]}      ");
            //}
            Debug.WriteLine(((App)Application.Current).Neptune().isActive());
            Debug.WriteLine(arg.State.AxesState[NeptuneControllerAxis.R2]);

            if (arg.State.AxesState[NeptuneControllerAxis.R2] == 32767)
            {
                InputTools.MouseAction(MouseEventFlags.LeftDown);
            }

            return Task.CompletedTask;
        }
    }
}
