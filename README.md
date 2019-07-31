# UrlMapper

[![Build status](https://ci.appveyor.com/api/projects/status/txgis3q0lqnnq818/branch/master?svg=true)](https://ci.appveyor.com/project/team-unic/sitecoreurlmapper/branch/master) ![Nuget](https://img.shields.io/nuget/v/Unic.UrlMapper)


## Feature overview

This URL mapping module for Sitecore allows authors to define redirects.

## Installation

Please follow the instructions in the README.txt of the nuget package. Please note that UrlMapper uses a custom SolR index.

## Usage

### {domain} token

The `{domain}` token can be used in the `Search URL` field, eg. `http://{domain}/redirect-me-pls`.The token will be replaced during indexing with the values defined in the `UrlMapper.Domain.Author` and `UrlMapper.Domain.Delivery` settings.

### CSV import

Redirects can be imported from within the *Dashboard* > *All Applications* > *Redirect Importer* dialog. The expected CSV format is as follows:

```
Name;SearchUrl;RedirectUrl;SubFolder;IsPermanent
Google;http://{domain}/sample-redirects/google;https://google.com;samples;true
Amazon;http://{domain}/sample-redirects/amazon;https://amazon.com;samples;false
```

### Tenant support

You can use the `allowedSites` and `restrictedSites` child nodes on the `HttpRequest` processor provided to specify the site context the module should run. 
To manage multiple sites at once, add a `tenant="mySites"` attribute to the sites you would like to manage with one setting and then use:

```xml
<allowedSites hint="list">
 <site>mySites</site>
</allowedSites>
```

## Development

### Integration project

The repository contains a pre-configured integration project based on Sitecore 8.2 which you can use for development. Using bob, the environment can be automatically be set up using the `bump` command.

### Docker setup for SolR

The project contains a docker-compose file you can use to set up a pre-configured SolR environment. Execute `\misc\Docker\install-docker.ps1` to start the container.

## Changelog

### 8.7

* Expose Redirector functionality

### 8.6

* Changes the default index configuration provided in `01_modules_solr` to use a custom `urlMapperSolrIndexConfiguration` instead of the `defaultSolrIndexConfiguration`. This prevents the calculated fields to run on unrelated indexes. The index configuration, however, is still based on `defaultSolrIndexConfiguration` in order to ensure backward compatibility.

### 8.5

* Add support for specifying `isPermanent` in import .csv on entry level. This value can either be `false`, `true` or empty. `true` and `false` values have higher priority than the checkbox in the import dialog.
* Add `mongo` to docker-compose. The instance is mapped to port `12984`
* If the target url cannot be resolved or is empty, the processor will now be aborted instead of trying to redirect.

### 8.4

* Add tenant Sitecore Site attribute support to allow white/blacklisting multiple site configurations at once
