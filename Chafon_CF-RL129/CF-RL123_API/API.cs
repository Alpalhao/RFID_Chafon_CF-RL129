using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CF_RL129_API
{
    public class API
    {
        private enum Commands { READ, BEEP, GET_ID, WRITE};
        private const string _READ_COMNAND = "AADD0003010C0D";
        private const string _BEEP_COMMAND = "AADD000401030A08";
        private const string _GET_ID_COMMAND = "";
        private const string _WRITE_COMMAND = "";
        private SerialPort _Port;

        public string Port { get; set; }

        public API(string port = null)
        {
            Port = port;
        }


        public void Connect()
        {
            if (_Port == null) InicializePort();

            _Port.Open();
        }

        public void Close()
        {
            if (_Port != null && _Port.IsOpen)
                _Port.Close();
        }

        public void Beep()
        {
            SendCommand(Commands.BEEP);
        }

        public void ReadTag()
        {
            SendCommand(Commands.READ);
        }

        private void InicializePort()
        {
            _Port = new SerialPort();
            _Port.PortName = Port;
            _Port.BaudRate = 38400;
        }

        private void CheckConnection()
        {
            if (_Port == null || !_Port.IsOpen)
                throw new Exception("You must connect first.");

        }

        private string SendCommand(Commands cmd, string toWrite = null)
        {
            CheckConnection();

            var str = string.Empty;

            switch (cmd)
            {
                case Commands.READ:
                    str = _READ_COMNAND;
                    break;
                case Commands.BEEP:
                    str = _BEEP_COMMAND;
                    break;
                case Commands.GET_ID:
                    str = _GET_ID_COMMAND;
                    break;
                case Commands.WRITE:
                    break;
                default:
                    break;
            }

            var buffer = TransformStringToHexBuffer(str);
            _Port.Write(buffer, 0, buffer.Length);

            var response = ReceiveResponse();

            return ProcessResponse(response, cmd);
        }

        private string ReceiveResponse()
        {
            Thread.Sleep(100);
            var buffer = new byte[_Port.BytesToRead];
            var bytesReaded = _Port.Read(buffer, 0, _Port.BytesToRead);
            var str = HexBytesToHexString(buffer);
            return str;
        }

        private string ProcessResponse(string response, Commands cmd)
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                return string.Empty;
            }

            var arr = SplitString(response);
            if (cmd == Commands.READ)
            {

                if (arr[6] == "01")
                {
                    // Nothing to read
                    Console.WriteLine("Nope!");
                }
                else if (arr[6] == "00")
                {
                    var key = string.Join("", arr.Skip(7));
                    Console.WriteLine("Key: " + key);
                }
            }

            return string.Empty;
        }

        static byte[] TransformStringToHexBuffer(string str)
        {
            int NumberChars = str.Length;
            byte[] bytes = new byte[NumberChars / 2];

            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(str.Substring(i, 2), 16);

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
