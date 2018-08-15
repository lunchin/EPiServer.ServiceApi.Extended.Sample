using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.ServiceApi.Commerce.Models.Order;
using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Business;
using Mediachase.BusinessFoundation.Data.Meta.Management;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;

namespace EPiServer.ServiceApi.Extended.Sample.Api
{
    internal static class CustomerMappings
    {
        public static ContactModelExtended ConvertToContactModel(this CustomerContact customerContact)
        {
            if (customerContact == null)
            {
                return null;
            }

            return new ContactModelExtended
            {
                PrimaryKeyId = customerContact.PrimaryKeyId.Value,
                Addresses = customerContact.ContactAddresses.Select(a => a.ConvertToAddressModel()).ToList(),
                FirstName = customerContact.FirstName,
                LastName = customerContact.LastName,
                Email = customerContact.Email,
                RegistrationSource = customerContact.RegistrationSource,
                MetaFields =
                    customerContact.GetMetaFields(new List<string>
                    {
                        "FirstName",
                        "LastName",
                        "Email",
                        "RegistrationSource"
                    })
            };
        }

        public static OrganizationModelExtended ConvertToOrganizationModel(this Organization organization)
        {
            if (organization == null) return null;

            var organizationModel = new OrganizationModelExtended
            {
                OrganizationType = organization.OrganizationType,
                OrgCustomerGroup = organization.OrgCustomerGroup,
                Contacts = organization.Contacts.Select(c => c.ConvertToContactModel()).ToList(),
                Addresses = organization.Addresses.Select(a => a.ConvertToAddressModel()).ToList(),
                ChildOrganizations =
                    organization.ChildOrganizations.Select(o => o.ConvertToOrganizationModel()).ToList(),
                MetaFields = organization.GetMetaFields(new List<string>())
            };

            if (organization.PrimaryKeyId.HasValue) organizationModel.PrimaryKeyId = organization.PrimaryKeyId.Value;

            return organizationModel;
        }


        public static void CreateContact(CustomerContact customerContact, Guid userId,
            ContactModelExtended contactModel)
        {
            customerContact.PrimaryKeyId = new PrimaryKeyId(userId);
            customerContact.FirstName = contactModel.FirstName;
            customerContact.LastName = contactModel.LastName;
            customerContact.Email = contactModel.Email;
            customerContact.UserId =
                "String:" + contactModel
                    .Email; // The UserId needs to be set in the format "String:{email}". Else a duplicate CustomerContact will be created later on.
            customerContact.RegistrationSource = contactModel.RegistrationSource;

            if (contactModel.Addresses != null)
            {
                foreach (var address in contactModel.Addresses)
                {
                    customerContact.AddContactAddress(
                        address.ConvertToCustomerAddress(CustomerAddress.CreateInstance()));
                }
            }

            UpdateMetaFields(customerContact, contactModel,
                new List<string> {"FirstName", "LastName", "Email", "RegistrationSource"});

            // The contact, or more likely its related addresses, must be saved to the database before we can set the preferred
            // shipping and billing addresses. Using an address id before its saved will throw an exception because its value
            // will still be null.
            customerContact.SaveChanges();

            // Once the contact has been saved we can look for any existing addresses.
            var defaultAddress = customerContact.ContactAddresses.FirstOrDefault();
            if (defaultAddress != null)
            {
                // If an addresses was found, it will be used as default for shipping and billing.
                customerContact.PreferredShippingAddress = defaultAddress;
                customerContact.PreferredBillingAddress = defaultAddress;

                // Save the address preferences also.
                customerContact.SaveChanges();
            }
        }

        public static CustomerAddress CreateOrUpdateCustomerAddress(CustomerContact contact, AddressModel addressModel)
        {
            var customerAddress = GetAddress(contact, addressModel.AddressId);
            var isNew = customerAddress == null;
            IEnumerable<PrimaryKeyId> existingId = contact.ContactAddresses.Select(a => a.AddressId).ToList();
            if (isNew)
            {
                customerAddress = CustomerAddress.CreateInstance();
            }

            customerAddress = addressModel.ConvertToCustomerAddress(customerAddress);

            if (isNew)
            {
                contact.AddContactAddress(customerAddress);
            }
            else
            {
                contact.UpdateContactAddress(customerAddress);
            }

            contact.SaveChanges();
            if (isNew)
            {
                customerAddress.AddressId = contact.ContactAddresses
                    .Where(a => !existingId.Contains(a.AddressId))
                    .Select(a => a.AddressId)
                    .Single();
                addressModel.AddressId = customerAddress.AddressId;
            }

            return customerAddress;
        }

        public static List<ExtendedMetaFieldProperty> GetMetaFields(this EntityObject entity, List<string> ignoreFields)
        {
            var metaFields = new List<ExtendedMetaFieldProperty>();
            var metaClass = DataContext.Current.MetaModel.MetaClasses.Cast<MetaClass>()
                .FirstOrDefault(x => x.Name.Equals(entity.MetaClassName));
            if (metaClass == null)
            {
                return metaFields;
            }

            foreach (var property in entity.Properties.Where(x => !ignoreFields.Contains(x.Name)))
            {
                var metaField = metaClass.Fields.Cast<MetaField>().FirstOrDefault(x => x.Name.Equals(property.Name));
                if (metaField == null)
                {
                    continue;
                }

                var value = BusinessFoundationFactory.GetMetaFieldSerializationValue(metaField, property.Value);
                if (value == null)
                {
                    continue;
                }

                metaFields.Add(new ExtendedMetaFieldProperty
                {
                    Name = property.Name,
                    Type = metaField.TypeName,
                    Values = new List<string> {value}
                });
            }

            return metaFields;
        }

        public static void UpdateMetaFields(this EntityObject entity, ExtendedBaseModel model,
            List<string> ignoreFields)
        {
            var metaClass = DataContext.Current.MetaModel.MetaClasses.Cast<MetaClass>()
                .FirstOrDefault(x => x.Name.Equals(entity.MetaClassName));
            if (metaClass == null)
            {
                return;
            }
            foreach (var property in entity.Properties.Where(x => !ignoreFields.Contains(x.Name)))
            {
                var metaField = metaClass.Fields.Cast<MetaField>().FirstOrDefault(x => x.Name.Equals(property.Name));
                if (metaField == null)
                {
                    continue;
                }

                var modelProperty = model.MetaFields.FirstOrDefault(x => x.Name.Equals(property.Name));
                if (modelProperty?.Values == null)
                {
                    continue;
                }

                var value = BusinessFoundationFactory.GetMetaFieldValue(metaField, modelProperty.Values);
                if (value == null)
                {
                    continue;
                }

                property.Value = value;
            }
        }

        private static CustomerAddress ConvertToCustomerAddress(this AddressModel addressModel,
            CustomerAddress customerAddress)
        {
            customerAddress.Name = addressModel.Name;
            customerAddress.City = addressModel.City;
            customerAddress.CountryCode = addressModel.CountryCode;
            customerAddress.CountryName = GetAllCountries().Where(x => x.Code == addressModel.CountryCode)
                .Select(x => x.Name).FirstOrDefault();
            customerAddress.FirstName = addressModel.FirstName;
            customerAddress.LastName = addressModel.LastName;
            customerAddress.Line1 = addressModel.Line1;
            customerAddress.Line2 = addressModel.Line2;
            customerAddress.DaytimePhoneNumber = addressModel.DaytimePhoneNumber;
            customerAddress.EveningPhoneNumber = addressModel.EveningPhoneNumber;
            customerAddress.PostalCode = addressModel.PostalCode;
            customerAddress.RegionName = addressModel.RegionName;
            customerAddress.RegionCode = addressModel.RegionCode;
            // Commerce Manager expects State to be set for addresses in order management. Set it to be same as
            // RegionName to avoid issues.
            customerAddress.State = addressModel.RegionName;
            customerAddress.Email = addressModel.Email;
            customerAddress.AddressType = CustomerAddressTypeEnum.Public |
                                          (addressModel.ShippingDefault ? CustomerAddressTypeEnum.Shipping : 0) |
                                          (addressModel.BillingDefault ? CustomerAddressTypeEnum.Billing : 0);

            return customerAddress;
        }

        private static AddressModel ConvertToAddressModel(this CustomerAddress address)
        {
            if (address == null) return null;

            return new AddressModel
            {
                Name = address.Name,
                City = address.City,
                CountryCode = address.CountryCode,
                CountryName = address.CountryName,
                FirstName = address.FirstName,
                LastName = address.LastName,
                Line1 = address.Line1,
                Line2 = address.Line2,
                DaytimePhoneNumber = address.DaytimePhoneNumber,
                EveningPhoneNumber = address.EveningPhoneNumber,
                PostalCode = address.PostalCode,
                RegionName = address.RegionName,
                RegionCode = address.RegionCode,
                Email = address.Email,
                AddressId = address.AddressId,
                ShippingDefault = address.AddressType.HasFlag(CustomerAddressTypeEnum.Shipping),
                BillingDefault = address.AddressType.HasFlag(CustomerAddressTypeEnum.Billing),
                Modified = address.Modified
            };
        }

        private static List<CountryDto.CountryRow> GetAllCountries()
        {
            return CountryManager.GetCountries().Country.ToList();
        }

        private static CustomerAddress GetAddress(CustomerContact contact, Guid? addressId)
        {
            return addressId.HasValue
                ? contact.ContactAddresses.FirstOrDefault(x => x.AddressId == addressId.GetValueOrDefault())
                : null;
        }
    }
}