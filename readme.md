Packager.NET 1.1
================

A sort-of port of Packager as used by MooTools for .NET Webforms.

Allows you to specify placeholders for both JavaScript and CSS files; you can then include JavaScript and/or CSS in any control or page, and it will automatically calculate dependencies from the YAML headers and sort the includes in the correct order.


## How To Use

 * Add a reference to the Packager, YUI minifier and Ecmascript.NET DLLs
 * A placeholder for CSS and JS must be present in either the parent Page or Master Page
 * Currently these must have IDs of 'CSSPlaceholder' and 'ScriptsPlaceholder' respectively (see 'plans for future features' for more)
 * There is a configuration file, '/Configuration/Packager.config', amend as necessary

Packages are registered in the relevant sections of the configuration file.

To include Packager.NET components in a Page, Master Page or Control you'll need to reference the DLL:

	<%@ Register Assembly="Packager.NET" Namespace="Packager" TagPrefix="Packager" />

Placeholders must be placed in a Master Page or Page for both JavaScript and CSS:

	<Packager:CSSHolder ID="CSSPlaceholder" runat="server" />
	<Packager:ScriptHolder ID="ScriptsPlaceholder" runat="server" />

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
 * ShowErrors: Defaults to 'true'. If set to false it will trap all errors silently (good for live environments).
 * You obviously then have your packages.

Note: Debug and ShowErrors can be overridden in the query string.

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