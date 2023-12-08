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
            s = new Serial();               //init serial controller
            sol = new Solar(100, 220, 220); //init voltage/amperage calculator with according resistor values
            volts = new Graph(gVoltage, 5); //init graphs, not fully implemented
            amps = new Graph(gAmperage, 4);
            s.RXcallback += receiveCallback; //attach callback function to fire when serial connection receives data
        }
        public void buttonRefreshPortsClicked(object sender, EventArgs e) { //Method to refresh ports list. Gets list of names, updates menu, resets connection if one is active
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
        public void serialDropdownUpdated(object sender, EventArgs e) //resets port connection if new selection is made.
        {
            if (serialDropdown.SelectedIndex == -1) return; //this event also fires when the menu is cleared and has no valid values, this prevents errors when that occurs
            log($"Selected port {serialDropdown.ItemsSource[serialDropdown.SelectedIndex]}");
            s.resetPort((string)serialDropdown.ItemsSource[serialDropdown.SelectedIndex]);
            connectButton.Text = "Connect";
        }
        public void buttonPortConnectClicked(object sender, EventArgs e) { //connect/disconnect serial port handler
            if (serialDropdown.SelectedIndex == -1) return; //extra check for if this method is called when no selection is made
            if (s.connected)
            {
                s.disconnect();
                connectButton.Text = "Connect";
            } else
            {
                s.connect();
                connectButton.Text = "Disconnect";
                updateLights(null, null); //update LEDs on connection
            }
        }
        public void buttonTXclicked(object sender, EventArgs e) //try to send packet on serial
        {
            try
            {
                s.send(TXentry.Text + "\r\n");
                log("TX: " + TXentry.Text);
            }
            catch (Exception ex) { log(ex.Message); }
        }
        public void receiveCallback(object sender, SerialDataReceivedEventArgs e) { //callback for when packet is received
            string tmp = s.line();
            MainThread.BeginInvokeOnMainThread(() => //invoke everything on main thread as other threads cannot update the UI
            {
                log("RX: " + tmp);
                parse(tmp);
            });
        }
        public void parse(string inp)
        {
            if (inp.StartsWith("###")) //if it's actually a packet...
            {
                int tmp = 0;
                for (int i = 3;i < 34; i++) tmp += (byte)inp[i];
                if (tmp%1000 == Convert.ToInt32(inp.Substring(34, 3))) //... and the checksum is valid, decode values and update the UI
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
            SEQ.Text = seq.ToString(); //raw values
            A00.Text = a0.ToString();
            A01.Text = a1.ToString();
            A02.Text = a2.ToString();
            A03.Text = a3.ToString();
            A04.Text = a4.ToString();
            A05.Text = a5.ToString();
            BIN.Text = $"{bin:D4}";
            CHK.Text = chk.ToString();

            sol.calc(a0, a1, a2, a3, a4); //perform math

            V_pv.Text = $"{sol.Vpv:0.000}V"; //useful values
            V_bat.Text = $"{sol.Vbat:0.000}V";
            V_bus.Text = $"{sol.Vbus:0.000}V";
            V_L0.Text = $"{sol.Vl0:0.000}V";
            V_L1.Text = $"{sol.Vl1:0.000}V";

            I_pv.Text = $"{sol.Ipv * 1000:00.00}mA";
            I_bat.Text = $"{sol.Ibat * 1000:00.00}mA";
            I_L0.Text = $"{sol.Il0 * 1000:00.00}mA";
            I_L1.Text = $"{sol.Il1 * 1000:00.00}mA";

            //volts.pushData(sol.Vpv, sol.Vbat, sol.Vbus, sol.Vl0, sol.Vl1); //update graph history; not implemented
        }
        public void updateLights(object sender, EventArgs e)
        {
            if (sender is not null) { ((Button)sender).Text = (((Button)sender).Text == "On") ? "Off" : "On"; } //if this was called from pressing a button, apply that button action
            string tmp = "";
            tmp += (L0.Text == "On") ? 0 : 1; //put together string to set lights with
            tmp += (L1.Text == "On") ? 0 : 1;
            tmp += (L2.Text == "On") ? 0 : 1;
            tmp += (L3.Text == "On") ? 0 : 1;
            if (s.connected) setLights(tmp);
        }

        public void setLights(string inp)
        {
            int chk = 0;
            for (int i = 0; i < 4; i++) chk += (byte)inp[i]; //calculate checksum
            Thread updater = new Thread(()=>updateLoop(inp, $"###{inp}{chk:D3}\r\n")); //build final packet, start the loop on a separate thread to avoid blocking
            updater.Start();
        }
        private void updateLoop(string raw, string msg) //repeatedly send packets every half a second until the lights are set to what they're meant to be. Circumvents issue where packets are dropped more often than not.
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
            MainThread.BeginInvokeOnMainThread(() => //in case this is called off the main thread
            {
                if (!message.EndsWith("\n")) message += "\n"; //append new line to serial log.
                logWindow.Text += message;
                if (logWindow.Text.Length > 20000) logWindow.Text = logWindow.Text.Substring(10000, logWindow.Text.Length - 10000); //memleak prevention (seems to not solve the problem fully but adding this significantly improved how long the program can run for before crashing)
            });
        }
    }
    class Serial
    {
        public bool connected;
        private SerialPort port;
        public SerialDataReceivedEventHandler RXcallback; //OnDataReceived callback passthrough
        public Serial() {
            connected = false;
            port = new SerialPort();
        }

        public void resetPort(string portName) //close existing connection and create a new instance
        {
            if (port is not null) port.Close();
            connected = false;
            port = new SerialPort(portName, 115200);
            port.ReceivedBytesThreshold = 1;
            port.DataReceived += RXcallback;
        }
        public string line() //line getter
        {
            return port.ReadLine();
        }
        public void connect() //connect/disconnect funcs
        {
            port.Open();
            connected = true;
        }
        public void disconnect()
        {
            port.Close();
            connected = false;
        }
        public string[] refreshSerialPorts() //portname getter
        {
            return SerialPort.GetPortNames();
        }
        public void send(string msg) //sender
        {
            byte[] m = Encoding.UTF8.GetBytes(msg);
            port.Write(m, 0, m.Length);
        }
    }

    class Solar
    {
        public double Vpv, Vbus, Vbat, Vl0, Vl1, Ipv, Ibat, Il0, Il1; //public facing output values
        private double Rbat, Rl0, Rl1; //internal values

        public Solar(double Rbat, double Rl0, double Rl1) {
            this.Rbat = Rbat;
            this.Rl0 = Rl0;
            this.Rl1 = Rl1;
        }
        public Solar calc(int a0, int a1, int a2, int a3, int a4) //does all the math to convert the raw ADC values to useful V/A values
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

    class Graph //Graph instance that should be bound to a graphicsView. contains all relevant data to draw aforementioned graph.
    {
        public class graphData //single series of data. Contains values in relation to time and a color to use when drawing.
        {
            public float[] data;
            public Color color;

            public graphData(float[] data, Color color)
            {
                this.data = data;
                this.color = color;
            }
        }

        public graphData[] dataset; //array of data streams to draw on the graph
        public long[] timestamps;   //and their corresponding timestamps
        private int sx, sy;         //size of canvas
        private float yMin, yMax, length; //bottom and top values for the Y axis, and length in seconds for the X axis
        private GraphicsView graphicsView; //attached graphicsView
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
        static void bindGraph(Graph graph) { GraphicsDrawable.graph = graph; } //same weird binding logic as used in the balls lab since I haven't figured out MVVM yet. Main thread updates what needs to be drawn, calls this method to inject it into the Drawable context, then calls render function.
        public void Draw(ICanvas canvas, RectF w)
        {
            //draw graphics using data from graph
        }
    }
}