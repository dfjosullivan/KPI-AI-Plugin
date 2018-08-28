// 
// Copyright (C) 2018, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using NinjaTrader.Gui.Tools;

#endregion

namespace NinjaTrader.NinjaScript.ShareServices
{
	public class Facebook : ShareService
	{
		private object icon;
		private const string appVersion = "v2.10";

		private class FacebookAuthJsonResultStub
		{
			public FacebookAuthJsonResult data { get; set; } 
		}

		private class FacebookAuthJsonResult
		{
			public string	app_id		{ get; set; }
			public string	user_id		{ get; set; }
			public string[] scopes		{ get; set; }
		}

		/// <summary>
		/// This MUST be overridden for any custom service properties to be copied over when instances of the service are created
		/// </summary>
		/// <param name="ninjaScript"></param>
		public override void CopyTo(NinjaScript ninjaScript)
		{
			base.CopyTo(ninjaScript);

			// Recompiling NinjaTrader.Custom after a Share service has been added will cause the Type to change.
			//  Use reflection to set the appropriate properties, rather than casting ninjaScript to Facebook.
			PropertyInfo[] props = ninjaScript.GetType().GetProperties();
			foreach (PropertyInfo pi in props)
			{
				if (pi.Name == "OAuth_Token")
					pi.SetValue(ninjaScript, OAuth_Token);
				else if (pi.Name == "FacebookUserId")
					pi.SetValue(ninjaScript, FacebookUserId);
				else if (pi.Name == "UserName")
					pi.SetValue(ninjaScript, UserName);
			}
		}

		public override object Icon
		{
			get
			{
				if (icon == null)
					icon = Application.Current.TryFindResource("ShareIconFacebook");
				return icon;
			}
		}

		public async override Task OnAuthorizeAccount()
		{
			//Here we go through the OAuth 2.0 sign in flow
			#region Facebook Invoke Login Dialog
			const string oauth_request_token_url	= "https://www.facebook.com/dialog/oauth?";
			const string oauth_app_id				= "895600370523827";
			const string oauth_callback				= "https://www.facebook.com/connect/login_success.html";
			const string navigationUri = oauth_request_token_url +
										"client_id="			+ oauth_app_id		+ "&" +
										"redirect_uri="			+ oauth_callback	+ "&" +
										"response_type="		+ "token"			+ "&" +
										"scope="				+ "publish_actions";

			//We're going to display a webpage in an NTWindow so the user can authorize our app to post on their behalf.
			//Because of WPF/WinForm airspace issues (see http://msdn.microsoft.com/en-us/library/aa970688.aspx for the gory details), 
			//	and because we want to have our pretty NT-styled windows, we need to finagle things a bit.
			//	1.) Create a modal NTWindow that will pop up when the user clicks "Connect"
			//	2.) Create a borderless window that will actually host the WebBrowser control
			//	3.) A window can have one Content object, so add a grid to the Window hosting the WebBrowser, and make the WeBrowser a child of the grid
			//	4.) Add another grid to the modal NTWindow. We'll use this to place where the WebBrowser goes
			//	5.) Handle the LocationChanged event for the NTWindow and the SizeChanged event for the placement grid. This will take care of making
			//		the hosted WebBrowser control look like it's part of the NTWindow
			//	6.) Make sure the Window hosting the WebBrowser is set to be TopMost so it appears on top of the NTWindow.
			NTWindow authWin = new NTWindow
			{
				Caption					= Custom.Resource.GuiAuthorize,
				IsModal					= true,
				Height					= 650,
				Width					= 900,
			};

			Window webHost = new Window
			{
				ResizeMode			= ResizeMode.NoResize,
				ShowInTaskbar		= false,
				WindowStyle			= WindowStyle.None,
			};

			WebBrowser browser = new WebBrowser
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment	= VerticalAlignment.Stretch,
			};

			Grid grid = new Grid();
			grid.Children.Add(browser);
			webHost.Content = grid;

			Grid placementGrid = new Grid();
			authWin.Content = placementGrid;
			
			authWin.LocationChanged		+= (o, e) => OnSizeLocationChanged(placementGrid, webHost);
			placementGrid.SizeChanged	+= (o, e) => OnSizeLocationChanged(placementGrid, webHost);

			string oauth_token = string.Empty;

			browser.Navigating += (o, e) =>
			{
				if (e.Uri.Host == "www.facebook.com")
				{
					if (e.Uri.Fragment.StartsWith("#access_token"))
					{
						//Successfully authorized! :D
						string		query = e.Uri.Fragment.TrimStart('#');
						string[]	pairs = query.Split('&');
						foreach (string pair in pairs)
						{
							string[] keyvalue = pair.Split('=');
							if (keyvalue[0] == "access_token")
								oauth_token = keyvalue[1];
						}

						authWin.DialogResult	= true;
						OAuth_Token				= oauth_token;
						authWin.Close();
					}
					else if (e.Uri.Query.StartsWith("?error"))
					{
						//User denied authorization :'(
						authWin.DialogResult = false;
						authWin.Close();
					}
				}
			};
			authWin.Closing += (o, e) => webHost.Close();

			browser.Navigate(new Uri(navigationUri));
			webHost.Visibility	= Visibility.Visible;
			webHost.Topmost		= true;
			authWin.ShowDialog();

			if (authWin.DialogResult != true) return;
			#endregion

			#region Facebook Authorize
			string result = await Core.Globals.ExchangeFacebookTokenAsync(oauth_token);

			if (result.StartsWith("ShareNotAuthorized"))
			{	//If facebook can't or won't authenticate the request, you'll get a 401 response
				LogAndPrint(typeof(Custom.Resource), "ShareNotAuthorized", new[] { result }, Cbi.LogLevel.Error);
				return;
			}
			else if (result.StartsWith("ShareForbidden"))
			{
				LogAndPrint(typeof(Custom.Resource), "ShareForbidden", new[] { result }, Cbi.LogLevel.Error);
				return;
			}

			using (HttpClient client = new HttpClient())
			{
				FacebookAuthJsonResultStub jsonResult = new JavaScriptSerializer().Deserialize<FacebookAuthJsonResultStub>(result);
				if (jsonResult == null || jsonResult.data == null)
				{
					LogAndPrint(typeof(Custom.Resource), "ShareFacebookNoResult", null, Cbi.LogLevel.Error);
					return;
				}

				if (jsonResult.data.scopes == null || jsonResult.data.scopes.Length == 0)
				{
					LogAndPrint(typeof(Custom.Resource), "ShareFacebookScopesNotFound", null, Cbi.LogLevel.Error);
					return;
				}

				if (!jsonResult.data.scopes.Contains("publish_actions"))
				{
					//User approved our app but forbade us to post on their behalf, so we just return
					LogAndPrint(typeof(Custom.Resource), "ShareFacebookPermissionDenied", null, Cbi.LogLevel.Error);
					return;
				}

				if (string.IsNullOrEmpty(jsonResult.data.app_id) || jsonResult.data.app_id != oauth_app_id)
				{
					LogAndPrint(typeof(Custom.Resource), "ShareFacebookCouldNotVerifyToken", null, Cbi.LogLevel.Error);
					return;
				}

				if (string.IsNullOrEmpty(jsonResult.data.user_id))
				{
					LogAndPrint(typeof(Custom.Resource), "ShareFacebookCouldNotRetrieveUser", null, Cbi.LogLevel.Error);
					return;
				}
					
				FacebookUserId = jsonResult.data.user_id;

				//Get user name from userId
				string				nameRequestUrl		= "https://graph.facebook.com/" + appVersion + "/" + FacebookUserId + "?fields=name&access_token=" + OAuth_Token;
				HttpResponseMessage facebookResponse	= await client.GetAsync(nameRequestUrl);
				result									= new StreamReader(facebookResponse.Content.ReadAsStreamAsync().Result).ReadToEnd();
				if (!facebookResponse.IsSuccessStatusCode)
				{
					switch (facebookResponse.StatusCode)
					{
						case HttpStatusCode.Unauthorized:
							//If facebook can't or won't authenticate the request, you'll get a 401 response
							LogAndPrint(typeof(Custom.Resource), "ShareNotAuthorized", new[] { result }, Cbi.LogLevel.Error);
							break;
						case HttpStatusCode.Forbidden:
							LogAndPrint(typeof(Custom.Resource), "ShareForbidden", new[] { result }, Cbi.LogLevel.Error);
							break;
						default:
							LogAndPrint(typeof(Custom.Resource), "ShareNonSuccessCode", new object[] { facebookResponse.StatusCode, result }, Cbi.LogLevel.Error);
							break;
					}

					return;
				}

				if (!string.IsNullOrEmpty(result))
				{
					Dictionary<string,object> nameResults = new JavaScriptSerializer().DeserializeObject(result) as Dictionary<string,object>;
					if (nameResults != null && nameResults.ContainsKey("name"))
						UserName = nameResults["name"] as string;
				}

				if (string.IsNullOrEmpty(UserName))
				{
					LogAndPrint(typeof(Custom.Resource), "ShareFacebookCouldNotRetrieveUser", null, Cbi.LogLevel.Error);
					return;
				}
			}
			#endregion

			if (!string.IsNullOrEmpty(OAuth_Token))
				IsConfigured = true;
		}

		public async override Task OnShare(string text, string imageFilePath)
		{
			if (string.IsNullOrEmpty(imageFilePath))
			{
				using (HttpClient client = new HttpClient())
				{
					string facebookStatusUrl	= "https://graph.facebook.com/" + appVersion + "/" + FacebookUserId + "/feed";
					string postContent =
						"message="			+ Core.Globals.UrlEncode(text)			+ "&" +
						"access_token="		+ OAuth_Token;
					HttpContent status = new StringContent(postContent);

					HttpResponseMessage facebookResponse = await client.PostAsync(facebookStatusUrl, status);
					string result = new StreamReader(facebookResponse.Content.ReadAsStreamAsync().Result).ReadToEnd();
					
					if (!facebookResponse.IsSuccessStatusCode)
					{
						switch (facebookResponse.StatusCode)
						{
							case HttpStatusCode.Unauthorized:
								//If facebook can't or won't authenticate the request, you'll get a 401 response
								LogAndPrint(typeof(Custom.Resource), "ShareNotAuthorized", new[] { result }, Cbi.LogLevel.Error);
								break;
							case HttpStatusCode.Forbidden:
								LogAndPrint(typeof(Custom.Resource), "ShareForbidden", new[] { result }, Cbi.LogLevel.Error);
								break;
							default:
								LogAndPrint(typeof(Custom.Resource), "ShareNonSuccessCode", new object[] { facebookResponse.StatusCode, result }, Cbi.LogLevel.Error);
								break;
						}
					}
					else
						LogAndPrint(typeof(Custom.Resource), "ShareFacebookSentSuccessfully", new[] { Name }, Cbi.LogLevel.Information);
				}
			}
			else
			{
				string facebookPhotoUrl = "https://graph.facebook.com/" + appVersion + "/"	+ FacebookUserId	+ "/photos?"	+
											"message="					+ Core.Globals.UrlEncode(text)			+ "&" +
											"access_token="				+ OAuth_Token;

				if (!File.Exists(imageFilePath))
				{
					LogAndPrint(typeof(Custom.Resource), "ShareImageNoLongerExists", new[] { imageFilePath }, Cbi.LogLevel.Error);
					return;
				}

				byte[]		imageBytes		= File.ReadAllBytes(imageFilePath);
				HttpContent imageContent	= new ByteArrayContent(imageBytes);

				using (HttpClient client = new HttpClient())
					using (MultipartFormDataContent formData = new MultipartFormDataContent())
					{
						formData.Add(imageContent, "source", "photo.png");	//<-- The filename parameter was required for this to work, as opposed to Twitter, which did not require it
						
						HttpResponseMessage facebookResponse = await client.PostAsync(facebookPhotoUrl, formData);
						string result = new StreamReader(facebookResponse.Content.ReadAsStreamAsync().Result).ReadToEnd();

						if (!facebookResponse.IsSuccessStatusCode)
						{
							switch (facebookResponse.StatusCode)
							{
								case HttpStatusCode.Unauthorized:
									//If facebook can't or won't authenticate the request, you'll get a 401 response
									LogAndPrint(typeof(Custom.Resource), "ShareNotAuthorized", new[] { result }, Cbi.LogLevel.Error);
									break;
								case HttpStatusCode.Forbidden:
									LogAndPrint(typeof(Custom.Resource), "ShareForbidden", new[] { result }, Cbi.LogLevel.Error);
									break;
								default:
									LogAndPrint(typeof(Custom.Resource), "ShareNonSuccessCode", new object[] { facebookResponse.StatusCode, result }, Cbi.LogLevel.Error);
									break;
							}
						}
						else
							LogAndPrint(typeof(Custom.Resource), "ShareFacebookSentSuccessfully", new[] { Name }, Cbi.LogLevel.Information);
					}
			}
		}
		
		private static void OnSizeLocationChanged(FrameworkElement placementTarget, Window webHost)
		{
			//Here we set the location and size of the borderless Window hosting the WebBrowser control. 
			//	This is based on the location and size of the child grid of the NTWindow. When the grid changes,
			//	the hosted WebBrowser changes to match.
			if (webHost.Visibility == Visibility.Visible)
				webHost.Show();

			webHost.Owner							= Window.GetWindow(placementTarget);
			Point				locationFromScreen	= placementTarget.PointToScreen(new Point(0, 0));
			PresentationSource	source				= PresentationSource.FromVisual(webHost);
			if (source != null && source.CompositionTarget != null)
			{
				Point targetPoints	= source.CompositionTarget.TransformFromDevice.Transform(locationFromScreen);
				webHost.Left		= targetPoints.X;
				webHost.Top			= targetPoints.Y;
			}

			webHost.Width	= placementTarget.ActualWidth;
			webHost.Height	= placementTarget.ActualHeight;
		}

		protected override void OnStateChange()
		{			
			if (State == State.SetDefaults)
			{
				CharacterLimit				= int.MaxValue;
				CharactersReservedPerMedia	= int.MaxValue;
				IsConfigured				= false;
				IsDefault					= false;
				IsImageAttachmentSupported	= true;
				Name						= Custom.Resource.FacebookServiceName;
				Signature					= string.Empty;
				UserName					= string.Empty;
				UseOAuth					= true;
			}
		}

		#region Properties
		[Gui.Encrypt]
		[Browsable(false)]
		public string FacebookUserId { get; set; }

		[Gui.Encrypt]
		[Browsable(false)]
		public string OAuth_Token { get; set; }

		[ReadOnly(true)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "ShareServiceUserName", GroupName = "ShareServiceParameters", Order = 1)]
		public string UserName
		{ get; set; }
		#endregion
	}
}
