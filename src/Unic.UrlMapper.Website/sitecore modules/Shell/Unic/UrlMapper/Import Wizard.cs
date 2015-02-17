using System;
using System.Web;
using Sitecore.Web.UI.HtmlControls;
using System.IO;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using Sitecore.Diagnostics;
using System.Collections.Specialized;
using Sitecore.Configuration;

namespace Unic.UrlMapper.Website.sitecore_modules.Shell.Unic.UrlMapper
{
    public class Import_Wizard : WizardForm
    {
        protected Listbox ItemsPerFolder;
        protected Literal litLastPage;

        protected bool Error = false;
        protected string ErrorMessage = String.Empty;
        
        protected override bool ActivePageChanging(string page, ref string newpage)
        {
            Assert.ArgumentNotNull(page, "page");
            Assert.ArgumentNotNull(newpage, "newpage");

            if (page == "Configuration" && newpage == "Importing")
            {
                string file = Sitecore.Context.ClientPage.ClientRequest.Form["ImportFile"];
                if (string.IsNullOrWhiteSpace(file))
                {
                    Sitecore.Context.ClientPage.ClientResponse.Alert("Please specify a file to import.");
                    return false;
                }
                else if (file.Length < 4 || Sitecore.StringUtil.Right(file, 4) != ".csv")
                {
                    Sitecore.Context.ClientPage.ClientResponse.Alert("Please specify a valid *.csv file to import.");
                    return false;
                }
            }
            else if (page == "LastPage" && newpage == "Importing")
            {
                newpage = "Configuration";
            }
            
            return base.ActivePageChanging(page, ref newpage);
        }

        protected override void ActivePageChanged(string page, string oldPage)
        {
            Assert.ArgumentNotNull(page, "page");
            Assert.ArgumentNotNull(oldPage, "oldPage");

            base.ActivePageChanged(page, oldPage);

            base.NextButton.Header = "Next >";

            if (page == "Configuration")
            {
                base.NextButton.Header = "Import";
            }
            else if (page == "Importing" && oldPage == "Configuration")
            {
                base.NextButton.Disabled = true;
                base.BackButton.Disabled = true;
                base.CancelButton.Disabled = true;

                Sitecore.Context.ClientPage.ClientResponse.Timer("SubmitForm", 10);
            }
            else if (page == "LastPage")
            {
                if (!Error)
                {
                    base.BackButton.Disabled = true;
                    litLastPage.Text = "The wizard has completed. Click Finish to close the Wizard.";
                }
                else
                {
                    litLastPage.Text = "The wizard has completed with errors. Click Finish to close the Wizard or Back to try again.<br/><br/>" +
                        "Error Message:<br/><br/>" +
                        "<font color=\"red\"><strong>" + ErrorMessage + "</strong></font>";
                }
            }
        }

        protected void EndImporting()
        {
            Error = false;
            Sitecore.Context.ClientPage.ClientResponse.SetDialogValue("ok");
            base.Next();
        }

        protected void EndImportingError()
        {
            string requestParameters = Sitecore.Context.ClientPage.ClientRequest.Parameters;
            NameValueCollection parameters = Sitecore.StringUtil.GetNameValues(Sitecore.StringUtil.Mid(requestParameters, requestParameters.IndexOf("(") + 1, requestParameters.Length - requestParameters.IndexOf("(") - 2), '=', ',');
            string errorCode = parameters.Get("errorCode");

            Error = true;
            switch (errorCode)
            {
                case "1":
                    ErrorMessage = "The number of redirects per folder is not valid.";
                    break;
                case "2":
                    ErrorMessage = "The file is not a CSV file.";
                    break;
                case "3":
                    string headerLine = Settings.GetSetting("UrlMapper.CsvHeaders");
                    ErrorMessage = "The file content is not valid. Please specify the first row as header with columns: " + headerLine + ".";
                    break;
                case "4":
                    ErrorMessage = "No valid template configuration available. Please contact your system administrator.";
                    break;
                case "5":
                    ErrorMessage = "Permission denied to create Redirects. Please contact your system administrator.";
                    break;
                case "6":
                    ErrorMessage = "Permission denied to delete Redirects. Please contact your system administrator.";
                    break;
                case "7":
                    ErrorMessage = "The Header Line of the CSV is not configured correctly .";
                    break;
                default:
                    ErrorMessage = "Unexpected error. Please contact your system administrator.";
                    break;
            }

            Sitecore.Context.ClientPage.ClientResponse.SetDialogValue("ok");
            base.Next();
        }

        protected void SubmitForm()
        {
            Sitecore.Context.ClientPage.ClientResponse.Eval("submit()");
        }

    }
}
