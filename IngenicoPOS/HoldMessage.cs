using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IngenicoPOS
{
    internal class HoldMessage
    {
        public enum HoldMessageCode
        {
            ECR_HOLD_UNKNOWN = 0,
            ECR_HOLD_PLEASEWAIT = 50,
            ECR_HOLD_INSERTCARD = 51,
            ECR_HOLD_REMOVECARD = 52,
            ECR_HOLD_SWIPECARD = 53,
            ECR_HOLD_CARDINSERTED = 54,
            ECR_HOLD_ENTERPIN = 55,
            ECR_HOLD_PINOK = 56,
            ECR_HOLD_HOSTCOMM = 57,
            ECR_HOLD_INPUTDATA = 58,
            ECR_HOLD_CANCELED = 59,
            ECR_HOLD_PRESENTCARD = 60
        }

        public string Message { get; set; }
        public HoldMessageCode Code { get; set; }

        public HoldMessage(string message)
        {
            var splited = message.Split((char)28);

            Message = splited[0].Substring(8);
            if (splited.Length > 1)
            {
                Code = (HoldMessageCode)int.Parse(splited[1]);
            }

        }
    }
}
