namespace Unic.UrlMapper.Core.Pipelines
{
    using System.Collections.Generic;
    using System.Linq;
    using Sitecore;
    using Sitecore.Diagnostics;
    using Sitecore.Sites;

    /// <summary>
    ///     This is a base processor for other conditional processors. Before starting the "Execute()" method of the processor,
    ///     it calls the "ShouldExecute()" method, which can be overriden by a custom processor. By default it's possible
    ///     to restrict sites in which a processor should be executed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ProcessorBase<T>
    {
        protected List<string> AllowedSites { get; } = new List<string>();

        protected List<string> RestrictedSites { get; } = new List<string>();

        public void Process(T args)
        {
            if (ShouldExecute(args))
                Execute(args);
        }

        protected virtual bool ShouldExecute(T args)
        {
            // if we don't have a site context, we generally don't want 
            // custom processors to run. If your processor needs to make
            // an exception, feel free to do so by overriding this method
            var siteContext = Context.Site;

            // check if we have any allowed sites
            return IsSiteAllowed(siteContext, AllowedSites, RestrictedSites);
        }

        public bool IsSiteAllowed(SiteContext siteContext, IList<string> allowedSites, IList<string> restrictedSites)
        {
            Assert.ArgumentNotNull(allowedSites, nameof(allowedSites));
            Assert.ArgumentNotNull(restrictedSites, nameof(restrictedSites));

            if (siteContext == null)
                return true;

            var siteInheritanceList = GetSiteInheritanceList(siteContext).ToList();

            // Check whether execution should prevented because of the blacklist
            if (restrictedSites.Any(siteInheritanceList.Contains))
                return false;

            // Check whether execution should prevented because of the whitelist
            return !allowedSites.Any() || allowedSites.Any(siteInheritanceList.Contains);
        }

        private static IEnumerable<string> GetSiteInheritanceList(SiteContext siteContext)
        {
            yield return siteContext.Name;

            var tenant = siteContext.Properties["tenant"];
            if (string.IsNullOrWhiteSpace(tenant)) yield break;

            yield return tenant;
        }

        protected abstract void Execute(T args);
    }
}