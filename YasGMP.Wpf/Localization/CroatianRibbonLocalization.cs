using Fluent.Localization;
using Fluent.Localization.Languages;

namespace YasGMP.Wpf.Localization;

/// <summary>
/// Croatian localization for Fluent.Ribbon surface strings.
/// </summary>
[RibbonLocalization("hr-HR", "Hrvatski")]
public sealed class CroatianRibbonLocalization : RibbonLocalizationBase
{
    public CroatianRibbonLocalization()
        : base("hr-HR", "Hrvatski")
    {
    }

    public override string Automatic => "Automatski";
    public override string BackstageBackButtonUid => "Close Backstage";
    public override string BackstageButtonKeyTip => "D";
    public override string BackstageButtonText => "Datoteka";
    public override string CustomizeStatusBar => "Prilagodi statusnu traku";
    public override string MoreColors => "Više boja...";
    public override string NoColor => "Bez boje";
    public override string QuickAccessToolBarDropDownButtonTooltip => "Prilagodi alatnu traku za brzi pristup";
    public override string QuickAccessToolBarMenuHeader => "Prilagodi alatnu traku za brzi pristup";
    public override string QuickAccessToolBarMenuShowAbove => "Prikaži iznad vrpce";
    public override string QuickAccessToolBarMenuShowBelow => "Prikaži ispod vrpce";
    public override string QuickAccessToolBarMoreControlsButtonTooltip => "Više kontrola";
    public override string RibbonContextMenuAddGallery => "Dodaj galeriju na brzu traku";
    public override string RibbonContextMenuAddGroup => "Dodaj grupu na brzu traku";
    public override string RibbonContextMenuAddItem => "Dodaj na brzu traku";
    public override string RibbonContextMenuAddMenu => "Dodaj izbornik na brzu traku";
    public override string RibbonContextMenuCustomizeQuickAccessToolBar => "Prilagodi alatnu traku za brzi pristup...";
    public override string RibbonContextMenuCustomizeRibbon => "Prilagodi vrpcu...";
    public override string RibbonContextMenuMinimizeRibbon => "Smanji vrpcu";
    public override string RibbonContextMenuRemoveItem => "Ukloni s brze trake";
    public override string RibbonContextMenuShowAbove => "Prikaži brzu traku iznad vrpce";
    public override string RibbonContextMenuShowBelow => "Prikaži brzu traku ispod vrpce";
    public override string ShowRibbon => "Prikaži vrpcu";
    public override string ExpandRibbon => "Proširi vrpcu";
    public override string MinimizeRibbon => "Smanji vrpcu";
    public override string RibbonLayout => "Izgled vrpce";
    public override string UseClassicRibbon => "_Koristi klasičnu vrpcu";
    public override string UseSimplifiedRibbon => "_Koristi pojednostavljenu vrpcu";
    public override string DisplayOptionsButtonScreenTipTitle => "Opcije prikaza vrpce";
    public override string DisplayOptionsButtonScreenTipText => "Konfigurirajte opcije prikaza vrpce.";
    public override string ScreenTipDisableReasonHeader => "Ova naredba trenutno nije dostupna.";
    public override string ScreenTipF1LabelHeader => "Pritisnite F1 za pomoć";
}
