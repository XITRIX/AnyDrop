using System;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

using StreamExtention;

namespace AnyDropCore
{
    public class Utils
    {
        public static byte[] IntToByteArray(int value)
        {
            return new byte[] {
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value};
        }

        public static int ByteArrayToInt(byte[] bytes)
        {
            return bytes[0] << 24 | (bytes[1] & 0xFF) << 16 | (bytes[2] & 0xFF) << 8 | (bytes[3] & 0xFF);
        }

        public static void SendSocketMessage(TcpClient socket, byte[] data)
        {
            NetworkStream stream = socket.GetStream();
            stream.Write(IntToByteArray(data.Length), 0, 4);
            stream.Write(data, 0, data.Length);
        }

        public static byte[] ReceiveSocketMessage(TcpClient socket)
        {
            NetworkStream stream = socket.GetStream();
            byte[] lengthBytes = new byte[4];
            stream.Read(lengthBytes, 0, 4);
            int length = ByteArrayToInt(lengthBytes);
            byte[] buffer = new byte[length];
            stream.ReadFully(buffer);
            return buffer;
        }

        public static String GetLocalIP()
        {
            foreach (var addr in Dns.GetHostEntry(string.Empty).AddressList)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    return addr.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public static String GetLocalDeviceName()
        {
            return Dns.GetHostName();
            //return System.Environment.MachineName;
        }

        public static String BuildMacNotification(String title, String subtitle, String message, String declineText, String acceptText, int timer = 0){
            title = title.Replace(" ", "\u00A0");
            subtitle = subtitle.Replace(" ", "\u00A0");
            message = message.Replace(" ", "\u00A0");
            declineText = declineText.Replace(" ", "\u00A0");
            acceptText = acceptText.Replace(" ", "\u00A0");

            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            //p.StartInfo.FileName = "./alerter -title " + title + " -subtitle " + subtitle + " -message " + message + " -closeLabel " + declineText + " -actions " + acceptText;
            p.StartInfo.FileName = "./alerter";
            if (timer.Equals("0"))
                p.StartInfo.Arguments = "-title " + title + " -subtitle " + subtitle + " -message " + message + " -closeLabel " + declineText + " -actions " + acceptText;
            else 
                p.StartInfo.Arguments = "-title " + title + " -subtitle " + subtitle + " -message " + message + " -closeLabel " + declineText + " -actions " + acceptText + " -timer " + timer;
            p.Start();

            // To avoid deadlocks, always read the output stream first and then wait.
            String output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            Console.WriteLine(output);
            return output;

        }

    }
}

namespace StreamExtention
{
    public static class Extention
    {
        public static void ReadFully(this NetworkStream stream, byte[] buffer)
        {
            int offset = 0;
            int readBytes;
            do
            {
                // If you are using Socket directly instead of a Stream:
                //readBytes = socket.Receive(buffer, offset, buffer.Length - offset,
                //                           SocketFlags.None);

                readBytes = stream.Read(buffer, offset, buffer.Length - offset);
                offset += readBytes;
            } while (readBytes > 0 && offset < buffer.Length);

            if (offset < buffer.Length)
            {
                throw new MissingMemberException();
            }
        }
    }
}