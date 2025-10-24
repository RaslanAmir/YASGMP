using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.AppCore.Services.Ai;

namespace YasGMP.Wpf.ViewModels
{
    public sealed partial class AiAssistantDialogViewModel : ObservableObject
    {
        private readonly IAiAssistantService _assistant;

        [ObservableProperty]
        private string _prompt = string.Empty;

        [ObservableProperty]
        private string _response = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        public ObservableCollection<string> History { get; } = new();

        public IAsyncRelayCommand SendCommand { get; }

        public AiAssistantDialogViewModel(IAiAssistantService assistant)
        {
            _assistant = assistant ?? throw new ArgumentNullException(nameof(assistant));
            SendCommand = new AsyncRelayCommand(SendAsync, CanSend);
        }

        private bool CanSend() => !IsBusy && !string.IsNullOrWhiteSpace(Prompt);

        private async Task SendAsync()
        {
            if (!CanSend()) return;
            IsBusy = true;
            try
            {
                History.Add($"> {Prompt}");
                var result = await _assistant.ChatAsync(Prompt, systemPrompt: "You are YasGMP's built-in assistant. Keep answers concise and actionable.");
                Response = result;
                History.Add(result);
                Prompt = string.Empty;
            }
            catch (Exception ex)
            {
                Response = $"AI error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                (SendCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            }
        }
    }
}

