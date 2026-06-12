using System.Windows;
using CartaPsicometrica.ViewModels;

namespace CartaPsicometrica.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}
