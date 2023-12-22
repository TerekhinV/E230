using System.Timers;

namespace Lab6
{
    public partial class MainPage : ContentPage
    {
        static System.Timers.Timer timer; //time control
        static Ball[] balls; //main array
        static int limit = 1000; //number of balls
        static double frameTarget = 60; //FPS target
        static int mspf = (int)(1000 / frameTarget); //to milliseconds
        static double multiplier = 1; //speed
        static double compmultiplier = multiplier * mspf * frameTarget / 1000; //adjust speed to FPS
        public MainPage()
        {
            InitializeComponent();
            timer = new System.Timers.Timer();
            timer.Interval = mspf;
            timer.Elapsed += rLoop;
            status.Text = "Setup "+mspf+"ms timer";

            balls = new Ball[limit]; //populate balls
            for (int i = 0; i < balls.Length; i++)
            {
                balls[i] = new Ball();
            }
            GraphicsDrawable.bindArray(balls); //link ball array to draw function
            status.Text = "created "+limit+" balls";

            timer.Start();
            status.Text = "loop started with params: "+limit+" balls, "+mspf+" mspf, "+compmultiplier+" multiplier";
        }
        public void updateValues(Object s, EventArgs e)
        {
            try
            {
                int n;
                double m;
                if (int.TryParse(nBalls.Text, out n))
                {
                    limit = n;
                    Array.Resize(ref balls, limit);
                    for (int i = 0; i < balls.Length; i++) if (balls[i] == null) balls[i] = new Ball(); //resize array, fill gaps, send new array ref to draw function
                    GraphicsDrawable.bindArray(balls);
                };
                if (double.TryParse(fps.Text, out m)) frameTarget = m;
                if (double.TryParse(spMult.Text, out m)) multiplier = m; //update timer controls for new FPS
                mspf = (int)(1000 / frameTarget);
                compmultiplier = multiplier * mspf * 60 / 1000;
                timer.Interval = mspf;

                status.Text = "updated params: " + limit + " balls, " + mspf + " mspf, " + compmultiplier + " multiplier";
            } catch { status.Text = "Failed to parse some or all values"; }
        }

        void rLoop(Object s, ElapsedEventArgs e) //main loop: run update logic for all balls, render
        {
            foreach (var ball in balls) ball.updatePosition(canvas.Width, canvas.Height, compmultiplier);
            canvas.Invalidate();
        }
    }
    public class GraphicsDrawable : IDrawable
    {
        static Ball[] b;
        static public void bindArray(Ball[] arr) { b = arr; } //array binder func
        public void Draw(ICanvas canvas, RectF w)
        {
            foreach (var ball in b) //render balls
            {
                canvas.FillColor = ball.c;
                canvas.FillCircle(new Point(ball.x, ball.y), (double)ball.s);
            }
        }
    }
    public class Ball
    {
        public double x, y, dx, dy;
        public int s;
        public Color c;
        static Random r = new Random();
        public Ball(double x, double y, double dx, double dy, int s, Color c) //main constructor
        {
            this.x = x;
            this.y = y;
            this.dx = dx;
            this.dy = dy;
            this.s = s;
            this.c = c;
        }
        public Ball() : this(5, 5, r.NextDouble()*10+5, r.NextDouble() * 10 + 5, r.Next(5, 15), Color.FromHsv(r.Next(0, 360), 255, 255)) { } //overload redirect to make random balls

        public void updatePosition(double maxX, double maxY, double mult)
        {
            if (x - s < 0 && dx < 0 || x + s > maxX && dx > 0) { dx *= -1; dy += r.NextDouble() - 0.5; } //check X collisions. On collide also adjust dY a bit for extra random
            if (y - s < 0 && dy < 0 || y + s > maxY && dy > 0) { dy *= -1; dx += r.NextDouble() - 0.5; } //same on other axis
            x += dx * mult; //move
            y += dy * mult;
        }
    }
}