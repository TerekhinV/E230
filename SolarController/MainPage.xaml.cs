using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.VisualBasic;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Media.Playback;
using Windows.Web.Http.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SolarController
{
    public partial class MainPage : ContentPage
    {
        private Serial s;
        private Solar sol;
        private int seq, a0, a1, a2, a3, a4, a5, bin, chk;
        private Graph volts, amps;
        private bool retry = false;

        public MainPage()
        {
            InitializeComponent();
            s = new Serial();               //init serial controller
            sol = new Solar(100, 220, 220); //init voltage/amperage calculator with according resistor values
            volts = new Graph(gVoltage, 5, new Color[5]{
                Color.FromArgb("ffff0000"),
                Color.FromArgb("ffffff00"),
                Color.FromArgb("ff00ff00"),
                Color.FromArgb("ff0000ff"),
                Color.FromArgb("ffff00ff")
            }, 0, 3.5, 7, 10, "{0:0.0}V");
            amps = new Graph(gAmperage, 4, new Color[4]{
                Color.FromArgb("ffff0000"),
                Color.FromArgb("ffffff00"),
                Color.FromArgb("ff00ff00"),
                Color.FromArgb("ff0000ff")
            }, -3, 8, 11, 10, "{0:0}mA");
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

            volts.push(new double?[] { sol.Vpv, sol.Vbat, sol.Vbus, sol.Vl0, sol.Vl1 });
            amps.push(new double?[] { sol.Ipv*1000, sol.Ibat * 1000, sol.Il0 * 1000, sol.Il1 * 1000 });

            volts.render();
            amps.render();
        }
        public void updateLights(object sender, EventArgs e)
        {
            if (sender is not null) { ((Button)sender).Text = (((Button)sender).Text == "On") ? "Off" : "On"; } //if this was called from pressing a button, apply that button action
            if (!s.connected && !retry) return; //end callback early if unable to send or loop is already running

            retry = true;
            Thread updater = new Thread(() => //on a new thread, start loop to build and send packet, repeating until lights are set correctly
            {
                string tmp;
                do
                {
                    tmp = (L0.Text == "On") ? "0" : "1";
                    tmp += (L1.Text == "On") ? 0 : 1;
                    tmp += (L2.Text == "On") ? 0 : 1;
                    tmp += (L3.Text == "On") ? 0 : 1;
                    int chk = 0;
                    for (int i = 0; i < 4; i++) chk += (byte)tmp[i];
                    string msg = $"###{tmp}{chk:D3}\r\n";
                    s.send(msg);
                    log("TX: " + msg);
                    Thread.Sleep(500);
                } while (s.connected && BIN.Text != tmp);
                retry = false;
            });
            updater.Start();
        }

        private void log(string message)
        {
            MainThread.BeginInvokeOnMainThread(() => //in case this is called off the main thread
            {
                if (!message.EndsWith("\n")) message += "\n"; //append new line to serial log.
                logWindow.Text += message;
                if (logWindow.Text.Length > 20000) logWindow.Text = ""; //trimming didn't work so we flat out delete the entire log now.
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

    public class Graph //Graph instance that should be bound to a graphicsView. contains all relevant data to draw aforementioned graph.
    {
        public class graphData //single series of data. Contains values in relation to time and a color to use when drawing.
        {
            public List<double?> data;
            public Color color;

            public graphData(Color color)
            {
                this.data = new List<double?>();
                this.color = color;
            }
        }

        public graphData[] dataset; //array of data streams to draw on the graph
        public List<long> timestamps;   //and their corresponding timestamps
        public string format;
        public int divs;         //division lines
        public double sx, sy, yMin, yMax, length; //size of canvas, bottom and top values for the Y axis, and length in seconds for the X axis
        public GraphicsView graphicsView; //attached graphicsView
        public Graph(GraphicsView view, int nSets, Color[] colors, double ymin, double ymax, int divisions, double length, string format) //biiiiig initializer
        {
            this.dataset = new graphData[nSets];
            for (int i = 0; i < nSets; i++) dataset[i] = new graphData(colors[i]);
            this.timestamps = new List<long>();
            this.format = format;
            this.divs = divisions;
            this.sx = view.Width;
            this.sy = view.Height;
            this.yMin = ymin;
            this.yMax = ymax;
            this.length = length;
            this.graphicsView = view;
            this.graphicsView.SizeChanged += (object sender, EventArgs e) => { this.sx = this.graphicsView.Width; this.sy = this.graphicsView.Height; };
            GraphicsDrawable tmp = new GraphicsDrawable();
            tmp.graph = this;
            this.graphicsView.Drawable = tmp;
        }
        public void render()
        {
            trim(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - Convert.ToInt32(length * 1000)); //trim excess data
            graphicsView.Invalidate();
        }

        public void trim(long timestamp)
        {
            int tmp = timestamps.FindLastIndex(x => x < timestamp) + 1; //get index of last point to remove
            timestamps.RemoveRange(0, tmp);
            foreach(var item in this.dataset) item.data.RemoveRange(0, tmp); //remove everything up to that point
        }

        public void push(double?[] data) {
            timestamps.Add(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            for(int i = 0; i < dataset.Length; i++) dataset[i].data.Add((i < data.Length)? data[i] : null);
        }
    }
    public class GraphicsDrawable : IDrawable
    {
        public Graph graph;
        static private double map(double inp, double imin, double imax, double omin, double omax) {
            return (inp - imin) / (imax - imin) * (omax - omin) + omin;
        }
        public void Draw(ICanvas canvas, RectF w)
        {
            if (graph is null || canvas is null) return; //seems to happen sometimes

            float bLeft = 20;
            float bTop = 10;
            float bRight = 50;
            float bBottom = 30;
            float tickLength = 5;
            canvas.FontSize = 14;
            canvas.Font = Microsoft.Maui.Graphics.Font.Default;
            canvas.FontColor = Color.FromArgb("ff333333");

            //borders
            canvas.StrokeSize = 2;
            canvas.StrokeColor = Color.FromArgb("ff333333");
            canvas.DrawLine(bLeft, (float)graph.sy - bBottom, (float)graph.sx - bRight, (float)graph.sy - bBottom); //horizontal
            canvas.DrawLine((float)graph.sx - bRight, bTop, (float)graph.sx - bRight, (float)graph.sy - bBottom); //vertical
            //tickmarks
            canvas.DrawLine(20, (float)graph.sy - bBottom, 20, (float)graph.sy - bBottom+tickLength);
            canvas.DrawLine((float)graph.sx - 50, (float)graph.sy - bBottom, (float)graph.sx - 50, (float)graph.sy - bBottom+tickLength);
            for (int i = 0; i <= graph.divs; i++) canvas.DrawLine((float)graph.sx - 50, bTop+((float)graph.sy - bBottom-bTop) /graph.divs*i, (float)graph.sx - 46, bTop + ((float)graph.sy - bBottom - bTop) / graph.divs * i);
            if (Math.Sign(graph.yMin) != Math.Sign(graph.yMax)) canvas.DrawLine(bLeft, (float)map(0, graph.yMin, graph.yMax, graph.sy - bBottom, 10), (float)graph.sx - bRight, (float)map(0, graph.yMin, graph.yMax, graph.sy - bBottom, 10)); //extra line through zero if applicable
            //writing
            canvas.DrawString("T+0", (float)graph.sx - bRight - 50, (float)graph.sy - bBottom + 10, 100, 20, HorizontalAlignment.Center, VerticalAlignment.Top);
            canvas.DrawString($"T+{graph.length:00}s", bLeft - 50, (float)graph.sy - bBottom + 10, 100, 20, HorizontalAlignment.Center, VerticalAlignment.Top);

            for (int i = 0; i <= graph.divs; i++) canvas.DrawString(string.Format(graph.format, map(i, 0, graph.divs, graph.yMin, graph.yMax)), (float)graph.sx-bRight+10, (float)map(i,0,graph.divs,graph.sy-bBottom,bTop)-10, 100, 20, HorizontalAlignment.Left, VerticalAlignment.Center);
            //line sets
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var data in graph.dataset)
            {
                PathF path = new PathF();
                for (int i = 0; i < graph.timestamps.Count; i++) {
                    if (data.data.ElementAt(i) is not null)
                    {
                        PointF loc = new PointF(
                            (float)map(
                                graph.timestamps.ElementAt(i),
                                now,
                                now - 10000,
                                graph.sx - 50,
                                20)
                            , (float)map(
                                (double)data.data.ElementAt(i),
                                graph.yMin,
                                graph.yMax,
                                graph.sy - bBottom,
                                10)
                            );
                        if (path.Count == 0) { path.MoveTo(loc); } else { path.LineTo(loc); }
                    }
                }
                canvas.StrokeColor = data.color;
                canvas.DrawPath(path);
            }
        }
    }
}