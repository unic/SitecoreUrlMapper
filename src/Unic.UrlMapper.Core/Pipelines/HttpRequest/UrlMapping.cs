
using Unic.UrlMapper.Core.Redirection;

namespace Unic.UrlMapper.Core.Pipelines.HttpRequest
{
    using Sitecore;
    using Sitecore.Pipelines.HttpRequest;

    [UsedImplicitly]
    public class UrlMapping : ProcessorBase<HttpRequestArgs>
    {
        protected override void Execute(HttpRequestArgs args) => this.GetRedirector()?.CheckAndPerformRedirect();

        protected virtual Redirector GetRedirector() => new Redirector();
    }
}
