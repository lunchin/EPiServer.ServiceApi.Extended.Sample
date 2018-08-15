using System.Collections.Generic;

namespace EPiServer.ServiceApi.Extended.Sample.Api
{
    public class ExtendedMetaFieldProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public List<string> Values { get; set; }
    }
}