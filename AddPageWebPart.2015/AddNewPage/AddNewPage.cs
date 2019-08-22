using System;
using System.ComponentModel;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using System.Collections.Generic;
using Microsoft.SharePoint.Publishing;
using Microsoft.SharePoint.WebPartPages;

namespace AddPageWebPart._2015.AddNewPage
{
    [ToolboxItemAttribute(false)]
    public class AddNewPage : Microsoft.SharePoint.WebPartPages.WebPart
    {
        Label lblHeading, lblHidden;
        TextBox tbNewPageName;
        Button btnAddNewPage;

        string HeadingText = "Add New Meeting";
        public string pHeadingText
        {
            set { HeadingText = value; }
            get { return HeadingText; }
        }
        string ButtonText = "Add New Meeting";
        public string pButtonText
        {
            set { ButtonText = value; }
            get { return ButtonText; }
        }
        string HiddenLabelText = "Default.aspx";
        public string pHiddenLabelText
        {
            set { HiddenLabelText = value; }
            get { return HiddenLabelText; }
        }

        //adding controls to webpart
        protected override void CreateChildControls()
        {
            // Declaring controls here
            lblHidden = new Label();
            lblHidden.Text = pHiddenLabelText;
            lblHidden.ID = "lblHidden";
            lblHidden.Visible = false;

            lblHeading = new Label();
            lblHeading.Text = pHeadingText;
            lblHeading.ID = "lblHeading";

            tbNewPageName = new TextBox();
            tbNewPageName.ID = "tbNewPageName";

            btnAddNewPage = new Button();
            btnAddNewPage.Text = pButtonText;
            btnAddNewPage.ID = "btnAddNewPage";
            btnAddNewPage.Click += new EventHandler(btnAddNewPage_Click);

            //adding controls to page
            this.Controls.Add(lblHidden);
            this.Controls.Add(new LiteralControl("<br />"));
            this.Controls.Add(lblHeading);
            this.Controls.Add(new LiteralControl("<br />"));
            this.Controls.Add(tbNewPageName);
            this.Controls.Add(btnAddNewPage);
        }

        // click event when add new page is clicked
        protected void btnAddNewPage_Click(object sender, EventArgs e)
        {
            string newPageName = tbNewPageName.Text;
            string pageLayoutName = lblHidden.Text;
            string createdPageURL = string.Empty;

            // if user left textbox empty then use EnterpriseWiki page to create a new page
            if(string.IsNullOrEmpty(pageLayoutName))
                pageLayoutName = "EnterpriseWiki.aspx";

            //here create a new publishing page
            if (!string.IsNullOrEmpty(newPageName))
                createdPageURL = CreatePublishingPage(newPageName, pageLayoutName, false);  // if this is the landing page then change false to true;

            //empty text box after words
            tbNewPageName.Text = "";

            //redirect user to new page
            Page.Response.Redirect(createdPageURL ?? "Default.aspx");
        }

        // method for creating publising page
        private string CreatePublishingPage(string pageName, string pageLayoutName, bool isLandingPage)
        {
            string createdPageURL = string.Empty;

            // elevated privilages as not all user will have permission to create a new page
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                // get current web
                SPWeb oWeb = SPContext.Current.Web;
                string fullPageUrl = string.Empty;
                PublishingWeb publishingWeb = PublishingWeb.GetPublishingWeb(oWeb);
                /* Get the publishing web page collection list. */
                PublishingPageCollection publishingPageCollection = publishingWeb.GetPublishingPages();
                //GetPageLayoutName(application); 
                if (!string.IsNullOrEmpty(pageLayoutName))
                {
                    /* Search for the page layout for creating the new page */
                    List<PageLayout> layouts = new List<PageLayout>(publishingWeb.GetAvailablePageLayouts());
                    PageLayout pageLayout = layouts.Find(
                    delegate(PageLayout l)
                    {
                        return l.Name.Equals(pageLayoutName, StringComparison.CurrentCultureIgnoreCase);
                    });
                    /*page layout exists*/
                    if (pageLayout != null)
                    {
                        PublishingPage newPage = null;
                        newPage = publishingPageCollection.Add(pageName + ".aspx", pageLayout);
                        newPage.Title = pageName;
                        newPage.Update();

                        SPList li = newPage.ListItem.ParentList;

                        if (li.EnableModeration == false)
                        {
                            li.EnableModeration = true;
                            li.Update();
                        }
                        newPage.CheckIn("page checked in");
                        newPage.ListItem.File.Publish("page published");
                        newPage.ListItem.File.Approve("page approved");
                        /* Set newly created page as a welcome page */
                        if (isLandingPage == true)
                        {
                            fullPageUrl = oWeb.Url + "/Pages/" + pageName + ".aspx";
                            SPFile fileNew = publishingWeb.Web.GetFile(fullPageUrl);
                            publishingWeb.DefaultPage = fileNew;
                        }
                        publishingWeb.Update();

                        createdPageURL = newPage.Uri.AbsoluteUri.ToString();
                    }
                }
            });

            // return new page url
            return createdPageURL;
        }

        //adding custom toolpart to webpart for custom properties
        public override Microsoft.SharePoint.WebPartPages.ToolPart[] GetToolParts()
        {
            ToolPart[] allToolParts = new ToolPart[2];
            //our custom toolpart
            allToolParts[0] = new MyToolPart();
            allToolParts[1] = new WebPartToolPart();
            return allToolParts;
        }
    }

    // class for custom properties
    public class MyToolPart : Microsoft.SharePoint.WebPartPages.ToolPart
    {
        //declaring controls for custom properties
        DropDownList m_Dropdown;
        Panel panel;
        TextBox ctbButtonText, ctbHeadingText;
        AddNewPage m_Parent = null;

        //set title of custom properties
        public MyToolPart()
        {
            this.Title = "Select Page Layout";
        }

        // prebuilt function for adding custom proeprties to webpart
        protected override void CreateChildControls()
        {
            panel = new Panel();

            panel.Controls.Add(new LiteralControl("<b>Set Heading Text:</b>"));
            ctbHeadingText = new TextBox();
            ctbHeadingText.ID = "ctbHeadingText";
            panel.Controls.Add(ctbHeadingText);

            panel.Controls.Add(new LiteralControl("<br />"));

            panel.Controls.Add(new LiteralControl("<b>Set Button Text:</b>"));
            ctbButtonText = new TextBox();
            ctbButtonText.ID = "ctbButtonText";
            panel.Controls.Add(ctbButtonText);

            panel.Controls.Add(new LiteralControl("<br />"));

            m_Dropdown = new DropDownList();
            foreach (string pgLayout in GetAllPublishingPage())
                m_Dropdown.Items.Add(pgLayout);
            panel.Controls.Add(m_Dropdown);

            panel.Controls.Add(new LiteralControl("<br />"));

            this.Controls.Add(panel);

            m_Parent = (AddNewPage)ParentToolPane.SelectedWebPart;

            if (m_Parent != null)
            {
                this.m_Dropdown.SelectedValue = m_Parent.pHiddenLabelText;
                this.ctbHeadingText.Text = m_Parent.pHeadingText;
                this.ctbButtonText.Text = m_Parent.pButtonText;
            }

            base.CreateChildControls();
        }

        // when apply changes button gets pressed, this event will run to save changes
        public override void ApplyChanges()
        {
            m_Parent = (AddNewPage)ParentToolPane.SelectedWebPart;
            m_Parent.pHiddenLabelText = m_Dropdown.SelectedValue;
            m_Parent.pHeadingText = ctbHeadingText.Text;
            m_Parent.pButtonText = ctbButtonText.Text;
            base.ApplyChanges();
        }

        // this method gets all publishing pages for the web
        private List<string> GetAllPublishingPage()
        {
            List<string> pgList = new List<string>();

            SPWeb web = SPContext.Current.Web;
            string fullPageUrl = string.Empty;
            PublishingWeb publishingWeb = PublishingWeb.GetPublishingWeb(web);
            PublishingPageCollection publishingPageCollection = publishingWeb.GetPublishingPages();
            List<PageLayout> layouts = new List<PageLayout>(publishingWeb.GetAvailablePageLayouts());

            foreach (PageLayout pl in layouts)
            {
                pgList.Add(pl.Name);
            }

            return pgList;
        }
    }
}
