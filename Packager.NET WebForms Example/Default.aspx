<%@ Page Title="" Language="C#" MasterPageFile="~/Master/Main.master" %>
<%@ Register Assembly="Packager.NET" Namespace="Packager" TagPrefix="Packager" %>

<asp:Content ContentPlaceHolderID="PageAssets" runat="server">
	<Packager:StyleSheets runat="server">
		<Packager:CSS href="/CSS/docs.css" />
	</Packager:StyleSheets>
	<Packager:Scripts runat="server">
		<Packager:Script src="/JS/app/app.js" />
	</Packager:Scripts>
</asp:Content>