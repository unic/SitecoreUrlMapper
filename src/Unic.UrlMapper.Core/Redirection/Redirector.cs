using System;
using System.Linq;
using System.Net;
using System.Web;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq;
using Sitecore.Data;
using Sitecore.Web;
using Unic.UrlMapper.Core.Indexing;

namespace Unic.UrlMapper.Core.Redirection
{
    [UsedImplicitly]
    public class Redirector
    {
        public virtual void CheckAndPerformRedirect()
        {
            var rawUrl = HttpContext.Current.Request.RawUrl;

            var requestUri = rawUrl.Split('?')[0];

            var filePath = HttpContext.Current.Request.FilePath;
            if (string.IsNullOrEmpty(filePath) || WebUtil.IsExternalUrl(filePath) || (filePath == requestUri && System.IO.File.Exists(HttpContext.Current.Server.MapPath(filePath))))
            {
                return;
            }

            // load configuration values
            var redirectRootIdSetting = Settings.GetSetting("UrlMapper.RootFolder");
            var redirectItemSetting = Settings.GetSetting("UrlMapper.ItemTemplateId");

            if (!ID.TryParse(redirectRootIdSetting, out var redirectRootId))
            {
                Sitecore.Diagnostics.Log.Info($"UrlMapper: Failed to parse {nameof(redirectRootIdSetting)} {redirectRootIdSetting}", this);
                return;
            }

            if (!ID.TryParse(redirectItemSetting, out var redirectItemTemplateId))
            {
                Sitecore.Diagnostics.Log.Info($"UrlMapper: Failed to parse {nameof(redirectRootIdSetting)} {redirectRootIdSetting}", this);
                return;
            }

            var searchUrl = WebUtil.GetFullUrl(rawUrl);
            searchUrl = new Uri(searchUrl).ToString().ToLower();

            Sitecore.Diagnostics.Log.Info("UrlMapper: Search URL: " + searchUrl + ".", this);

            var searchUrlEncode = HttpUtility.UrlPathEncode(WebUtil.GetFullUrl(WebUtil.GetRawUrl()));
            searchUrlEncode = new Uri(searchUrlEncode).ToString().ToLower();

            this.RedirectUsingContentSearch(redirectRootId, redirectItemTemplateId, searchUrl, searchUrlEncode);
        }

        protected virtual void RedirectUsingContentSearch(ID redirectRootId, ID redirectItemTemplateId, string searchUrl,
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
                        .Filter(resultItem => resultItem.BaseTemplates.Contains(redirectItemTemplateId.Guid) || resultItem.TemplateId == redirectItemTemplateId)
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
                var redirectUrl = redirectItem.RedirectUrlLowerCaseUntokenized;
                if (string.IsNullOrWhiteSpace(redirectUrl))
                {
                    Sitecore.Diagnostics.Log.Error(
                        $"UrlMapper: Redirect from {searchUrl} will be aborted since the target url is empty.", this);
                    return;
                }

                var statusCode = redirectItem.IsPermanentRedirect
                    ? HttpStatusCode.MovedPermanently
                    : HttpStatusCode.Redirect;

                HttpContext.Current.Response.StatusCode = (int)statusCode;
                Sitecore.Diagnostics.Log.Info(
                    $"UrlMapper: Redirect {searchUrl} to {redirectUrl} (HTTP {HttpContext.Current.Response.StatusCode}).", this);

                if (statusCode == HttpStatusCode.MovedPermanently)
                {
                    HttpContext.Current.Response.RedirectPermanent(redirectUrl);
                }
                else
                {
                    HttpContext.Current.Response.Redirect(redirectUrl);
                }
            }
            else
            {
                Sitecore.Diagnostics.Log.Info($"UrlMapper: No redirect found for {searchUrl}", this);
            }
        }
    }
}
