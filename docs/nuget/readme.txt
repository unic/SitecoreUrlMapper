  _   _    ___     _     __  __    ___      ___     ___    ___     ___   
 | | | |  | _ \   | |   |  \/  |  /   \    | _ \   | _ \  | __|   | _ \  
 | |_| |  |   /   | |__ | |\/| |  | - |    |  _/   |  _/  | _|    |   /  
  \___/   |_|_\   |____||_|__|_|  |_|_|   _|_|_   _|_|_   |___|   |_|_\  
_|"""""|_|"""""|_|"""""|_|"""""|_|"""""|_| """ |_| """ |_|"""""|_|"""""| 
"`-0-0-'"`-0-0-'"`-0-0-'"`-0-0-'"`-0-0-'"`-0-0-'"`-0-0-'"`-0-0-'"`-0-0-' 

Items
-----
The serialized items must be copied from the package directory to the serialization path of your site.

Manual item creation
--------------------
Create a redirect root folder based on the "UrlMapper/Common/Redirect Folder" template.

Roles
-----
Unicorn will import the "sitecore/Redirect Importer" role. This role can be used for non-admin users
who need to use the importer or create redirects manually. You will have to set the access rights for
this role on the redirect root folder accordingly.

Bob.config
----------
1)	Ensure that you have all required folders in KeepAppConfigIncludes. 
	urlmapper will use the following config folders:
	- 01_modules
	- 01_modules_author
	- 01_modules_lucene *OR* 01_modules_solr
	- 01_modules_lucene-author *OR* 01_modules_solr-author

Base settings (required for all search providers)
-------------------------------------------------
1)	Setting "UrlMapper.RootFolder":
	Set this to the ID of your redirect root folder.

2)	Setting "UrlMapper.IndexName":
	Set this to the name of the index you would like to use (eg "urlmapper_master" for author, "urlmapper_web" for delivery).

3)	Index Root:
	Unic.UrlMapper.Index.Web and Unic.UrlMapper.Index.Master are pointing to /sitecore by default.
	Patch /locations/crawler/Root to point to your RootFolder to reduce index size and query time.

Solr config
-----------
1)	For the delivery server, you have to patch-remove the strategies node from Unic.UrlMapper.Index.Web.

Misc
----
1)	Don't forget to publish.