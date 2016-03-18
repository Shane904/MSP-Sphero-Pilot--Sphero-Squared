using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using RobotKit;
using System.Diagnostics;
using Windows.UI.Popups;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Sphero_Squared
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //Holds the master SpheroController
        public SpheroController master;
        //Holds the follower SpheroController
        public SpheroController follower;

        //Holds the list of Spheros that are paired with the computer
        private List<Robot> _robots = new List<Robot>();

        //Holds the X, Y, and Z that are considered as the origin for the master Sphero
        private float _defaultX = 0;
        private float _defaultY = 0;
        private float _defaultZ = 0;

        //Holds the current X, Y, and Z values for the master Sphero
        private float _currentX = 0;
        private float _currentY = 0;
        private float _currentZ = 0;

        //Holds the X, Y, and Z values for how far the master Sphero is tilted away from the origin
        private float _offsetX = 0;
        private float _offsetY = 0;
        private float _offsetZ = 0;

        //Constructor
        public MainPage()
        {
            this.InitializeComponent();

            //Hide the panel that holds the controls for the master
            panelMaster.Visibility = Visibility.Collapsed;
            //Hide the panel that holds the controls for the follower
            panelFollower.Visibility = Visibility.Collapsed;

            //Find the Spheros paired with the computer
            _findSpheros();
        }

        //Find the Spheros paired with the computer
        private void _findSpheros()
        {
            //Get the SharedProvider
            RobotProvider provider = RobotProvider.GetSharedProvider();
            //Add event for when a paired Sphero is found in the devices list
            provider.DiscoveredRobotEvent += _onSpheroDiscovered;
            //Looks for Spheros that are in the devices list
            provider.FindRobots();
        }

        //Called when a paired Sphero is found in the devices list
        private void _onSpheroDiscovered(object sender, Robot discoveredSphero)
        {
            //Adds discoveredSphero to the list of paired Spheros
            _robots.Add(discoveredSphero);

            //Creates a new ComboboxRobot
            ComboboxRobot item = new ComboboxRobot(discoveredSphero);

            //Adds item to the ComboBox for selecting the master Sphero
            comboMasterSelector.Items.Add(item);
            //Adds item to the ComboBox for selecting the follower Sphero
            comboFollowerSelector.Items.Add(item);
        }

        //Called when the master Sphero connection toggle is used
        private async void toggleSpheroMaster_Toggled(object sender, RoutedEventArgs e)
        {
            //If toggled on
            if (toggleSpheroMaster.IsOn)
            {
                //If follower Sphero connection toggle is also toggled on AND is connected to the Sphero that is going to be connected to
                if (toggleSpheroFollower.IsOn && comboMasterSelector.SelectedItem.ToString() == comboFollowerSelector.SelectedItem.ToString())
                {
                    //Turn master Sphero connection toggle off
                    toggleSpheroMaster.IsOn = false;
                    //Create error message
                    MessageDialog dialog = new MessageDialog("The Sphero you selected is already being used as the follower. Please select a different Sphero.", "Error!");
                    //Show error message
                    await dialog.ShowAsync();
                }
                //If the Sphero isn't in use
                else
                {
                    //Make a new SpheroController using the selected Sphero as the master
                    master = new SpheroController(((ComboboxRobot)comboMasterSelector.SelectedItem).value, true, this);
                    //Connect to the selected Sphero
                    master.connectSphero();
                    //Update the toggle's header
                    toggleSpheroMaster.Header = "Connecting";
                    //Disable the ComboBox for selecting master Sphero
                    comboMasterSelector.IsEnabled = false;
                }
            }
            //If toggled off
            else
            {
                //If master is not null
                if (master != null)
                {
                    //Disconnect from the Sphero and run some housekeeping
                    master.disconnect();
                    //Set master to null
                    master = null;
                    //Update the toggle's header
                   toggleSpheroMaster.Header = "Disconnected";
                }
                //Enable the ComboBox for selecting master Sphero
                comboMasterSelector.IsEnabled = true;
            }
        }

        //Called when the follower Sphero connection toggle is used
        private async void toggleSpheroFollower_Toggled(object sender, RoutedEventArgs e)
        {
            //If toggled on
            if (toggleSpheroFollower.IsOn)
            {
                //If master Sphero connection toggle is also toggled on AND is connected to the Sphero that is going to be connected to
                if (toggleSpheroMaster.IsOn && comboFollowerSelector.SelectedItem.ToString() == comboMasterSelector.SelectedItem.ToString())
                {
                    //Turn follower Sphero connection toggle off
                    toggleSpheroFollower.IsOn = false;
                    //Create error message
                    MessageDialog dialog = new MessageDialog("The Sphero you selected is already being used as the master. Please select a different Sphero.", "Error!");
                    //Show error message
                    await dialog.ShowAsync();
                }
                //If the Sphero isn't in use
                else
                {
                    //Make a new SpheroController using the selected Sphero as the follower
                    follower = new SpheroController(((ComboboxRobot)comboFollowerSelector.SelectedItem).value, false, this);
                    //Connect to the selected Sphero
                    follower.connectSphero();
                    //Update the toggle's header
                    toggleSpheroFollower.Header = "Connecting";
                    //Disable the ComboBox for selecting follower Sphero
                    comboFollowerSelector.IsEnabled = false;
                }
            }
            //If toggled off
            else
            {
                //If follower is not null
                if (follower != null)
                {
                    //Disconnect from the Sphero and run some housekeeping
                    follower.disconnect();
                    //Set follower to null
                    follower = null;
                    //Update the toggle's header
                    toggleSpheroFollower.Header = "Disconnected";
                }
                //Enable the ComboBox for selecting follower Sphero
                comboFollowerSelector.IsEnabled = true;
            }
        }

        //Updates the toggle's header for a Sphero
        public void spheroConnected(bool isMaster, string name)
        {
            //Set the message to use
            string message = "Connected to " + name;

            //If master Sphero
            if (isMaster)
            {
                //Update master Sphero connection toggle's header
                toggleSpheroMaster.Header = message;
            }
            //If follower Sphero
            else
            {
                //Update follower Sphero connection toggle's header
                toggleSpheroFollower.Header = message;
            }
        }

        //When the orientation slider for the master Sphero is changed
        private void masterOrientationSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            //Update master's direction if master is not null
            if(master != null)
            master.direction = (int)masterOrientationSlider.Value;
        }

        //When the orientation slider for the follower Sphero is changed
        private void followerOrientationSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            //Update follower's direction if follower is not null
            if (follower != null)
            follower.direction = (int)followerOrientationSlider.Value;
        }

        //When the toggle for stabilizing master Sphero is toggled
        private void toggleStabilizeMaster_Toggled(object sender, RoutedEventArgs e)
        {
            //Update master's stabilization if master is not null
            if(master != null)
            master.setStabilization(toggleStabilizeMaster.IsOn);
        }

        //Update the gyro position of master
        public void updateGyroPosition(float x, float y, float z)
        {
            //Set the current X, Y, and Z
            _currentX = x;
            _currentY = y;
            _currentZ = z;

            //Update the labels with 4 digits and one decimal point after
            textMasterGyroX.Text = _currentX.ToString("0000.0");
            textMasterGyroY.Text = _currentY.ToString("0000.0");
            textMasterGyroZ.Text = _currentZ.ToString("0000.0");

            //Update the offset
            updateOffset();

        }

        //Update the offset of master
        public void updateOffset()
        {
            //Add the current X, Y, and Z to the offsets of X, Y, and Z
            _offsetX += _currentX;
            _offsetY += _currentY;
            _offsetZ += _currentZ;

            //Update the labels with 4 digits and one decimal point after
            textMasterOffsetX.Text = _offsetX.ToString("0000.0");
            textMasterOffsetY.Text = _offsetY.ToString("0000.0");
            textMasterOffsetZ.Text = _offsetZ.ToString("0000.0");
        }

        //Set the "origin" for the master Sphero
        public void setDefaultGyroPosition()
        {
            //Set the origin X, Y, and Z to the current X, Y, and Z
            _defaultX = _currentX;
            _defaultY = _currentY;
            _defaultZ = _currentZ;

            //Set the offset X, Y, and Z to 0
            _offsetX = 0;
            _offsetY = 0;
            _offsetZ = 0;

            //Update the labels with 4 digits and one decimal point after
            textMasterGyroDefaultX.Text = _defaultX.ToString("0000.0");
            textMasterGyroDefaultY.Text = _defaultY.ToString("0000.0");
            textMasterGyroDefaultZ.Text = _defaultZ.ToString("0000.0");
        }

        //When the master's Reset Default Position button is clicked
        private void buttonResetMasterPosition_Click(object sender, RoutedEventArgs e)
        {
            //Set the "origin" for the master Sphero
            setDefaultGyroPosition();
        }

        //When the ComboBox for selecting the master Sphero is changed
        private void comboMasterSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            panelMaster.Visibility = Visibility.Visible;
            textComboMaster.Text = "Master Sphero Selected";
        }

        //When the ComboBox for selecting the follower Sphero is changed
        private void comboFollowerSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            panelFollower.Visibility = Visibility.Visible;
            textComboFollower.Text = "Follower Sphero Selected";
        }
    }
}
