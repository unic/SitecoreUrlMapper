﻿namespace Unic.UrlMapper.Integration.Website.Pipelines.Initialize
{
    using Microsoft.Practices.Unity;
    using Sitecore.ContentSearch.SolrProvider.UnityIntegration;
    using Sitecore.Diagnostics;
    using Sitecore.Pipelines;

    public class InitializeSolrContainer
    {
        public void Process(PipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            new UnitySolrStartUp(new UnityContainer()).Initialize();
        }
    }
}