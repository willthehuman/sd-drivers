using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using neptune_hidapi.net;

namespace sd_drivers
{
    internal static class NeptuneInputInterpreter
    {
        public static Task Neptune_OnControllerInputReceived(NeptuneControllerInputEventArgs arg)
        {
            foreach (var btn in arg.State.ButtonState.Buttons)
            {
                Debug.WriteLine($"{btn}: {arg.State.ButtonState[btn]}      ");
            }
            foreach (var axis in arg.State.AxesState.Axes)
            {
                Debug.WriteLine($"{axis}: {arg.State.AxesState[axis]}      ");
            }
            return Task.CompletedTask;
        }
    }
}
