using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Mvc;
using EPiServer.ServiceApi.Configuration;
using EPiServer.ServiceApi.Validation;
using EPiServer.Shell.Security;
using Mediachase.BusinessFoundation.Data;
using Mediachase.Commerce.Customers;

namespace EPiServer.ServiceApi.Extended.Sample.Api
{
    [System.Web.Http.RoutePrefix("episerverapi/commerce/customerextended")]
    [Configuration.RequireHttps]
    [ValidateReadOnlyMode(AllowedVerbs = HttpVerbs.Get)]
    [ExceptionHandling]
    [RequestLogging]
    public class CustomerExtendedController : ApiController
    {
        private const string AllowedChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ0123456789!@$?_-";
        private readonly Random _random = new Random();
        private readonly UIUserProvider _userProvider;

        public CustomerExtendedController(UIUserProvider userProvider)
        {
            _userProvider = userProvider;
        }

        /// <summary>
        ///     Returns contact.
        /// </summary>
        /// <param name="id">Contact ID (GUID)</param>
        /// <returns>Contact</returns>
        [ResponseType(typeof(ContactModelExtended))]
        [System.Web.Http.Route("contact/{id}", Name = "GetContactExtended")]
        [System.Web.Http.HttpGet]
        [AuthorizePermission(Permissions.GroupName, Permissions.Read)]
        [ModelValidation]
        public virtual IHttpActionResult GetContact(Guid id)
        {
            var contact = CustomerContext.Current.GetContactById(id);
            return Ok(contact.ConvertToContactModel());
        }

        /// <summary>
        ///     Returns contacts.
        /// </summary>
        /// <returns>Array of contacts</returns>
        [ResponseType(typeof(IEnumerable<ContactModelExtended>))]
        [System.Web.Http.Route("contact", Name = "GetContactsExtended")]
        [System.Web.Http.HttpGet]
        [AuthorizePermission(Permissions.GroupName, Permissions.Read)]
        [ModelValidation]
        public virtual IHttpActionResult GetContact()
        {
            var contacts = CustomerContext.Current.GetContacts();
            return Ok(contacts.Select(c => c.ConvertToContactModel()));
        }

        /// <summary>
        ///     Returns organization.
        /// </summary>
        /// <param name="id">Organization ID</param>
        /// <returns>Organization</returns>
        [ResponseType(typeof(OrganizationModelExtended))]
        [System.Web.Http.Route("organization/{id}", Name = "GetOrganizationExtended")]
        [System.Web.Http.HttpGet]
        [AuthorizePermission(Permissions.GroupName, Permissions.Read)]
        [ModelValidation]
        public virtual IHttpActionResult GetOrganization(string id)
        {
            var organization = CustomerContext.Current.GetOrganizationById(id).ConvertToOrganizationModel();
            return Ok(organization);
        }

        /// <summary>
        ///     Returns organizations.
        /// </summary>
        /// <returns>Array of organizations</returns>
        [ResponseType(typeof(IEnumerable<ContactModelExtended>))]
        [System.Web.Http.Route("organization", Name = "GetOrganizationsExtended")]
        [System.Web.Http.HttpGet]
        [AuthorizePermission(Permissions.GroupName, Permissions.Read)]
        [ModelValidation]
        public virtual IHttpActionResult GetOrganization()
        {
            var organizations = CustomerContext.Current.GetOrganizations().Select(o => o.ConvertToOrganizationModel());
            return Ok(organizations);
        }

        /// <summary>
        ///     Updates contact.
        /// </summary>
        /// <param name="contactId">Contact ID</param>
        /// <param name="model">Contact model</param>
        [System.Web.Http.Route("contact/{id}", Name = "UpdateContactExtended")]
        [System.Web.Http.HttpPut]
        [AuthorizePermission(Permissions.GroupName, Permissions.Write)]
        [ModelValidation]
        public virtual IHttpActionResult PutContact(Guid id, [FromBody] ContactModelExtended model)
        {
            var existingContact = CustomerContext.Current.GetContactById(id);

            if (existingContact == null) return NotFound();

            if (CreateUserIfNotExists(existingContact))
                existingContact.UserId =
                    "String:" + model
                        .Email; // The UserId needs to be set in the format "String:{email}". Else a duplicate CustomerContact will be created later on.

            existingContact.FirstName = model.FirstName;
            existingContact.LastName = model.LastName;
            existingContact.Email = model.Email;

            existingContact.RegistrationSource = model.RegistrationSource;

            if (model.Addresses != null)
            {
                foreach (var address in model.Addresses)
                {
                    CustomerMappings.CreateOrUpdateCustomerAddress(existingContact, address);
                }
            }
            existingContact.UpdateMetaFields(model, new List<string> {"FirstName", "LastName", "Email", "RegistrationSource"});
            existingContact.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        /// <summary>
        ///     Updates organization.
        /// </summary>
        /// <param name="orgId">The organization id.</param>
        /// <param name="model">Organization model</param>
        [System.Web.Http.Route("organization/{id}", Name = "UpdateOrganizationExtended")]
        [System.Web.Http.HttpPut]
        [AuthorizePermission(Permissions.GroupName, Permissions.Write)]
        [ModelValidation]
        public virtual IHttpActionResult PutOrganization(string id, [FromBody] OrganizationModelExtended model)
        {
            var existingOrg = CustomerContext.Current.GetOrganizationById(PrimaryKeyId.Parse(id));
            if (existingOrg == null)
            {
                return NotFound();
            }

            existingOrg.OrgCustomerGroup = model.OrgCustomerGroup;
            existingOrg.OrganizationType = model.OrganizationType;
            existingOrg.UpdateMetaFields(model, new List<string>());
            existingOrg.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        /// <summary>
        ///     Creates contact.
        /// </summary>
        /// <param name="id">User ID (GUID)</param>
        /// <param name="model">Contact model</param>
        /// <returns>Contact</returns>
        [ResponseType(typeof(ContactModelExtended))]
        [System.Web.Http.Route("contact/{id}", Name = "CreateContactExtended")]
        [System.Web.Http.HttpPost]
        [AuthorizePermission(Permissions.GroupName, Permissions.Write)]
        [ModelValidation]
        public virtual IHttpActionResult PostContact(Guid id, [FromBody] ContactModelExtended model)
        {
            var customerContact = CustomerContact.CreateInstance();
            CustomerMappings.CreateContact(customerContact, id, model);
            CreateUserIfNotExists(customerContact);
            model = customerContact.ConvertToContactModel();

            return CreatedAtRoute("GetContact", new {contactId = model.PrimaryKeyId}, model);
        }

        /// <summary>
        ///     Creates organization.
        /// </summary>
        /// <param name="model">Organization model</param>
        /// <returns>Organization</returns>
        [ResponseType(typeof(OrganizationModelExtended))]
        [System.Web.Http.Route("organization", Name = "CreateOrganizationExtended")]
        [System.Web.Http.HttpPost]
        [AuthorizePermission(Permissions.GroupName, Permissions.Write)]
        [ModelValidation]
        public virtual IHttpActionResult PostOrganization([FromBody] OrganizationModelExtended model)
        {
            var newOrganization = Organization.CreateInstance();
            newOrganization.PrimaryKeyId = new PrimaryKeyId(model.PrimaryKeyId);
            newOrganization.OrgCustomerGroup = model.OrgCustomerGroup;
            newOrganization.OrganizationType = model.OrganizationType;
            newOrganization.UpdateMetaFields(model, new List<string>());
            newOrganization.SaveChanges();

            model = newOrganization.ConvertToOrganizationModel();

            return CreatedAtRoute("GetOrganization", new {orgId = model.PrimaryKeyId}, model);
        }

        private bool CreateUserIfNotExists(CustomerContact existingContact)
        {
            if (string.IsNullOrEmpty(existingContact.UserId))
            {
                var users = _userProvider.FindUsersByEmail(existingContact.Email, 0, 100, out var records);
                if (!users.Any())
                {
                    _userProvider.CreateUser(existingContact.Email, CreateRandomPassword(15), existingContact.Email,
                        "Question", "Answer", true, out var status, out var errors);
                    return true;
                }
            }

            return false;
        }

        private string CreateRandomPassword(int passwordLength)
        {
            var chars = new char[passwordLength];
            for (var i = 0; i < passwordLength; i++) chars[i] = AllowedChars[_random.Next(0, AllowedChars.Length)];

            return new string(chars);
        }
    }
}