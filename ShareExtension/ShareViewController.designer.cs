// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace ShareExtension
{
    [Register ("ShareViewController")]
    partial class ShareViewController
    {
        [Outlet]
        AppKit.NSTableView DeviceTableView { get; set; }

        [Outlet]
        AppKit.NSButton SendButton { get; set; }

        [Outlet]
        AppKit.NSTextField TitleText { get; set; }

        [Action ("Cancel:")]
        partial void Cancel (Foundation.NSObject sender);

        [Action ("Close:")]
        partial void Close (Foundation.NSObject sender);

        [Action ("Send:")]
        partial void Send (Foundation.NSObject sender);
        
        void ReleaseDesignerOutlets ()
        {
            if (DeviceTableView != null) {
                DeviceTableView.Dispose ();
                DeviceTableView = null;
            }

            if (TitleText != null) {
                TitleText.Dispose ();
                TitleText = null;
            }

            if (SendButton != null) {
                SendButton.Dispose ();
                SendButton = null;
            }
        }
    }
}
