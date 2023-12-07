using Microsoft.UI.Xaml.Media.Animation;
using System.IO.Ports;
using System.Text;

namespace SolarController
{
    public partial class MainPage : ContentPage
    {
        private Serial s;
        private int seq, a0, a1, a2, a3, a4, a5, bin, chk;

        public MainPage()
        {
            InitializeComponent();
            s = new Serial();
            s.RXcallback += receiveCallback;
        }
        public void buttonRefreshPortsClicked(object sender, EventArgs e) {
            try
            {
                string[] p = s.refreshSerialPorts();
                log($"found {p.Length} ports");
                foreach (string p2 in p) log("port: "+p2);
                serialDropdown.ItemsSource = p.ToList();
                serialDropdown.SelectedIndex = 0;
                serialDropdownUpdated(null, null);
            }
            catch (Exception ex) { log(ex.Message); }
        }
        public void serialDropdownUpdated(object sender, EventArgs e)
        {
            if (serialDropdown.SelectedIndex == -1) return;
            log($"Selected port {serialDropdown.ItemsSource[serialDropdown.SelectedIndex]}");
            s.resetPort((string)serialDropdown.ItemsSource[serialDropdown.SelectedIndex]);
            connectButton.Text = "Connect";
        }
        public void buttonPortConnectClicked(object sender, EventArgs e) {
            if (serialDropdown.SelectedIndex == -1) return;
            if (s.connected)
            {
                s.disconnect();
                connectButton.Text = "Connect";
            } else
            {
                s.connect();
                connectButton.Text = "Disconnect";
            }
        }
        public void buttonTXclicked(object sender, EventArgs e)
        {
            try
            {
                s.send(TXentry.Text + "\r\n");
                log("TX: " + TXentry.Text);
            }
            catch (Exception ex) { log(ex.Message); }
        }
        public void receiveCallback(object sender, SerialDataReceivedEventArgs e) {
            string tmp = s.line();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                log("RX: " + tmp);
                parse(tmp);
            });
        }
        public void parse(string inp)
        {
            if (inp.StartsWith("###"))
            {
                int tmp = 0;
                for (int i = 3;i < 34; i++) tmp += (byte)inp[i];
                if (tmp%1000 == Convert.ToInt32(inp.Substring(34, 3)))
                {
                    seq = Convert.ToInt32(inp.Substring(3, 3));
                    a0 = Convert.ToInt32(inp.Substring(6, 4));
                    a1 = Convert.ToInt32(inp.Substring(10, 4));
                    a2 = Convert.ToInt32(inp.Substring(14, 4));
                    a3 = Convert.ToInt32(inp.Substring(18, 4));
                    a4 = Convert.ToInt32(inp.Substring(22, 4));
                    a5 = Convert.ToInt32(inp.Substring(26, 4));
                    bin = Convert.ToInt32(inp.Substring(30, 4));
                    chk = tmp % 1000;
                    updateVals();
                }
            }
        }
        public void updateVals()
        {
            SEQ.Text = seq.ToString();
            A00.Text = a0.ToString();
            A01.Text = a1.ToString();
            A02.Text = a2.ToString();
            A03.Text = a3.ToString();
            A04.Text = a4.ToString();
            A05.Text = a5.ToString();
            BIN.Text = $"{bin:D4}";
            CHK.Text = chk.ToString();
        }

        private void log(string message)
        {
            logWindow.Text += message + "\n";
            if (logWindow.Text.Length > 20000) logWindow.Text = logWindow.Text.Substring(10000, logWindow.Text.Length - 10000); //overflow prevention
        }
    }
    class Serial
    {
        public bool connected;
        private SerialPort port;
        public SerialDataReceivedEventHandler RXcallback;
        public Serial() {
            connected = false;
            port = new SerialPort();
        }

        public void resetPort(string portName)
        {
            if (port is not null) port.Close();
            connected = false;
            port = new SerialPort(portName, 115200);
            port.ReceivedBytesThreshold = 1;
            port.DataReceived += RXcallback;
        }
        public string line()
        {
            return port.ReadLine();
        }
        public void connect()
        {
            port.Open();
            connected = true;
        }
        public void disconnect()
        {
            port.Close();
            connected = false;
        }
        public string[] refreshSerialPorts()
        {
            return SerialPort.GetPortNames();
        }
        public void send(string msg)
        {
            byte[] m = Encoding.UTF8.GetBytes(msg);
            port.Write(m, 0, m.Length);
        }
    }
}