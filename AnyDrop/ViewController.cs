using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using AppKit;
using Foundation;

using AnyDropCore;

namespace AnyDrop
{
    public partial class ViewController : NSViewController, INSUserNotificationCenterDelegate
    {
        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            new Thread(Network.BroadcastReceiverThread).Start();
            Network.GetFile();

            // Do any additional setup after loading the view.
        }

        public override NSObject RepresentedObject
        {
            get
            {
                return base.RepresentedObject;
            }
            set
            {
                base.RepresentedObject = value;
                // Update the view, if already loaded.
            }
        }
    }
}
