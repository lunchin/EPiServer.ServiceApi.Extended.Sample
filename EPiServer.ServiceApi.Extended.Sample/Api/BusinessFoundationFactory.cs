using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Logging;
using Mediachase.BusinessFoundation.Data.Business;
using Mediachase.BusinessFoundation.Data.Meta;
using Mediachase.BusinessFoundation.Data.Meta.Management;

namespace EPiServer.ServiceApi.Extended.Sample.Api
{
    public static class BusinessFoundationFactory
    {
        private static readonly ILogger _log = LogManager.GetLogger(typeof(BusinessFoundationFactory));

        public static bool IsComplexType(McDataType type)
        {
            return !(type == McDataType.Currency
                     || type == McDataType.DateTime
                     || type == McDataType.Decimal
                     || type == McDataType.Double
                     || type == McDataType.Guid
                     || type == McDataType.Integer
                     || type == McDataType.String);
        }

        public static bool IsElementaryType(McDataType type)
        {
            return !IsComplexType(type);
        }

        public static object GetDefaultValue(MetaField field)
        {
            return DefaultValue.Evaluate(field);
        }

        public static object ParseSimpleMetaFieldValue(MetaField metaField, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            var metaType = metaField.GetMetaType();
            return IsElementaryType(metaType.McDataType) ? PropertyValueUtil.ConvertToObject(metaField, value) : null;
        }

        public static string GetMetaFieldSerializationValue(MetaField metaField, object value)
        {
            if (value == null)
            {
                return null;
            }

            var metaType = metaField.GetMetaType();
            if (metaType.McDataType != McDataType.Enum)
            {
                return PropertyValueUtil.ConvertToString(metaField, value);
            }

            if (value is MetaEnumItem enumSingleValue)
            {
                return enumSingleValue.Name;
            }
            return !(value is MetaEnumItem[] enumMultiValue)
                ? null
                : string.Join("|", enumMultiValue.Select(item => item.Name));

        }

        public static DateTime ParseToUtc(string stringValue)
        {
            return ParseValueToUtc(DateTime.Parse(stringValue));
        }

        public static List<string> GetValues(MetaField metaField, EntityObject metaObject)
        {
            var data = new List<string>();

            if (metaObject == null) return data;

            var value = GetMetaFieldSerializationValue(metaField, metaObject.Properties[metaField.Name]?.Value);
            if (string.IsNullOrEmpty(value)) return data;

            data.Add(PropertyValueUtil.ConvertToString(metaField, value));

            return data;
        }

        public static object GetMetaFieldValue(MetaField mf, IEnumerable<string> dataCollection)
        {
            var data = string.Join("|", dataCollection);
            if (string.IsNullOrEmpty(data)) return null;

            object value = null;
            try
            {
                MetaEnumItem item = null;
                var metaType = mf.GetMetaType();
                switch (metaType.McDataType)
                {
                    case McDataType.Enum:
                        var dicFieldValues = new List<MetaEnumItem>();
                        var items = data.Split('|');
                        var metaItems = MetaEnum.GetItems(metaType);
                        foreach (var val in items)
                        {
                            item = metaItems.FirstOrDefault(i =>
                                string.Equals(i.Name, val, StringComparison.OrdinalIgnoreCase));
                            if (item != null)
                            {
                                dicFieldValues.Add(item);
                            }
                            else
                            {
                                if (string.IsNullOrEmpty(val))
                                {
                                    continue;
                                }
                                var newId = MetaEnum.AddItem(metaType, val, 0);
                                if (newId <= 0 || newId > metaItems.Length - 1)
                                {
                                    continue;
                                }
                                item = MetaEnum.GetItems(metaType).FirstOrDefault(x => x.Handle == newId);
                                if (item != null)
                                {
                                    dicFieldValues.Add(item);
                                }
                            }
                        }

                        if (dicFieldValues.Count > 0)
                        {
                            value = dicFieldValues.ToArray();
                        }
                        break;
                    default:
                        value = ParseSimpleMetaFieldValue(mf, data);
                        break;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return value;
        }

        private static DateTime ParseValueToUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) :
                value.Kind == DateTimeKind.Local ? value.ToUniversalTime() :
                value;
        }
    }

    public static class McDataTypeExtensions
    {
        public static bool IsDateTimeType(this McDataType dataType)
        {
            return dataType == McDataType.DateTime;
        }
    }
}