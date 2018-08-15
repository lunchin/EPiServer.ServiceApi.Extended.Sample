using System;
using System.Collections.Generic;
using EPiServer.ServiceApi.Commerce.Models.Order;

namespace EPiServer.ServiceApi.Extended.Sample.Api
{
    public abstract class ExtendedBaseModel
    {
        public Guid PrimaryKeyId { get; set; }
        public List<AddressModel> Addresses { get; set; }
        public List<ExtendedMetaFieldProperty> MetaFields { get; set; }
    }
}