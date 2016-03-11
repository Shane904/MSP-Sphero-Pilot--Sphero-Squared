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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Sphero_Squared
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        public SpheroController master;
        public SpheroController follower;

        private float _defaultX = 0;
        private float _defaultY = 0;
        private float _defaultZ = 0;

        private float _currentX = 0;
        private float _currentY = 0;
        private float _currentZ = 0;

        private float _offsetX = 0;
        private float _offsetY = 0;
        private float _offsetZ = 0;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void toggleSpheroMaster_Toggled(object sender, RoutedEventArgs e)
        {
            if (toggleSpheroMaster.IsOn)
            {
                master = new SpheroController(this, true);
                master.connectSphero();
                toggleSpheroMaster.Header = "Connecting";
            }
            else
            {
                master.disconnect();
                master = null;
                toggleSpheroMaster.Header = "Disconnected";
            }

        }

        private void toggleSpheroFollower_Toggled(object sender, RoutedEventArgs e)
        {
            if (toggleSpheroFollower.IsOn)
            {
                follower = new SpheroController(this, false);
                follower.connectSphero();
                toggleSpheroFollower.Header = "Connecting";
            }
            else
            {
                follower.disconnect();
                follower = null;
                toggleSpheroFollower.Header = "Disconnected";
            }

        }


        public bool isAlreadyConnected(string name)
        {
            return ((master != null && name == master.bluetoothName) || (follower != null && name == follower.bluetoothName));
        }

        public void spheroConnected(bool isMaster, string name)
        {
            string message = "Connected to " + name;
            if (isMaster)
            {
                toggleSpheroMaster.Header = message;
            }
            else
            {
                toggleSpheroFollower.Header = message;
            }
        }

        public void spheroConnectFailed(bool isMaster)
        {
            string message = "Failed to connect";
            ToggleSwitch t;
            if (isMaster)
            {
                t = toggleSpheroMaster;
            }
            else
            {
                t = toggleSpheroFollower;
            }
            t.Header = message;
            t.IsOn = false;
        }

        private void masterOrientationSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if(master != null)
            master.direction = (int)masterOrientationSlider.Value;
        }

        private void followerOrientationSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if(follower != null)
            follower.direction = (int)followerOrientationSlider.Value;
        }

        private void toggleStabilizeMaster_Toggled(object sender, RoutedEventArgs e)
        {
            if(master != null)
            master.setStabilization(toggleStabilizeMaster.IsOn);
        }

        public void updateGyroPosition(float x, float y, float z)
        {
            _currentX = x;
            _currentY = y;
            _currentZ = z;

            textMasterGyroX.Text = _currentX.ToString("0000.0");
            textMasterGyroY.Text = _currentY.ToString("0000.0");
            textMasterGyroZ.Text = _currentZ.ToString("0000.0");

            updateOffset();

        }

        public void updateOffset()
        {
            _offsetX += _currentX;
            _offsetY += _currentY;
            _offsetZ += _currentZ;

            textMasterOffsetX.Text = _offsetX.ToString("0000.0");
            textMasterOffsetY.Text = _offsetY.ToString("0000.0");
            textMasterOffsetZ.Text = _offsetZ.ToString("0000.0");
        }

        public void setDefaultGyroPosition()
        {
            _defaultX = _currentX;
            _defaultY = _currentY;
            _defaultZ = _currentZ;

            _offsetX = 0;
            _offsetY = 0;
            _offsetZ = 0;

            textMasterGyroDefaultX.Text = _defaultX.ToString("0000.0");
            textMasterGyroDefaultY.Text = _defaultY.ToString("0000.0");
            textMasterGyroDefaultZ.Text = _defaultZ.ToString("0000.0");
        }

        private void buttonResetMasterPosition_Click(object sender, RoutedEventArgs e)
        {
            setDefaultGyroPosition();
        }
    }
}
