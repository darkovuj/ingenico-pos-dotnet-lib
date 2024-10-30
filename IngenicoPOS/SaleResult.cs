using System;
using System.Collections.Generic;
using System.Text;

namespace IngenicoPOS {
    public class SaleResult {
        private bool _success;
        public string ErrorMessage { get; set; }
        private POSMessage _lastMessage;

        public bool Success { get { return _success; } }
        public POSMessage Message { get { return _lastMessage; } }

        public SaleResult(bool success, POSMessage lastMessage) {
            this._success = success;
            this._lastMessage = lastMessage;
        }
        public SaleResult(   string errorMessage)
        {
            this._success = false ;
            this.ErrorMessage = errorMessage;
        }
    }
}
