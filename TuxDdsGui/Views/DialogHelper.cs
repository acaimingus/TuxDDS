using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;

namespace TuxDdsGui.Views;

public static class DialogHelper
{
    public static Task ShowErrorDialog(Window parentWindow, string title, string message)
    {
        var okButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 15, 0, 0)
        };
        
        var dialog = new Window
        {
            Title = title,
            Width = 500,
            Height = 250,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = true,
            Topmost = true,
            Content = new Grid
            {
                RowDefinitions = new RowDefinitions("*,Auto"),
                Margin = new Avalonia.Thickness(15),
                Children =
                {
                    new ScrollViewer
                    {
                        Content = new SelectableTextBlock
                        {
                            Text = message,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap
                        }
                    },
                    okButton
                }
            }
        };
        
        var grid = (Grid)dialog.Content;
        Grid.SetRow(grid.Children[0], 0);
        Grid.SetRow(grid.Children[1], 1);
        
        okButton.Click += (sender, args) => dialog.Close();
        
        return dialog.ShowDialog(parentWindow);
    }
}