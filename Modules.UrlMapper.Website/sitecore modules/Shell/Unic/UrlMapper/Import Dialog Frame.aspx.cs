using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using Sitecore.Web.UI.XmlControls;
using Sitecore.Shell.Web.UI;
using System.IO;
using System.Configuration;
using Sitecore.Data.Items;
using Sitecore.Configuration;

namespace Unic.SitecoreCMS.Modules.UrlMapper.Website.sitecore_modules.Shell.Unic.UrlMapper
{
    public partial class Import_Dialog_Frame : SecurePage
    {
        protected override void OnInit(EventArgs e)
        {
            Control child = ControlFactory.GetControl("UrlMapperImportWizard");
            if (child != null)
            {
                this.Controls.Add(child);
            }

            base.OnInit(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!base.IsEvent && (base.Request.Files.Count > 0))
            {
                bool error = false;
                int errorCode = 0;
                int itemsPerFolder = 0;

                // correct items per folder value?
                Int32.TryParse(Request.Form["ItemsPerFolder"], out itemsPerFolder);
                if (itemsPerFolder > 0)
                {
                    // do we have a file?
                    if (Request.Files != null && Request.Files.Count == 1)
                    {
                        HttpPostedFile file = Request.Files[0];

                        // do we have a csv file?
                        if (file.FileName.Length > 3 || Sitecore.StringUtil.Right(file.FileName, 4) == ".csv")
                        {
                            using (StreamReader stream = new StreamReader(file.InputStream))
                            {
                                string line = stream.ReadLine();

                                // is the first row of our file correct?
                                if (!string.IsNullOrWhiteSpace(line) && line.ToLower() == "oldurl;newurl")
                                {
                                    Item rootFolder = Sitecore.Configuration.Factory.GetDatabase("master").GetItem(Settings.GetSetting("UrlMapper.RootFolder"));
                                    TemplateItem folderTemplate = Sitecore.Configuration.Factory.GetDatabase("master").GetTemplate(Settings.GetSetting("UrlMapper.FolderTemplateId"));
                                    TemplateItem redirectsTemplate = Sitecore.Configuration.Factory.GetDatabase("master").GetTemplate(Settings.GetSetting("UrlMapper.ItemTemplateId"));

                                    // do we have all the needed items? Id's come from the web.config
                                    if (rootFolder != null && folderTemplate != null && redirectsTemplate != null)
                                    {
                                        // do we have permission to create new folders?
                                        if (rootFolder.Security.CanCreate(Sitecore.Context.User))
                                        {
                                            Item importRoot = null;

                                            try
                                            {
                                                // create a new folder for every import
                                                string date = Sitecore.DateUtil.IsoNow;
                                                importRoot = rootFolder.Add(date, folderTemplate);
                                                Item currentFolder = null;
                                                int counter = 0;

                                                while ((line = stream.ReadLine()) != null)
                                                {
                                                    string[] values = line.Split(';');
                                                    if (values != null && values.Length == 2)
                                                    {
                                                        string oldUrl = values[0];
                                                        string newUrl = values[1];

                                                        // check the two values for old and new url
                                                        if (!string.IsNullOrWhiteSpace(oldUrl) && !string.IsNullOrWhiteSpace(newUrl))
                                                        {
                                                            // create new folder if chunk size is reached
                                                            if ((counter % itemsPerFolder) == 0)
                                                            {
                                                                currentFolder = importRoot.Add((counter / itemsPerFolder) + 1 + "", folderTemplate);
                                                            }

                                                            // get name for the new item
                                                            string name = Sitecore.StringUtil.GetLastPart(newUrl, '/', "redirect " + counter);
                                                            if (name.IndexOf(".") > -1)
                                                            {
                                                                name = Sitecore.StringUtil.Left(name, name.IndexOf("."));
                                                            }

                                                            // create redirect and set the fields
                                                            Item redirectItem = currentFolder.Add(name, redirectsTemplate);
                                                            redirectItem.Editing.BeginEdit();
                                                            redirectItem["Old Url"] = oldUrl;
                                                            redirectItem["New Url"] = newUrl;
                                                            redirectItem.Editing.EndEdit();

                                                            counter++;
                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                if (importRoot != null)
                                                {
                                                    importRoot.Delete();
                                                }

                                                Sitecore.Diagnostics.Log.Error("UrlMapper: Import :: Unexpected error", ex, this);
                                            }
                                        }
                                        else
                                        {
                                            error = true;
                                            errorCode = 5;
                                            Sitecore.Diagnostics.Log.Warn("UrlMapper: Import :: Permission for creating new redirects denied", this);
                                        }
                                    }
                                    else
                                    {
                                        error = true;
                                        errorCode = 4;
                                        Sitecore.Diagnostics.Log.Error("UrlMapper: Import :: Template configuration error", this);
                                    }

                                }
                                else
                                {
                                    error = true;
                                    errorCode = 3;
                                    Sitecore.Diagnostics.Log.Info("UrlMapper: Import :: No valid content of header row: " + line, this);
                                }
                            }
                        }
                        else
                        {
                            error = true;
                            errorCode = 2;
                            Sitecore.Diagnostics.Log.Info("UrlMapper: Import :: No valid csv file choosen", this);
                        }
                    }
                    else
                    {
                        error = true;
                        errorCode = 2;
                        Sitecore.Diagnostics.Log.Info("UrlMapper: Import :: No valid csv file choosen", this);
                    }
                }
                else
                {
                    error = true;
                    errorCode = 1;
                    Sitecore.Diagnostics.Log.Info("UrlMapper: Import :: Number of Items per folder invalid", this);
                }

                // post output to the dialog
                if (error)
                {
                    HttpContext.Current.Response.Write("<html><head><script type=\"text/JavaScript\" language=\"javascript\">window.top.frames[0].scForm.postRequest(\"\", \"\", \"\", \"EndImportingError(errorCode=" + errorCode + ")\")</script></head><body>Error</body></html>");
                }
                else
                {
                    HttpContext.Current.Response.Write("<html><head><script type=\"text/JavaScript\" language=\"javascript\">window.top.frames[0].scForm.postRequest(\"\", \"\", \"\", \"EndImporting\")</script></head><body>Done</body></html>");
                }
            }
        }
    }
}