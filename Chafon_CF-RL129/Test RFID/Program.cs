using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Test_RFID
{
    class Program
    {

       

        static void Main(string[] args)
        {

            var api = new CF_RL129_API.API("COM2");
            api.Connect();
            var running = true;

            
            while (running)
            {
                var cmd = Console.ReadLine();

                switch (cmd)
                {
                    case "b":
                        api.Beep();
                        break;
                    case "r":
                        api.ReadTag();
                        break;
                    case "exit":
                        running = false;
                        break;
                }
            }

            api.Close();

        }

        

    }
}
