namespace Unic.UrlMapper.Core.Indexing.Fields
{
    using Sitecore.ContentSearch;
    using Sitecore.ContentSearch.ComputedFields;
    using Sitecore.Data.Items;

    public class SearchUrlLowerCaseUntokenizedField : IComputedIndexField
    {
        private const string searchUrlFieldName = "search url";

        public object ComputeFieldValue(IIndexable indexable)
        {
            var item = (Item) (indexable as SitecoreIndexableItem);

            return item?[searchUrlFieldName].ToLowerInvariant();
        }

        public string FieldName { get; set; }
        public string ReturnType { get; set; }
    }
}
