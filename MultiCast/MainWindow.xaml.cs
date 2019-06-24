using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;


namespace MultiCast
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string IpMultiCast = "224.1.1.1";
        public static int port = 12345;
        public static int mcTTL = 1; // 255(max)

        public MainWindow()
        {
            InitializeComponent();
            { // 1 - 0.0.0.0
                Param p1 = new Param(IPAddress.Any, 12345);
                //Param p1 = new Param(IPAddress.Parse("10.1.4.82"), 12345);
                p1.ipMulticast = IPAddress.Parse(IpMultiCast);
                p1.txtBox = txtBox2;
                Thread th1 = new Thread(ThreadProcReciv);
                th1.IsBackground = true;
                th1.Start(p1);
            }
            { // 2 - 192.168.56.1
                Param p2 = new Param(IPAddress.Parse("192.168.56.1"), 12345);
                p2.ipMulticast = IPAddress.Parse(IpMultiCast);
                p2.txtBox = txtBox3;
                Thread th2 = new Thread(ThreadProcReciv);
                th2.IsBackground = true;
                th2.Start(p2);
            }

            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Param pp = new Param(IPAddress.Parse("192.168.56.1"), 
                12345); //  порт
            ThreadPool.QueueUserWorkItem(ThreadProcSend);
        }

        delegate void AppendTextOut(TextBox tb, string str);
        void AppendTextProc(TextBox txtBox, string str)
        {
            txtBox.AppendText(str + "\n");
        }

        // Получатель (сервер) сообщений Multicast
        void ThreadProcReciv(object obj)
        {
            Param p = obj as Param;
            Dispatcher.Invoke(new AppendTextOut(AppendTextProc), p.txtBox, "Start TreadProcReciv()");
            Dispatcher.Invoke(new AppendTextOut(AppendTextProc), p.txtBox, "EP: " + p.ip.ToString() + ":" + p.port.ToString());
            Dispatcher.Invoke(new AppendTextOut(AppendTextProc), p.txtBox, "Multicast IP: " + p.ipMulticast.ToString());
            // настройка сокета на Multicast
            Socket sockRec = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sockRec.Bind(new IPEndPoint(p.ip, p.port));
            sockRec.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership,
              new MulticastOption(p.ipMulticast, p.ip));

            byte[] buf = new byte[4 * 1024];
            // цикл приёма широковещатекльных сообщений - Multicast
            while (!p.isStop)
            {
                int size = sockRec.Receive(buf);
                Dispatcher.Invoke(new AppendTextOut(AppendTextProc), p.txtBox, Encoding.Default.GetString(buf, 0, size));
            }
            sockRec.Close();
        }

        // клиент - рассыльщик, рассылает Multicast
        void ThreadProcSend(object obj)
        {
            // UDP - socket
            Socket sockMulticast = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sockMulticast.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, mcTTL);

            IPAddress ipDest = IPAddress.Parse(IpMultiCast);
            //IPAddress ipDest = IPAddress.Parse("224.5.5.5");
            //int port = 12345;
            sockMulticast.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(ipDest));

            IPEndPoint ipEP = new IPEndPoint(ipDest, port);
            sockMulticast.Connect(ipEP);

            string message = "Hello Network!";
            for (int i = 1; i <= 5; i++)
            {
                string temp = i.ToString() + ") " + message;
                Dispatcher.Invoke(new AppendTextOut(AppendTextProc), txtBox1, temp);
                sockMulticast.Send(Encoding.Default.GetBytes(temp));
                Thread.Sleep(500);
            }
            Dispatcher.Invoke(new AppendTextOut(AppendTextProc), txtBox1, "");
            sockMulticast.Close();
        }
    }

    public class Param
    {
        public IPAddress ip { get; set; }
        public IPAddress ipMulticast { get; set; }
        public int port { get; set; }
        public bool isStop { get; set; }
        public TextBox txtBox { get; set; }
        public Param(IPAddress ipAddr, int Port)
        {
            ipMulticast = IPAddress.Parse("224.5.5.5");
            ip = ipAddr;
            port = Port;
            isStop = false;
        }
    }
}
