using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using RobotKit;
using Windows.UI.Popups;
using Windows.UI;
using Microsoft.ApplicationInsights;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Sphero_Squared
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //Holds the number for the minimum pitch to move
        public const float MIN_MOVE_PITCH = 10;

        //Holds the number for the maximum pitch to move (treat anything above it as maximum)
        public const float MAX_MOVE_PITCH = 60;

        //Holds the number for the maximum speed the Sphero can go
        public float max_speed;

        //Holds the number for the minimum roll to move
        public const float MIN_MOVE_ROLL = 10;

        //Holds the number for the maximum roll to move (treat anything above it as maximum)
        public const float MAX_MOVE_ROLL = 60;

        //Holds the maximum possible value of r (treat anything above it as maximum)
        public const float MAX_MOVE_R = 60;

        //Holds the color for master when not moving
        public Color STOPPED_COLOR = Color.FromArgb(1, 0, 0, 0);

        //Holds the color for master when moving slowly
        public Color SLOW_COLOR = Color.FromArgb(1, 255, 0, 0);

        //Holds the color for master when moving medium speed
        public Color MEDIUM_COLOR = Color.FromArgb(1, 255, 255, 0);

        //Holds the color for master when moving fast
        public Color FAST_COLOR = Color.FromArgb(1, 0, 255, 0);

        //Holds the direction for the follower
        public int direction = 0;

        //Holds the last speed the follower went
        public float last_speed = 0f;

        //The Azure Telemetry Client
        private TelemetryClient tc = new TelemetryClient();

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

            float r = 0;

            float x = 0;
            float y = 0;

            if(Math.Abs(pitch) >= MIN_MOVE_PITCH)
            {
                y = pitch - MIN_MOVE_PITCH;
            }

            if(Math.Abs(roll) >= MIN_MOVE_ROLL)
            {
                x = roll - MIN_MOVE_ROLL;
            }


            //Use Pythagorean Theorem to convert to distance (we'll use it to calculate speed)
            r = Convert.ToSingle(Math.Sqrt(y*y + x*x));

            if(r > MAX_MOVE_R)
            {
                r = MAX_MOVE_R;
            }

            //Calculate speed
            speed = (r / MAX_MOVE_R * max_speed);



            //Use inverse tangent to calculate theta (we'll use it for direction)
            if(x > 0)
            {
                direction = Convert.ToInt16(Math.Atan(y / x) * 180/Math.PI);
            }
            else if(x < 0)
            {
                direction = Convert.ToInt16(Math.Atan(y / x) * 180 / Math.PI) - 180;
            }
            else
            {
                if(y > 0)
                {
                    direction = 90;
                }
                else if(y < 0)
                {
                    direction = -90;
                }
                //In the case of y and x being 0, we'll just leave direction as it was since the Sphero won't be rolling.
            }

            //0 degrees for the Sphero means it moves forward from its tail light. So subtract 90 degrees from the direction we calculated.
            direction -= 90;

            

            


            tc.TrackTrace("{r: " + speed + ", theta: "+ direction +"}");



            //If the follower Sphero is connected, roll it
            if (follower != null && follower.isConnected)
            {
                //Don't do this if the last speed was 0 and this speed was 0. It spams the follower and the follower gets overloaded
                if (!(last_speed == 0 && last_speed == speed))
                {
                    //Set last_speed to current speed
                    last_speed = speed;

                    tc.TrackTrace("Rolling follower: {Direction: " + direction + "; Speed: " + speed + "}");

                    //Roll the follower
                    follower.roll(direction, speed);

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

        private void sliderCalibrate_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if(follower != null && follower.isConnected)
            {
                follower.roll(Convert.ToInt16(sliderCalibrate.Value), 0, true);

                follower.calibrate(Convert.ToInt16(sliderCalibrate.Value));

                tc.TrackTrace("Set heading to " + sliderCalibrate.Value);
            }
        }

        private void sliderCalibrate_PointerEntered(object sender, PointerRoutedEventArgs e)
        { 
            tc.TrackTrace("Calibration slider pressed, turn on back LED");
            if (follower != null && follower.isConnected)
            {
                follower.sphero.SetBackLED(1);
            }
        }

        private void sliderCalibrate_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            tc.TrackTrace("Calibration slider released, turn off back LED");
            if (follower != null && follower.isConnected)
            {
                follower.sphero.SetBackLED(0);
            }
        }
    }
}
