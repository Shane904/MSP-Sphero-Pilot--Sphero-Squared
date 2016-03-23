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
using Windows.UI;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Sphero_Squared
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //Holds the number for the minimum pitch to move forward
        public const float MIN_MOVE_PITCH = 15;

        //Holds the number for the maximum pitch to move forward (treat anything above it as maximum)
        public const float MAX_MOVE_PITCH = 60;

        //Holds the number for the maximum speed the Sphero can go
        public float max_speed;

        //Holds the number for the minimum roll to move sideways
        public const float MIN_MOVE_ROLL = 8;

        //Holds the number for the maximum roll to move sideways (treat anything above it as maximum)
        public const float MAX_MOVE_ROLL = 50;

        //Holds the color for master when not moving
        public Color STOPPED_COLOR = Color.FromArgb(1, 0, 0, 0);

        //Holds the color for master when moving slowly
        public Color SLOW_COLOR = Color.FromArgb(1, 255, 0, 0);

        //Holds the color for master when moving medium speed
        public Color MEDIUM_COLOR = Color.FromArgb(1, 255, 255, 0);

        //Holds the color for master when moving fast
        public Color FAST_COLOR = Color.FromArgb(1, 0, 255, 0);

        //Holds the number for the maximum amount of degrees the Sphero can turn from an action
        public int max_turn;

        //Holds the direction for the follower
        public int direction = 0;

        //Holds the last speed the follower went
        public float last_speed = 0f;

        //Holds the master SpheroController
        public SpheroController master;
        //Holds the follower SpheroController
        public SpheroController follower;

        //Holds the list of Spheros that are paired with the computer
        private List<Robot> _robots = new List<Robot>();

      

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

            //Set max speed to initial slider value
            max_speed = Convert.ToSingle(sliderMaxSpeed.Value / 100);

            //Set max turn to initial slider value
            max_turn = Convert.ToInt16(sliderMaxTurn.Value);
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

        //When the toggle for stabilizing master Sphero is toggled
        private void toggleStabilizeMaster_Toggled(object sender, RoutedEventArgs e)
        {
            //Update master's stabilization if master is not null
            if(master != null)
            master.setStabilization(toggleStabilizeMaster.IsOn);
        }

        //When the ComboBox for selecting the master Sphero is changed
        private void comboMasterSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Make the panel with the controls for the master Sphero visible
            panelMaster.Visibility = Visibility.Visible;
            //Update label above the master Sphero selector
            textComboMaster.Text = "Master Sphero Selected";
        }

        //When the ComboBox for selecting the follower Sphero is changed
        private void comboFollowerSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Make the panel with the controls for the follower Sphero visible
            panelFollower.Visibility = Visibility.Visible;
            //Update label above the follower Sphero selector
            textComboFollower.Text = "Follower Sphero Selected";
        }

        //Handles when the Master sphero changes attitude (pitch and roll)
        public void handleMasterAttitude(float pitch, float roll)
        {
            //Create the speed variable (0-1)
            float speed = 0;

            //If pitch is < 0, set pitch to -pitch and reverse the direction
            if(pitch < 0)
            {
                pitch = -pitch;
                direction += 180;
            }

            //If the pitch is more than the minumum
            if(pitch > MIN_MOVE_PITCH)
            {
                //If the pitch is more than the maximum, set it to the maximum
                if(pitch > MAX_MOVE_PITCH)
                {
                    pitch = MAX_MOVE_PITCH;
                }

                //Calculate the speed
                speed = ((pitch - MIN_MOVE_PITCH) / (MAX_MOVE_PITCH - MIN_MOVE_PITCH) * max_speed); 
            }

            //If the absolute value of roll is more than the minimum
            if(Math.Abs(roll) > MIN_MOVE_ROLL)
            {
                //If the absolute value of roll is more than the maximum, set it to the maximum (same sign as original value)
                if(Math.Abs(roll) > MAX_MOVE_ROLL)
                {
                    roll = MAX_MOVE_ROLL * Math.Sign(roll);
                }

                //Calculate the roll
                direction += Convert.ToInt16((roll - MIN_MOVE_ROLL) / (MAX_MOVE_ROLL - MIN_MOVE_ROLL) * max_turn);
            }

            //The direction for the Roll has to be 0-359. Loop to make sure it is greater than 0
            while (direction < 0)
            {
                direction += 360;
            }

            //The direction for the Roll has to be 0-359. Loop to make sure it is less than 359
            while (direction > 359)
            {
                direction -= 360;
            }

            //If the follower Sphero is connected, roll it
            if (follower != null && follower.isConnected)
            {
                //Don't do this if the last speed was 0 and this speed was 0. It spams the follower and the follower gets overloaded
                if (!(last_speed == 0 && last_speed == speed))
                {
                    //Set last_speed to current speed
                    last_speed = speed;

                    Debug.WriteLine("Rolling follower: {Direction: " + direction + "; Speed: " + speed + "}");

                    //Roll the follower
                    follower.sphero.Roll(direction, speed);

                    //Set master color based on speed
                    if(speed > max_speed/3*2)
                    {
                        master.color = FAST_COLOR;
                    }
                    else if(speed > max_speed/3)
                    {
                        master.color = MEDIUM_COLOR;
                    }
                    else if(speed > 0)
                    {
                        master.color = SLOW_COLOR;
                    }
                    else
                    {
                        master.color = STOPPED_COLOR;
                    }

                }
            }
        }

        private void sliderMaxSpeed_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            max_speed = Convert.ToSingle(sliderMaxSpeed.Value / 100);
        }

        private void sliderMaxTurn_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            max_turn = Convert.ToInt16(sliderMaxTurn.Value);
        }
    }
}
