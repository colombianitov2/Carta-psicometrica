namespace CartaPsicometrica;

public static class AppEntry
{
    [System.STAThreadAttribute]
    public static void Main()
    {
        System.Windows.Application application = new();
        application.Run(new MainWindow());
    }
}
