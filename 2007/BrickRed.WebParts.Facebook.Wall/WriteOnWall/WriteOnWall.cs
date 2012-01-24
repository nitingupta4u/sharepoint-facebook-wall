﻿/*
 ===========================================================================
 Copyright (c) 2010 BrickRed Technologies Limited

 Permission is hereby granted, free of charge, to any person obtaining a copy
 of this software and associated documentation files (the "Software"), to deal
 in the Software without restriction, including without limitation the rights
 to use, copy, modify, merge, publish, distribute, sub-license, and/or sell
 copies of the Software, and to permit persons to whom the Software is
 furnished to do so, subject to the following conditions:

 The above copyright notice and this permission notice shall be included in
 all copies or substantial portions of the Software.

 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 THE SOFTWARE.
 ===========================================================================
 */
using System;
using System.Runtime.InteropServices;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Serialization;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;
using System.Net;
using System.IO;
using System.ComponentModel;
using System.Drawing;
using System.Collections.Generic;

namespace BrickRed.WebParts.Facebook.Wall
{
    [Guid("e9aa91c6-b605-4ba0-bbac-7ae12d2bd58f")]
    public class WriteOnWall : System.Web.UI.WebControls.WebParts.WebPart,IWebEditable
    {
        Label LblMessage;
        TextBox textWall;

        #region Webpart Properties

        [WebBrowsable(false),
       Personalizable(PersonalizationScope.Shared)]

        public string OAuthCode { get; set; }

        [WebBrowsable(false),
       Personalizable(PersonalizationScope.Shared)]
        public string OAuthClientID { get; set; }

        [WebBrowsable(false),
       Personalizable(PersonalizationScope.Shared)]
        public string OAuthRedirectUrl { get; set; }

        [WebBrowsable(false),
       Personalizable(PersonalizationScope.Shared)]
        public string OAuthClientSecret { get; set; }

        [WebBrowsable(false),
       Personalizable(PersonalizationScope.Shared)]
        public bool EnableShowUserName { get; set; }

        [WebBrowsable(false),
      Personalizable(PersonalizationScope.Shared)]
        public bool PostOnProfile { get; set; }

        [WebBrowsable(false),
      Personalizable(PersonalizationScope.Shared)]
        public bool PostOnGroupWall { get; set; }

        [WebBrowsable(false),
      Personalizable(PersonalizationScope.Shared)]
        public bool PostAsPage { get; set; }

        [WebBrowsable(false),
       Personalizable(PersonalizationScope.Shared)]
        public string OAuthPageID { get; set; }

        [WebBrowsable(false),
      Personalizable(PersonalizationScope.Shared)]
        public string UserID { get; set; }

        [WebBrowsable(false),
      Personalizable(PersonalizationScope.Shared)]
        public bool ShowHeader { get; set; }

        [WebBrowsable(false),
      Personalizable(PersonalizationScope.Shared)]
        public bool ShowHeaderImage { get; set; }

        #endregion

        #region CreateChildControls event

        protected override void CreateChildControls()
        {
            if (!String.IsNullOrEmpty(this.OAuthCode) ||
                   !String.IsNullOrEmpty(this.OAuthClientID) ||
                   !String.IsNullOrEmpty(this.OAuthRedirectUrl) ||
                   !String.IsNullOrEmpty(this.OAuthClientSecret) ||
                   !String.IsNullOrEmpty(this.UserID)
                   )
            {
                this.Page.Header.Controls.Add(CommonHelper.InlineStyle());

                base.CreateChildControls();
                try
                {
                    Table mainTable;
                    TableRow tr;
                    TableCell tc;

                    Button buttonWriteOnWall;

                    mainTable = new Table();
                    mainTable.Width = Unit.Percentage(100);
                    mainTable.CellSpacing = 0;
                    mainTable.CellPadding = 0;

                    this.Controls.Add(mainTable);

                    //Create the header
                    if (this.ShowHeader)
                    {
                        tr = new TableRow();
                        mainTable.Rows.Add(tr);
                        tc = new TableCell();
                        tr.Cells.Add(tc);
                        tc.Controls.Add(CommonHelper.CreateHeader(this.UserID, this.ShowHeaderImage));
                        tc.CssClass = "fbHeaderTitleBranded";
                    }

                    tr = new TableRow();
                    mainTable.Rows.Add(tr);
                    tc = new TableCell();
                    tc.Width = Unit.Percentage(30);
                    tr.Cells.Add(tc);
                    textWall = new TextBox();
                    textWall.TextMode = TextBoxMode.MultiLine;
                    textWall.MaxLength = 140;
                    textWall.CssClass = "fbTextBox";
                    tc.Controls.Add(textWall);

                    tr = new TableRow();
                    mainTable.Rows.Add(tr);
                    tc = new TableCell();
                    tc.HorizontalAlign = HorizontalAlign.Right;
                    tc.Width = Unit.Percentage(30);
                    tr.Cells.Add(tc);
                    buttonWriteOnWall = new Button();
                    buttonWriteOnWall.ForeColor = Color.White;
                    buttonWriteOnWall.BackColor = Color.FromArgb(84, 116, 186);
                    buttonWriteOnWall.Text = "Share";
                    buttonWriteOnWall.Click += new EventHandler(buttonWriteOnWall_Click);
                    tc.Controls.Add(buttonWriteOnWall);
                }
                catch (Exception Ex)
                {
                    LblMessage = new Label();
                    LblMessage.Text = Ex.Message;
                    this.Controls.Add(LblMessage);
                }
            }
            else
            {
                LblMessage = new Label();
                LblMessage.Text = "Please set the values of facebook settings in webpart properties section in edit mode.";
                this.Controls.Add(LblMessage);
            }
        }

        #endregion

        #region OnPreRender event

        protected override void OnPreRender(EventArgs e)
        {
            if (textWall != null)
            {
                textWall.Text = string.Empty;
                if (this.EnableShowUserName)
                {
                    //if anonymous access is enabled,then don't show any user name
                    if (SPContext.Current.Web.CurrentUser != null)
                    {
                        textWall.Text = SPContext.Current.Web.CurrentUser.Name + " : ";
                    }
                }
            }
        }

        #endregion

        #region 'Share' button click

        void buttonWriteOnWall_Click(object sender, EventArgs e)
        {
            try
            {

                string oAuthToken;
                bool userMemberOfGroup = false;

                string url = string.Format("https://graph.facebook.com/oauth/access_token?client_id={0}&redirect_uri={1}&client_secret={2}&code={3}&scope=publish_stream", OAuthClientID, OAuthRedirectUrl, OAuthClientSecret, OAuthCode);
                
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string retVal = reader.ReadToEnd();
                    oAuthToken = retVal.Substring(retVal.IndexOf("=") + 1, retVal.Length - retVal.IndexOf("=") - 1);
                }

                string postUrl = string.Empty;

                if (this.PostOnProfile)
                {
                    postUrl = string.Format("https://graph.facebook.com/me/feed?access_token={0}&message={1}", oAuthToken, textWall.Text.Trim());
                }
                else if (this.PostOnGroupWall)      //To Post on the Group wall
                {
                    //get the groups
                    JSONObject group = GetUserGroups(oAuthToken);

                    if (group.Dictionary["data"] != null)
                    {
                        //Check if the user is a group member
                        JSONObject[] userGroups = group.Dictionary["data"].Array;

                        if (userGroups.Length > 0)
                        {
                            foreach (JSONObject userGroup in userGroups)
                            {
                                if (userGroup.Dictionary["id"].String.Equals(this.OAuthPageID))
                                {
                                    //if its in the group form the url for posting the post
                                    userMemberOfGroup = true;
                                    postUrl = string.Format("https://graph.facebook.com/{0}/feed?access_token={1}&message={2}", this.OAuthPageID, oAuthToken, textWall.Text.Trim());
                                    break;
                                }
                            }
                            if (!userMemberOfGroup)
                            {
                                LblMessage = new Label();
                                LblMessage.Text = "You are not the member of the given group";
                                this.Controls.Add(LblMessage);
                            }
                        }
                        else
                        {
                            LblMessage = new Label();
                            LblMessage.Text = "Either you are not the member of the given group ID OR User groups permission has not been given to this application.In order for this application to post on your group wall as your account, you need to give this application 'Access User Groups' permission.Please go to the following url to grant this permission:" + string.Format("<a target='_blank' href='https://www.facebook.com/dialog/oauth?client_id={0}&redirect_uri={1}&scope=user_groups&response_type=token'>https://www.facebook.com/dialog/oauth?client_id={0}&redirect_uri={1}&scope=user_groups&response_type=token</a>", this.OAuthClientID, this.OAuthRedirectUrl);
                            this.Controls.Add(LblMessage);
                        }

                    }
                    else
                    {
                        LblMessage = new Label();
                        LblMessage.Text = "You are not the member of the given group";
                        this.Controls.Add(LblMessage);
                    }
                }
                else
                {
                    if (this.PostAsPage)
                    {

                        JSONObject me = GetUserPages(oAuthToken);

                        if (me.Dictionary["data"] != null)
                        {
                            JSONObject[] userAccounts = me.Dictionary["data"].Array;

                            if (userAccounts.Length > 0)
                            {
                                if (!userAccounts[0].Dictionary.ContainsKey("access_token"))
                                {
                                    LblMessage = new Label();
                                    LblMessage.Text = "Manage pages permission has not been given to this application.In order for this application to post on your page as your page's account, you need to give this application 'Manage Pages' permission.Please go to the following url to grant this permission:" + string.Format("<a target='_blank' href='https://www.facebook.com/dialog/oauth?client_id={0}&redirect_uri={1}&scope=manage_pages&response_type=token'>https://www.facebook.com/dialog/oauth?client_id={0}&redirect_uri={1}&scope=manage_pages&response_type=token</a>", this.OAuthClientID, this.OAuthRedirectUrl);
                                    this.Controls.Add(LblMessage);

                                }
                                else
                                {

                                    bool userAccountFound = false;

                                    foreach (JSONObject userAccount in userAccounts)
                                    {

                                        if (userAccount.Dictionary["id"].String.Equals(this.OAuthPageID.Trim()))
                                        {
                                            userAccountFound = true;
                                            postUrl = string.Format("https://graph.facebook.com/{0}/feed?access_token={1}&message={2}", this.OAuthPageID.Trim(), userAccount.Dictionary["access_token"].String, textWall.Text.Trim());
                                            break;
                                        }


                                    }


                                    if (!userAccountFound)
                                    {
                                        LblMessage = new Label();
                                        LblMessage.Text = "The given page was not found in the list of pages.Please make sure that this user is the admin of the page that you have specified.";
                                        this.Controls.Add(LblMessage);

                                    }

                                }

                            }
                            else
                            {
                                LblMessage = new Label();
                                LblMessage.Text = "The given page was not found.";
                                this.Controls.Add(LblMessage);
                            }

                        }
                        else
                        {
                            LblMessage = new Label();
                            LblMessage.Text = "No pages found for the given account.";
                            this.Controls.Add(LblMessage);

                        }

                    }
                    else
                    {

                        postUrl = string.Format("https://graph.facebook.com/{0}/feed?access_token={1}&message={2}", this.OAuthPageID, oAuthToken, textWall.Text.Trim());

                    }

                }


                if (!String.IsNullOrEmpty(postUrl))
                {

                    HttpWebRequest request2 = WebRequest.Create(postUrl) as HttpWebRequest;
                    request2.Method = "post";
                    using (HttpWebResponse response2 = request2.GetResponse() as HttpWebResponse)
                    {
                        StreamReader reader = new StreamReader(response2.GetResponseStream());
                        string retVal = reader.ReadToEnd();

                        if (!String.IsNullOrEmpty(retVal))
                        {
                            LblMessage = new Label();
                            if (this.PostOnProfile)
                            {
                                LblMessage.Text = "Message successfully posted on wall.";
                            }
                            else if (this.PostOnGroupWall)
                            {
                                LblMessage.Text = "Message successfully posted on your Group wall.";
                            }
                            else
                            {
                                LblMessage.Text = "Message successfully posted on page.";
                            }

                            this.Controls.Add(LblMessage);

                        }
                    }

                }


            }
            catch (Exception Ex)
            {
                LblMessage = new Label();
                LblMessage.Text = "An error occurred while posting on wall:" + Ex.Message;
                this.Controls.Add(LblMessage);
            }
        }

        #endregion
    
        #region Method to get user accounts(apps,pages etc.) for the given access token

        private JSONObject GetUserPages(string oAuthToken)
        {
            JSONObject obj = null;
            string url;
            HttpWebRequest request;

            try
            {
                url = string.Format("https://graph.facebook.com/me/accounts?access_token={0}", oAuthToken);
                request = WebRequest.Create(url) as HttpWebRequest;
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string retVal = reader.ReadToEnd();

                    obj = JSONObject.CreateFromString(retVal);
                    if (obj.IsDictionary && obj.Dictionary.ContainsKey("error"))
                    {
                        throw new Exception(obj.Dictionary["error"].Dictionary["type"].String, new Exception(obj.Dictionary["error"].Dictionary["message"].String));
                    }
                }
            }
            catch (Exception Ex)
            {
                LblMessage = new Label();
                LblMessage.Text = Ex.Message;
                this.Controls.Add(LblMessage);
            }
            return obj;
        }

        private JSONObject GetUserGroups(string oAuthToken)
        {
            JSONObject obj = null;
            string url;
            HttpWebRequest request;

            try
            {
                url = string.Format("https://graph.facebook.com/me/groups?access_token={1}", this.OAuthPageID.Trim().ToString(), oAuthToken);

                request = WebRequest.Create(url) as HttpWebRequest;
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string retVal = reader.ReadToEnd();

                    obj = JSONObject.CreateFromString(retVal);
                    if (obj.IsDictionary && obj.Dictionary.ContainsKey("error"))
                    {
                        throw new Exception(obj.Dictionary["error"].Dictionary["type"].String, new Exception(obj.Dictionary["error"].Dictionary["message"].String));
                    }
                }
            }
            catch (Exception Ex)
            {
                LblMessage = new Label();
                LblMessage.Text = Ex.Message;
                this.Controls.Add(LblMessage);
            }
            return obj;
        }

        #endregion

        #region IWebEditable members

        EditorPartCollection IWebEditable.CreateEditorParts()
        {
            EditorPartCollection defaultEditors = base.CreateEditorParts();
            List<EditorPart> editors = new List<EditorPart>();
            editors.Add(new WriteOnWallEditorPart(this.ID));
            return new EditorPartCollection(defaultEditors, editors);
        }

        object IWebEditable.WebBrowsableObject
        {
            get { return this; }
        }

        #endregion
    }
}
