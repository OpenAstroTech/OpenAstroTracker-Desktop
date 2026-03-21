using System;
using Avalonia.Threading;

namespace OATCommunications.Avalonia
{
    public static class AvaloniaUtilities
    {
        public static void RunOnUiThread(Action action)
        {
            if (Dispatcher.UIThread.CheckAccess())
                action();
            else
                Dispatcher.UIThread.Post(action);
        }
    }
}
