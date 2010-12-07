Packager.NET 1.0 beta 1
=======================

A sort-of port of Packager as used by MooTools for .NET Webforms.

Allows you to specify placeholders for both JavaScript and CSS files; you can then include JavaScript and/or CSS in any control or page, and it will automatically calculate dependencies from the YAML headers and sort the includes in the correct order.


## How To Use

 * Add a reference to the Packager, YUI minifier and Ecmascript.NET DLLs
 * A placeholder for CSS and JS must be present in either the parent Page or Master Page
 * Currently these must have IDs of 'CSSHolder' and 'ScriptsHolder' respectively (see 'plans for future features' for more)
 * There is a configuration file, '/Configuration/Packager.config', amend as necessary

Packages are registered in the relevant sections of the configuration file.

To include Packager.NET components in a Page, Master Page or Control you'll need to reference the DLL:

	<%@ Register Assembly="Packager.NET" Namespace="Packager" TagPrefix="Packager" />

You can then include CSS as follows:

	<Packager:StyleSheets runat="server">
		<Packager:CSS href="/CSS/foo.css" />
		<Packager:CSS href="/CSS/bar.css" />
	</Packager:StyleSheets>

Or JavaScript as follows:

	<Packager:Scripts runat="server">
		<Packager:Script src="/JS/app.js" />
	</Packager:Scripts>

Packager will then automatically parse the YAML headers of the files and pull in any dependencies!


## Configuration Options

 * RootFolder: Root of the site
 * CacheFolder: Where to store the compressed cached files
 * Debug: This will not concatenate the files and just output all the dependencies and includes if set to 'true'
 * Compress: CSS and JavaScript will be minified (using the YUI minifier) if this is set to 'true'
 * Optimise: Currently this doesn't do anything but see below ('planned automatic optimisation')
 * You obviously then have your packages.
 

## Planned Automatic Optimisation

This is currently in alpha and seems to be working OK! The idea is, that if you have a large site with multiple collections of dependencies,
having a different file for each page (even if compressed) is not efficient.

The plan is to record each page's requirements. If a requirement is included on a percentage of pages (say at least 40%) it will be included in
a single cacheable file that will be included on every page.

In this way, the first hit to a site will cache the majority of requirements for the entire site. Less common includes (say you have a gallery on
just a single page) will be served compressed as and when required.

This will be configurable, automated with the option to manually administer, and can be disabled for deployment to live environments (with cached
 / saved settings).

This feature will be available in the second beta!


## Planned Development Timescales

We're using this in a project at work currently so I expect any bugs to be ironed out soon. It's currently in the 'make it work' phase; next
phase is 'make it elegant'; final phase is 'make it fast'.

I expect to release two more betas in the coming weeks, with a Release Candidate early 2011.


## How To Help

Try it! You can report any issues you find in the Issues section of this repository or if you feel generous, fork it, fix it and send me a pull
request :D 


## Credits

 * [Valerio Proietti](http://github.com/kamicane) and the [MooTools Dev Team](http://mootools.net/developers) for the original [PHP Packager](http://github.com/kamicane/packager) implementation
 * Aleks Andjelkovic for his advice and help
 * [Abacus e-Media](http://www.abacusemedia.com/) for letting me do (some) work during office hours on this
 * Tawani Anyangwe for the [original Topological Sorter script](http://tawani.blogspot.com/2009/02/topological-sorting-and-cyclic.html) (itself adapted from a [Java version](http://www.java2s.com/Code/Java/Collections-Data-Structure/Topologicalsorting.htm))


## Plans for future features
	
 * Multiple placeholders for output scripts (so you can specify for instance to include a script at the top or bottom of a page)
 * Should be able to specify the IDs of the above placeholders and should be optional (for instance not require CSS)
 * Copyright comments should be retained and output
 * Ability to have optional blocks like the original Packager. For instance allowing you to strip out MooTools 1.2 compatibility from Core 1.3
 * An implementation for use with ASP.MVC
 * And more!