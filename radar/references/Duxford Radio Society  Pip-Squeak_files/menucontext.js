//Top Nav bar script v2- http://www.dynamicdrive.com/dynamicindex1/sm/index.htm

function showToolbar()
{
// AddItem(id, text, hint, location, alternativeLocation);
// AddSubItem(idParent, text, hint, location);

	menu = new Menu();
	menu.addItem("blankspaceid", "&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;", "", null, null);	
	menu.addItem("homepageid", "Home", "Duxford Radio Society Home Page", "/index.html", null);
	menu.addItem("drsnewsid", "DRS News", "DRS news updates",  null, null);
	menu.addItem("restoreid", "Equipment Conservation", "Conservation & restoration activities",  null, null);
	menu.addItem("displayid", "Equipment Display", "Display and demonstrations",  null, null);
	menu.addItem("radioid", "Radio Station", "I.W.M. Amateur Radio Station",  null, null);
	menu.addItem("infoid", "Information", "DRS information",  null, null);
	menu.addItem("blankspace2id", "&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;", "", null, null);
	menu.addItem("blankspace3id", "", "", null, null);	


	menu.addSubItem("drsnewsid", "Latest News", "Latest news",  "/whatsnew/latest.html");
//	menu.addSubItem("drsnewsid", "Recent Events", "Recent events",  "/whatsnew/recent.html");
	menu.addSubItem("drsnewsid", "Web Site Updates", "Web site updates",  "/updates/updates.html");
	menu.addSubItem("drsnewsid", "DRS Journal", "DRS Journal contents list",  "/whatsnew/newsletter.html");


	menu.addSubItem("restoreid", "Conservation Definitions", "Conservation & restoration terminology",  "/restoration/definitions.html");
	menu.addSubItem("restoreid", "Conserved Equipment", "Conserved & restored equipment",  "/restoration/restoration.html");
	menu.addSubItem("restoreid", "Equipment History Index", "Index of history files",  "/equiphist/equiphist.html");
	menu.addSubItem("restoreid", "Can You Help?", "Can you help us?",  "/canyouhelp/canyouhelp.html");


	menu.addSubItem("displayid", "Display & Demonstration", "Equipment on display",  "/display/display.html");
	menu.addSubItem("displayid", "Special Events", "Special event demonstrations",  "/display/sp-events.html");
	

	menu.addSubItem("radioid", "G B 2 I W M", "GB2IWM radio station",  "/radiostation/radiostation.html");
	menu.addSubItem("radioid", "Schedules & Operators", "GB2IWM schedules and operator details",  "/radiostation/schedules/schedules.html");
	menu.addSubItem("radioid", "Station Blog", "GB2IWM Radio Station Weblog",  "/radiostation/blog/blog.html");
	menu.addSubItem("radioid", "Radio Station View", "The view from the radio station",  "/radiostation/windowview/windowview.html");

	menu.addSubItem("infoid", "About DRS", "About DRS",  "/information/info.html");
	menu.addSubItem("infoid", "DRS History", "DRS history",  "/information/drshistory.html");
	menu.addSubItem("infoid", "DRS Needs You", "DRS needs volunteers",  "/information/drsneedsyou.html");	
	menu.addSubItem("infoid", "Duxford Site Map", "Site map",  "/information/sitemap.html");
	menu.addSubItem("infoid", "Selected Links", "Internet links",  "/information/links.html");

	menu.showMenu();
}