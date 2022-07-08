using System.Windows;

namespace Agritite.Symlinker
{
    /// <summary>
    /// AboutWIndow.xaml 的互動邏輯
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void Ok_Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}