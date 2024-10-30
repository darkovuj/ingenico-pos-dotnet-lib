using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading.Tasks;

namespace IngenicoPOS
{

    public class POS : IDisposable
    {

        TaskCompletionSource<SaleResult> _tcs;

        private SerialPort POSPort;
        private bool _connected = false;

        public int NextTransactionNo = 0;
        public bool POSPrints = true;
        public int CurrencyISO = 941;
        public int CashierID = 0;
        public string Language = "00";


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
                return Task.FromResult(new SaleResult(false, null));

            _tcs = new TaskCompletionSource<SaleResult>();

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
                setResult(new SaleResult(false, lastPOSMsg));
            }
            else
            {
                // Message probably not valid, send NACK
                POSPort.Write(((char)0x15).ToString());
                setResult(new SaleResult(false, null));
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
            try
            {
                POSPort.Open();
                POSPort.ReadExisting(); // Clear the buffer
            }
            catch (Exception)
            {
                // In unlikely case of the port being used or some error..
                _connected = false;
                return false;
            }
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
            string message = ((SerialPort)sender).ReadExisting();
            // Debug.WriteLine(BitConverter.ToString(System.Text.Encoding.Default.GetBytes(message)));  // Write HEX to the debug
            Debug.WriteLine(message);

            if (message == ("\u0006"))
            {  // Received ACK, set the flag
            }
            else if (message == ("\u0015"))
                setResult(new SaleResult(false, null));
            else if (message.StartsWith("20"))
            {
                POSPort.Write(((char)0x06).ToString()); // Received HOLD Message, send ACK to confirm the hold
            }
            else if (message.StartsWith("22"))
            {
                var msg = message.Substring(9, message.Length-11);

                setResult(new SaleResult(msg));
            }
            else
            {
                messageReceived(message);
            }
        }

        public void Dispose()
        {
            Disonnect();
        }
    }
}