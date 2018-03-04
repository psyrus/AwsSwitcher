using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AwsSwitcher
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow parentWindow = (MainWindow)Application.Current.MainWindow;            
            Dictionary<string, string> settings = new Dictionary<string, string>();

            settings["access_id"] = access_id.Text;
            settings["access_key"] = access_key.Text;
            settings["instance_id"] = instance_id.Text;
            settings["vpn_config"] = vpn_config.Text;

            parentWindow.SaveSettings(parentWindow.CONFIG_FILE, settings);
            this.Close();
        }

        internal void PopulateSettings(Dictionary<string, string> settings)
        {
            access_id.Text = settings["access_id"];
            access_key.Text = settings["access_key"];
            instance_id.Text = settings["instance_id"];
            vpn_config.Text = settings["vpn_config"];
        }
    }
}
