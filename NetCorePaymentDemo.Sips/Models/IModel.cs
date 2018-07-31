using System.Collections.Generic;

namespace NetCorePaymentDemo.Sips.Models
{
    public interface IModel
    {
        SortedDictionary<string, string> ToSortedDictionary();
    }
}