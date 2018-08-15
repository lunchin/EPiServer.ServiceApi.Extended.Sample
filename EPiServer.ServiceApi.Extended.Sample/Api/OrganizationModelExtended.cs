using System.Collections.Generic;

namespace EPiServer.ServiceApi.Extended.Sample.Api
{
    public class OrganizationModelExtended : ExtendedBaseModel
    {
        public List<OrganizationModelExtended> ChildOrganizations { get; set; }
        public List<ContactModelExtended> Contacts { get; set; }
        public string OrganizationType { get; set; }
        public string OrgCustomerGroup { get; set; }
    }
}