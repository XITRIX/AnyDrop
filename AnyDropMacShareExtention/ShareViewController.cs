using System;
using System.Threading;
using System.Linq;
using AppKit;
using Foundation;

using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

using AnyDropCore;

namespace AnyDropMacShareExtention
{
    public partial class ShareViewController : NSViewController, INSTableViewDataSource, INSTableViewDelegate
    {

        nint selected = -1;
        String[] data = { "Kek1", "Kek2" };
        //String url;

        bool run = true;

        List<String> ipList = new List<string>();
        List<String> nameList = new List<string>();
        List<String> urls;

        public ShareViewController (IntPtr handle) : base (handle)
        {
        }

        public override void LoadView ()
        {
            base.LoadView ();

            // Show the URL of the item that was requested.


            nameList = new List<string>();
            ipList = new List<string>();
            urls = new List<string>();

            NSExtensionItem item = ExtensionContext.InputItems.First();
            foreach (var provider in item.Attachments) {
                provider.LoadItem("public.url", null, (arg1, arg2) =>
                {
                    urls.Add(WebUtility.UrlDecode(((NSUrl)arg1).ToString()));
                });
                Console.WriteLine(urls);
            }

            new Thread(BroadcastReceiverThread).Start();
            new Thread(SendRequetThread).Start();

            DeviceTableView.DataSource = this;
            DeviceTableView.Delegate = this;
        }

        public override void ViewWillDisappear()
        {
            base.ViewWillDisappear();
            run = false;
        }

        [Export("numberOfRowsInTableView:")]
        public nint GetRowCount(NSTableView tableView)
        {
            return nameList.Count;
        }

        [Export("tableView:viewForTableColumn:row:")]
        public NSView GetViewForItem(NSTableView tableView, NSTableColumn tableColumn, nint row)
        {
            var cell = (NSTableCellView)tableView.MakeView("UserName", null);
            cell.TextField.StringValue = nameList[Convert.ToInt32(row)];
            return cell;
        }

        [Export("tableViewSelectionDidChange:")]
        public void SelectionDidChange(NSNotification notification)
        {
            selected = DeviceTableView.SelectedRow;
            SendButton.Enabled = selected <= 0;
        }

        public void SendRequetThread(){
            while (run) {
                Network.SendDatagramMessageBroadcast("255.255.255.255", Network.ASKTORECEIVE);
                Thread.Sleep(3000);
            }
        }

        //public void ReceiverThread()
        //{
        //    TcpListener listener = new TcpListener(IPAddress.Any, Network.PORT_BROADCAST);
        //    listener.Start();
        //    while (run){
        //        TcpClient client = listener.AcceptTcpClient();
        //        byte[] data = Utils.ReceiveSocketMessage(client);
        //        var text = Encoding.UTF8.GetString(data);
        //        if (text.StartsWith(Network.AGREE, StringComparison.Ordinal))
        //        {
        //            String ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
        //            if (!ipList.Contains(ip))
        //            {
        //                String name = text.Replace(Network.AGREE, "");
        //                ipList.Add(ip);
        //                nameList.Add(name + ": " + ip);
        //                InvokeOnMainThread(() =>
        //                {
        //                    DeviceTableView.ReloadData();
        //                });
        //            }
        //            //Console.WriteLine(from.Address + ": " + datagram);
        //        }
        //    }
        //}

        public void BroadcastReceiverThread()
        {
            var from = new IPEndPoint(0, 0);
            UdpClient client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            //client.ExclusiveAddressUse = false;
            client.Client.Bind(new IPEndPoint(IPAddress.Any, Network.PORT_TRANSFER));
            while (run)
            {
                byte[] data = client.Receive(ref from);
                String datagram = Encoding.UTF8.GetString(data);
                if (from.Address.ToString().Equals(Utils.GetLocalIP()))
                {
                    continue;
                }
                else if (datagram.StartsWith(Network.AGREE, StringComparison.Ordinal))
                {
                    if (!ipList.Contains(from.Address.ToString()))
                    {
                        String name = datagram.Replace(Network.AGREE, "");
                        ipList.Add(from.Address.ToString());
                        nameList.Add(name);
                        InvokeOnMainThread(() =>
                        {
                            DeviceTableView.ReloadData();
                        });
                    }
                    //Console.WriteLine(from.Address + ": " + datagram);
                }

            }
            client.Close();
        }

        partial void Close (NSObject sender)
        {
            run = false;
            NSError cancelError = NSError.FromDomain(NSError.CocoaErrorDomain, 3072, null);
            ExtensionContext.CancelRequest(cancelError);

        }

        partial void Send(Foundation.NSObject sender)
        {
            run = false;

            var outputItem = new NSExtensionItem();
            var outputItems = new[] { outputItem };

            ExtensionContext.CompleteRequest(outputItems, null);

            TcpClient socket = new TcpClient(ipList[Convert.ToInt32(selected)], Network.PORT_TRANSFER);
            if (urls.Count > 1 || (urls.Count == 1 && urls[0].StartsWith("file://")))
            {
                List<String> fixdUrls = new List<string>();
                foreach (var url in urls){
                    fixdUrls.Add(url.Replace("file://", ""));
                }
                Network.SendFile(socket, fixdUrls);
            }
            else
            {
                Network.SendLink(socket, urls[0]);
            }
            socket.Close();
        }

    }
}

