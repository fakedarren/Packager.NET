<%@ Page Language="C#" %>
<%
if (!Packager.Config.Loaded)
{
	Packager.Config.Load();
}
Packager.Config.Log();
%>