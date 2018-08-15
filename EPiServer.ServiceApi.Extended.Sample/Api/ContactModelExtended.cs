namespace EPiServer.ServiceApi.Extended.Sample.Api
{
    public class ContactModelExtended : ExtendedBaseModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string RegistrationSource { get; set; }
    }
}