# Changelog

## 8.7

* Expose Redirector functionality

## 8.6

* Changes the default index configuration provided in `01_modules_solr` to use a custom `urlMapperSolrIndexConfiguration` instead of the `defaultSolrIndexConfiguration`. This prevents the calculated fields to run on unrelated indexes. The index configuration, however, is still based on `defaultSolrIndexConfiguration` in order to ensure backward compatibility.

## 8.5

* Add support for specifying `isPermanent` in import .csv on entry level. This value can either be `false`, `true` or empty. `true` and `false` values have higher priority than the checkbox in the import dialog.
* Add `mongo` to docker-compose. The instance is mapped to port `12984`
* If the target url cannot be resolved or is empty, the processor will now be aborted instead of trying to redirect.

## 8.4

* Add tenant Sitecore Site attribute support to allow white/blacklisting multiple site configurations at once
