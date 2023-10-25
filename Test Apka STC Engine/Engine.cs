using System;
using System.Windows.Forms;
using System.Threading;
//using System.Windows;
//using System.Drawing;
//using System.Numerics;


namespace STCEngine.Engine
{   
    /// <summary>
    /// The main class of the engine, the Game class derives off this one
    /// </summary>
    public abstract class EngineClass
    {
        private Vector2 screenSize;
        private string title;
        private Canvas window;
        public Thread gameLoopThread;

        public static List<GameObject> registeredGameObjects = new List<GameObject>();
        public Color backgroundColor;

        /// <summary>
        /// Starts the game loop and the game window
        /// </summary>
        public EngineClass(Vector2 screenSize, string title)
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
        /// <summary>
        /// The main game loop thread, calls the Update funcions
        /// </summary>
        private void GameLoop()
        {
            bool cancelationToken = false;
            while (!cancelationToken && gameLoopThread.IsAlive)
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
                    Debug.LogWarning("Window not found, exiting thread...");
                    cancelationToken = true;
                }
            }
        }
        /// <summary>
        /// Takes care of rendering things inside the game window
        /// </summary>
        private void Renderer(object sender, PaintEventArgs args)
        {
            Graphics graphics = args.Graphics;
            
            graphics.Clear(backgroundColor);

            foreach(GameObject gameObject in registeredGameObjects)
            {
                graphics.FillRectangle(new SolidBrush(Color.Green), gameObject.transform.position.x, gameObject.transform.position.y, gameObject.transform.size.x, gameObject.transform.size.y);
            }
        }
        public abstract void Update();
        public abstract void LateUpdate();
        public abstract void OnLoad();
        public abstract void OnExit();
        /// <summary>
        /// Registers the GameObject to the list of existing GameObjects
        /// </summary>
        public static void RegisterGameObject(GameObject gameObject) { registeredGameObjects.Add(gameObject); }
        /// <summary>
        /// Unregisters the GameObject from the list of existing GameObjects
        /// </summary>
        public static void UnregisterGameObject(GameObject gameObject) { registeredGameObjects.Remove(gameObject); }
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

    /// <summary>
    /// An ordered pair of floats
    /// </summary>
    public class Vector2
    {
        public float x { get; set; }
        public float y { get; set; }

        /// <summary>
        /// (0, 0)
        /// </summary>
        public static Vector2 zero { get => new Vector2(0, 0); }
        /// <summary>
        /// (1, 1)
        /// </summary>
        public static Vector2 one { get => new Vector2(1, 1); }
        /// <summary>
        /// (0, 1)
        /// </summary>
        public static Vector2 up { get => new Vector2(0, 1); }
        /// <summary>
        /// (1, 0)
        /// </summary>
        public static Vector2 right { get => new Vector2(1, 0); }
        /// <summary>
        /// Length of the vector as a float
        /// </summary>
        public float length { get => MathF.Sqrt(x*x + y*y); }



        //public Vector2()
        //{
        //    x = 0;
        //    y = 0;
        //}
        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

    }
    
}
