using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

using UIKit;

namespace AnyDropCore
{
    public class Network
    {
        public static readonly int PORT_BROADCAST = 12647;
        public static readonly int PORT_TRANSFER = 12648;
        public static readonly String ASKTORECEIVE = "HEY, YOU!";
        public static readonly String AGREE = "AGREE!";
        public static readonly String LINK = "1";
        public static readonly String FILE = "0";

        public static List<String> ipList = new List<string>();
        public static List<String> nameList = new List<string>();
        public static Action update;

        public static void BroadcastReceiverThread()
        {
            var from = new IPEndPoint(0, 0);
            UdpClient client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            //client.ExclusiveAddressUse = false;
            client.Client.Bind(new IPEndPoint(IPAddress.Any, PORT_BROADCAST));
            while (true)
            {
                byte[] data = client.Receive(ref from);
                String datagram = Encoding.UTF8.GetString(data);
                if (from.Address.ToString().Equals(Utils.GetLocalIP()))
                {
                    continue;
                }
                else if (datagram.Equals(ASKTORECEIVE))
                {
                    SendDatagramMessage(from.Address.ToString(), AGREE + Utils.GetLocalDeviceName());
                    Console.WriteLine(from.Address + ": " + datagram);
                }
                /*else if (datagram.StartsWith(AGREE, StringComparison.Ordinal))
                {
                    ipList.Add(from.Address.ToString());
                    nameList.Add(Utils.GetLocalDeviceName());
                    update?.Invoke();
                    Console.WriteLine(from.Address + ": " + datagram);
                }*/
                else
                {
                    Console.WriteLine(from.Address + ": " + datagram);
                }

            }
        }

        public static void SendDatagramMessage(String ip, String message)
        {
            UdpClient client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.ExclusiveAddressUse = false;
            client.Send(Encoding.UTF8.GetBytes(message), Encoding.UTF8.GetBytes(message).Length, ip, PORT_TRANSFER );
            client.Close();
        }

        public static void SendDatagramMessageBroadcast(String ip, String message)
        {
            UdpClient client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.ExclusiveAddressUse = false;
            client.Send(Encoding.UTF8.GetBytes(message), Encoding.UTF8.GetBytes(message).Length, ip, PORT_BROADCAST);
            client.Close();
        }

        public static void SendFile(TcpClient socket, List<String> paths)
        {
            Utils.SendSocketMessage(socket, Encoding.UTF8.GetBytes(FILE));
            Utils.SendSocketMessage(socket, Encoding.UTF8.GetBytes(Utils.GetLocalDeviceName()));
            Utils.SendSocketMessage(socket, Utils.IntToByteArray(paths.Count));
            foreach (var path in paths)
            {
                byte[] file = File.ReadAllBytes(path);
                Utils.SendSocketMessage(socket, Encoding.UTF8.GetBytes(Path.GetFileName(path)));
                Utils.SendSocketMessage(socket, file);
            }
        }

        public static void SendFileBytes(TcpClient socket, List<String> names, List<byte[]> bytes)
        {
            Utils.SendSocketMessage(socket, Encoding.UTF8.GetBytes(FILE));
            Utils.SendSocketMessage(socket, Encoding.UTF8.GetBytes(Utils.GetLocalDeviceName()));
            Utils.SendSocketMessage(socket, Utils.IntToByteArray(names.Count));
            for (int i = 0; i < names.Count; i++) {
                Utils.SendSocketMessage(socket, Encoding.UTF8.GetBytes(Path.GetFileName(names[i])));
                Utils.SendSocketMessage(socket, bytes[i]);
            }
        }

        public static void SendLink(TcpClient socket, String link)
        {
            Utils.SendSocketMessage(socket, Encoding.UTF8.GetBytes(LINK));
            Utils.SendSocketMessage(socket, Encoding.UTF8.GetBytes(Utils.GetLocalDeviceName()));
            Utils.SendSocketMessage(socket, Encoding.UTF8.GetBytes(link));
        }

        public static void GetFile()
        {

            TcpListener server = new TcpListener(IPAddress.Any, PORT_TRANSFER);
            server.Start();

            while (true)
            {
                Console.Write("Waiting for a connection... ");
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("Connected!");

                String type = Encoding.UTF8.GetString(Utils.ReceiveSocketMessage(client));
                String hostName = Encoding.UTF8.GetString(Utils.ReceiveSocketMessage(client));
                if (type.Equals(FILE))
                {
                    Boolean confirm = false;
                    int fileCount = Utils.ByteArrayToInt(Utils.ReceiveSocketMessage(client));
                    for (int i = 0; i < fileCount; i++)
                    {
                        String fileName = Encoding.UTF8.GetString(Utils.ReceiveSocketMessage(client));
                        if (i == 0)
                        {
                            if (fileCount == 1)
                                confirm = Utils.BuildMacNotification("AnyDrop", "Файл " + fileName + " от", "«" + hostName + "»", "Отклонить", "Принять").Equals("Принять");
                            else
                                confirm = Utils.BuildMacNotification("AnyDrop", "Файлы: (" + fileCount + "шт.) от", "«" + hostName + "»", "Отклонить", "Принять").Equals("Принять");
                        }
                        if (confirm)
                        {
                            byte[] file = Utils.ReceiveSocketMessage(client);

                            String path = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/Downloads/" + fileName;
                            Console.WriteLine(path);
                            if (File.Exists(path))
                            {
                                int count = 1;
                                String rootPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/Downloads/";
                                String fileNameNoExt = Path.GetFileNameWithoutExtension(path);
                                String ext = Path.GetExtension(path);
                                while (File.Exists(rootPath + fileNameNoExt + "_" + count + ext))
                                {
                                    count++;
                                }
                                path = rootPath + fileNameNoExt + "_" + count + ext;
                            }
                            File.Create(path).Write(file, 0, file.Length);
                            if (i == fileCount - 1 && Utils.BuildMacNotification("AnyDrop", "Файл " + fileName, "от " + "«" + hostName + "»", "Закрыть", "Показать", 3).Equals("Показать"))
                            {
                                Process p = new Process();
                                p.StartInfo.UseShellExecute = false;
                                p.StartInfo.RedirectStandardOutput = true;
                                p.StartInfo.FileName = "open";
                                p.StartInfo.Arguments = path;
                                p.Start();
                            }

                        }
                        else
                            break;
                    }
                }
                else if (type.Equals(LINK))
                {
                    String link = Encoding.UTF8.GetString(Utils.ReceiveSocketMessage(client));
                    if (Utils.BuildMacNotification("AnyDrop", "Ссылка " + link, "от " + "«" + hostName + "»", "Отклонить", "Открыть").Equals("Открыть"))
                    {
                        Process p = new Process();
                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.RedirectStandardOutput = true;
                        p.StartInfo.FileName = "open";
                        p.StartInfo.Arguments = link;
                        p.Start();
                    }
                }

                client.Close();
            }
        }

        public static void GetFileIOS(UIViewController view)
        {

            TcpListener server = new TcpListener(IPAddress.Any, PORT_TRANSFER);
            server.Start();

            while (true)
            {
                Console.Write("Waiting for a connection... ");
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("Connected!");

                String type = Encoding.UTF8.GetString(Utils.ReceiveSocketMessage(client));
                String hostName = Encoding.UTF8.GetString(Utils.ReceiveSocketMessage(client));
                if (type.Equals(FILE))
                {
                    Boolean confirm = false;
                    int fileCount = Utils.ByteArrayToInt(Utils.ReceiveSocketMessage(client));
                    String fileName = Encoding.UTF8.GetString(Utils.ReceiveSocketMessage(client));

                    String message = "";
                    if (fileCount == 1)
                        message = "Файл " + fileName + " от\n" + "«" + hostName + "»";
                    else
                        message = "Файлы: (" + fileCount + "шт.) от\n" + "«" + hostName + "»";

                    var alert = UIAlertController.Create("AnyDrop", message, UIAlertControllerStyle.Alert);
                    alert.AddAction(UIAlertAction.Create("Принять", UIAlertActionStyle.Default, (obj) => {
                        for (int i = 0; i < fileCount; i++)
                        {

                            if (i == 0)
                            {
                                
                            }
                            if (confirm)
                            {
                                byte[] file = Utils.ReceiveSocketMessage(client);

                                String path = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/Downloads/" + fileName;
                                Console.WriteLine(path);
                                if (File.Exists(path))
                                {
                                    int count = 1;
                                    String rootPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/Downloads/";
                                    String fileNameNoExt = Path.GetFileNameWithoutExtension(path);
                                    String ext = Path.GetExtension(path);
                                    while (File.Exists(rootPath + fileNameNoExt + "_" + count + ext))
                                    {
                                        count++;
                                    }
                                    path = rootPath + fileNameNoExt + "_" + count + ext;
                                }
                                File.Create(path).Write(file, 0, file.Length);
                            }
                            else
                                break;
                        }
                    }));
                }
                else if (type.Equals(LINK))
                {
                    String link = Encoding.UTF8.GetString(Utils.ReceiveSocketMessage(client));
                    var alert = UIAlertController.Create("AnyDrop", "Ссылка " + link + "\nот " + "«" + hostName + "»", UIAlertControllerStyle.Alert);
                    alert.AddAction(UIAlertAction.Create("Принять", UIAlertActionStyle.Default, (obj) => {
                        
                    }));
                    view.PresentViewController(alert, true, null);
                }

                client.Close();
            }
        }

    }


}
