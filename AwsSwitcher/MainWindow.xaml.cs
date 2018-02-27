using System.Windows;
using Amazon.EC2;
using Amazon;
using System;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using Amazon.EC2.Model;
using System.IO;

namespace AwsSwitcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string CONFIG_FILE = "aws_config.xml";
        private DispatcherTimer dispatcherTimer = new DispatcherTimer();
        private List<Task> runningTasks = new List<Task>();
        private Dictionary<string, string> settings;
        private Amazon.EC2.Model.Instance targetInstance;

        public MainWindow()
        {
            InitializeComponent();
            settings = GetSettings(CONFIG_FILE);

            UpdateUI();

            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
            dispatcherTimer.Start();
        }
        void ShowHideDetails(object sender, RoutedEventArgs e)
        {
            for (var vis = sender as Visual; vis != null; vis = VisualTreeHelper.GetParent(vis) as Visual)
                if (vis is DataGridRow)
                {
                    var row = (DataGridRow)vis;
                    row.DetailsVisibility =
                    row.DetailsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                    break;
                }
        }

        void ToggleInstanceState(object sender, RoutedEventArgs e)
        {
            if (this.targetInstance == null)
            {
                Console.WriteLine("Error: Instance does not exist");
                return;
            }
            ActionButton.Content = "Please wait...";

            using (var awsClient = GetClient())
            { 
                if (targetInstance.State.Code > 64)
                {
                    StartInstancesRequest req = new Amazon.EC2.Model.StartInstancesRequest();
                    req.InstanceIds.Add(targetInstance.InstanceId);
                    awsClient.StartInstances(req);
                }
                else
                {
                    StopInstancesRequest req = new Amazon.EC2.Model.StopInstancesRequest();
                    req.InstanceIds.Add(targetInstance.InstanceId);
                    awsClient.StopInstances(req);
                }
            }
            UpdateUI();
        }

        /// <summary>
        /// This function will update the UI periodically to check if a button needs to be changed
        /// </summary>
        void UpdateUI()
        {
            dispatcherTimer.Start();
            bool isChangingState = false;
            Regex searchRegex = new Regex(@"^remote ((?:\d+\.?)+) (\d+)", RegexOptions.Multiline);
            using (var awsClient = GetClient())
            {
                var req = new Amazon.EC2.Model.DescribeInstancesRequest();
                req.InstanceIds.Add(settings["instance_id"]);
                var response = awsClient.DescribeInstances(req);
                targetInstance = response.Reservations[0].Instances[0];
                Instances.ItemsSource = new Amazon.EC2.Model.Instance[] { targetInstance };
                if ((new int[] { 0, 32, 64 }).Contains(targetInstance.State.Code))
                {
                    ActionButton.IsEnabled = false;
                    isChangingState = true;
                }
            }
            
            if (!isChangingState)
            {
                dispatcherTimer.Stop();
                ActionButton.IsEnabled = true;
                ActionButton.Content = GetInstanceAction(targetInstance);
                ConfigureVPN(targetInstance);
            }
        }

        private void ConfigureVPN(Instance targetInstance)
        {
            string text = File.ReadAllText(settings["vpn_config"]);
            text = Regex.Replace(text, @"^remote ((?:\d*\.?)+) 1194", String.Format("remote {0} 1194", targetInstance.PublicIpAddress ?? "0.0.0.0") ,RegexOptions.Multiline);
            File.WriteAllText(settings["vpn_config"], text);
        }

        private object GetInstanceAction(Instance targetInstance)
        {
            return targetInstance.State.Code > 64 ? "Start instance" : "Shut down instance";
        }

        AmazonEC2Client GetClient()
        {
            RegionEndpoint region = RegionEndpoint.APSoutheast2;

            return new AmazonEC2Client(settings["access_id"], settings["access_key"], region);
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {

            UpdateUI();
            return;
            //Check all current tasks to make sure all are complete.
            //for (int i = 0; i < runningTasks.Count; i++)
            //{
            //    Task item = runningTasks[i];
            //    if (item.IsCompleted)
            //    {
            //        runningTasks.Remove(item);
            //    }
            //    else
            //    {
            //        //One of the tasks has not yet finished, thus we should skip this update round.
            //        return;
            //    }
            //}
        }

        public Dictionary<string, string> GetSettings(string path)
        {

            var document = XDocument.Load(path);

            var root = document.Root;
            var results =
              root
                .Elements()
                .ToDictionary(element => element.Name.ToString(), element => element.Value);

            return results;

        }

        private void InstanceButton_Click(object sender, RoutedEventArgs e)
        {

            
        }

        private void VpnButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }

}
