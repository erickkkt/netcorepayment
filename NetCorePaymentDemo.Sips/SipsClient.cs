using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Http;
using NetCorePaymentDemo.Sips.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace NetCorePaymentDemo.Sips
{
    public class SipsClient
    {
        #region Useful objects and data
        public string GetTransactionReference(string prefix, int orderId)
        {
            var trans = string.Format("{0}{1}", prefix, orderId);
            return trans.Substring(0, Math.Min(35, trans.Length));
        }
 
        public PaymentRequest GetPaymentRequest()
        {
            var request = new PaymentRequest();
            return request;
        }

        public PaymentRequest GetPaymentRequest(string merchantId, string interfaceVersion, string transactionReference, string normalReturnUrl = null, string automaticResponseUrl = null)
        {
            var request = GetPaymentRequest();
            //IPHostEntry hostInfo = Dns.GetHostEntry(HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"]);
            //var ipAddress = hostInfo.AddressList.FirstOrDefault(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString();
            request.MerchantId = merchantId;
            request.InterfaceVersion = interfaceVersion;
            request.NormalReturnUrl = normalReturnUrl;
            request.AutomaticResponseUrl = automaticResponseUrl;
            request.TransactionReference = transactionReference;
            //request.CustomerIpAddress = ipAddress;
            return request;
        }

        public RedirectionModel SendPaymentRequest(string jsonresult)
        {
            var json = JsonConvert.DeserializeObject<RedirectionModel>(jsonresult, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });

            if (json != null) return json;

            return new RedirectionModel { RedirectionStatusCode = "01", RedirectionStatusMessage = "Worldline payment request failed: invalid json result string." };
        }

        public PaymentInfo GetPaymentInfo(NameValueCollection form)
        {
            var data = string.Empty;
            var encode = string.Empty;
            var seal = string.Empty;
            var interfaceVersion = string.Empty;

            string[] keys = form.AllKeys;
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i] == "Data") data = form[keys[i]];
                else if (keys[i] == "Encode") encode = form[keys[i]];
                else if (keys[i] == "Seal") seal = form[keys[i]];
                else if (keys[i] == "InterfaceVersion") interfaceVersion = form[keys[i]];
            }
            return GetPaymentInfo(data, encode, seal, interfaceVersion);
        }

        public PaymentInfo GetPaymentInfo(string data, string encode, string seal, string interfaceVersion)
        {
            var paymentInfo = new PaymentInfo()
            {
                Encode = encode,
                Seal = seal,
                InterfaceVersion = interfaceVersion
            };
            var dict = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(data))
            {
                var paymentData = data.Split('|');
                foreach (var s in paymentData)
                {
                    if (s.Split('=').Length > 1) dict.Add(s.Split('=')[0], s.Split('=')[1]);
                }
            }
            paymentInfo.Data = dict;
            return paymentInfo;
        }

        #endregion

        #region Interaction with Worldline
        public RedirectionModel SendPaymentRequest(PaymentRequest request, string paymentRequestUrl)
        {
            var result = _sendPaymentRequest(request, paymentRequestUrl);
            if (!string.IsNullOrEmpty(result))
                return SendPaymentRequest(result);
            return null;
        }

        private string _sendPaymentRequest(PaymentRequest request, string paymentRequestUrl)
        {
            var dataToSend = JsonConvert.SerializeObject(
                request,
                new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }
                );
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(paymentRequestUrl);

            httpWebRequest.Method = "POST";
            httpWebRequest.ContentLength = Encoding.UTF8.GetByteCount(dataToSend);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Accept = "application/json";

            var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream());

            streamWriter.Write(dataToSend);
            streamWriter.Close();

            var reponseStream = (httpWebRequest.GetResponse() as HttpWebResponse)?
                .GetResponseStream();
            if (reponseStream != null)
            {
                var streamReader = new StreamReader(reponseStream);
                var stringResult = streamReader.ReadToEnd();
                return stringResult;
            }
            return null;
        }

        public string RedirectToWorldlineUrl(RedirectionModel redirect)
        {
            return RedirectToWorldlineUrl(redirect.RedirectionUrl, redirect.RedirectionVersion, redirect.RedirectionData);
        }

        public string RedirectToWorldlineUrl(string redirectionUrl, string redirectionVersion, string redirectionData)
        {
            var url = string.Format("{0}?redirectionVersion={1}&redirectionData={2}", redirectionUrl, HttpUtility.UrlEncode(redirectionVersion), HttpUtility.UrlEncode(redirectionData));
            return url;
        }

        #endregion
    }
}