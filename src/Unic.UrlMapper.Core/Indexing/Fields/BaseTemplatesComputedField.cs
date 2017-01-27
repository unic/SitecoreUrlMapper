namespace Unic.UrlMapper.Core.Indexing.Fields
{
    using Sitecore.ContentSearch;
    using Sitecore.ContentSearch.ComputedFields;
    using Sitecore.Data.Items;
    using Unic.Framework.Core.Items.Extensions;

    public class BaseTemplatesComputedField : AbstractComputedIndexField
    {
        public override object ComputeFieldValue(IIndexable indexable)
        {
            var item = (Item)(indexable as SitecoreIndexableItem);

            return item?.GetBaseTemplatesIds();
        }
    }
}
