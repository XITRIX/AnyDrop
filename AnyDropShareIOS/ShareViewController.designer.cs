// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace AnyDropShareIOS
{
    [Register ("ShareViewController")]
    partial class ShareViewController
    {
        [Outlet]
        UIKit.UIView MainView { get; set; }

        [Outlet]
        UIKit.UIBarButtonItem SendButton { get; set; }

        [Outlet]
        UIKit.UITableView TableView { get; set; }

        [Action ("Cancel:")]
        partial void Cancel (UIKit.UIBarButtonItem sender);

        [Action ("Send:")]
        partial void Send (UIKit.UIBarButtonItem sender);
        
        void ReleaseDesignerOutlets ()
        {
            if (MainView != null) {
                MainView.Dispose ();
                MainView = null;
            }

            if (TableView != null) {
                TableView.Dispose ();
                TableView = null;
            }

            if (SendButton != null) {
                SendButton.Dispose ();
                SendButton = null;
            }
        }
    }
}
