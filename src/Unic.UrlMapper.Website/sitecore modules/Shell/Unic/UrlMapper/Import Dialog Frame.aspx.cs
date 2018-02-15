using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using Sitecore.Web.UI.XmlControls;
using Sitecore.Shell.Web.UI;
using System.IO;
using Sitecore.Data.Items;
using Sitecore.Configuration;
using System.Text.RegularExpressions;
using Sitecore.Data;
using Sitecore.Publishing;
using Sitecore.Collections;
using Sitecore.Globalization;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;

namespace Unic.UrlMapper.Website.sitecore_modules.Shell.Unic.UrlMapper
{
    public partial class Import_Dialog_Frame : SecurePage
    {

        private int ItemsPerFolder = 0;
        private bool ErrorState = false;
        private int ErrorCode = 0;
        private string IsoNow = Sitecore.DateUtil.IsoNow;
        private Item ImportRoot = null;
        
        #region Configuration

        /// <summary>
        /// Item contains the actual Content Database
        /// </summary>
        private Database ContentDatabase;
        
        /// <summary>
        /// Item contains the root Folder for all Imports
        /// </summary>
        private Item rootFolder;

        /// <summary>
        /// Item contains the template Item of a Redirect Folder
        /// </summary>
        private TemplateItem folderTemplate;

        /// <summary>
        /// Item contains the template Item of a Redirect
        /// </summary>
        private TemplateItem redirectsTemplate;

        #endregion

        #region Initialize

        /// <summary>
        /// Configure the Configuratione Variables in Constructor
        /// </summary>
        public Import_Dialog_Frame()
        {
            ContentDatabase = Sitecore.Context.ContentDatabase;
            rootFolder = ContentDatabase.GetItem(Settings.GetSetting("UrlMapper.RootFolder"));
            folderTemplate = ContentDatabase.GetTemplate(Settings.GetSetting("UrlMapper.FolderTemplateId"));
            redirectsTemplate = ContentDatabase.GetTemplate(Settings.GetSetting("UrlMapper.ItemTemplateId"));
        }

        /// <summary>
        /// Initialize the Wizard
        /// </summary>
        protected override void OnInit(EventArgs e)
        {
            Control child = ControlFactory.GetControl("UrlMapperImportWizard");
            if (child != null)
            {
                this.Controls.Add(child);
            }

            base.OnInit(e);
        }

        /// <summary>
        /// Initialize the Import. 
        /// 1. If all Parameters of the Form are Valid Start the Import.
        /// 2. If there is no Error while Importing Datas and the Delete Option is choosen Delete all old Imports.
        /// 3. If there is no Error while Importing / Deleting and the Auto Publish Option is choosen Publish all Redirect Folders and Datas. 
        ///    There is no check for User Rights but if a User has Rights to Create and Delete the Items will be published 
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!base.IsEvent && (base.Request.Files.Count > 0))
            {
                // Check the Item per Folder Input
                Int32.TryParse(Request.Form["ItemsPerFolder"], out ItemsPerFolder);
                if (ItemsPerFolder > 0)
                {
                    // Check if a File is available
                    if (Request.Files != null && Request.Files.Count == 1)
                    {
                        //Import Datas
                        importData();

                        //Delete
                        if (Request.Form["DeleteImport"] == "1" && !ErrorState)
                        {
                            deleteOldImports();
                        }

                        //Publish
                        if (Request.Form["AutoPublish"] == "1" && !ErrorState)
                        {
                            publishItems();
                        }

                    }
                    else
                    {
                        ErrorState = true;
                        ErrorCode = 2;
                        Log.Info("UrlMapper: Import :: No valid csv file choosen", this);
                    }
                }
                else
                {
                    ErrorState = true;
                    ErrorCode = 1;
                    Log.Info("UrlMapper: Import :: Number of Items per folder invalid", this);
                }

                // post output to the dialog
                if (ErrorState)
                {
                    HttpContext.Current.Response.Write("<html><head><script type=\"text/JavaScript\" language=\"javascript\">window.parent.frames[0].scForm.postRequest(\"\", \"\", \"\", \"EndImportingError(errorCode=" + ErrorCode + ")\")</script></head><body>Error</body></html>");
                }
                else
                {
                    HttpContext.Current.Response.Write("<html><head><script type=\"text/JavaScript\" language=\"javascript\">window.parent.frames[0].scForm.postRequest(\"\", \"\", \"\", \"EndImporting\")</script></head><body>Done</body></html>");
                }
            }
        }
        #endregion

        #region Import

        /// <summary>
        /// Import Data from CSV
        /// </summary>
        private void importData()
        {
            HttpPostedFile file = Request.Files[0];

            // do we have a csv file?
            if (file.FileName.Length > 3 || Sitecore.StringUtil.Right(file.FileName, 4) == ".csv")
            {
                using (StreamReader stream = new StreamReader(file.InputStream))
                {
                    string line = stream.ReadLine();
                    string headerLine = Settings.GetSetting("UrlMapper.CsvHeaders");

                    //Check if Settings for Header Line are Correct
                    if (!string.IsNullOrWhiteSpace(headerLine))
                    {
                        // is the first row of our file correct?
                        if (!string.IsNullOrWhiteSpace(line) && line.ToLower() == headerLine.ToLower())
                        {
                            // do we have all the needed items? Id's come from the web.config
                            if (rootFolder != null && folderTemplate != null && redirectsTemplate != null)
                            {
                                // do we have permission to create new folders?
                                if (rootFolder.Security.CanCreate(Sitecore.Context.User))
                                {
                                    try
                                    {
                                        // create a new folder for every import
                                        ImportRoot = rootFolder.Add(IsoNow, folderTemplate);
                                        Item chunkFolder = null;
                                        Item subFolderItem = null;

                                        string permanentRedirect = "0";

                                        //Set value for permanent Redirect or not
                                        if (Request.Form["PermanentRedirect"] == "1" && !ErrorState)
                                        {
                                            permanentRedirect = "1";
                                        }

                                        var counters = new Dictionary<string, int>();
                                        while ((line = stream.ReadLine()) != null)
                                        {
                                            string[] values = line.Split(';');
                                            if (values != null && values.Length >= 3)
                                            {
                                                string itemName = values[0].Trim();
                                                string searchUrl = values[1].Trim();
                                                string redirectUrl = values[2].Trim();
                                                string subFolder = string.Empty;
                                                if (values.Length >= 4)
                                                {
                                                    subFolder = values[3].Trim();
                                                }

                                                // check the two values for old and new url
                                                if (!string.IsNullOrWhiteSpace(searchUrl) && !string.IsNullOrWhiteSpace(redirectUrl))
                                                {
                                                    if (!counters.TryGetValue(subFolder, out var counter))
                                                    {
                                                        counter = 0;
                                                    }

                                                    // Create subfolder if defined and it doesn't exist yet
                                                    var workingFolder = ImportRoot;
                                                    if (!string.IsNullOrWhiteSpace(subFolder))
                                                    {
                                                        workingFolder = ImportRoot.Children[subFolder]
                                                            ?? ImportRoot.Add(subFolder, folderTemplate);
                                                    }

                                                    // determine chunk folder
                                                    if (counter % ItemsPerFolder == 0)
                                                    {
                                                        // create new folder if chunk size is reached
                                                        chunkFolder = workingFolder.Add(
                                                            (counter / ItemsPerFolder) + 1 + "", folderTemplate);
                                                    }
                                                    else
                                                    {
                                                        // get the existing chunk folder based on counter
                                                        chunkFolder =
                                                            workingFolder.Children[
                                                                (counter / ItemsPerFolder + 1).ToString()];
                                                    }

                                                    // get name for the new item
                                                    string name = string.Empty;

                                                    if (!string.IsNullOrWhiteSpace(itemName))
                                                    {
                                                        name = itemName;
                                                    }
                                                    else
                                                    {
                                                        name = Sitecore.StringUtil.GetLastPart(redirectUrl, '/', "redirect " + counter);
                                                    }

                                                    if (name.IndexOf(".") > -1)
                                                    {
                                                        name = Sitecore.StringUtil.Left(name, name.IndexOf("."));
                                                    }

                                                    // create redirect and set the fields
                                                    Item redirectItem = chunkFolder.Add(name, redirectsTemplate);

                                                    if (redirectItem != null)
                                                    {
                                                        redirectItem.Editing.BeginEdit();
                                                        redirectItem["Search URL"] = searchUrl;
                                                        redirectItem["Redirect URL"] = redirectUrl;
                                                        redirectItem["Permanent Redirect"] = permanentRedirect;
                                                        redirectItem.Editing.EndEdit();
                                                    }

                                                    counters[subFolder] = ++counter;
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        ImportRoot?.Delete();

                                        ErrorState = true;
                                        Log.Error("UrlMapper: Import :: Unexpected error", ex, this);
                                    }
                                }
                                else
                                {
                                    ErrorState = true;
                                    ErrorCode = 5;
                                    Log.Error("UrlMapper: Import :: Permission for creating new redirects denied", this);
                                }
                            }
                            else
                            {
                                ErrorState = true;
                                ErrorCode = 4;
                                Log.Error("UrlMapper: Import :: Template configuration error", this);
                            }

                        }
                        else
                        {
                            ErrorState = true;
                            ErrorCode = 3;
                            Log.Error("UrlMapper: Import :: No valid content of header row: " + line, this);
                        }
                    }
                    else
                    {
                        ErrorState = true;
                        ErrorCode = 7;
                        Log.Error("UrlMapper: Import :: Header Line is not correctly configured", this);
                    }
                }
            }
            else
            {
                ErrorState = true;
                ErrorCode = 2;
                Sitecore.Diagnostics.Log.Error("UrlMapper: Import :: No valid csv file choosen", this);
            }

        }

        #endregion

        #region Delete

        /// <summary>
        /// Delete old Imports
        /// </summary>
        private void deleteOldImports()
        {
            if (rootFolder.Security.CanDelete(Sitecore.Context.User))
            {
                try
                {
                    string query = "fast://*[@@id='" + rootFolder.ID + "']//*[@@templateid='" + folderTemplate.ID + "']";
                    IEnumerable<Item> imports = ContentDatabase.SelectItems(query);
                    Regex reg = new Regex("[\\d]{8}[T][\\d]{6}");

                    foreach (Item import in imports)
                    {
                        if (reg.IsMatch(import.Name) && import.Name != IsoNow.ToString())
                        {
                            import.Delete();
                        }
                    }

                }
                catch (Exception ex)
                {
                    ErrorState = true;
                    Log.Error("UrlMapper: Import :: Unexpected error", ex, this);
                }
            }
            else
            {
                ErrorState = true;
                ErrorCode = 6;
                Log.Error("UrlMapper: Import :: Permission for deleting redirects denied", this);
            }
        }

        #endregion

        #region publication

        /// <summary>
        /// Async Publication of all Items from Redirect root Folder
        /// </summary>
        private void publishItems()
        {
            LanguageCollection languages = LanguageManager.GetLanguages(ContentDatabase);
            ItemList publishingTargets = Sitecore.Publishing.PublishManager.GetPublishingTargets(ContentDatabase);
            foreach (Item target in publishingTargets)
            {
                Database targetDatabase = Sitecore.Configuration.Factory.GetDatabase(target["Target database"]);

                foreach (Language language in languages)
                {
                    PublishOptions options = new PublishOptions(ContentDatabase, targetDatabase, PublishMode.Smart, language, DateTime.Now);
                    options.RootItem = ImportRoot.Parent;
                    options.Deep = true;

                    Publisher publisher = new Publisher(options);
                    publisher.PublishAsync();

                    Log.Info("UrlMapper: Import :: Async publish job started to target database " + target["Target database"] + " for language " + language.Name + " from root item " + ImportRoot.Paths.FullPath + "'", this);
                }
            }
        }

        #endregion
    }
}