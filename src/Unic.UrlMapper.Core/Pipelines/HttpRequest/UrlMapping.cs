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
    using Sitecore.Data;
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
            var redirectRootIdSetting = Settings.GetSetting("UrlMapper.RootFolder");
            var redirectItemSetting = Settings.GetSetting("UrlMapper.ItemTemplateId");

            ID redirectRootId;
            if (!ID.TryParse(redirectRootIdSetting, out redirectRootId))
            {
                Sitecore.Diagnostics.Log.Info($"UrlMapper: Failed to parse {nameof(redirectRootIdSetting)} {redirectRootIdSetting}", this);
                return;
            }
            Guid redirectItemTemplateId;
            if (!Guid.TryParse(redirectItemSetting, out redirectItemTemplateId))
            {
                Sitecore.Diagnostics.Log.Info($"UrlMapper: Failed to parse {nameof(redirectRootIdSetting)} {redirectRootIdSetting}", this);
                return;
            }

            var searchUrl = WebUtil.GetFullUrl(WebUtil.GetRawUrl());
            searchUrl = new Uri(searchUrl).ToString().ToLower();

            Sitecore.Diagnostics.Log.Info("UrlMapper: UrlMapping: Search URL: " + searchUrl + ".", this);

            var searchUrlEncode = HttpUtility.UrlPathEncode(WebUtil.GetFullUrl(WebUtil.GetRawUrl()));
            searchUrlEncode = new Uri(searchUrlEncode).ToString().ToLower();

            RedirectUsingContentSearch(redirectRootId, redirectItemTemplateId, searchUrl, searchUrlEncode);
        }

        protected virtual void RedirectUsingContentSearch(ID redirectRootId, Guid redirectItemTemplateId, string searchUrl,
            string searchUrlEncode)
        {
            RedirectResultItem redirectItem = null;
            var indexName = Settings.GetSetting("UrlMapper.IndexName");
            if (string.IsNullOrWhiteSpace(indexName))
            {
                Sitecore.Diagnostics.Log.Info($"UrlMapper: No index specified.", this);
                return;
            }

            try
            {
                using (var context = ContentSearchManager.GetIndex(indexName).CreateSearchContext())
                {
                    var query = context.GetQueryable<RedirectResultItem>()
                        .Filter(resultItem => resultItem.Paths.Contains(redirectRootId))
                        .Filter(resultItem => resultItem.BaseTemplates.Contains(redirectItemTemplateId))
                        .Filter(
                            resultItem =>
                                resultItem.SearchUrlLowerCaseUntokenized == searchUrl ||
                                resultItem.SearchUrlLowerCaseUntokenized == searchUrlEncode)
                        .Filter(resultItem => resultItem.IsLatestVersion);

                    redirectItem = query.FirstOrDefault();
                }
            }
            catch (Exception e)
            {
                Sitecore.Diagnostics.Log.Info($"UrlMapper: Failed to query Index {indexName}", this);
                return;
            }

            if (redirectItem != null)
            {
                var redirectUrl = redirectItem.RedirectUrl;

                var statusCode = redirectItem.IsPermanentRedirect
                    ? HttpStatusCode.MovedPermanently
                    : HttpStatusCode.Redirect;

                HttpContext.Current.Response.StatusCode = (int) statusCode;
                if (statusCode == HttpStatusCode.MovedPermanently)
                {
                    HttpContext.Current.Response.RedirectPermanent(redirectUrl);
                }
                else
                {
                    HttpContext.Current.Response.Redirect(redirectUrl);
                }

                Sitecore.Diagnostics.Log.Info(
                    "UrlMapper: UrlMapping: Redirect " + redirectUrl + " to " + redirectUrl + ".", this);
            }
            else
            {
                Sitecore.Diagnostics.Log.Info($"UrlMapper: No redirect found for {searchUrl}", this);
            }
        }
    }
}
