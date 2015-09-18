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

        private static SerialPort _port;
        private static int BAUD_RATE = 38400;
        private static bool _WatchoutRunning = false;
        private static string _LastKey = string.Empty;

        static void Main(string[] args)
        {

            _port = new SerialPort("COM3");
            _port.BaudRate = BAUD_RATE;
            _port.Open();
            var running = true;

            
            while (running)
            {
                var cmd = Console.ReadLine();

                switch (cmd)
                {
                    case "b":
                        Beep();
                        break;
                    case "r":
                        SendRead();
                        break;
                    case "exit":
                        running = false;
                        break;
                    case "start":
                        Thread thread = new Thread(Watchout);
                        _WatchoutRunning = true;
                        thread.Start();
                        break;
                    case "stop":
                        _WatchoutRunning = false;
                        
                        break;
                }
            }

            if (_port.IsOpen)
            {
                _port.Close();
            }
        }

        private static void ReadPort()
        {
            Thread.Sleep(100);
            var buffer = new byte[_port.BytesToRead];
            _port.Read(buffer, 0, buffer.Length);

            var value = HexBytesToHexString(buffer);

            ProcessRead(value);
        }

        private static void Beep()
        {
            SendCommand("AADD000401030A08");
            ReadPort();
        }

        static void Watchout()
        {
            while (_WatchoutRunning)
            {
                SendRead();
                Thread.Sleep(500);
            }
        }


        static void ProcessRead(string read)
        {
            if (string.IsNullOrWhiteSpace(read))
            {
                return;
            }

            var arr = SplitString(read);

            //Read
            if (arr[6] == "01")
            {
                // Nothing to read
                Console.WriteLine("Nothing");
            }
            else if (arr[6] == "00")
            {
                var key = string.Join("", arr.Skip(7));
                Console.WriteLine("Key: " + key);
                if (_LastKey != key)
                {
                   // Beep();
                   Console.WriteLine("###################  Beep!!!!  ##########");
                }

                _LastKey = key;

            }
            else
            {
                Console.WriteLine("Device: " + read);
            }

        }


        static void SendRead()
        {
            SendCommand("AADD0003010C0D");
            ReadPort();
        }


        private static void SendCommand(string cmd)
        {
            var buffer = TransformStringToHexBuffer(cmd);

            if (!_port.IsOpen)
            {
                Console.WriteLine("The port is closed. Please open port first.");
                return;
            }

            _port.Write(buffer, 0, buffer.Length);
        }


        private static byte[] TransformStringToHexBuffer(string str)
        {
            int NumberChars = str.Length;
            byte[] bytes = new byte[NumberChars / 2];

            try
            {
                for (int i = 0; i < NumberChars; i += 2)
                    bytes[i / 2] = Convert.ToByte(str.Substring(i, 2), 16);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }

            return bytes;
        }


        static string HexBytesToHexString(byte[] buffer)
        {
            string hex = BitConverter.ToString(buffer);
            return hex.Replace("-", "");
        }

        static string[] SplitString(string str)
        {
            var list = new List<string>();

            for (var i = 0; i < str.Length;)
            {
                list.Add(str.Substring(i, 2));

                i += 2;
            }

            return list.ToArray();
        }

    }
}
