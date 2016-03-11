using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RobotKit;
using RobotKit.Internal;
using System.Diagnostics;

namespace Sphero_Squared
{
    public class SpheroController : EventArgs
    {
        private Sphero _sphero;
        private MainPage _mainPage;

        private bool _isMaster;

        private GyrometerReading _gyrometerReading;


        public Sphero sphero
        {
            get
            {
                return _sphero;
            }
        }

        public string name
        {
            get
            {
                return _sphero.Name;
            }
        }

        public string bluetoothName
        {
            get
            {
                if (_sphero != null)
                {
                    return _sphero.BluetoothName;
                }
                else
                {
                    return null;
                }
            }
        }

        public int direction
        {
            set
            {
                if(_sphero != null)
                sphero.Roll(value, 0);
            }
        }

        public SpheroController(MainPage mainPage, bool isMaster)
        {
            _mainPage = mainPage;
            _isMaster = isMaster;
        }

        public void connectSphero()
        {
            RobotProvider provider = RobotProvider.GetSharedProvider();
            provider.ConnectedRobotEvent += _onSpheroConnected;
            provider.DiscoveredRobotEvent += _onSpheroDiscovered;
            provider.NoRobotsEvent += _noSpheroDiscovered;
            provider.FindRobots();
        }

        private void _onSpheroDiscovered(object sender, Robot discoveredSphero)
        {
            if (_sphero == null)
            {
                Debug.WriteLine("Discovered " + (_isMaster ? "Master" : "Follower") + " Sphero: " + discoveredSphero.BluetoothName);
                //if (!_mainPage.isAlreadyConnected(discoveredSphero.BluetoothName))
                if((_isMaster && discoveredSphero.BluetoothName == "Sphero-WRB") || (!_isMaster && discoveredSphero.BluetoothName == "Sphero-YRP"))
                {
                    Debug.WriteLine("Going to connect to  " + (_isMaster ? "Master" : "Follower") + " Sphero: " + discoveredSphero.BluetoothName);
                    RobotProvider provider = RobotProvider.GetSharedProvider();


                    provider.DiscoveredRobotEvent -= _onSpheroDiscovered;
                    provider.NoRobotsEvent -= _noSpheroDiscovered;

                    provider.ConnectRobot(discoveredSphero);

                    Debug.WriteLine("Connecting to  " + (_isMaster ? "Master" : "Follower") + " Sphero: " + discoveredSphero.BluetoothName);
                }
                else
                {
                    Debug.WriteLine("Cannot connect as " + (_isMaster ? "Master" : "Follower") + " Sphero. Already connected: " + discoveredSphero.BluetoothName);
                }
            }
        } 

        private void _noSpheroDiscovered(object sender, EventArgs e)
        {
            _mainPage.spheroConnectFailed(_isMaster);
            _sphero = null;
        }

        private void _onSpheroConnected(object sender, Robot connectedSphero)
        {
            RobotProvider provider = RobotProvider.GetSharedProvider();
            provider.ConnectedRobotEvent -= _onSpheroConnected;

            _sphero = (Sphero)connectedSphero;
            _mainPage.spheroConnected(_isMaster, name);

            _sphero.SetBackLED(1);

            if (_isMaster)
            {
              //  setStabilization(false);
            }

                _sphero.SensorControl.GyrometerUpdatedEvent += _sensorControl_GyrometerUpdated;


            Debug.WriteLine("Connected to " + (_isMaster ? "Master" : "Follower") + " Sphero: " + connectedSphero.BluetoothName);
        }

        private void _sensorControl_GyrometerUpdated(object sender, GyrometerReading gyrometerReading)
        {
            _gyrometerReading = gyrometerReading;
            string readingText = String.Format(bluetoothName + " Gyro Reading: ({0}, {1}, {2})", _gyrometerReading.X, _gyrometerReading.Y, _gyrometerReading.Z);
            

            if (_isMaster)
            {
                _mainPage.updateGyroPosition(_gyrometerReading.X, _gyrometerReading.Y, _gyrometerReading.Z);
            }
        }

        public void disconnect()
        {
            if (_sphero != null)
            {
                setStabilization(true);
                _sphero.SensorControl.StopAll();
                _sphero.Sleep();
                _sphero = null;
            }
        }

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

            //Write the message to send to the Sphero. The first 0x02 points to the motor. The second 0x02 points to stabilization.
            //Values determined from the code of Sphero's SDK for other platforms.
            DeviceMessage msg = new DeviceMessage(0x02, 0x02, param);

            //Send the message to the Sphero.
            _sphero.WriteToRobot(msg);
        }
        

    }
}
