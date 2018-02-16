using Sitecore.ContentSearch;

namespace Unic.UrlMapper.Core.Indexing.Fields
{
    using System;
    using Sitecore.Configuration;
    using Sitecore.ContentSearch.ComputedFields;
    using Sitecore.Data.Items;

    public abstract class LowerCaseUrlFieldBase : AbstractComputedIndexField
    {
        protected abstract string fieldName { get; set; }

        private const string domainAuthorSettingName = "UrlMapper.Domain.Author";
        private const string domainDeliverySettingName = "UrlMapper.Domain.Delivery";
        private const string domainToken = "{domain}";
        private const string webDbName = "web";

        public override object ComputeFieldValue(IIndexable indexable)
        {
            var item = (Item)(indexable as SitecoreIndexableItem);

            var url = item?[fieldName]?.ToLowerInvariant();
            url = this.ReplaceTokens(url, item);

            return string.IsNullOrWhiteSpace(url) ? null : url;
        }

        protected virtual string ReplaceTokens(string url, Item item)
        {
            return this.ReplaceDomain(url, item);
        }

        protected virtual string ReplaceDomain(string url, Item item)
        {
            var settingName = item.Database.Name.Equals(webDbName, StringComparison.OrdinalIgnoreCase)
                ? domainDeliverySettingName
                : domainAuthorSettingName;

            var domain = Settings.GetSetting(settingName, null);

            return url?.Replace(domainToken, domain);
        }
    }
}
