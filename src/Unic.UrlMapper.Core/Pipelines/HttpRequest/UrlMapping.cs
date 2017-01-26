namespace Unic.UrlMapper.Core.Pipelines.HttpRequest
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Web;
    using Sitecore.Configuration;
    using Sitecore.Data.Items;
    using Sitecore.Pipelines.HttpRequest;
    using Sitecore.Web;
    using Security.Filter;
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
    ///		        type="Unic.UrlMapper.Core.Pipelines.HttpRequest.UrlMapping, Unic.SitecoreCMS.Modules.UrlMapper" /&gt;
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
            string rawUrl = WebUtil.GetRawUrl();
            if (Sitecore.Context.Item != null || Sitecore.Context.Site == null || Sitecore.Context.Database == null || rawUrl == null)
            {
                return;
            }

            string requestUri = rawUrl.Split(new[] { '?' })[0];

            string filePath = Sitecore.Context.Request.FilePath.ToLower();
            if (string.IsNullOrEmpty(filePath) || WebUtil.IsExternalUrl(filePath) || (filePath == requestUri && System.IO.File.Exists(HttpContext.Current.Server.MapPath(filePath))))
            {
                return;
            }


            // load configuration values
            string redirectItemTemplateId = Settings.GetSetting("UrlMapper.ItemTemplateId");
            string redirectRootId = Settings.GetSetting("UrlMapper.RootFolder");

            FastQueryFilter filter = new FastQueryFilter();

            string searchURL = WebUtil.GetFullUrl(WebUtil.GetRawUrl());
            searchURL = new Uri(searchURL).ToString();
            searchURL = filter.Filter(searchURL);

            Sitecore.Diagnostics.Log.Info("UrlMapper: UrlMapping: Search URL: " + searchURL + ".", this);

            string searchUrlEncode = HttpUtility.UrlPathEncode(WebUtil.GetFullUrl(WebUtil.GetRawUrl()));
            searchUrlEncode = filter.Filter(searchUrlEncode);
            searchUrlEncode = new Uri(searchUrlEncode).ToString();

            RedirectUsingContentSearch(redirectRootId, new ID(redirectItemTemplateId), searchURL?.ToLower(), searchUrlEncode?.ToLower());
        }

        private void RedirectUsingFastQuery(string redirectRootId, string redirectItemTemplateId, string searchURL,
            string searchUrlEncode)
        {
            string query = "fast://*[@@id='" + redirectRootId + "']//*[@@templateid='" + redirectItemTemplateId +
                           "' and (@#Search URL# = '" + searchURL + "' or @#Search URL# = '" + searchUrlEncode + "')]";

            // HACK: replacement for Sitecore.Context.Database.SelectSingleItem(query); as the context database
            // never switched to web on delivery environments.
            string contextdb = Sitecore.Context.Database.ConnectionStringName;
            Item redirect = Factory.GetDatabase(contextdb).SelectSingleItem(query);

            if (redirect != null)
            {
                string redirectURL = redirect["Redirect URL"];
                HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.MovedPermanently;
                HttpContext.Current.Response.RedirectPermanent(redirectURL);

                Sitecore.Diagnostics.Log.Info("UrlMapper: UrlMapping: Redirect " + searchURL + " to " + redirectURL + ".", this);
            }
        }

        private void RedirectUsingContentSearch(string redirectRootId, ID redirectItemTemplateId, string searchUrl,
            string searchUrlEncode)
        {
            var rootFolder = Sitecore.Context.Database.GetItem(redirectRootId);
            if (rootFolder == null) return;

            RedirectResultItem redirectItem = null;
            string debug = "unic_urlmapper_master";

            using (var context = ContentSearchManager.GetIndex(debug).CreateSearchContext())
            {
                var query = context.GetQueryable<RedirectResultItem>()
                    .Filter(resultItem => resultItem.TemplateId == redirectItemTemplateId)
                    .Filter(resultItem => resultItem.SearchUrlLowerCaseUntokenized == searchUrl || resultItem.SearchUrlLowerCaseUntokenized == searchUrlEncode)
                    .Filter(resultItem => resultItem.IsLatestVersion);

                redirectItem = query.FirstOrDefault();
            }

            if (redirectItem != null)
            {
                var redirectUrl = redirectItem.GetItem().Fields["Redirect URL"].Value;
                HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.MovedPermanently;
                HttpContext.Current.Response.RedirectPermanent(redirectUrl);

                Sitecore.Diagnostics.Log.Info("UrlMapper: UrlMapping: Redirect " + redirectUrl + " to " + redirectUrl + ".", this);
            }
        }
    }
}
