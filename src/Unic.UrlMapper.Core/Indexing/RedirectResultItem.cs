namespace Unic.UrlMapper.Core.Indexing
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Sitecore.ContentSearch;
    using Sitecore.ContentSearch.Converters;
    using Sitecore.ContentSearch.SearchTypes;

    public class RedirectResultItem : SearchResultItem
    {
        [IndexField("basetemplates")]
        [TypeConverter(typeof(IndexFieldEnumerableConverter))]
        public virtual IEnumerable<Guid> BaseTemplates { get; set; }

        [IndexField("_latestversion")]
        public bool IsLatestVersion { get; set; }

        [IndexField("search_url")]
        public string SearchUrl { get; set; }

        [IndexField("redirect_url")]
        public string RedirectUrl { get; set; }
    }
}
