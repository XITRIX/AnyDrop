using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using Foundation;
using UIKit;
using Social;

using AnyDropCore;

namespace AnyDropShareIOS
{
    public partial class ShareViewController : UIViewController, IUITableViewDataSource, IUITableViewDelegate
    {
        
        List<String> ipList;
        List<String> nameList;
        List<String> urls;
        List<String> fileNames;
        List<byte[]> fileBytes;


        bool run = true;

        nint selected = -1;

        protected ShareViewController(IntPtr handle) : base(handle)
        {
        }

        public override void DidReceiveMemoryWarning()
        {
            // Releases the view if it doesn't have a superview.
            base.DidReceiveMemoryWarning();

            // Release any cached data, images, etc that aren't in use.
        }

        public UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var cell = tableView.DequeueReusableCell("Cell") as MyTableViewCell;
            /*if (cell == null)
                cell = new UITableViewCell(UITableViewCellStyle.Default, "Cell");*/
            cell.Title.Text = nameList[indexPath.Row];
            //cell.TextLabel.Text = nameList[indexPath.Row];
            return cell;
        }

        [Export("tableView:didSelectRowAtIndexPath:")]
        public void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            selected = indexPath.Row;
            SendButton.Enabled = true;
        }

        public nint RowsInSection(UITableView tableView, nint section)
        {
            return nameList.Count;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            nameList = new List<string>();
            ipList = new List<string>();
            urls = new List<string>();
            fileNames = new List<string>();
            fileBytes = new List<byte[]>();

            NSExtensionItem item = ExtensionContext.InputItems[0];
            foreach (var provider in item.Attachments)
            {
                if (provider.HasItemConformingTo("public.url"))
                    provider.LoadItem("public.url", null, (arg1, arg2) =>
                {
                    Console.WriteLine("URL: " + ((NSUrl)arg1).ToString());
                    if (((NSUrl)arg1).ToString().StartsWith("file://", StringComparison.Ordinal))
                    {
                        fileNames.Add(((NSUrl)arg1).LastPathComponent);
                        fileBytes.Add((NSData.FromUrl((NSUrl)arg1).ToArray()));
                    }
                    else
                    {
                        urls.Add(WebUtility.UrlDecode(((NSUrl)arg1).ToString()));
                    }
                });
                else if (provider.HasItemConformingTo("public.file-url"))
                    provider.LoadItem("public.file-url", null, (arg1, arg2) =>
                    {
                        Console.WriteLine("URL: " + ((NSUrl)arg1).ToString());
                        urls.Add(WebUtility.UrlDecode(((NSUrl)arg1).ToString()));
                    });
                else if (provider.HasItemConformingTo("public.jpeg"))
                {
                    provider.LoadItem("public.jpeg", null, (arg1, error) =>
                    {
                        if (arg1 != null)
                        {
                            Console.WriteLine("URL: " + ((NSUrl)arg1).ToString());
                            fileNames.Add(((NSUrl)arg1).LastPathComponent);
                            fileBytes.Add((NSData.FromUrl((NSUrl)arg1).ToArray()));
                            //filename = "filename";
                        }
                    });
                }
            }

            Console.WriteLine(item);

            new Thread(BroadcastReceiverThread).Start();
            new Thread(SendRequetThread).Start();

            TableView.RegisterNibForCellReuse(MyTableViewCell.Nib, "Cell");

            TableView.DataSource = this;
            TableView.Delegate = this;

            // Do any additional setup after loading the view.
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            //MainView.Transform = CoreGraphics.CGAffineTransform.MakeTranslation(0, View.Frame.Size.Height);//new CoreGraphics.CGAffineTransform(0, 0, 0, 0, 0, View.Frame.Size.Height);
            MainView.Layer.CornerRadius = 9;
            MainView.ClipsToBounds = true;
            MainView.Transform = CoreGraphics.CGAffineTransform.MakeScale(1.2f, 1.2f);
            MainView.Alpha = 0.5f;
            View.Alpha = 0f;
            UIView.Animate(0.3,() => {
                MainView.Alpha = 1f;
                View.Alpha = 1f;
                MainView.Transform = CoreGraphics.CGAffineTransform.MakeIdentity();
            });
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
            run = false;
        }

        public void SendRequetThread()
        {
            while (run)
            {
                Network.SendDatagramMessageBroadcast("255.255.255.255", Network.ASKTORECEIVE);
                Thread.Sleep(3000);
            }
        }

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
                            TableView.ReloadData();
                        });
                    }
                    Console.WriteLine(from.Address + ": " + datagram);
                }
                else {
                    Console.WriteLine(from.Address + ": " + datagram + " : bullshit!");
                }

            }
            client.Close();
        }

        partial void Cancel(UIBarButtonItem sender)
        {
            run = false;
            MainView.Transform = CoreGraphics.CGAffineTransform.MakeIdentity();
            MainView.Alpha = 1f;
            UIView.Animate(0.3, () => {
                MainView.Alpha = 0f;
                View.Alpha = 0f;
                MainView.Transform = CoreGraphics.CGAffineTransform.MakeScale(0.8f, 0.8f);
            },() => {
                NSError cancelError = NSError.FromDomain(NSError.CocoaErrorDomain, 3072, null);
                ExtensionContext.CancelRequest(cancelError);
            });
        }

        partial void Send(UIBarButtonItem sender)
        {
            run = false;

            var outputItem = new NSExtensionItem();
            var outputItems = new[] { outputItem };

            MainView.Transform = CoreGraphics.CGAffineTransform.MakeIdentity();//new CoreGraphics.CGAffineTransform(0, 0, 0, 0, 0, View.Frame.Size.Height);
            UIView.Animate(0.5, () => {
                MainView.Transform = CoreGraphics.CGAffineTransform.MakeTranslation(0, -View.Frame.Size.Height*2);
            },() => {
                ExtensionContext.CompleteRequest(outputItems, null);

                TcpClient socket = new TcpClient(ipList[Convert.ToInt32(selected)], Network.PORT_TRANSFER);
                //if (urls.Count > 1 || (urls.Count == 1 && urls[0].StartsWith("file://")))
                //{
                //    List<String> fixdUrls = new List<string>();
                //    foreach (var url in urls)
                //    {
                //        fixdUrls.Add(url.Replace("file://", ""));
                //    }
                //    Network.SendFile(socket, fixdUrls);
                //}
                if (urls.Count == 0) {
                    Network.SendFileBytes(socket, fileNames, fileBytes);
                }
                else if (urls.Count != 0 && urls[0].StartsWith("file:///")) {
                    Network.SendFile(socket, urls);
                }
                else
                {
                    Network.SendLink(socket, urls[0]);
                }
                socket.Close();
            });

        }
    }
}
