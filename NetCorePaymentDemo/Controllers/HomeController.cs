using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NetCorePaymentDemo.Models;
using NetCorePaymentDemo.Sips;
using NetCorePaymentDemo.Sips.Models;

namespace NetCorePaymentDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Payment(PaymentModel model)
        {
            if (ModelState.IsValid)
            {
                var error = string.Empty;
                var amount = model.Amount;  
                var currencyCode = model.CurrencyCode;

                int orderId = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                var client = new SipsClient();

                // 1. Generate a unique identifier for this transaction (so you keep track of the transaction history)
                var transactionReference = client.GetTransactionReference("NetcorePayment", orderId);

                // 2. Create and initialize a PaymentRequest
                var merchantId = _configuration["Sips_MerchantId"]; // Use "002001000000001" if using the Sandbox uri
                var interfaceVersion = _configuration["Sips_InterfaceVersion"]; // "IR_WS_2.14" is the current interface version
                var normalReturnUrl = Url.Action("Completed", "Home", null, Request.Scheme);
                var automaticResponseUrl = Url.Action("OrderPaid", "Home", null, Request.Scheme);
                var paymentRequest = client.GetPaymentRequest(merchantId, interfaceVersion, transactionReference, normalReturnUrl, automaticResponseUrl);

                // 3. Set PaymentRequest data (not Seal, SealAlgorithm, Key & KeyVersion)
                paymentRequest.SetCustomer("TestCustomer", CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
                paymentRequest.SetOrderDetails(orderId.ToString(), (int)Math.Round(amount * 100), currencyCode);

                paymentRequest.PaymentMeanBrandList.Add("VISA");
                paymentRequest.PaymentMeanBrandList.Add("MASTERCARD");

                // 4. Set Seal, SealAlgorithm, Key & KeyVersion for PaymentRequest
                var secretKey = _configuration["Sips_SecretKey"];
                paymentRequest.SetSealAndKeyVersion(secretKey); // Use "002001000000001_KEY1" if using the Sandbox uri

                // 5. Send PaymentRequest to Worldline & receive a RedirectionModel (with redirection Uri and data)
                var redirection = client.SendPaymentRequest(paymentRequest, _configuration["Sips_PaymentUrl"]);

                // 6. If the payment request was successful (RedirectionStatusCode == "00") redirect to Worldline
                if (redirection != null && redirection.RedirectionStatusCode == "00")
                {
                    var url = client.RedirectToWorldlineUrl(redirection);
                    return Redirect(url);
                }

                // 7. If the payment request was not succesful show an error message
                if (redirection != null)
                {
                    switch (redirection.RedirectionStatusCode)
                    {
                        case "30": error = "Request format is not valid."; break;
                        case "34": error = "There is a security problem: for example, the calculated seal is incorrect."; break;
                        case "94": error = "Transaction already exists."; break;
                        case "99": error = "Service temporarily unavailable."; break;
                        default: error = redirection.RedirectionStatusMessage; break;
                    }
                }
                else
                    error = "Unable to retrieve a redirection model.";
            }
            return RedirectToAction("Error");
        }

        public async Task<ActionResult> Completed()
        {
            var form = HttpContext.Request.ReadFormAsync();
            return View();

        }

        public async Task<ActionResult> OrderPaid()
        {
            var form = HttpContext.Request.ReadFormAsync();
            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
