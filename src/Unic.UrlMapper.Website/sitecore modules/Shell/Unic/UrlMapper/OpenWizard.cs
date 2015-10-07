using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Shell.Framework.Commands;

namespace Unic.UrlMapper.Website.sitecore_modules.Shell.Unic.UrlMapper
{
    [Serializable]
    public class OpenWizard : Command
    {
        public OpenWizard()
        {
        
        }
        
        public override void Execute(CommandContext context)
        {
            string controlUrl = "/sitecore modules/Shell/Unic/UrlMapper/Import Dialog.aspx";
            Sitecore.Context.ClientPage.ClientResponse.Broadcast(Sitecore.Context.ClientPage.ClientResponse.ShowModalDialog(controlUrl), "Shell");
        }
        
        public override CommandState QueryState(CommandContext context)
        {
            return base.QueryState(context);
        }
        
        protected void Run(Sitecore.Web.UI.Sheer.ClientPipelineArgs args)
        {
        
        }
    }
}