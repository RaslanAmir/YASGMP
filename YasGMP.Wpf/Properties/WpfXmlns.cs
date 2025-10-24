using System.Windows.Markup;

// Map the Xceed AvalonDock XML namespace to the Dirkster.AvalonDock CLR namespaces
// so XAML can use xmlns:ad="http://schemas.xceed.com/wpf/xaml/avalondock".
[assembly: XmlnsDefinition("http://schemas.xceed.com/wpf/xaml/avalondock", "AvalonDock")]
[assembly: XmlnsDefinition("http://schemas.xceed.com/wpf/xaml/avalondock", "AvalonDock.Layout")]
[assembly: XmlnsDefinition("http://schemas.xceed.com/wpf/xaml/avalondock", "AvalonDock.Controls")]

