using System;
using System.Runtime.Serialization;

namespace Tools.Models
{
    [DataContract]
    public class UnpreparedOrder : OrderModel
    {
        [DataMember(IsRequired = true)]
        public int ID_Request { get; set; }

        [DataMember]
        public string District { get; set; }

        [DataMember]
        public string City { get; set; }

        [DataMember]
        public string Town { get; set; }

        [DataMember]
        public string Street { get; set; }

        [DataMember]
        public string Home { get; set; }

        [DataMember]
        public string Corp { get; set; }

        [DataMember]
        public string Flat { get; set; }

        [DataMember]
        public string Square { get; set; }

        public DateTime RecivedAt { get; set; }

        public string Address
        {
            get
            {
                string Address = string.Empty;

                if (!string.IsNullOrEmpty(City))
                {
                    Address += $"г.{City}";

                    if (!string.IsNullOrEmpty(Street))
                        Address += $", ул.{Street}";

                    if (!string.IsNullOrEmpty(Home))
                        Address += $", д.{Home}";

                    if (!string.IsNullOrEmpty(Corp))
                        Address += $", корп.{Corp}";

                    if (!string.IsNullOrEmpty(Flat))
                        Address += $", кв.{Flat}";
                }

                else if (!string.IsNullOrEmpty(District) || !string.IsNullOrEmpty(Town))
                {
                    if (!string.IsNullOrEmpty(District))
                        Address += $"р.{District}";

                    if (!string.IsNullOrEmpty(Town))
                        Address += $", {Town}";

                    if (!string.IsNullOrEmpty(Street))
                        Address += $", ул.{Street}";

                    if (!string.IsNullOrEmpty(Home))
                        Address += $", д.{Home}";

                    if (!string.IsNullOrEmpty(Corp))
                        Address += $", корп.{Corp}";

                    if (!string.IsNullOrEmpty(Flat))
                        Address += $", кв.{Flat}";
                }

                return Address;
            }
        }
    }
}