using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Web;
using Unic.UrlMapper.Core.Indexing;

namespace Unic.UrlMapper.Core.Redirection
{
    [UsedImplicitly]
    public class Redirector
    {
        public virtual void CheckAndPerformRedirect()
        {
            if (Context.Item != null || Context.Site == null || Context.Database == null) return;

            var httpContext = HttpContext.Current;
            var rawUrl = httpContext.Request.RawUrl;

            if (HasPerformedCheckDuringRequest(httpContext))
            {
                Log.Debug(
                    $"UrlMapper: Check for {rawUrl} has already been performed during request. Subsequent check will be skipped",
                    this);
                return;
            }

            var requestUri = rawUrl.Split('?')[0];

            var filePath = httpContext.Request.FilePath;
            if (string.IsNullOrEmpty(filePath) || WebUtil.IsExternalUrl(filePath) ||
                filePath == requestUri && File.Exists(httpContext.Server.MapPath(filePath)))
            {
                return;
            }

            // load configuration values
            var redirectRootIdSetting = Settings.GetSetting("UrlMapper.RootFolder");
            var redirectItemSetting = Settings.GetSetting("UrlMapper.ItemTemplateId");

            if (!ID.TryParse(redirectRootIdSetting, out var redirectRootId))
            {
                Log.Info($"UrlMapper: Failed to parse {nameof(redirectRootIdSetting)} {redirectRootIdSetting}", this);
                return;
            }

            if (!ID.TryParse(redirectItemSetting, out var redirectItemTemplateId))
            {
                Log.Info($"UrlMapper: Failed to parse {nameof(redirectRootIdSetting)} {redirectRootIdSetting}", this);
                return;
            }

            var searchUrl = WebUtil.GetFullUrl(rawUrl);
            searchUrl = new Uri(searchUrl).ToString().ToLower();

            Log.Info("UrlMapper: Search URL: " + searchUrl + ".", this);

            var searchUrlEncode = HttpUtility.UrlPathEncode(WebUtil.GetFullUrl(WebUtil.GetRawUrl()));
            if (searchUrlEncode == null) return;

            searchUrlEncode = new Uri(searchUrlEncode).ToString().ToLower();

            RedirectUsingContentSearch(redirectRootId, redirectItemTemplateId, searchUrl, searchUrlEncode, httpContext);
        }

        protected virtual bool HasPerformedCheckDuringRequest(HttpContext httpContext)
        {
            const string key = "UrlMapperCheckPerformed";
            var existingValue = httpContext.Items[key];

            if (existingValue != null) return true;

            httpContext.Items[key] = true;

            return false;
        }

        protected virtual void RedirectUsingContentSearch(ID redirectRootId, ID redirectItemTemplateId,
            string searchUrl,
            string searchUrlEncode, HttpContext httpContext)
        {
            RedirectResultItem redirectItem = null;
            var indexName = Settings.GetSetting("UrlMapper.IndexName");
            if (string.IsNullOrWhiteSpace(indexName))
            {
                Log.Info($"UrlMapper: No index specified.", this);
                return;
            }

            try
            {
                using (var context = ContentSearchManager.GetIndex(indexName).CreateSearchContext())
                {
                    var searchUrlWithoutQueryString = searchUrl.Split('?')[0];
                    var searchUrlWithoutQueryStringEncode = searchUrlEncode.Split('?')[0];
                    var query = context.GetQueryable<RedirectResultItem>()
                        .Filter(resultItem => resultItem.Paths.Contains(redirectRootId))
                        .Filter(resultItem =>
                            resultItem.BaseTemplates.Contains(redirectItemTemplateId.Guid) ||
                            resultItem.TemplateId == redirectItemTemplateId)
                        .Filter(
                            resultItem =>
                                resultItem.SearchUrlLowerCaseUntokenized == searchUrl ||
                                resultItem.SearchUrlLowerCaseUntokenized == searchUrlEncode ||
                                resultItem.IgnoreQueryString && 
                                (resultItem.SearchUrlLowerCaseUntokenized == searchUrlWithoutQueryString || 
                                 resultItem.SearchUrlLowerCaseUntokenized == searchUrlWithoutQueryStringEncode))
                        .Filter(resultItem => resultItem.IsLatestVersion);

                    redirectItem = query.FirstOrDefault();
                }
            }
            catch (Exception e)
            {
                Log.Info($"UrlMapper: Failed to query Index {indexName}", this);
                return;
            }

            if (redirectItem != null)
            {
                var redirectUrl = redirectItem.RedirectUrlLowerCaseUntokenized;
                if (string.IsNullOrWhiteSpace(redirectUrl))
                {
                    Log.Error(
                        $"UrlMapper: Redirect from {searchUrl} will be aborted since the target url is empty.", this);
                    return;
                }

                var statusCode = redirectItem.IsPermanentRedirect
                    ? HttpStatusCode.MovedPermanently
                    : HttpStatusCode.Redirect;

                httpContext.Response.StatusCode = (int) statusCode;
                Log.Info(
                    $"UrlMapper: Redirect {searchUrl} to {redirectUrl} (HTTP {httpContext.Response.StatusCode}).",
                    this);

                if (statusCode == HttpStatusCode.MovedPermanently)
                    httpContext.Response.RedirectPermanent(redirectUrl);
                else
                    httpContext.Response.Redirect(redirectUrl);
            }
            else
            {
                Log.Info($"UrlMapper: No redirect found for {searchUrl}", this);
            }
        }
    }
}