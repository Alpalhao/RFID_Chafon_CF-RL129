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
        private const string _GET_ID_COMMAND = "AADD0003010203";
        private SerialPort _Port;
        private StringBuilder _LOG;

        public string Port { get; set; }

        public StringBuilder Log { get { return _LOG; } }
        public bool IsConnected { get { return _Port.IsOpen; } }

        public API(string port = null)
        {
            Port = port;
            _LOG = new StringBuilder();
        }

        public void Connect()
        {
            if (_Port == null) InicializePort();

            _LOG.Clear();
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

        public string ReadTag()
        {
            return SendCommand(Commands.READ);
        }

        public string GetId()
        {
           return  SendCommand(Commands.GET_ID);
        }

        public void WriteTag(string tag)
        {
            SendCommand(Commands.WRITE, tag);
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
                    SendWriteTagCommand(toWrite);
                    str = _BEEP_COMMAND;
                    break;
                default:
                    break;
            }

            var buffer = TransformStringToHexBuffer(str);
            _Port.Write(buffer, 0, buffer.Length);

            var response = ReceiveResponse();

            return ProcessResponse(response, cmd);
        }

        private void SendWriteTagCommand(string tag)
        {
            var cmd_array = new List<string>(){ "AA", "DD", "00", "09"};
            var payload = new List<string>() { "03", "0C", "00" };

            var splitTag = SplitString(tag);
            payload.AddRange(splitTag);

            for(var i=0; i<8; i++)
            {
                if (payload[i] == "AA")
                {
                    cmd_array.Add("AA");
                }
                cmd_array.Add(payload[i]);
            }

            string csum = CalculateChecksum(payload);
            cmd_array.Add(csum);
            if(csum == "AA")
            {
                // ensure the byte stream never has data with 0xAA,0xDD
                // by inserting an extra 0xAA when a 0xAA is found
                cmd_array.Add(csum);
            }

            var cmd = string.Join("", cmd_array);

            var buffer = TransformStringToHexBuffer(cmd);
            _Port.Write(buffer, 0, buffer.Length);

            Thread.Sleep(100);
            var cnt = ReceiveResponse();
            
            // send second write command
            payload[0] = "02";
            csum = CalculateChecksum(payload);
            cmd_array[cmd_array.Count-1] = csum;
            if (csum == "AA")
            {
                // ensure the byte stream never has data with 0xAA,0xDD
                // by inserting an extra 0xAA when a 0xAA is found
                cmd_array.Add(csum);
            }

            cmd = string.Join("", cmd_array);

            buffer = TransformStringToHexBuffer(cmd);
            _Port.Write(buffer, 0, buffer.Length);

            Thread.Sleep(100);
            cnt = ReceiveResponse();

        }


        private string CalculateChecksum(List<string> cmd)
        {
            byte csum = 0;
            for (var i = 0; i < 8; i++)
            {
                var str = cmd[i];
                csum ^= Convert.ToByte(str, 16);
            }
            string strCSum = BitConverter.ToString(new[] { csum });
            return strCSum;
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
                    _LOG.AppendLine("No TAG!");
                    //Console.WriteLine("No TAG!");
                }
                else if (arr[6] == "00")
                {
                    var key = string.Join("", arr.Skip(7));
                    _LOG.AppendFormat("TAG readed: {0}", key);
                    _LOG.AppendLine();
                    //Console.WriteLine("Key: " + key);
                    return key;
                }
            }else if(cmd == Commands.GET_ID)
            {
                var hardwareId = string.Join("", arr.Skip(7));
                //Console.WriteLine("Hardware id: " + hardwareId);
                _LOG.AppendFormat("Hardware id: {0}", hardwareId);
                _LOG.AppendLine();
                return hardwareId;
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
