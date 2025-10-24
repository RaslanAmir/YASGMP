using System;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace YasGMP.Wpf.Helpers
{
    /// <summary>
    /// Helper to safely notify command CanExecute changes on the UI thread.
    /// </summary>
    public static class UiCommandHelper
    {
        public static void NotifyCanExecuteOnUi(ICommand? command)
        {
            if (command is null) return;

            void Invoke()
            {
                switch (command)
                {
                    case IAsyncRelayCommand asyncRelay:
                        asyncRelay.NotifyCanExecuteChanged();
                        break;
                    case IRelayCommand relay:
                        relay.NotifyCanExecuteChanged();
                        break;
                }
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                Invoke();
            }
            else
            {
                dispatcher.BeginInvoke(new Action(Invoke));
            }
        }

        public static void NotifyManyOnUi(params ICommand?[] commands)
        {
            if (commands == null || commands.Length == 0) return;
            foreach (var cmd in commands)
            {
                NotifyCanExecuteOnUi(cmd);
            }
        }
    }
}
