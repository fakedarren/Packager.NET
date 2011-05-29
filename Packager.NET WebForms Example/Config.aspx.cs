using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class Config : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
		if (!Packager.Config.Loaded)
		{
			Packager.Config.Load();
		}

		Packager.Config.Log();
	}
}