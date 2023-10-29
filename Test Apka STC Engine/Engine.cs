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

        public static Dictionary<string, GameObject> registeredGameObjects = new Dictionary<string, GameObject>();
        public static List<GameObject> spritesToRender = new List<GameObject>();
        public static List<Animation> runningAnimations = new List<Animation>();
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
            window.Initialize();

            OnLoad();
            gameLoopThread = new Thread(GameLoop);
            gameLoopThread.Start();
            
            Application.Run(window);

            OnExit();
            window.Exit();
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

                    RunAnimations();
                
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

            //foreach(KeyValuePair<string, GameObject> stringGameObject in registeredGameObjects)
            //{
            //    graphics.FillRectangle(new SolidBrush(Color.Green), stringGameObject.Value.transform.position.x, stringGameObject.Value.transform.position.y, stringGameObject.Value.transform.size.x, stringGameObject.Value.transform.size.y);
            //}
            foreach (GameObject gameObject in spritesToRender)
            {
                Sprite image = gameObject.GetComponent<Sprite>();
                graphics.DrawImage(image.image, gameObject.transform.position.x, gameObject.transform.position.y, image.image.Width*gameObject.transform.size.x, image.image.Height * gameObject.transform.size.y);
            }
        }
        public static void RunAnimations()
        {
            foreach(Animation anim in runningAnimations) { anim.RunAnimation(); }
        }
        public abstract void Update();
        public abstract void LateUpdate();
        public abstract void OnLoad();
        public abstract void OnExit();
        /// <summary>
        /// Registers the GameObject to the list of existing GameObjects
        /// </summary>
        public static void RegisterGameObject(GameObject GameObject) { registeredGameObjects.Add(GameObject.name, GameObject); }
        /// <summary>
        /// Unregisters the GameObject from the list of existing GameObjects
        /// </summary>
        public static void UnregisterGameObject(GameObject GameObject) { registeredGameObjects.Remove(GameObject.name); }
        /// <summary>
        /// Registers the GameObject with a Sprite to the render queue
        /// </summary>
        public static void AddSpriteToRender(GameObject GameObject) { spritesToRender.Add(GameObject); }
        /// <summary>
        /// Unregisters the GameObject with a Sprite from the render queue
        /// </summary>
        public static void RemoveSpriteToRender(GameObject GameObject) { spritesToRender.Remove(GameObject); }
        /// <summary>
        /// Registers the Animation to the animation queue
        /// </summary>
        public static void AddSpriteAnimation(Animation Animation) { runningAnimations.Add(Animation); }
        /// <summary>
        /// Unregisters the Animation from the animation queue
        /// </summary>
        public static void RemoveSpriteAnimation(Animation Animation) { runningAnimations.Remove(Animation); }
        public static void Destroy(GameObject GameObject) { GameObject.DestroySelf(); UnregisterGameObject(GameObject); }
        public static void Destroy(Component component) { component.DestroySelf(); }
    }
    public class Canvas : Form
    {
        private static System.Windows.Forms.Timer animationTimer = new System.Windows.Forms.Timer();
        public void AnimationTimer(object? sender, EventArgs e)
        {
            EngineClass.RunAnimations();
        }
        public Canvas()
        {
            this.DoubleBuffered = true;
        }
        public void Initialize()
        {
            animationTimer.Tick += new EventHandler(AnimationTimer);
            animationTimer.Interval = 10;
            animationTimer.Start();
        }
        public void Exit() { animationTimer.Stop(); }
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
