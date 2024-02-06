using System.Runtime.Serialization;

namespace Tools.Models
{
    [DataContract]
    public class OrderModel
    {
        public int ID { get; set; }
        public string Source { get; set; }
    }
}