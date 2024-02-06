using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Tools.Models
{
    [DataContract]
    public class OrderPackage
    {
        [DataMember(IsRequired = true)]
        public string Source_Key { get; set; }

        [DataMember(IsRequired = true)]
        public List<UnpreparedOrder> Orders { get; set; }


        public OrderPackage(IEnumerable<UnpreparedOrder> orders, string key)
        {
            Orders = orders.ToList();
            Source_Key = key;
        }
    }
}