using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.VisualBasic;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Text;

namespace SolarController
{
    public partial class MainPage : ContentPage
    {
        private Serial s;
        private Solar sol;
        private int seq, a0, a1, a2, a3, a4, a5, bin, chk;
        private Graph volts, amps;

        public MainPage()
        {
            InitializeComponent();
            s = new Serial();
            sol = new Solar(100, 220, 220);
            volts = new Graph(gVoltage, 5);
            amps = new Graph(gAmperage, 4);
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
                updateLights(null, null);
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

            sol.calc(a0, a1, a2, a3, a4); //TODO

            V_pv.Text = $"{sol.Vpv:0.000}V";
            V_bat.Text = $"{sol.Vbat:0.000}V";
            V_bus.Text = $"{sol.Vbus:0.000}V";
            V_L0.Text = $"{sol.Vl0:0.000}V";
            V_L1.Text = $"{sol.Vl1:0.000}V";

            I_pv.Text = $"{sol.Ipv * 1000:00.00}mA";
            I_bat.Text = $"{sol.Ibat * 1000:00.00}mA";
            I_L0.Text = $"{sol.Il0 * 1000:00.00}mA";
            I_L1.Text = $"{sol.Il1 * 1000:00.00}mA";

            //volts.pushData(sol.Vpv, sol.Vbat, sol.Vbus, sol.Vl0, sol.Vl1);
        }
        public void updateLights(object sender, EventArgs e)
        {
            if (sender is not null) { ((Button)sender).Text = (((Button)sender).Text == "On") ? "Off" : "On"; }
            string tmp = "";
            tmp += (L0.Text == "On") ? 0 : 1;
            tmp += (L1.Text == "On") ? 0 : 1;
            tmp += (L2.Text == "On") ? 0 : 1;
            tmp += (L3.Text == "On") ? 0 : 1;
            if (s.connected) setLights(tmp);
        }

        public void setLights(string inp)
        {
            int chk = 0;
            for (int i = 0; i < 4; i++) chk += (byte)inp[i];
            Thread updater = new Thread(()=>updateLoop(inp, $"###{inp}{chk:D3}\r\n"));
            updater.Start();
        }
        private void updateLoop(string raw, string msg)
        {
            do
            {
                s.send(msg);
                log("TX: " + msg);
                Thread.Sleep(500);
            } while (s.connected && BIN.Text != raw);
        }

        private void log(string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!message.EndsWith("\n")) message += "\n";
                logWindow.Text += message;
                if (logWindow.Text.Length > 20000) logWindow.Text = logWindow.Text.Substring(10000, logWindow.Text.Length - 10000); //overflow prevention
            });
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

    class Solar
    {
        public double Vpv, Vbus, Vbat, Vl0, Vl1, Ipv, Ibat, Il0, Il1;
        private double Rbat, Rl0, Rl1;

        public Solar(double Rbat, double Rl0, double Rl1) {
            this.Rbat = Rbat;
            this.Rl0 = Rl0;
            this.Rl1 = Rl1;
        }
        public Solar calc(int a0, int a1, int a2, int a3, int a4)
        {
            Vpv = a0 / 1000.0;
            Vbus = a1 / 1000.0;
            Vbat = a2 / 1000.0;
            Vl1 = a3 / 1000.0;
            Vl0 = a4 / 1000.0;

            Ibat = (Vbus - Vbat) / Rbat;
            Il0 = (Vbus - Vl0) / Rl0;
            Il1 = (Vbus - Vl1) / Rl1;
            Ipv = Il0 + Il1 + Ibat;
            return this;
        }
    }

    class Graph
    {
        public class graphData
        {
            public float[] data;
            public Color color;

            public graphData(float[] data, Color color)
            {
                this.data = data;
                this.color = color;
            }
        }

        public graphData[] dataset;
        public long[] timestamps;
        private int sx, sy;
        private float yMin, yMax, length;
        private GraphicsView graphicsView;
        public Graph(GraphicsView view, int nSets)
        {
            //set up graph and callbacks
        }

        public void trim(long timestamp)
        {
            //remove data older than timestamp
        }
    }
    public class GraphicsDrawable : IDrawable
    {
        static Graph graph;
        static void bindGraph(Graph graph) { GraphicsDrawable.graph = graph; }
        public void Draw(ICanvas canvas, RectF w)
        {
            //draw graphics using data from graph
        }
    }
}