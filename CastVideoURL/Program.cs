using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Drawing;
using MonoTouch.CoreGraphics;
using ChromeCast;
using System.Threading.Tasks;



namespace SingleFileSolution
{
	public class TypedArgs<T> : EventArgs 
	{
		public T Value { get; protected set; }

		public TypedArgs(T arg)
		{
			this.Value = arg;
		}
	}

	public class DeviceManagerListener : GCKDeviceManagerListener
	{
		public override void DeviceDidComeOnline(GCKDevice device)
		{
			Console.WriteLine("Device did come online " + device);
			CameOnline(this, new TypedArgs<GCKDevice>(device));
		}

		public override void DeviceDidGoOffline(GCKDevice device)
		{
			Console.WriteLine("Device did go offline " + device);
			WentOffline(this, new TypedArgs<GCKDevice>(device));
		}

		public override void ScanStarted()
		{
			Console.WriteLine("Began scanning...");
		}

		public override void ScanStopped()
		{
			Console.WriteLine("Stopped scanning...");
		}

		public event EventHandler<TypedArgs<GCKDevice>> CameOnline = delegate {};
		public event EventHandler<TypedArgs<GCKDevice>> WentOffline = delegate {};
	}

	public class ChromeCaster : GCKApplicationSessionDelegate
	{
		//TODO: You'll need to set these based on your Google "whitelist" application : https://docs.google.com/forms/d/1E-vka5QP8LkF0nbfz-omN1DjNSX1uLGyqHdbpEFh6zg/viewform
		private const string WHITELISTED_URL = "http://10.0.1.35/XamCast";
		private const string APPLICATION_GUID = "93d43262-ffff-ffff-ffff-5249f0766cc";


		private const string THUMBNAIL_URL = "http://xamarin.com/guide/img/xs-icon.png";

		GCKApplicationSession session;
		NSUrl videoToPlay;
		NSUrl thumbnailUrl = NSUrl.FromString(THUMBNAIL_URL);

		public ChromeCaster()
		{
		}

		public void Initialize()
		{
			var userAgent = "net.knowing.xamxcast";
			var gckContext = new GCKContext(userAgent);
			var deviceManager = new GCKDeviceManager(gckContext);
			var dmListener = new DeviceManagerListener();
			dmListener.CameOnline += (s,e) => {
				session = CreateSession(gckContext, e.Value);
				SessionCreated(this, new TypedArgs<GCKApplicationSession>(session));
			};
			deviceManager.AddListener(dmListener);
			deviceManager.StartScan();
		}

		private GCKApplicationSession CreateSession(GCKContext context, GCKDevice device)
		{
			var session = new GCKApplicationSession(context, device);
			session.Delegate = this;
			return session;
		}

		public void Cast(NSUrl url)
		{
			//Precondition: session != null
			videoToPlay = url;
			var args = new GCKMimeData(WHITELISTED_URL, "text/plain");
			//This GUID defines the URL to which the Chromecast will attach (in my case, http://10.0.1.35/XamCast/) 
			var didStart = session.StartSessionWithApplication(APPLICATION_GUID, args);
			Console.WriteLine("Did start? " + didStart);
		}

		public override void ApplicationSessionDidStart()
		{
			var channel = session.Channel; 
			if(channel == null)
			{
				Console.WriteLine("Channel is null");
			}
			else
			{
				Console.WriteLine("We have a channel");
				var mpms = new GCKMediaProtocolMessageStream();
				Console.WriteLine("Initiated ramp");
				channel.AttachMessageStream(mpms);

				LoadMedia(mpms);
			}
		}

		private void LoadMedia(GCKMediaProtocolMessageStream mpms)
		{
			Console.WriteLine("Loading media...");
			var mediaUrl = videoToPlay;
			var mediaContentId = mediaUrl.ToString();
			var dict = new NSDictionary();
			var mData = new GCKContentMetadata("Video", thumbnailUrl, dict);

			Console.WriteLine(mData);
			var cmd = mpms.LoadMediaWithContentID(mediaContentId, mData, true);
			Console.WriteLine("Command executed?  " + cmd);
		}

		public override void ApplicationSessionDidEnd(GCKApplicationSessionError error)
		{
			Console.WriteLine("Session did end");
		}

		public override void ApplicationSessionDidFailToStart(GCKApplicationSessionError error)
		{
			var baseError = error.CausedByError;

			var code = error.Code;
			OutputError(code);

			var errDom = error.Domain;

			var desc = error.LocalizedDescription;

			var x = error.UserInfo;

			Console.WriteLine("That didn't work : " + error.DebugDescription + " code = " + code);
		}

		private void OutputError(int code)
		{
			if(code == GCKApplicationSessionError.GCKApplicationSessionErrorCodeApplicationStopped)
			{
				Console.WriteLine("Application Stopped");
			}
			if(code == GCKApplicationSessionError.GCKApplicationSessionErrorCodeChannelDisconnected)
			{
				Console.WriteLine("Channel Disconnected");
			}
			if(code == GCKApplicationSessionError.GCKApplicationSessionErrorCodeFailedToConnectChannel)
			{
				Console.WriteLine("Failed to connect to channel");
			}
			if(code == GCKApplicationSessionError.GCKApplicationSessionErrorCodeFailedToCreateChannel)
			{
				Console.WriteLine("Failed to create channel");
			}
			if(code == GCKApplicationSessionError.GCKApplicationSessionErrorCodeFailedToQueryApplication)
			{
				Console.WriteLine("Failed to query application");
			}
			if(code == GCKApplicationSessionError.GCKApplicationSessionErrorCodeFailedToStartApplication)
			{
				Console.WriteLine("Failed to start application");
			}
			if(code == GCKApplicationSessionError.GCKApplicationSessionErrorCodeUnknownError)
			{
				Console.WriteLine("Unknown error");
			}
		}


		public event EventHandler<TypedArgs<GCKApplicationSession>> SessionCreated = delegate {};
	}

	public class CastAnUrlView : UIView
	{
		UIButton castButton;
		UITextView urlView;

		public CastAnUrlView()
		{
			BackgroundColor = UIColor.LightGray;

			var hPadding = 10;
			var vPadding = 10;
			var componentHeight = 30;
			var buttonWidth = 100;

			var lbl = new UILabel(new RectangleF(hPadding, vPadding, UIScreen.MainScreen.Bounds.Width - hPadding * 2, componentHeight));
			lbl.Text = "Enter video URL";
			AddSubview(lbl);

			urlView = new UITextView(new RectangleF(hPadding, vPadding * 2 + componentHeight, UIScreen.MainScreen.Bounds.Width - hPadding * 2, componentHeight));
			urlView.Text = "http://";
			AddSubview(urlView);

			castButton = UIButton.FromType(UIButtonType.RoundedRect);
			var buttonY = vPadding * 4 + componentHeight * 2;
			castButton.Frame = new RectangleF(hPadding, buttonY, buttonWidth, componentHeight);
			castButton.TouchUpInside += (o,e) => CastRequested(this, new TypedArgs<String>(urlView.Text));

			castButton.SetTitle("Cast it", UIControlState.Normal);
			castButton.Enabled = false;
			AddSubview(castButton);
		}

		public void CastEnable(bool b)
		{
			castButton.Enabled = b;
		}

		public event EventHandler<TypedArgs<string>> CastRequested = delegate {};
	}

	public class CastViewController : UIViewController
	{
		ChromeCaster chromeCaster;

		public CastViewController() : base ()
		{
		}

		public override void DidReceiveMemoryWarning()
		{
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning();
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();


			
			var view = new CastAnUrlView();
			view.CastRequested += (o,e) => {
				var url = StringToUrl(e.Value);
				chromeCaster.Cast(url);
			};

			chromeCaster = new ChromeCaster();
			//Want to be explicit about this need: bg thread segfaults
			InvokeOnMainThread(() => chromeCaster.Initialize());
			chromeCaster.SessionCreated += (s,e) => view.CastEnable(true);
					
			this.View = view;
		}

		private NSUrl StringToUrl(string s)
		{
			try
			{
				return NSUrl.FromString(s);
			}
			catch(Exception x)
			{
				new UIAlertView("Trouble", x.ToString(), null, "OK", null).Show();
				return null;
			}
		}
	}

	[Register ("AppDelegate")]
	public  class AppDelegate : UIApplicationDelegate
	{
		UIWindow window;
		CastViewController viewController;

		public override bool FinishedLaunching(UIApplication app, NSDictionary options)
		{
			window = new UIWindow(UIScreen.MainScreen.Bounds);

			viewController = new CastViewController();
			window.RootViewController = viewController;

			window.MakeKeyAndVisible();
			
			return true;
		}
	}

	public class Application
	{
		static void Main(string[] args)
		{
			UIApplication.Main(args, null, "AppDelegate");
		}
	}
}

