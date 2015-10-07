using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Sitecore.Shell.Web.UI;
using Sitecore.Web;

namespace Unic.UrlMapper.Website.sitecore_modules.Shell.Unic.UrlMapper
{
    public partial class Import_Dialog : SecurePage
    {
        private void InitializeComponent()
        {
            base.Load += new EventHandler(this.Page_Load);
        }

        protected override void OnInit(EventArgs e)
        {
            this.InitializeComponent();
            base.OnInit(e);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            this.ImportWizard.Attributes["src"] = "/sitecore modules/Shell/Unic/UrlMapper/Import Dialog Frame.aspx?" + WebUtil.GetQueryString();
        }
    }
}