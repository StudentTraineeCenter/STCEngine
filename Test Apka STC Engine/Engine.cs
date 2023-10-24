using System;
using System.Windows.Forms;
using System.Threading;
//using System.Windows;
//using System.Drawing;
//using System.Numerics;


namespace STCEngine.Engine
{
    public abstract class Engine
    {
        private Vector2 screenSize;
        private string title;
        private Canvas window;
        public Thread gameLoopThread;

        public static List<GameObject> gameObjects = new List<GameObject>();
        public Color backgroundColor;
        public Engine(Vector2 screenSize, string title)
        {
            this.screenSize = screenSize;
            this.title = title;
            window = new Canvas();
            window.Size = new Size((int)this.screenSize.x, (int)this.screenSize.y);
            window.Text = this.title;
            window.Paint += Renderer;

            OnLoad();
            gameLoopThread = new Thread(GameLoop);
            gameLoopThread.Start();
            
            Application.Run(window);

            OnExit();
        }
        private void GameLoop()
        {
            while (gameLoopThread.IsAlive)
            {
                try
                {
                    Update();
                    window.BeginInvoke((MethodInvoker)delegate { window.Refresh(); });
                    LateUpdate();
                
                    Thread.Sleep(1);
                }
                catch
                {
                    Console.WriteLine("Loading...");
                }
            }
        }
        private void Renderer(object sender, PaintEventArgs args)
        {
            Graphics graphics = args.Graphics;
            graphics.Clear(backgroundColor);
        }
        public abstract void Update();
        public abstract void LateUpdate();
        public abstract void OnLoad();
        public abstract void OnExit();
        public static void AddGameObject(GameObject gameObject) { gameObjects.Add(gameObject); }
    }

    public class Canvas : Form
    {
        public Canvas()
        {
            this.DoubleBuffered = true;
        }
    }


}
namespace STCEngine
{

    class Program
    {
        static void Main(string[] args)
        {
            Game testGame = new Game();
            //var a = new Vector2();
            //a.x = 1;
            //a.y = 23;
            //var b = new Vector2(454, 312);
            //Console.WriteLine("a == (" + a.x + ", " + a.y + "), length: " + a.length);
            //Console.WriteLine("b == (" + b.x + ", " + b.y + ")");
        }
    }

    public class Vector2
    {
        public float x { get; set; }
        public float y { get; set; }

        public static Vector2 zero { get => new Vector2(0, 0); }
        public static Vector2 up { get => new Vector2(0, 1); }
        public static Vector2 right { get => new Vector2(1, 0); }
        public float length { get => MathF.Sqrt(x*x + y*y); }



        public Vector2()
        {
            x = 0;
            y = 0;
        }
        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

    }
    public class GameObject
    {
        public Vector2 position;
        public float rotation;
        public Vector2 size;

        public GameObject(Vector2 position = null, float rotation = 0, Vector2 size = null)
        {
            this.position = position == null ? Vector2.zero : position;
            this.rotation = rotation;
            this.size = size == null ? Vector2.zero : size;
        }
        //public 
        //public GameObject() { }
    }
    public class Sprite : GameObject
    {
        public Image image;
        public Sprite(Image image)
        {
            this.image = image;
        }
    }
}
