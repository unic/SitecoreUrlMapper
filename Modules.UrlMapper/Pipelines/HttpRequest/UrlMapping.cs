using System;
using Sitecore.Pipelines.HttpRequest;
using System.Net;
using System.Web;
using Sitecore.Data.Items;
using Sitecore.Configuration;

namespace Unic.SitecoreCMS.Modules.UrlMapper.Pipelines.HttpRequest
{
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
    ///		        type="Unic.SitecoreCMS.Modules.UrlMapper.Pipelines.HttpRequest.UrlMapping, Unic.SitecoreCMS.Modules.UrlMapper" /&gt;
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
            
            if (Sitecore.Context.Item != null || Sitecore.Context.Site == null || Sitecore.Context.Database == null)
            {
                return;
            }
            
            string filePath = Sitecore.Context.Request.FilePath.ToLower();
            if (String.IsNullOrEmpty(filePath) || Sitecore.Web.WebUtil.IsExternalUrl(filePath) || System.IO.File.Exists(HttpContext.Current.Server.MapPath(filePath)))
            {
                return;
            }


            // load configuration values
            string redirectItemTemplateId = Settings.GetSetting("UrlMapper.ItemTemplateId");
            string redirectRootId = Settings.GetSetting("UrlMapper.RootFolder");

            string searchURL = Sitecore.Web.WebUtil.GetFullUrl(Sitecore.Web.WebUtil.GetRawUrl());
            Sitecore.Diagnostics.Log.Info("UrlMapper: UrlMapping: Search URL: " + searchURL + ".", this);
           
            string searchUrlEncode = HttpUtility.UrlPathEncode(Sitecore.Web.WebUtil.GetFullUrl(Sitecore.Web.WebUtil.GetRawUrl()));
            string query = "fast://*[@@id='" + redirectRootId + "']//*[@@templateid='" + redirectItemTemplateId + "' and (@#Search URL# = '" + searchURL + "' or @#Search URL# = '" + searchUrlEncode + "')]";

            // HACK: replacement for Sitecore.Context.Database.SelectSingleItem(query); as the context database
            // never switched to web on delivery environments.
            string contextdb = Sitecore.Context.Database.ConnectionStringName;
            Item redirect = Sitecore.Configuration.Factory.GetDatabase(contextdb).SelectSingleItem(query);

            if (redirect != null)
            {
                string redirectURL = redirect["Redirect URL"];
                System.Web.HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.MovedPermanently;
                System.Web.HttpContext.Current.Response.RedirectPermanent(redirectURL);

                Sitecore.Diagnostics.Log.Info("UrlMapper: UrlMapping: Redirect " + searchURL + " to " + redirectURL + ".", this);
            }

        }
    }
}
