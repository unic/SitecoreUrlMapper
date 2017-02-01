namespace Unic.UrlMapper.Core.Indexing.Fields
{
    using Sitecore.ContentSearch;
    using Sitecore.ContentSearch.ComputedFields;
    using Sitecore.Data.Items;

    public class SearchUrlLowerCaseUntokenizedField : AbstractComputedIndexField
    {
        private const string searchUrlFieldName = "search url";

        public override object ComputeFieldValue(IIndexable indexable)
        {
            var item = (Item) (indexable as SitecoreIndexableItem);

            var res = item?[searchUrlFieldName]?.ToLowerInvariant();
            return string.IsNullOrWhiteSpace(res) ? null : res;
        }
    }
}
