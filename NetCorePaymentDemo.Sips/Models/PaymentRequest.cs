using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Nemo.Payment.Sips.Formatting;

namespace NetCorePaymentDemo.Sips.Models
{
    public class PaymentRequest : BaseModel
    {
        public PaymentRequest()
        {
            PaymentMeanBrandList = new List<string>();
        }

        public string MerchantId { get; set; }
        public string InterfaceVersion { get; set; }
        public string NormalReturnUrl { get; set; }
        public string AutomaticResponseUrl { get; set; }
        public string TransactionReference { get; set; }
        public string Seal { get; set; }
        public string KeyVersion { get; set; }
        public string SealAlgorithm { get; set; }
        public string OrderId { get; set; }
        public string Amount { get; set; }
        public string CurrencyCode { get; set; }
        public string OrderChannel { get; set; }
        public string CustomerId { get; set; }
        public string CustomerIpAddress { get; set; }
        public string CustomerLanguage { get; set; }
        public List<string> PaymentMeanBrandList { get; set; }

        #region Utilities
        public override SortedDictionary<string, string> ToSortedDictionary()
        {
            var sd = base.ToSortedDictionary();
            for (var i = 0; i < PaymentMeanBrandList.Count; i++)
            {
                sd.Add($"PaymentMeanBrandList[{i}]", PaymentMeanBrandList[i]);
            }
            return sd;
        }

        private string _byteArrayToHEX(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }
        #endregion

        #region Methods
        public void SetCustomer(string customerId, string customerLanguage)
        {
            CustomerId = customerId;
            CustomerLanguage = customerLanguage;
        }

        /// <summary>
        /// Adds order details to the PaymentRequest object.
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="totalAmount">Integer value for total amount (f.e. € 9,99 becomes 999)</param>
        /// <param name="currency">ISO 4217 numeric currency code (f.e. 978 for EUR)</param>
        public void SetOrderDetails(string orderId, int totalAmount, int currency)
        {
            OrderId = orderId;
            Amount = totalAmount.ToString(CultureInfo.InvariantCulture);
            if (Iso4217CurrencyCode.IsDefined(currency))
                CurrencyCode = currency.ToString();
            OrderChannel = "INTERNET";
        }

        /// <summary>
        /// Adds order details to the PaymentRequest object.
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="totalAmount">Integer value for total amount (f.e. € 9,99 becomes 999)</param>
        /// <param name="currency">ISO 4217 currency code (f.e. EUR)</param>
        public void SetOrderDetails(string orderId, int totalAmount, string currency)
        {
            SetOrderDetails(
                orderId,
                totalAmount,
                Iso4217CurrencyCode.GetNumeric(currency)
                );
        }
        
        public string GetSeal(string key, string sealAlgorithm = "HMAC-SHA-256")
        {
            var sd = ToSortedDictionary();
            string sChain = string.Concat(sd.Values);
            UTF8Encoding utf8 = new UTF8Encoding();
            byte[] encodedBytes = utf8.GetBytes(sChain);

            HMAC hmac = null;
            switch (sealAlgorithm)
            {
                case "HMAC-SHA-1":
                    hmac = new HMACSHA1();
                    break;
                case "HMAC-SHA-384":
                    hmac = new HMACSHA384();
                    break;
                case "HMAC-SHA-512":
                    hmac = new HMACSHA512();
                    break;
                default:
                    hmac = new HMACSHA256();
                    break;
            }
            hmac.Key = utf8.GetBytes(key);
            hmac.Initialize();

            byte[] shaResult = hmac.ComputeHash(encodedBytes);
            return _byteArrayToHEX(shaResult);
        }
        
        public void SetSealAndKeyVersion(string key, string seal = null, string sealAlgorithm = "HMAC-SHA-256", string keyVersion = "1")
        {
            if (string.IsNullOrEmpty(seal))
                seal = GetSeal(key, sealAlgorithm);
            Seal = seal;
            SealAlgorithm = sealAlgorithm;
            KeyVersion = keyVersion;
        }

        #endregion
    }
}