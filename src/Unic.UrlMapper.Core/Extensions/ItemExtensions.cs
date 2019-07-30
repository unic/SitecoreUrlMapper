using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Unic.UrlMapper.Core.Extensions
{
    public static class ItemExtensions
    {
        public static IEnumerable<Guid> GetBaseTemplatesIds(this Item item)
        {
            if (item == null)
            {
                return Enumerable.Empty<Guid>();
            }

            var baseTemplates = new List<Guid>();
            var itemTemplateId = item.TemplateID;

            if (itemTemplateId != (ID)null)
            {
                baseTemplates.Add(itemTemplateId.ToGuid());
            }

            var stack = new Stack<TemplateItem>();

            // push each base template from the current template to the stack
            var itemTemplate = item.Template;

            if (itemTemplate == null)
            {
                return baseTemplates;
            }

            foreach (
                var template in
                itemTemplate.BaseTemplates.Where(template => template.ID != TemplateIDs.StandardTemplate))
            {
                stack.Push(template);
            }

            // process each template
            while (stack.Count > 0)
            {
                var templatesToProcess = ProcessAndGetBaseTemplates(stack.Pop(), ref baseTemplates);

                // push all the base templates again to the stack to process them as well
                foreach (var t in templatesToProcess.Where(template => template.ID != TemplateIDs.StandardTemplate))
                {
                    stack.Push(t);
                }
            }

            return baseTemplates.Distinct();
        }

        private static IEnumerable<TemplateItem> ProcessAndGetBaseTemplates(TemplateItem templateItem, ref List<Guid> baseTemplates)
        {
            if (templateItem == null)
            {
                return new TemplateItem[0];
            }

            baseTemplates.Add(templateItem.ID.ToGuid());
            return templateItem.BaseTemplates;
        }
    }
}
