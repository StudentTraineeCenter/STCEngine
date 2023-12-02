using System;
using System.Windows.Forms;
using System.Threading;
//using System.Windows;
//using System.Drawing;
//using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace STCEngine.Engine
{
    /// <summary>
    /// The main class of the engine, the Game class derives off this one
    /// </summary>
    public abstract class EngineClass
    {
        public readonly bool testDebug = true;

        public Vector2 screenSize { get => new Vector2(window.Width, window.Height); set { window.Size = new Size((int)value.x, (int)value.y); } }
        private string title;
        private Canvas window;
        //public Thread gameLoopThread;
        private static System.Windows.Forms.Timer animationTimer = new System.Windows.Forms.Timer();
        private static System.Windows.Forms.Timer gameLoopTimer = new System.Windows.Forms.Timer();
        public Button resumeButton, quitButton;
        public static bool paused;

        public static Dictionary<string, GameObject> registeredGameObjects = new Dictionary<string, GameObject>();
        public static List<GameObject> spritesToRender = new List<GameObject>();
        public static List<GameObject> UISpritesToRender = new List<GameObject>();

        public static List<BoxCollider> debugRectangles = new List<BoxCollider>();
        public static List<Animation> runningAnimations = new List<Animation>();
        public static List<Collider> registeredColliders = new List<Collider>();

        public Color backgroundColor;

        public Vector2 cameraPosition = Vector2.zero;

        /// <summary>
        /// Starts the game loop and the game window
        /// </summary>
        public EngineClass(Vector2 screenSize, string title)
        {
            window = new Canvas();
            this.screenSize = screenSize;
            this.title = title;

            //Rendering
            //window.Size = new Size((int)this.screenSize.x, (int)this.screenSize.y); already done by changing screen size above
            window.Text = this.title;
            window.Paint += Renderer;

            //Animations
            animationTimer.Tick += new EventHandler(RunAnimations);
            animationTimer.Interval = 10;
            animationTimer.Start();

            //Input
            window.KeyDown += Window_KeyDown;
            window.KeyUp += Window_KeyUp;

            OnLoad();
            //gameLoopThread = new Thread(GameLoop);
            //gameLoopThread.Start();
            gameLoopTimer.Tick += new EventHandler(GameLoop); //timer funguje, thread ne
            gameLoopTimer.Interval = 1;
            gameLoopTimer.Start();




            Application.Run(window);

            OnExit();
            //gameLoopThread.Interrupt();
            animationTimer.Stop();
            gameLoopTimer.Stop();
        }

        public void CreatePauseScreenButtons()
        {
            quitButton = new Button();
            quitButton.BackColor = Color.White; 
            quitButton.ForeColor = Color.Black;
            quitButton.Text = "Quit";
            quitButton.Font = new Font(quitButton.Font.FontFamily, 30);
            quitButton.Size = new Size(300, 150);
            quitButton.Location = new Point((int)screenSize.x / 2 - 200 - quitButton.Size.Width/2, (int)screenSize.y / 3 * 2);
            quitButton.MouseEnter += new EventHandler((object? o, EventArgs e) => quitButton.BackColor = Color.SteelBlue);
            quitButton.MouseLeave += new EventHandler((object? o, EventArgs e) => quitButton.BackColor = Color.White);
            window.Controls.Add(quitButton);
            
            resumeButton = new Button();
            resumeButton.BackColor = Color.White;
            resumeButton.ForeColor = Color.Black; 
            resumeButton.Text = "Resume";
            resumeButton.Font = new Font(resumeButton.Font.FontFamily, 30);
            resumeButton.Size = new Size(300, 150);
            resumeButton.Location = new Point((int)screenSize.x / 2 + 200 - resumeButton.Size.Width / 2, (int)screenSize.y / 3 * 2);
            resumeButton.MouseEnter += new EventHandler((object? o, EventArgs e) => resumeButton.BackColor = Color.SteelBlue);
            resumeButton.MouseLeave += new EventHandler((object? o, EventArgs e) => resumeButton.BackColor = Color.White);
            window.Controls.Add(resumeButton);

            quitButton.Enabled = false;
            resumeButton.Enabled = false;
            quitButton.Visible = false;
            resumeButton.Visible = false;
            
        }

        #region Inner logic classes (GameLoop, Renderer, Animations,...)

        /// <summary>
        /// The main game loop running on a timer, calls the Update funcions
        /// </summary>
        private void GameLoop(object? sender, EventArgs args)
        {
            if (!paused) { Update(); }
            window.BeginInvoke((MethodInvoker)delegate { window.Refresh(); });
            if (!paused) { LateUpdate(); }
        }

        /// <summary>
        /// Takes care of rendering things inside the game window
        /// </summary>
        private void Renderer(object? sender, PaintEventArgs args)
        {
            try
            {
                Graphics graphics = args.Graphics;
            
                graphics.Clear(backgroundColor);

                foreach (GameObject gameObject in spritesToRender)
                {
                    Sprite? sprite = gameObject.GetComponent<Sprite>();
                    //if(gameObject.name == "Kamena stena") { Debug.Log(sprite.image.Height.ToString()); }
                    if (sprite != null && sprite.enabled && gameObject.isActive)
                    {
                        //foreach(Sprite sprite in gameObject.GetComponents<Sprite>()) //pro vice spritu na jednom objektu by to teoreticky fungovat mohlo, ale pak by nesel odstranit specificky sprite
                        //{
                        graphics.DrawImage(sprite.image, gameObject.transform.position.x - sprite.image.Width * gameObject.transform.size.x / 2, gameObject.transform.position.y - sprite.image.Height * gameObject.transform.size.y / 2, sprite.image.Width * gameObject.transform.size.x, sprite.image.Height * gameObject.transform.size.y);
                        //}
                    }
                    Tilemap? tilemap = gameObject.GetComponent<Tilemap>();
                    if(tilemap != null && tilemap.enabled && gameObject.isActive)
                    {
                        graphics.DrawImage(tilemap.tileMapImage, gameObject.transform.position.x - tilemap.tileMapImage.Width / 2, gameObject.transform.position.y - tilemap.tileMapImage.Height / 2, tilemap.tileMapImage.Width, tilemap.tileMapImage.Height);
                        //for(int y = 0; y < tilemap.mapSize.y; y++)
                        //{
                        //    for (int x = 0; x < tilemap.mapSize.x; x++)
                        //    {
                        //        //graphics.DrawImage(tilemap.tileMapImage, gameObject.transform.position.x - tilemap.tileMapImage.Width/2, gameObject.transform.position.y - tilemap.tileMapImage.Height / 2, tilemap.tileMapImage.Width, tilemap.tileMapImage.Height);//(tilemap.tiles[x, y], gameObject.transform.position.x + x * tilemap.tileSize.x, gameObject.transform.position.y + y * tilemap.tileSize.y, tilemap.tileSize.x, tilemap.tileSize.x);
                        //    }
                        //}
                    }
                }
                foreach(GameObject gameObject in UISpritesToRender)
                {
                    UISprite? UISprite = gameObject.GetComponent<UISprite>();
                    if (UISprite != null && UISprite.enabled && gameObject.isActive)
                    {
                        //foreach(Sprite sprite in gameObject.GetComponents<Sprite>()) //pro vice spritu na jednom objektu by to teoreticky fungovat mohlo, ale pak by nesel odstranit specificky sprite
                        //{
                        //Debug.Log($"graphics.DrawImage({gameObject.transform.position.x - UISprite.image.Width * gameObject.transform.size.x / 2 + UISprite.offset.x + UISprite.screenAnchorOffset.x}, {gameObject.transform.position.y - UISprite.image.Height * gameObject.transform.size.y / 2 + UISprite.offset.y + UISprite.screenAnchorOffset.y}, {UISprite.image.Width * gameObject.transform.size.x}, {UISprite.image.Height * gameObject.transform.size.y}");
                        graphics.DrawImage(UISprite.image, gameObject.transform.position.x - UISprite.image.Width * gameObject.transform.size.x / 2 + UISprite.offset.x + UISprite.screenAnchorOffset.x, gameObject.transform.position.y - UISprite.image.Height * gameObject.transform.size.y / 2 + UISprite.offset.y + UISprite.screenAnchorOffset.y, UISprite.image.Width * gameObject.transform.size.x, UISprite.image.Height * gameObject.transform.size.y);
                        if(gameObject.name == "Pause Screen")
                        {
                            quitButton.Enabled = true;
                            resumeButton.Enabled = true;
                            quitButton.Visible = true;
                            resumeButton.Visible = true;
                        }
                        //}
                    }
                    else if (gameObject.name == "Pause Screen")
                    {
                        quitButton.Enabled = false;
                        quitButton.Enabled = false;
                        quitButton.Visible = false;
                        resumeButton.Visible = false;
                        window.Focus();
                    }
                }
                //DEBUG ---------
                if (testDebug)
                {
                    foreach (BoxCollider box in debugRectangles)
                    {
                        if (box == null) { Debug.LogError("ERROR LOADING COLLIDER DEBUG RECTANGLE"); continue; }
                        graphics.DrawRectangle(new Pen(box.isTrigger ? Color.Cyan : Color.Orange, 1), box.gameObject.transform.position.x + box.offset.x - box.size.x / 2, box.gameObject.transform.position.y + box.offset.y - box.size.y / 2, box.size.x, box.size.y);
                    }
                    foreach (KeyValuePair<string, GameObject> gameObject in registeredGameObjects)
                    {
                        //graphics.DrawRectangle(new Pen(Color.Black), gameObject.Value.transform.position.x, gameObject.Value.transform.position.y, 2, 2);
                        graphics.DrawString(gameObject.Value.name, new Font("Arial", 11, FontStyle.Regular), new SolidBrush(Color.Black), gameObject.Value.transform.position.x - gameObject.Value.name.Length * 4f, gameObject.Value.transform.position.y - 5.5f);
                    }
                }

                graphics.TranslateTransform(cameraPosition.x, cameraPosition.y);
            }
            catch (Exception e) { Debug.LogError("Error running renderer, error message: " + e.Message); }
        }
        public static void RunAnimations(object? sender, EventArgs eventArgs)
        {
            if (paused) { return; }
            try
            {
                foreach (Animation anim in runningAnimations)
                {
                    anim.RunAnimation();
                }
            }catch(Exception e) { Debug.LogError("Error running animations, error message: " + e.Message); }
        }
        #endregion

        #region Game logic classes
        public abstract void Update();
        public abstract void LateUpdate();
        public abstract void OnLoad();
        public abstract void OnExit();
        private void Window_KeyDown(object? sender, KeyEventArgs e) { GetKeyDown(e); } public abstract void GetKeyDown(KeyEventArgs e);
        private void Window_KeyUp(object? sender, KeyEventArgs e) { GetKeyUp(e); } public abstract void GetKeyUp(KeyEventArgs e);
        #endregion

        #region List Registrations ------------------------------------------------------------------------
        /// <summary>
        /// Registers the GameObject to the list of existing GameObjects
        /// </summary>
        public static void RegisterGameObject(GameObject GameObject) { registeredGameObjects.Add(GameObject.name, GameObject); }
        /// <summary>
        /// Unregisters the GameObject from the list of existing GameObjects
        /// </summary>
        public static void UnregisterGameObject(GameObject GameObject) { registeredGameObjects.Remove(GameObject.name); }
        #region Sprite rendering and animations
        //Sprites list
        /// <summary>
        /// Registers the GameObject with a Sprite to the render queue at the given index or at the end (drawn over everything)
        /// </summary>
        public static void AddSpriteToRender(GameObject GameObject, int order = int.MaxValue) { if (GameObject == null) { Debug.LogError("Tried to add empty sprite component!"); } if (order < 0) { order = 0; } if (order != int.MaxValue && order < spritesToRender.Count) { spritesToRender.Insert(order, GameObject); return; } spritesToRender.Add(GameObject); }
        /// <summary>
        /// Unregisters the GameObject with a Sprite from the render queue
        /// </summary>
        public static void RemoveSpriteToRender(GameObject GameObject) { spritesToRender.Remove(GameObject); }
        /// <summary>
        /// Moves the given GameObject to the given index in the render queue
        /// </summary>
        /// <param name="GameObject"></param>
        /// <param name="order"></param>
        public static void ChangeSpriteRenderOrder(GameObject GameObject, int order) { spritesToRender.Remove(GameObject); spritesToRender.Insert(order, GameObject); }

        //UI elements list
        /// <summary>
        /// Registers the UI GameObject with a Sprite to the render queue at the given index or at the end (drawn over everything)
        /// </summary>
        /// <param name="GameObject"></param>
        /// <param name="order"></param>
        public static void AddUISpriteToRender(GameObject GameObject, int order = int.MaxValue) { if (GameObject == null) { Debug.LogError("Tried to add empty UISprite component!"); } if (order < 0) { order = 0; } if (order != int.MaxValue && order < UISpritesToRender.Count) { UISpritesToRender.Insert(order, GameObject); return; } UISpritesToRender.Add(GameObject); }
        /// <summary>
        /// Unregisters the UI GameObject with a Sprite from the render queue
        /// </summary>
        /// <param name="GameObject"></param>
        public static void RemoveUISpriteToRender(GameObject GameObject) { UISpritesToRender.Remove(GameObject); }
        /// <summary>
        /// Moves the given UI GameObject to the given index in the render queue
        /// </summary>
        /// <param name="GameObject"></param>
        /// <param name="order"></param>
        public static void ChangeUISpriteRenderOrder(GameObject GameObject, int order) { UISpritesToRender.Remove(GameObject); UISpritesToRender.Insert(order, GameObject); }

        //Debug boxes list
        public static void AddDebugRectangle(BoxCollider BoxCollider, int order = int.MaxValue) { if (order != int.MaxValue && order < debugRectangles.Count) { debugRectangles.Insert(order, BoxCollider); return; } debugRectangles.Add(BoxCollider); }
        public static void RemoveDebugRectangle(BoxCollider BoxCollider) { debugRectangles.Remove(BoxCollider); }
        
        /// <summary>
        /// Registers the Animation to the animation queue
        /// </summary>
        public static void AddSpriteAnimation(Animation Animation) { runningAnimations.Add(Animation); }
        /// <summary>
        /// Unregisters the Animation from the animation queue
        /// </summary>
        public static void RemoveSpriteAnimation(Animation Animation) { runningAnimations.Remove(Animation); }
        #endregion
        #region Colliders
        public static void RegisterCollider(Collider Collider) { registeredColliders.Add(Collider); }
        public static void UnregisterCollider(Collider Collider) { registeredColliders.Remove(Collider); }
        #endregion
        #endregion

        public static void Destroy(GameObject GameObject) { GameObject.DestroySelf(); UnregisterGameObject(GameObject); }
        public static void Destroy(Component component) { component.DestroySelf(); }
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
            STCEngine.Game.Game testGame = new Game.Game();
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
        [JsonIgnore] public static Vector2 zero { get => new Vector2(0, 0); }
        /// <summary>
        /// (1, 1)
        /// </summary>
        [JsonIgnore] public static Vector2 one { get => new Vector2(1, 1); }
        /// <summary>
        /// (0, 1)
        /// </summary>
        [JsonIgnore] public static Vector2 up { get => new Vector2(0, 1); }
        /// <summary>
        /// (1, 0)
        /// </summary>
        [JsonIgnore] public static Vector2 right { get => new Vector2(1, 0); }
        /// <summary>
        /// Length of the vector as a float
        /// </summary>
        [JsonIgnore] public float length { get => MathF.Sqrt(x*x + y*y); }
        [JsonIgnore] public Vector2 normalized { get => length == 0 ? Vector2.zero : new Vector2(x / length, y / length); }
        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
        public static Vector2 operator +(Vector2 a, Vector2 b)
        {
            return new Vector2(a.x + b.x, a.y + b.y);
        }
        public static Vector2 operator -(Vector2 a, Vector2 b)
        {
            return new Vector2(a.x - b.x, a.y - b.y);
        }
        public static Vector2 operator -(Vector2 a)
        {
            return new Vector2(-a.x, -a.y);
        }
        public static Vector2 operator *(Vector2 a, float b)
        {
            return new Vector2(a.x * b, a.y * b);
        }
        public static Vector2 operator /(Vector2 a, float b)
        {
            return new Vector2(a.x / b, a.y / b);
        }
        public static Vector2 operator ^(Vector2 a, float b)
        {
            return new Vector2((float)Math.Pow(a.x, b), (float)Math.Pow(a.y, b));
        }
        public override string ToString()
        {
            return "(" + x + ", " + y + ")";
        }
    }
    
}
