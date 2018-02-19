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

namespace AwsSwitcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer dispatcherTimer = new DispatcherTimer();
        private List<Task> runningTasks = new List<Task>();
        public MainWindow()
        {
            InitializeComponent();

            UpdateUI();

            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 10);
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
            Console.WriteLine(sender);
            Amazon.EC2.Model.Instance x = (Amazon.EC2.Model.Instance)((Button)sender).DataContext;
            Console.WriteLine(x.PrivateIpAddress);

            using (var awsClient = GetClient())
            { 
                if (x.State.Code > 64)
                {
                    Amazon.EC2.Model.StartInstancesRequest req = new Amazon.EC2.Model.StartInstancesRequest();
                    req.InstanceIds.Add(x.InstanceId);
                    var y = awsClient.StartInstances(req);
                }
                else
                {
                    Amazon.EC2.Model.StopInstancesRequest req = new Amazon.EC2.Model.StopInstancesRequest();
                    req.InstanceIds.Add(x.InstanceId);
                    var y = awsClient.StopInstances(req);
                }
                //Amazon.EC2.Model.StartInstancesRequest req = new Amazon.EC2.Model.StartInstancesRequest();
                //req.InstanceIds.Add(instanceID);
                //var x = client.StartInstances(req);
            }
            UpdateUI();
        }

        /// <summary>
        /// This function will update the UI periodically to check if a button needs to be changed
        /// </summary>
        void UpdateUI()
        {
            string instanceID = "i-09fb8f217e449cf50";

            bool anyChangingState = false;
            Regex searchRegex = new Regex(@"^remote ((?:\d+\.?)+) (\d+)", RegexOptions.Multiline);
            using (var awsClient = GetClient())
            {
                var response = awsClient.DescribeInstances();
                var index = 0;
                foreach (var reservation in response.Reservations)
                {
                    Instances.DataContext = reservation.Instances;
                    foreach (var i in reservation.Instances)
                    {
                        if (i.InstanceId != instanceID)
                        {
                            continue;
                        }
                        if ((new int[] { 0, 32, 64 }).Contains(i.State.Code))
                        {
                            anyChangingState = true;
                        }
                        Console.WriteLine(i.PublicIpAddress);
                        //Somehow get each row's button reference, then change the text appropriately
                    }
                    index++;
                }
            }
            if (!anyChangingState)
            {
                dispatcherTimer.Stop();
            }
        }

        AmazonEC2Client GetClient()
        {
            var settings = GetSettings("aws_config.xml");
            RegionEndpoint region = RegionEndpoint.APSoutheast2;

            return new AmazonEC2Client(settings["access_id"], settings["access_key"], region);
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {

            UpdateUI();
            return;
            //Check all current tasks to make sure all are complete.
            for (int i = 0; i < runningTasks.Count; i++)
            {
                Task item = runningTasks[i];
                if (item.IsCompleted)
                {
                    runningTasks.Remove(item);
                }
                else
                {
                    //One of the tasks has not yet finished, thus we should skip this update round.
                    return;
                }
            }
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
    }

}
