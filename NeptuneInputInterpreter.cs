using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using neptune_hidapi.net;

namespace sd_drivers
{
    internal static class NeptuneInputInterpreter
    {
        public static async Task Neptune_OnControllerInputReceived(NeptuneControllerInputEventArgs arg)
        {
            foreach (var btn in arg.State.ButtonState.Buttons)
            {
                Console.WriteLine($"{btn}: {arg.State.ButtonState[btn]}      ");
            }
            foreach (var axis in arg.State.AxesState.Axes)
            {
                Console.WriteLine($"{axis}: {arg.State.AxesState[axis]}      ");
            }
        }
    }
}
