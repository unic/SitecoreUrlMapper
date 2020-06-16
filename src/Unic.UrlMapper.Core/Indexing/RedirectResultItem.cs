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
        [IndexField("_basetemplates")]
        [TypeConverter(typeof(IndexFieldEnumerableConverter))]
        public virtual IEnumerable<Guid> BaseTemplates { get; set; }

        [IndexField("_latestversion")]
        public bool IsLatestVersion { get; set; }

        [IndexField("search_url_lowercase_untokenized")]
        public string SearchUrlLowerCaseUntokenized { get; set; }

        [IndexField("redirect_url_lowercase_untokenized")]
        public string RedirectUrlLowerCaseUntokenized { get; set; }

        [IndexField("permanent_redirect")]
        public bool IsPermanentRedirect { get; set; }

        [IndexField("match_start")]
        public bool MatchStart { get; set; }

        [IndexField("ignore_suffix")]
        public bool IgnoreSuffix { get; set; }
    }
}
