using System;
using RobotKit;
using RobotKit.Internal;
using Windows.UI;
using Microsoft.ApplicationInsights;
using System.Diagnostics;

namespace Sphero_Squared
{
    public class SpheroController : EventArgs
    {
       
        //The Hz of refreshing sensor data
        public const int UPDATES_PER_SECOND = 4;

        //The Sphero Object
        private Sphero _sphero;

        //The MainPage that has this SpheroController
        private MainPage _mainPage;

        //If this SpheroController is the Master or Follower
        private bool _isMaster;

        //The last AccelerometerReading receieved
        private AccelerometerReading _accelerometerReading;

        //If SpheroController is connected via Bluetooth to a Sphero
        private bool _isConnected = false;

        //The Azure Telemetry Client
        private TelemetryClient tc = new TelemetryClient();

        //Roll (X)
        private float _roll = 0;
        //Pitch (Y)
        private float _pitch = 0;

        //Color of Sphero
        private Color _color = Color.FromArgb(0, 0, 0, 0);

        //Direction when pointing forward (add to direction when rolling)
        private int _calibrateOffset = 0;

        //Getter for _sphero
        public Sphero sphero
        {
            get
            {
                return _sphero;
            }
        }

        //Getter and setter for _color
        public Color color
        {
            get
            {
                return _color;
            }

            set
            {
                //Only update if new color, that way Sphero doesn't get spammed with commands
                if(!_color.Equals(value))
                {
                    _color = value;
                    sphero.SetRGBLED(_color.R, color.G, color.B);
                }
            }
        }

        //Getter for _isConnected
        public bool isConnected
        {
            get
            {
                return _isConnected;
            }
        }

        //Getter for sphero.BluetoothName
        public string bluetoothName
        {
            get
            {
                return sphero.BluetoothName;
            }
        }

        //Setter for sphero's direction
        public int direction
        {
            set
            {
                if(_sphero != null)
                sphero.Roll(value, 0);
            }
        }

        //Constructor. Requires the Robot intended to connect to, if it is the master, and the MainPage the SpheroController belongs to
        public SpheroController(Robot spheroToConnect, bool isMaster, MainPage mainPage)
        {
            _sphero = (Sphero)spheroToConnect;

            _isMaster = isMaster;

            _mainPage = mainPage;

            tc.TrackTrace("Created new " + (_isMaster ? "Master" : "Follower") + " SpheroController for: " + spheroToConnect.BluetoothName);
        }

        //Attempt to connect to the Sphero.
        public void connectSphero()
        {
            //Get the SharedProvider
            RobotProvider provider = RobotProvider.GetSharedProvider();

            //Add event for when the Sphero connects
            provider.ConnectedRobotEvent += _onSpheroConnected;

            //Try to connect to the Sphero
            provider.ConnectRobot(sphero);
        }
       
        //Triggers when ConnectedRobotEvent happens
        private void _onSpheroConnected(object sender, Robot connectedSphero)
        {
            tc.TrackTrace("About to connect to " + (_isMaster ? "Master" : "Follower") + " Sphero: " + connectedSphero.BluetoothName);
            //Only set everything if it is the correct Sphero
            if (connectedSphero.BluetoothName == sphero.BluetoothName)
            {
                //Get the SharedProvider
                RobotProvider provider = RobotProvider.GetSharedProvider();

                //Remove event for when the Sphero connects
                provider.ConnectedRobotEvent -= _onSpheroConnected;

                //Set _sphero to the newly connected Sphero
                _sphero = (Sphero)connectedSphero;

                //Call the _mainPage's spheroConnected
                _mainPage.spheroConnected(_isMaster, bluetoothName);


                //Turn off stabilization automatically and back LED on if master, otherwise turn stabilization on and back LED off. Allows for better experience while controlling
                if (_isMaster)
                {
                    setStabilization(false);
                    _sphero.SetBackLED(1);
                }
                else
                {
                    setStabilization(true);
                    _sphero.SetBackLED(0);
                }

                //Set updates per second
                _sphero.SensorControl.Hz = UPDATES_PER_SECOND;

                //Add event for when _sphero reports the Accelerometer has updated if master
                if (_isMaster)
                {
                    _sphero.SensorControl.AccelerometerUpdatedEvent += _sensorControl_AccelerometerUpdated;
                    tc.TrackTrace("Added AccelerometerUpdated Event for Master Sphero " + connectedSphero.BluetoothName);
                }
                //Add event for when _sphero reports a Collision with a wall if follower
                else
                {
                    _sphero.CollisionControl.StartDetectionForWallCollisions();
                    _sphero.CollisionControl.CollisionDetectedEvent += _collisionControl_CollisionDetected;
                    tc.TrackTrace("Added CollisionDetected Event for Follower Sphero " + connectedSphero.BluetoothName);
                }


                tc.TrackTrace("Connected to " + (_isMaster ? "Master" : "Follower") + " Sphero: " + connectedSphero.BluetoothName);

                //Set the _isConnected variable to true
                _isConnected = true;
            }
        }

        //Triggers when _sphero reports a collision with a wall
        private void _collisionControl_CollisionDetected(object sender, CollisionData collisionData)
        {
            _mainPage.collisionDetected();
        }

        //Triggers when _sphero reports the Accelerometer has updated
        private void _sensorControl_AccelerometerUpdated(object sender, AccelerometerReading accelerometerReading)
        {
            //Update the _accelerometerReading variable with the newest reading
            _accelerometerReading = accelerometerReading;

            //Calculate the attitude
            _calculateAttitude();
        }

        //Calculate the attitude (pitch, roll)
        private void _calculateAttitude()
        {            
            //Calculate the pitch from the _accelerometerReading
            _pitch = Convert.ToSingle(180 * Math.Atan(_accelerometerReading.Y / Math.Sqrt(_accelerometerReading.X * _accelerometerReading.X + _accelerometerReading.Z * _accelerometerReading.Z)) / Math.PI);

            //Calculate the roll from the _accelerometerReading
            _roll = -Convert.ToSingle(180 * Math.Atan(_accelerometerReading.X / Math.Sqrt(_accelerometerReading.Y * _accelerometerReading.Y + _accelerometerReading.Z * _accelerometerReading.Z)) / Math.PI);

            //If master Sphero, report the pitch and roll to the MainPage
            _mainPage.handleMasterAttitude(_pitch, _roll);
        }

        //Disconnect from the Sphero (if connected)
        public void disconnect()
        {
            //If _sphero exists AND is connected to
            if (_sphero != null && isConnected)
            {
                tc.TrackTrace("Disconnecting from " + bluetoothName);

                //Turn stabilization back on in the case it was turned off
                setStabilization(true);

                //Get the SharedProvider
                RobotProvider provider = RobotProvider.GetSharedProvider();

                //Remove event for when the Sphero connects
                provider.ConnectedRobotEvent -= _onSpheroConnected;

                //Remove event for when _sphero reports the Gyrometer has updated
                _sphero.SensorControl.AccelerometerUpdatedEvent -= _sensorControl_AccelerometerUpdated;

                //Remove event for when _sphero reports a collision
                _sphero.CollisionControl.CollisionDetectedEvent -= _collisionControl_CollisionDetected;

                //Tell _sphero to stop sending updates when the sensor is updated
                _sphero.SensorControl.StopAll();

                //Tell _sphero to stop sending updates on collision
                _sphero.CollisionControl.StopDetection();

                //Tell the _sphero to turn off
                _sphero.Sleep();

                //Set the _sphero to null
                _sphero = null;

                //Set _isConnected to false
                _isConnected = false;

            }
            else
            {
                tc.TrackTrace("Disconnect called, but Sphero doesn't exist or isn't connected.");
            }
        }

        //Turn stabilization on or off
        public void setStabilization(bool stabilize)
        {
            //Initialize paramByte. 0 means no stabilization (disable stabilization - lock motors)
            byte paramByte = (byte)0;

            //If stabilize is true, set paramByte to 1 (enable stabilization)
            if(stabilize)
            {
                paramByte = (byte)1;
            }

            //Parameters have to be passed as an array of bytes.
            Byte[] param = new Byte[] { paramByte };

            //Write the message to send to the Sphero. The first 0x02 points to the motor. The second 0x02 points to stabilization
            //Values determined from the code of Sphero's SDK for other platforms
            DeviceMessage msg = new DeviceMessage(0x02, 0x02, param);

            //Send the message to the Sphero
            _sphero.WriteToRobot(msg);
        }

        //Set the calibration offset
        public void calibrate(int offset)
        {
            _calibrateOffset = offset;
        }

        //Roll the Sphero and take the _calibrateOffset into account (unless otherwise told)
        public void roll(int direction, float speed, bool ignoreCalibrateOffset = false)
        {

            if (!ignoreCalibrateOffset)
            {
                direction += _calibrateOffset;
            }

            //If the direction is less than 0, convert it to not be less than 0
            while (direction < 0)
            {
                direction += 360;
            }

            //If the direction is greater than 359, convert it to not be greater than 359
            while (direction > 359)
            {
                direction -= 360;
            }

            sphero.Roll(direction, speed);
        }




    }
}
