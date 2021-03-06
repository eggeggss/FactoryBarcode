﻿using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Widget;
using Android.Util;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using Java.Interop;
using MyUtil;
using Newtonsoft.Json;
using PublicStruct.cs;
using System;
using ZXing.Mobile;

namespace FactoryBarcode
{
    public interface IBarCodeToHtmlBehavior
    {
        void CallWebViewSendData();
    }

    [Activity(Label = "HTML5 Barcoder", MainLauncher = false, Icon = "@drawable/icon512", ConfigurationChanges = Android.Content.PM.ConfigChanges.Keyboard | Android.Content.PM.ConfigChanges.KeyboardHidden | Android.Content.PM.ConfigChanges.Orientation, ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class MainActivity : Activity, IBarCodeToHtmlBehavior, CustomSwipeRefreshLayout.CanChildScrollUpCallback
    {
    
        private WebView wv;
        private CustomSwipeRefreshLayout refresher;
        private Item item;
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            RequestWindowFeature(WindowFeatures.NoTitle);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            /**/
            var j = this.Intent.GetStringExtra("ItemRow");
            item = JsonConvert.DeserializeObject<Item>(j);

            ColorDrawable color = new ColorDrawable(Color.OrangeRed);

            var actionBar=FindViewById<LinearLayout>(Resource.Id.actonBar);

            actionBar.SetBackgroundDrawable(color);

            FindViewById<TextView>(Resource.Id.txtTitle).Text = item.Descrip;
            //back
            ImageButton btnBack = this.FindViewById<ImageButton>(Resource.Id.btnBack);
            btnBack.Click += (s1, e1) =>
            {
                this.Finish();
                // wv.GoBack();
            };
            btnBack.SetBackgroundResource(Resource.Drawable.back);
            //scan
            ImageButton btnScan = this.FindViewById<ImageButton>(Resource.Id.btnScan);

            btnScan.Click += (s1, e1) =>
           {
               CallWebViewSendData();
           };

            btnScan.SetBackgroundResource(Resource.Drawable.Barcode2);

            Information info = new Information() { user_id = "A110018", user_name = "Roger Roan" };
            String content = JsonConvert.SerializeObject(info, Formatting.None);

            MobileBarcodeScanner.Initialize(Application);

            wv = this.FindViewById<WebView>(Resource.Id.webview);
            
            refresher = FindViewById<CustomSwipeRefreshLayout>(Resource.Id.refresher);

            refresher.setCanChildScrollUpCallback(this);

            refresher.SetColorScheme(Resource.Color.xam_dark_blue,
                                      Resource.Color.xam_purple,
                                      Resource.Color.xam_gray,
                                      Resource.Color.xam_green);
            
            wv.Settings.JavaScriptEnabled = true;

            wv.AddJavascriptInterface(new JsInteration(this), "control");

            wv.Settings.SetSupportZoom(true);
            wv.Settings.BuiltInZoomControls = true;
            

            wv.Settings.UseWideViewPort = true;
            wv.Settings.LoadWithOverviewMode = true;

            WebChromeClient wc = new WebChromeClient();
            
            wv.SetWebChromeClient(wc);

            MyWebViewClient wvc = new MyWebViewClient(refresher);

            wv.SetWebViewClient(wvc);

            wv.LoadUrl(item.Link);
            
            refresher.Refresh += delegate
            {
                wv.Reload();
            };
            
        }

        protected override void OnStart()
        {
            base.OnStart();

        }

        protected override void OnStop()
        {
            base.OnStop();

           // wv.LoadUrl("about:blank");

            //wv.Destroy();
        }

        public async void CallWebViewSendData()
        {
            var scanner = new ZXing.Mobile.MobileBarcodeScanner();
            
            scanner.AutoFocus();
            var options = new ZXing.Mobile.MobileBarcodeScanningOptions();
            
            options.DelayBetweenAnalyzingFrames = 1000;
      

            scanner.TopText = "Scanning.."; 
                     
            var result = await scanner.Scan(options);

            if (result != null)
            {
                wv.LoadUrl("javascript:msg(" + @"'" + result.Text + @"'" + ");");
            }
        }

        public override void OnConfigurationChanged(Configuration newConfig)
        {
            //base.OnConfigurationChanged(newConfig);
        }

        public bool CanSwipeRefreshChildScrollUp()
        {
            //System.Diagnostics.Debug.WriteLine(wv.ScrollY.ToString(), "debug");
            return wv.ScrollY > 0;
            //throw new NotImplementedException();
        }

        public static implicit operator MainActivity(Index1Activity v)
        {
            throw new NotImplementedException();
        }
    }
    
    public class CustomSwipeRefreshLayout: SwipeRefreshLayout
    {
        public CustomSwipeRefreshLayout(Context context):base(context)
        {
        }
        public CustomSwipeRefreshLayout(Context context, IAttributeSet attrs) : base(context,attrs)
        {
        }
        
        private CanChildScrollUpCallback mCanChildScrollUpCallback;

        public interface CanChildScrollUpCallback
        {
            
            bool CanSwipeRefreshChildScrollUp();
        }

        public void setCanChildScrollUpCallback(CanChildScrollUpCallback canChildScrollUpCallback)
        {
            mCanChildScrollUpCallback = canChildScrollUpCallback;
        }

        public override bool CanChildScrollUp()
        {
            if (mCanChildScrollUpCallback != null)
            {
                return mCanChildScrollUpCallback.CanSwipeRefreshChildScrollUp();
            }
            return base.CanChildScrollUp();
        }
        
    }


    public class MyWebViewClient : WebViewClient
    {
        public CustomSwipeRefreshLayout _refresher;

        public MyWebViewClient(CustomSwipeRefreshLayout refresher)
        {
            this._refresher = refresher;
        }
        
        public override void OnPageStarted(WebView view, string url, Bitmap favicon)
        {
            base.OnPageStarted(view, url, favicon);
            this._refresher.Refreshing = true;
        }

        public override void OnPageFinished(WebView view, string url)
        {
            base.OnPageFinished(view, url);
            this._refresher.Refreshing = false;
        }

        public bool shouldOverrideUrlLoading(WebView view, String url)
        {
            view.LoadUrl(url);
            return true;
        }


    }

    public class Information
    {
        public String user_name { set; get; }
        public String user_id { set; get; }
    }

    public class JsInteration : Java.Lang.Object
    {
        private Context context;

        public JsInteration(Context context)
        {
            this.context = context;
        }

        [Export]
        [JavascriptInterface]
        public void HtmlMessage(String msg)
        {
            IBarCodeToHtmlBehavior Ibarcodebehavior = (MainActivity)this.context;
            Ibarcodebehavior.CallWebViewSendData();
        }
    }
}