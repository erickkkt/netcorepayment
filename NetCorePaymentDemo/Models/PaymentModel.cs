using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NetCorePaymentDemo.Models
{
    public class PaymentModel
    {
        [Required]
        public float Amount { get; set; }
        public string CurrencyCode { get; set; }
    }
}
