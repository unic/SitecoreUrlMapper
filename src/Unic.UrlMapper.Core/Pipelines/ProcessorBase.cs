namespace Unic.UrlMapper.Core.Pipelines
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// This is a base processor for other conditional processors. Before starting the "Execute()" method of the processor,
    /// it calls the "ShouldExecute()" method, which can be overriden by a custom processor. By default it's possible
    /// to restrict sites in which a processor should be executed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ProcessorBase<T>
    {
        protected List<string> AllowedSites { get; } = new List<string>();

        protected List<string> RestrictedSites { get; } = new List<string>();

        public void Process(T args)
        {
            var processorType = this.GetType();

            Sitecore.Diagnostics.Log.Debug($"[ProcessorBase] Process <{processorType}>", this);

            if (this.ShouldExecute(args))
            {
                Sitecore.Diagnostics.Log.Debug($"[ProcessorBase] Execute <{processorType}>", this);
                this.Execute(args);
            }
        }

        protected virtual bool ShouldExecute(T args)
        {
            // if we don't have a site context, we generally don't want
            // custom processors to run. If your processor needs to make
            // an exception, feel free to do so by overriding this method
            var site = Sitecore.Context.Site;
            if (site == null) return false;

            // check if current site is restricted
            if (this.RestrictedSites.Contains(site.Name)) return false;

            // check if we have any allowed sites
            return !this.AllowedSites.Any() || this.AllowedSites.Contains(site.Name);
        }

        protected abstract void Execute(T args);
    }
}