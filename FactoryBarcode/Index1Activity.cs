using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Widget;
using Android.Views;
using Android.Widget;
using Common.Util;
using Java.IO;
using MyUtil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PublicStruct.cs;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static Android.Views.View;

namespace FactoryBarcode
{
    public interface ICommucatable
    {
        void DeleteItem(Item item);
    }

    [Activity(Label = "HTML5 BarCoder", MainLauncher = true, Icon = "@drawable/icon512")]
    public class Index1Activity : Activity, ICommucatable
    {
        public ItemDB _itemdb;
        private MyList adapter;
        public Int32 mDeviceWidth, mDeviceHeight;
        
        public AppSetting AppSetting {
            
            get
            {
                var appsettings = _itemdb.SelectAppSetting();
                AppSetting appsetting = null;
                if (appsettings.Count > 0)
                {
                    appsetting = appsettings[0];
                }
                else
                {
                    appsetting = new AppSetting();
                    appsetting.WebAPI = Resource2.DefaultWebAPI;
                }
                return appsetting;
            }

        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.action_add:
                    View v = this.LayoutInflater.Inflate(Resource.Layout.Dialog, null);

                    EventHandler<DialogClickEventArgs> ok = new EventHandler<DialogClickEventArgs>((s2, e2) =>
                    {
                        String descrip = v.FindViewById<EditText>(Resource.Id.editDescrip).Text;

                        if (!(String.IsNullOrEmpty(descrip) || descrip == ""))
                        {

                            String uri = v.FindViewById<EditText>(Resource.Id.editUri).Text;

                            Item itemNew = new Item() { Descrip = descrip, Link = uri };

                            _itemdb.InsertItem(itemNew);
                            adapter.List.Add(itemNew);
                            adapter.List.Sort();
                            adapter.NotifyDataSetChanged();
                        }
                    });

                    Util.InputDialog(this, v, ok, null);

                    return true;

                case Resource.Id.action_settings:

                    View viewAppsetting = this.LayoutInflater.Inflate(Resource.Layout.AppSetting, null);
                    var editUri= viewAppsetting.FindViewById<EditText>(Resource.Id.editUri);
                    var appsettings = this._itemdb.SelectAppSetting();
                    AppSetting appsetting = this.AppSetting;
                    
                    editUri.Text = appsetting.WebAPI;

                    EventHandler<DialogClickEventArgs> oksetting = new EventHandler<DialogClickEventArgs>((s2, e2) =>
                    {
                        String WebUri= viewAppsetting.FindViewById<EditText>(Resource.Id.editUri).Text;

                        appsetting.WebAPI = WebUri;

                        this._itemdb.UpdateOrInsertAppSetting(appsetting);

                    });

                    Util.InputDialog(this, viewAppsetting, oksetting, null);

                    //Toast.MakeText(this, "setting", ToastLength.Short).Show();
                    return true;

                default:
                    // If we got here, the user's action was not recognized.
                    // Invoke the superclass to handle it.
                    return base.OnOptionsItemSelected(item);
            }
            return base.OnOptionsItemSelected(item);
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            RequestWindowFeature(WindowFeatures.ActionBar);
            SetContentView(Resource.Layout.Index1);

            Display display= this.WindowManager.DefaultDisplay;

            this.mDeviceHeight = display.Height;
            this.mDeviceWidth = display.Width;

            ColorDrawable color = new ColorDrawable(Color.OrangeRed);

            this.ActionBar.SetBackgroundDrawable(color);
            //http://eggeggss.ddns.net/sse/Request.aspx?catelog=GetBarCodeItem
            List<Item> list = new List<Item>();
            list.Add(new Item() { Descrip = "DevExpress", Link = "https://www.devexpress.com/" });
            list.Add(new Item() { Descrip = "Xamarin", Link = "https://www.xamarin.com/" });
            list.Add(new Item() { Descrip = "Microsoft", Link = "https://www.microsoft.com/zh-cn" });
            list.Add(new Item() { Descrip = "百度雲", Link = "https://login.bce.baidu.com/" });
            list.Add(new Item() { Descrip = "小米官網", Link = "http://www.mi.com/tw/events/school831/" });
            list.Add(new Item() { Descrip = "奇摩", Link = "http://www.yahoo.com.tw" });

           // list.Add(new Item() { Descrip = "Xpage測試報表", Link = "http://arc-ap2.arcadyan.com.tw/GP/VendorReport.nsf/TEST.xsp" });

            //list.Add(new Item() { Descrip = "測試報表", Link = "http://eggeggss.ddns.net/notesbarcode/notesservice1.aspx" });

            string folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);

            _itemdb = new ItemDB(folder);

            List<Item> listFromDb = _itemdb.SelectItem();

            if (listFromDb.Count==0)
            {
                _itemdb.InsertAllItem(list);
                listFromDb = _itemdb.SelectItem();
            }
            var listview = this.FindViewById<ListView>(Resource.Id.listview);

            adapter = new MyList();

            adapter.Context = this;
            adapter.List = listFromDb;
            adapter.DeviceHeight = this.mDeviceHeight;
            adapter.DeviceWidth = this.mDeviceWidth;
            listview.Adapter = adapter;

            var refresh = this.FindViewById<SwipeRefreshLayout>(Resource.Id.refresher1);

            refresh.Refresh +=async (s1, e1) =>
            {
                try
                {
                    String uri = this.AppSetting.WebAPI;

                    if (!(String.IsNullOrEmpty(uri)))
                    {
                        
                        var items = await WebApi.DownloadJsonDataCustom<IEnumerable<Item>>(uri);
                        
                        foreach (var item in items)
                        {
                            _itemdb.InsertItem(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, "下載失敗 " + ex.Message, ToastLength.Short).Show();
                    //Util.Dialog(this, "Information", "Please Check NetWork Status", null, null);
                }
                var dbitems = _itemdb.SelectItem();
                adapter.List = dbitems;
                adapter.NotifyDataSetChanged();
                refresh.Refreshing = false;
                
            };
            // Create your application here
        }

        public void DeleteItem(Item item)
        {
            _itemdb.DeleteItem(item);
            // throw new NotImplementedException();
        }
    }

    public class MyList : BaseAdapter<Item>
    {
        public Index1Activity Context;
        public List<Item> List;
        public Int32 DeviceWidth,DeviceHeight;

        public override Item this[int position]
        {
            get
            {
                return this.List[position];
                //throw new NotImplementedException();
            }
        }

        public override int Count
        {
            get
            {
                return this.List.Count;
                //throw new NotImplementedException();
            }
        }

        public void DeleteItem(Item item)
        {
            throw new NotImplementedException();
        }

        public override long GetItemId(int position)
        {
            return position;
            //throw new NotImplementedException();
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            Button btnDescrip=null;
            //前面幾筆進來時回收區還是空的
            //註冊在這邊是因為後面的view都用這個回收的所以初始註冊一次即可
            if (convertView == null)
            {
                convertView = this.Context.LayoutInflater.Inflate(Resource.Layout.ItemRow, null);

                var btn_update = convertView.FindViewById<Button>(Resource.Id.btn_update);
                //opent
                btn_update.Click += (s1, e1) =>
                {
                    var view=this.Context.LayoutInflater.Inflate(Resource.Layout.Dialog,null);

                    var editDescript=view.FindViewById<EditText>(Resource.Id.editDescrip);
                    var editUri = view.FindViewById<EditText>(Resource.Id.editUri);

                    var thisbtn = s1 as Button;
                    Int32 ll_position = Convert.ToInt32(thisbtn.Tag);
                    var item_row = this.List[ll_position];
                    //String itemRowJson = JsonConvert.SerializeObject(item_row);

                    editDescript.Text = item_row.Descrip;
                    editUri.Text = item_row.Link;

                    EventHandler<DialogClickEventArgs> ok = new EventHandler<DialogClickEventArgs>((s2, e2) =>
                    {
                        String descrip = view.FindViewById<EditText>(Resource.Id.editDescrip).Text;
                        String uri = view.FindViewById<EditText>(Resource.Id.editUri).Text;

                        item_row.Descrip = descrip;
                        item_row.Link = uri;                     

                        this.Context._itemdb.UpdateItem(item_row);
                        
                        //Toast.MakeText(this, descrip, ToastLength.Short).Show();
                    });

                    Util.InputDialog(this.Context, view, ok, null);

                    /*
                    var thisbtn = s1 as Button;
                    Int32 ll_position = Convert.ToInt32(thisbtn.Tag);

                    var item_row = this.List[ll_position];
                    String itemRowJson = JsonConvert.SerializeObject(item_row);

                    Intent intent = new Intent();
                    intent.SetClass(this.Context, typeof(MainActivity));
                    intent.PutExtra("ItemRow", itemRowJson);

                    this.Context.StartActivity(intent);
                    */
                };
                //delete
                var btn_delete = convertView.FindViewById<Button>(Resource.Id.btn_delete);

                btn_delete.SetTextColor(Android.Graphics.Color.Red);

                btn_delete.Click += (s1, e1) =>
               {
                   EventHandler<DialogClickEventArgs> okDelegate = new EventHandler<DialogClickEventArgs>((s2, e2) =>
                   {
                       var delbtn = s1 as Button;
                       Int32 ll_position = Convert.ToInt32(delbtn.Tag);

                       var item_row = this.List[ll_position];

                       List.Remove(item_row);
                       ICommucatable icomucate = this.Context as ICommucatable;

                       icomucate.DeleteItem(item_row);

                       List.Sort();
                       this.NotifyDataSetChanged();
                   });

                   Util.Dialog(this.Context, "Information", "Are You Sure To Delete?", okDelegate, null);
               };

                btnDescrip = convertView.FindViewById<Button>(Resource.Id.descrip);

                btnDescrip.Click += (s1, e1) => {
                    var thisbtn = s1 as Button;
                    Int32 ll_position = Convert.ToInt32(thisbtn.Tag);

                    var item_row = this.List[ll_position];
                    String itemRowJson = JsonConvert.SerializeObject(item_row);

                    Intent intent = new Intent();
                    intent.SetClass(this.Context, typeof(MainActivity));
                    intent.PutExtra("ItemRow", itemRowJson);

                    this.Context.StartActivity(intent);

                };
            }

            Item item = List[position];
            var btn = convertView.FindViewById<Button>(Resource.Id.btn_update);
            var btnDel = convertView.FindViewById<Button>(Resource.Id.btn_delete);
            btnDescrip = convertView.FindViewById<Button>(Resource.Id.descrip);
            btn.Tag = position;
            btnDescrip.Tag = position;
            btnDel.Tag = position;
            
            btnDescrip.Text = item.Descrip;

            var layoutDescrip=btnDescrip.LayoutParameters;
            layoutDescrip.Width = DeviceWidth-150;

            var layoutBtn = btn.LayoutParameters;
            layoutBtn.Width =   150;

            var layoutDel = btnDel.LayoutParameters;
            layoutDel.Width = 150;


            //btnDescrip.SetWidth(1000);
            return convertView;

            //throw new NotImplementedException();
        }

        //btnDescrip.SetWidth(btnDescrip.Width - 1000);

        


    }
}