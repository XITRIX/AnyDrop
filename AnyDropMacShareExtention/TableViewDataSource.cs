using System;
using AppKit;
using Foundation;

namespace AnyDropMacShareExtention
{
    public class TableViewDataSource : NSTableViewDataSource
    {
        
        public TableViewDataSource()
        {
        }

        public override nint GetRowCount(NSTableView tableView)
        {
            return 0;//nameList.Count;
        }

    }

    public class TableViewDelegate : NSTableViewDelegate
    {
        TableViewDataSource dataSource;
        public TableViewDelegate(TableViewDataSource source){
            dataSource = source;
        }

        public override NSView GetViewForItem(NSTableView tableView, NSTableColumn tableColumn, nint row)
        {
            var cell = (NSTableCellView)tableView.MakeView("UserName", null);
            //cell.TextField.StringValue = dataSource.nameList[Convert.ToInt32(row)];
            return cell;
        }
    }
}
