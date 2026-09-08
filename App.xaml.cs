using System.Configuration;
using System.Data;
using System.Windows;

namespace RecluseEdit
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            DispatcherUnhandledException += (s, e) =>
            {
                MessageBox.Show($"An unexpected error occurred:\n{e.Exception.Message}", "RecluseEdit Error", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            };
        }
    }

}
