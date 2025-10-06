using CommunityToolkit.Mvvm.ComponentModel;

namespace YasGMP.Wpf.ViewModels
{
    /// <summary>
    /// Base class for all dockable content hosted in AvalonDock.
    /// Provides common metadata used during layout serialization.
    /// </summary>
    public abstract partial class DockItemViewModel : ObservableObject
    {
        private string _title = string.Empty;
        private string _contentId = string.Empty;
        private string _automationId = string.Empty;

        /// <summary>Gets or sets the tab/header title.</summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>Unique identifier persisted in the serialized layout.</summary>
        public string ContentId
        {
            get => _contentId;
            set
            {
                if (SetProperty(ref _contentId, value) && string.IsNullOrEmpty(AutomationId))
                {
                    AutomationId = value;
                }
            }
        }

        /// <summary>Automation identifier exposed to accessibility tooling.</summary>
        public string AutomationId
        {
            get => _automationId;
            set => SetProperty(ref _automationId, value);
        }
    }

    /// <summary>Document pane item (tab).</summary>
    public abstract class DocumentViewModel : DockItemViewModel
    {
    }

    /// <summary>Anchorable pane item (tool window such as module tree/cockpit).</summary>
    public abstract class AnchorableViewModel : DockItemViewModel
    {
    }
}
