using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace IngenicoPOS
{
    public class POSMessage
    {
        private int
            _terminalID,
            _sourceID,
            _sequentialNumber,
            _transactionNumber,
            _batchNumber,
            _amountCurrency;

        private Int64
            _transactionAmount,
            _PINBlock,
            _transactionAmountCash,
            _DCCNumber,
            _DCCAmount;

        private string
            _MIDNumber,
            _TIDNumber,
            _transactionType,
            _transactionFlag,
            _transactionDate,
            _transactionTime,
            _cardDataSource,
            _cardNumber,
            _expirationDate,
            _authorizationCode,
            _companyName,
            _displayMessage,
            _inputData,
            _EMVData,
            _acquirerName,
            _debitTransactionCount,
            _debitTransactionAmount,
            _refundTransactionCount,
            _refundTransactionAmount,
            _installmentsNumber,
            _fullResponseCode,
            _transactionStatus,
            _SPDHTerminalTotals,
            _SPDHHostTotals,
            _cardholderName,
            _RRN,
            _payservicesData,
            _availableBalance,
            _loyaltyData,
            _formattedTTP,
            _DCCProvider,
            _DCCExchangeRate,
            _DCCExchangeRateDate,
            _DCCMarkUpPercent,
            _DCCDisclaimer,
            _DCCStatus,
            _DCCCurrencySymbol,
            _InstantPaymentReference,
            _PersonalVehicleCardData;

        private bool
            _signatureLinePrintFlag,
            _moreMessagesFlag,
            _PINFlag;


        public string TerminalID { get { return _TIDNumber; } }
        public string TransactionFlag { get { return _transactionFlag; } }
        public string TransactionType { get { return _transactionType; } }
        public Int64 TransactionAmount { get { return _transactionAmount; } }
        public string TransactionDate { get { return _transactionDate; } }
        public string TransactionTime { get { return _transactionTime; } }
        public string CardDataSource { get { return _cardDataSource; } }
        public string AuthorizationCode { get { return _authorizationCode; } }
        public string CardNumber { get { return _cardNumber; } }
        public string DisplayMessage { get { return _displayMessage; } }
        public string CardBrand { get { return _companyName; } }
        public string MID { get { return _MIDNumber; } }
        public string BatchNumber { get { return _batchNumber.ToString(); } }
        public string TransactionNumber { get { return _transactionNumber.ToString(); } }
        public string RRN { get { return _RRN; } }
        public string ResponseCode { get { return _fullResponseCode; } }


        public EMVData EMV { get; set; }
        public POSMessage()
        {
        }
        public POSMessage(string parseData)
        {
            ParseAssign(parseData);
        }

        public void ParseAssign(string parseData)
        {
            string[] parseArr = parseData.Split((char)0x1c);
            _terminalID = int.Parse(parseArr[0].Substring(3, 2));
            _sequentialNumber = int.Parse(parseArr[0].Substring(7, 4));
            _transactionType = parseArr[0].Substring(11, 2);
            _transactionFlag = parseArr[0].Substring(13, 2);
            _transactionNumber = int.Parse(parseArr[0].Substring(15, 6));
            _batchNumber = int.Parse(parseArr[0].Substring(21, 4));

            _transactionDate = parseArr[0].Substring(25, 6);
            _transactionTime = parseArr[0].Substring(31, 6);

            // FS
            _transactionAmount = Int64.Parse(parseArr[1]);
            // FS FS FS
            _amountCurrency = int.Parse(parseArr[4]);
            // FS
            _cardDataSource = parseArr[5];
            // FS
            _cardNumber = parseArr[6];
            // FS
            _expirationDate = parseArr[7];
            // FS FS FS
            _authorizationCode = parseArr[10];

            /*
             * For some reason, the POS is not sending */
            // FS
            _TIDNumber = parseArr[11];
            // FS
            _MIDNumber = parseArr[12];
            // FS
            _companyName = parseArr[13];
            // FS FS FS FS FS FS FS
            _displayMessage = parseArr[20].Trim();
            // FS FS
            _inputData = parseArr[22];
            // FS
            EMV = new EMVData(parseArr[23]);

            // FS
            _signatureLinePrintFlag = (parseArr[24] == "1");
            // FS
            if (parseArr[25].Length == 42)
            {
                _acquirerName = parseArr[25].Substring(0, 10);
                _debitTransactionCount = parseArr[25].Substring(10, 4);
                _debitTransactionAmount = parseArr[25].Substring(14, 12);
                _refundTransactionCount = parseArr[25].Substring(26, 4);
                _refundTransactionAmount = parseArr[25].Substring(30, 12);
            }
            // FS
            _moreMessagesFlag = (parseArr[26] == "1");
            // FS
            _installmentsNumber = parseArr[27];
            // FS
            _fullResponseCode = parseArr[28].Trim();
            // FS
            _transactionStatus = parseArr[29];
            // FS
            _SPDHTerminalTotals = parseArr[30];

            //_SPDHHostTotals
            _ = parseArr[31];
            //pin block
            _ = parseArr[32];
            //cardholder name
            _ = parseArr[33];

            _RRN = parseArr[34];

            //pin flag
            _ = parseArr[35];


            // _transactionAmount  - optional
            _ = parseArr[36];




            //78.Payservices  Data
            // Optional 600 max
            // Alphanumeric  TLV based data(Product code, service provider information,...) 
            _ = parseArr[37];

            //80.Available balance
            // Optional 41 max Alpha numeric
            // Balance of Cardholder account
            _ = parseArr[38];


            //82.Offline cryptogram
            //Optional 32  Numeric Used for Borica host protocol,  only
            _ = parseArr[39];


            // 84.Loyalty Data
            // Optional 1000 max
            // Alphanumeric             TLV based data(loyalty information)
            _ = parseArr[40];


            //86.Formatted text to print
            //Optional 800 max
            //Alpha numeric  Formated data to print on ECR
            _ = parseArr[41];


            //88.DCC Number Optional 16 Numeric  DCC Number
            _ = parseArr[42];

            // 90.DCC Provider
            // Optional 30 max
            _ = parseArr[43];



            // 92.DCC Exchange Rate
            // Optional 23 max
            _ = parseArr[44];

            // 94.DCC Exchange Rate Date
            // Optional 10 Alpha numeric
            _ = parseArr[45];

            // 96.DCC Mark Up Percent
            // Optional 8max
            _ = parseArr[46];


            // 98.DCC Amount
            // Optional 12 max
            _ = parseArr[47];


            // 100.DCC Disclaimer
            // Optional 1000 max
            _ = parseArr[48];

            //102.DCC Status
            //Optional 2 Alpha numeric
            //DCC status ('A ' – Accepted, 'D ' –Declined)
            _ = parseArr[49];

            //104.DCC Currency Symbol
            //Optional 3 Alpha numeric
            _ = parseArr[50];

            // 106.Instant Payment Reference
            // Optional 35 max
            // Numeric  Reference number for Instant Payment – generated by terminal on Instant Payment Purchase or Inquiry.The same value is required in Instant Payment Refund request
            _ = parseArr[51];


            // 108.Personal / Vehicle card data
            // Optional 5000 max
            // TLV based data(after initiated transaction type 78 and 79).
            _ = parseArr[52];


            // 110.M Mifare card type
            // Optional 2
            // Aplha numeric
            // Type of the Mifare card:
            // MF_CLASSIC    0x40
            // MF_ULTRALIGHT 0x10
            // MF_4K         0x08
            // MF_MINI       0x04
            // MF_PLUS_L2    0x02
            _ = parseArr[53];


            //112.M Mifare card UID
            //Optonal 32 max
            _ = parseArr[54];


            //114.F4G code
            // Optional 35 max
            // F4G code for specific Instant Payment implementation
            _ = parseArr[55];


            // 116.Radcom / transportation specific data
            // Optional 1000 max
            _ = parseArr[56];


            // 118.Acquirer ID
            // Optional 2 Numeric  Acquirer identifier – present in  all transaction responses and transaction reports.
            // Might be used to determine which acquirer / merchant is used for transactions and for matching issuer and acquirer totals
            _ = parseArr[57];

            // 120.Transaction ID
            // Optional 36 max
            _ = parseArr[58];


            // 122.Credit type
            // Optional 1 Alpha numeric
            // Type of creditor for Installments (possbile values: S – Store; F – Financial institution)
            _ = parseArr[59];


            // 124.Installments Delay
            // Optional 3 Alpha numeric
            // Postpone for Installments. Format is: Dxx or Mxx where first character represents postpone type(Days or Months) and xx is postponement(two digit number)
            _ = parseArr[60];

            // 126.SPDH sequence number
            // Optional 9 Numeric
            // Sequence number for SPDH host protocol
            _ = parseArr[61];


        }
    }
}

