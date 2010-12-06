Packager.NET
============

A sort-of port of Packager as used by MooTools for .NET Webforms.

Allows you to specify placeholders for both JavaScript and CSS files; you can then include JavaScript and/or CSS in any control or page, and it will automatically calculate dependencies from the YAML headers and sort the includes in the correct order.


### Plans for future features
	
 * Multiple placeholders for output scripts (so you can specify for instance to include a script at the top or bottom of a page)
 * Compression and collation of the includes using YUI minifier
 * Hardcore caching of the above combined files
 * And more!