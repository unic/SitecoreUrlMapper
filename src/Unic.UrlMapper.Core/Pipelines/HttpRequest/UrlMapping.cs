namespace Unic.UrlMapper.Core.Pipelines.HttpRequest
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Web;
    using Sitecore.Configuration;
    using Sitecore.Pipelines.HttpRequest;
    using Sitecore.Web;
    using Sitecore.ContentSearch;
    using Sitecore.ContentSearch.Linq;
    using Unic.UrlMapper.Core.Indexing;

    /// <summary>
    /// This class is called after the item resolver and maps old urls to new one.
    /// If a page is not found (got a 404) the class searches for available redirects under /sitecore/content/configuration/redirecs
    /// and make a permanent redirect to the new url if the old url was found. The root folder of the redirects and the template id
    /// shoudl be configured in the web.config.
    /// <br/><br/>
    /// This pipeline extension should be configure in the included file as follow:
    /// 
    /// <code>
    /// &lt;pipelines&gt;
    ///	    &lt;httpRequestBegin&gt;
    ///		    &lt;processor patch:after="processor[@type='Sitecore.Pipelines.HttpRequest.ItemResolver, Sitecore.Kernel']"
    ///		        type="Unic.UrlMapper.Core.Pipelines.HttpRequest.UrlMappinunicg, Unic.SitecoreCMS.Modules.UrlMapper" /&gt;
    ///     &lt;/httpRequestBegin&gt;
    /// &lt;/pipelines&gt;
    /// </code>
    /// </summary>
    public class UrlMapping : HttpRequestProcessor
    {
        /// <summary>
        /// Check if there is matching redirect item available under the configured root path and then make a permanent redirect
        /// to the new url.
        /// </summary>
        /// <param name="args">current httprequest arguments</param>
        public override void Process(HttpRequestArgs args)
        {
            var rawUrl = WebUtil.GetRawUrl();
            if (Sitecore.Context.Item != null || Sitecore.Context.Site == null || Sitecore.Context.Database == null || rawUrl == null)
            {
                return;
            }

            var requestUri = rawUrl.Split(new[] { '?' })[0];

            var filePath = Sitecore.Context.Request.FilePath.ToLower();
            if (string.IsNullOrEmpty(filePath) || WebUtil.IsExternalUrl(filePath) || (filePath == requestUri && System.IO.File.Exists(HttpContext.Current.Server.MapPath(filePath))))
            {
                return;
            }

            // load configuration values
            var redirectItemTemplateId = Settings.GetSetting("UrlMapper.ItemTemplateId");

            var searchUrl = WebUtil.GetFullUrl(WebUtil.GetRawUrl());
            searchUrl = new Uri(searchUrl).ToString().ToLower();

            Sitecore.Diagnostics.Log.Info("UrlMapper: UrlMapping: Search URL: " + searchUrl + ".", this);

            var searchUrlEncode = HttpUtility.UrlPathEncode(WebUtil.GetFullUrl(WebUtil.GetRawUrl()));
            searchUrlEncode = new Uri(searchUrlEncode).ToString().ToLower();

            RedirectUsingContentSearch(Guid.Parse(redirectItemTemplateId), searchUrl, searchUrlEncode);
        }

        protected virtual void RedirectUsingContentSearch(Guid redirectItemTemplateId, string searchUrl,
            string searchUrlEncode)
        {
            RedirectResultItem redirectItem = null;
            var indexName = Settings.GetSetting("UrlMapper.IndexName");

            using (var context = ContentSearchManager.GetIndex(indexName).CreateSearchContext())
            {
                var query = context.GetQueryable<RedirectResultItem>()
                    .Filter(resultItem => resultItem.BaseTemplates.Contains(redirectItemTemplateId))
                    .Filter(resultItem => resultItem.SearchUrlLowerCaseUntokenized == searchUrl || resultItem.SearchUrlLowerCaseUntokenized == searchUrlEncode)
                    .Filter(resultItem => resultItem.IsLatestVersion);

                redirectItem = query.FirstOrDefault();
            }

            if (redirectItem != null)
            {
                var redirectUrl = redirectItem.RedirectUrl;
                HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.MovedPermanently;
                HttpContext.Current.Response.RedirectPermanent(redirectUrl);

                Sitecore.Diagnostics.Log.Info("UrlMapper: UrlMapping: Redirect " + redirectUrl + " to " + redirectUrl + ".", this);
            }
        }
    }
}
