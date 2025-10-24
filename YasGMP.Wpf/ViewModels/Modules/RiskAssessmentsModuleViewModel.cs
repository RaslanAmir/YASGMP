using System;
using YasGMP.Services.Interfaces;
using YasGMP.ViewModels;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// Lightweight module adapter that projects the shared <see cref="RiskAssessmentViewModel"/> into the WPF shell.
/// </summary>
public sealed class RiskAssessmentsModuleViewModel : ModuleDocumentViewModel
{
    /// <summary>Stable registry key consumed by the modules pane.</summary>
    public const string ModuleKey = "RiskAssessments";

    /// <summary>Initializes a new instance of the <see cref="RiskAssessmentsModuleViewModel"/> class.</summary>
    public RiskAssessmentsModuleViewModel(
        RiskAssessmentViewModel riskAssessments,
        ILocalizationService localization,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, localization.GetString("Module.Title.RiskAssessments"), localization, cflDialogService, shellInteraction, navigation)
    {
        RiskAssessments = riskAssessments ?? throw new ArgumentNullException(nameof(riskAssessments));
    }

    /// <summary>Gets the shared risk assessments view model surfaced for bindings.</summary>
    public RiskAssessmentViewModel RiskAssessments { get; }
}
