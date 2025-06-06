using Serilog;
using Serilog.Core;
using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;

namespace IngenicoPOS
{

    public class POS : IDisposable
    {
        public delegate void StatusUpdatedDelegate(string message);
        public event StatusUpdatedDelegate StatusUpdated;

        TaskCompletionSource<SaleResult> _tcs;

        private SerialPort POSPort;
        private bool _connected = false;

        private string _receiveBuffer = string.Empty;

        public int NextTransactionNo = 0;
        public bool POSPrints = true;
        public int CurrencyISO = 941;
        public int CashierID = 0;
        public string Language = "00";

        const byte ACK = 0x06; // Acknowledge character
        const byte NACK = 0x15; // Negative Acknowledge character
        const byte FS = 28; // separator
        const byte ETX = 0x03; // End of Text 

#if DEBUG
        string _DebugMsg = "\u0002100000000401020000360004250525173135\u001c000000010000\u001c\u001c+0\u001c941\u001cC\u001c5356049999995057\u001c9999\u001c\u001c\u001c711496\u001c11111111\u001c11111111\u001cMASTERCARD\u001c\u001c\u001c\u001c\u001c\u001c\u001cODOBRENO                 \u001c\u001c\u001c8407A0000000041010950500000080019F12104465626974204D6173746572636172649F2608DC89685D144FD15F9F2701809F34031F0302\u001c1\u001c\u001c0\u001c\u001c00\u001cC1\u001c\u001c\u001c\u001c\u001c444405374444\u001c0\u001c\u001c\u001c\u001c\u001c\u001c\u001c\u001c\u001c\u001c\u001c\u001c000000000000\u001c\u001c\u001c\u001c\u001c\u001c\u001c\u001c\u001c\u001c02\u001c\u001c\u001c\u001c\u001c\u0003";
        //string x =         "\u0002100000000001020000200004250525102446\u001c000000013000\u001c\u001c+0\u001c941\u001cC\u001c5356049999995057\u001c9999\u001c\u001c\u001c746084\u001c11111111\u001c11111111\u001cMASTERCARD\u001c\u001c\u001c\u001c\u001c\u001c\u001cODOBRENO                 \u001c\u001c\u001c8407A0000000041010950500000080019F12104465626974204D6173746572636172649F260890AF66634C70C0D19F270180;"
#endif

        public bool IsConnected { get { return _connected; } }

        public POS(string Port, int baud = 115200)
        {
            // Initialize The Serial Port
            POSPort = new SerialPort(Port, baud, Parity.None, 8, StopBits.One);
            // Assign DataReceived event
            POSPort.DataReceived += new SerialDataReceivedEventHandler(pos_DataReceived);
        }

        public Task<SaleResult> Sale(Int64 Amount)
        {
            if (!_connected)
            {
                return Task.FromResult(new SaleResult(false, null));
            }

            _tcs = new TaskCompletionSource<SaleResult>();

#if DEBUG
            if (!string.IsNullOrEmpty(_DebugMsg))
            {
                messageReceived(_DebugMsg);
                return _tcs.Task;
            }
#endif


            // Build the message to send to the device
            ECRMessage msg = new ECRMessage();
            msg.NextTransactionNo = NextTransactionNo;
            msg.CurrencyISO = CurrencyISO;
            msg.POSPrints = POSPrints;
            msg.CashierID = CashierID;
            msg.TransactionAmount = Amount;
            msg.TransactionType = Consts.TransactionType.SALE;
            msg.LanguageID = Language;

            // Clear the buffer
            POSPort.ReadExisting();

            Log.Debug("Sending ECR message: {message}", msg.Message);

            // Send the message to the device
            POSPort.Write(msg.Message);

            /// To verify that the device got our message, we wait for ACK or 0x06 response
            /// If the device got our message, and it was not in correct format, we will get 0x15 as response, which is NACK
            /// We have allowed time for the device to get back to us and that, by specification, is 1 second
            /// After 1 second we send it the message once again, and hopefully it will receive it this time
            /// After 3 tries, the transaction will be marked as unsuccessful, with communication error.


            return _tcs.Task;
        }

        void messageReceived(string message)
        {

            var lastPOSMsg = new POSMessage(message);

            var opt = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };

            string jresult = System.Text.Json.JsonSerializer.Serialize(lastPOSMsg, opt);

            Log.Verbose("Received message", message);
            Log.Verbose("Parsed message {0}", jresult);

            /// Message received, check it and send the response if message is correct and
            /// if transaction was successful
            if (lastPOSMsg.TransactionFlag == Consts.TransactionFlag.ACCEPTED_WITH_AUTH ||
                    lastPOSMsg.TransactionFlag == Consts.TransactionFlag.ACCEPTED_WITHOUT_AUTH)
            {
                // Transaction was successful :D
                // Send ACK and return transaction successful
                POSPort.Write(((char)0x06).ToString());
            }
            else if (lastPOSMsg.TransactionFlag == Consts.TransactionFlag.REFUSED ||
                        lastPOSMsg.TransactionFlag == Consts.TransactionFlag.ERROR ||
                        lastPOSMsg.TransactionFlag == Consts.TransactionFlag.COMMUNICATION_ERROR)
            {
                // Transaction wasn't successful :(
                // Send ACK and return transaction unsuccessful
                POSPort.Write(((char)0x06).ToString());
                setResult(new SaleResult(lastPOSMsg.DisplayMessage));
                return;
            }
            else
            {
                // Message probably not valid, send NACK
                POSPort.Write(((char)0x15).ToString());
                setResult(new SaleResult(false, null));
                return;
            }

            // If everything goes as planned, this lines of code should be executed
            NextTransactionNo++;
            setResult(new SaleResult(true, lastPOSMsg));
        }

        void setResult(SaleResult result)
        {

            _tcs.SetResult(result);

            Disonnect();
        }
        public bool Connect()
        {

            POSPort.Open();
            POSPort.ReadExisting(); // Clear the buffer
            /*
            var functionRequest = $"31092{(char)FS}{(char)FS}{(char)FS}{(char)ETX}";
            functionRequest = ((char)0x02).ToString() + ((char)0x02).ToString() + functionRequest + (char)Utils.GetLRC(Encoding.ASCII.GetBytes(functionRequest));
            var x = Encoding.ASCII.GetBytes(functionRequest);
            var y = BitConverter.ToString(x);

            POSPort.Write(functionRequest);*/


            _connected = true;
            return true;
        }

        public void Disonnect()
        {
            try
            {
                POSPort.Close();
            }
            catch (Exception) { }
            _connected = false;
        }

        private void pos_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var port = (SerialPort)sender;
            string message = port.ReadExisting();

            // Append received data to buffer
            _receiveBuffer += message;

            if (_receiveBuffer.Length > 0 && _receiveBuffer[0] == ACK)
            {
                _receiveBuffer = _receiveBuffer.Substring(1); // Remove ACK character
            }
            if (_receiveBuffer.Length > 0 && _receiveBuffer[0] == NACK)
            {
                _receiveBuffer = _receiveBuffer.Substring(1); // Remove ACK character
                setResult(new SaleResult(false, null));
            }


            // Check if buffer contains the end-of-transmission character (0x03)
            int eotIndex;
            while ((eotIndex = _receiveBuffer.IndexOf((char)0x03)) != -1)
            {


                // We need at least one more byte after 0x03 for the LRC
                if (_receiveBuffer.Length <= eotIndex + 1)
                    break; // Wait for more data

                // Extract the complete message up to and including 0x03
                string completeMessage = _receiveBuffer.Substring(0, eotIndex + 1);
                // Extract the LRC byte (immediately after 0x03)
                byte receivedLrc = (byte)_receiveBuffer[eotIndex + 1];

                // Validate LRC
                if (ValidateLrc(completeMessage, receivedLrc))
                {
                    processMessage(completeMessage);
                }
                else
                {
                    Log.Warning("LRC check failed for message: {message}", completeMessage);
                    // Optionally, send NACK or handle error
                    POSPort.Write(((char)0x15).ToString());
                }

                // Remove the processed message and LRC from the buffer
                _receiveBuffer = _receiveBuffer.Substring(eotIndex + 2);
            }
        }

        // LRC calculation: XOR of all bytes in the message (including 0x03)
        // ignore first STX char
        private bool ValidateLrc(string message, byte receivedLrc)
        {
            byte lrc = 0;
            foreach (char c in message.Substring(1))
                lrc ^= (byte)c;
            return lrc == receivedLrc;
        }

        private void processMessage(string completeMessage)
        {
            Log.Debug("Received ECR message: {message}", completeMessage);

            if (completeMessage.StartsWith("20") || completeMessage.StartsWith("25"))
            {
                /// message
                var holdMessage = new HoldMessage(completeMessage.Substring(1));
                StatusUpdated?.Invoke(holdMessage.Message);

                SendACK();
            }
            else if (completeMessage.StartsWith("22"))
            {
                // error
                var msg = completeMessage.Substring(9, completeMessage.Length - 11);

                SendACK();

                setResult(new SaleResult(msg));
            }
            else if (completeMessage.StartsWith("26"))
            {
                // error
                var splited = completeMessage.Split((char)0x1c);
                var msg = splited[0].Substring(9, splited[0].Length - 9);

                SendACK();

                setResult(new SaleResult(msg));
            }
            else
            {
                // transaction result 
                messageReceived(completeMessage);
            }
        }

        private void SendACK()
        {
            POSPort.Write(((char)0x06).ToString()); // Received HOLD Message, send ACK to confirm the hold
        }

        public void Dispose()
        {
            Disonnect();
        }
    }
}