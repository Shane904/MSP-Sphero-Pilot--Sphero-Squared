using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RobotKit;

namespace Sphero_Squared
{
    class ComboboxRobot
    {
        //Holds the name of the robot
        public string name { get; set; }

        //Holds the Robot
        public Robot value { get; set; }

        //Constructor
        public ComboboxRobot(Robot r)
        {
            value = r;
            name = r.BluetoothName;
        }

        //Returns the name of the robot when ToString() is called
        public override string ToString()
        {
            return name;
        }
    }
}
