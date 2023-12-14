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

        public static InventoryItemSlots playerInventoryUI = new InventoryItemSlots(), otherInventoryUI = new InventoryItemSlots();
        public Panel playerInventoryPanel = new Panel(), otherInventoryPanel = new Panel();

        public static Dictionary<string, GameObject> registeredGameObjects { get; private set; } = new Dictionary<string, GameObject>();
        public static List<GameObject> spritesToRender { get; private set; } = new List<GameObject>();
        public static List<GameObject> UISpritesToRender { get; private set; } = new List<GameObject>();

        public static List<Collider> debugRectangles { get; private set; } = new List<Collider>();
        public static List<Animation> runningAnimations { get; private set; } = new List<Animation>();
        public static List<Collider> registeredColliders { get; private set; } = new List<Collider>();

        public Color backgroundColor;
        public static readonly Bitmap emptyImage = new Bitmap(1, 1);

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

        public void InitializeInventories()
        {

            //playerInventoryPanel.BackgroundImage = Image.FromFile("Assets/Inventory-Background.png");
            playerInventoryPanel.BackgroundImage = null;//Image.FromFile("Assets/Inventory-Background.png");
            //playerInventoryPanel.BackgroundImageLayout = ImageLayout.Tile;
            playerInventoryPanel.Controls.Add(playerInventoryUI);
            playerInventoryPanel.Location = new Point(50, 50);
            playerInventoryPanel.Size = new Size(640, 384);
            playerInventoryPanel.Name = "player inv panel";
            System.Reflection.PropertyInfo pi2 = typeof(Panel).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            pi2.SetValue(playerInventoryPanel, true, null);

            //otherInventoryPanel.BackgroundImage = Image.FromFile("Assets/Inventory-Background.png");
            //otherInventoryPanel.BackgroundImageLayout = ImageLayout.Tile;
            playerInventoryPanel.BackgroundImage = null;
            otherInventoryPanel.Controls.Add(otherInventoryUI);
            otherInventoryPanel.Location = new Point(50, 500);
            otherInventoryPanel.Size = new Size(640, 384);
            otherInventoryPanel.Name = "other inv panel";
            System.Reflection.PropertyInfo pi3 = typeof(Panel).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            pi3.SetValue(otherInventoryPanel, true, null);

            #region playerInventoryUI setup
            playerInventoryUI.ColumnHeadersVisible = false;
            playerInventoryUI.RowHeadersVisible = false;
            playerInventoryUI.ScrollBars = ScrollBars.None;
            playerInventoryUI.BorderStyle = BorderStyle.None;
            playerInventoryUI.CellBorderStyle = DataGridViewCellBorderStyle.None;
            playerInventoryUI.Size = new Size(640, 384);
            playerInventoryUI.AllowUserToAddRows = false;playerInventoryUI.AllowUserToDeleteRows = false;playerInventoryUI.AllowUserToOrderColumns = false;playerInventoryUI.AllowUserToResizeRows = false;playerInventoryUI.AllowUserToResizeColumns = false;

            otherInventoryUI.ColumnHeadersVisible = false;
            otherInventoryUI.RowHeadersVisible = false;
            otherInventoryUI.ScrollBars = ScrollBars.None;
            otherInventoryUI.Size = new Size(640, 320);

            playerInventoryUI.Rows.Clear();
            playerInventoryUI.Columns.Clear();

            playerInventoryUI.Columns.Add(new DataGridViewImageColumn());
            playerInventoryUI.Columns[0].Width = 128;

            playerInventoryUI.Rows.Add(new DataGridViewRow());
            playerInventoryUI.Rows[0].Height = 128;

            
            DataGridViewImageColumn template = playerInventoryUI.Columns[0] as DataGridViewImageColumn;
            DataGridViewRow template2 = playerInventoryUI.Rows[0];
            

            playerInventoryUI.Rows.Clear();
            playerInventoryUI.Columns.Clear();

            playerInventoryUI.Columns.AddRange(template.Clone() as DataGridViewImageColumn, template.Clone() as DataGridViewImageColumn, template.Clone() as DataGridViewImageColumn, template.Clone() as DataGridViewImageColumn, template.Clone() as DataGridViewImageColumn);
            playerInventoryUI.Rows.AddRange(template2.Clone() as DataGridViewRow, template2.Clone() as DataGridViewRow, template2.Clone() as DataGridViewRow);
            playerInventoryPanel.Controls.Add(playerInventoryUI);
            playerInventoryUI.Dock = DockStyle.None;//DockStyle.Top | DockStyle.Right | DockStyle.Bottom | DockStyle.Left; 
            
            for(int i = 0; i < playerInventoryUI.Rows.Count; i++) 
            { 
                for(int j = 0; j < playerInventoryUI.Rows[i].Cells.Count; j++) 
                { 
                    playerInventoryUI.Rows[i].Cells[j].Value = emptyImage; 
                    playerInventoryUI.Rows[i].Cells[j].ToolTipText = "";
                } 
            }
            playerInventoryUI.CellClick += new DataGridViewCellEventHandler(Game.Game.MainGameInstance.playerInventory.ItemClicked);
            #endregion

            #region otherInventoryUI setup
            otherInventoryUI.ColumnHeadersVisible = false;
            otherInventoryUI.RowHeadersVisible = false;
            otherInventoryUI.ScrollBars = ScrollBars.None;
            otherInventoryUI.BorderStyle = BorderStyle.None;
            otherInventoryUI.CellBorderStyle = DataGridViewCellBorderStyle.None;
            otherInventoryUI.Size = new Size(640, 384);
            otherInventoryUI.AllowUserToAddRows = false; otherInventoryUI.AllowUserToDeleteRows = false; otherInventoryUI.AllowUserToOrderColumns = false; otherInventoryUI.AllowUserToResizeRows = false; otherInventoryUI.AllowUserToResizeColumns = false; otherInventoryUI.Rows.Clear();

            otherInventoryUI.Rows.Clear();
            otherInventoryUI.Columns.Clear();

            otherInventoryUI.Columns.AddRange(template.Clone() as DataGridViewImageColumn, template.Clone() as DataGridViewImageColumn, template.Clone() as DataGridViewImageColumn, template.Clone() as DataGridViewImageColumn, template.Clone() as DataGridViewImageColumn);
            otherInventoryUI.Rows.AddRange(template2.Clone() as DataGridViewRow, template2.Clone() as DataGridViewRow, template2.Clone() as DataGridViewRow);
            otherInventoryPanel.Controls.Add(otherInventoryUI);
            otherInventoryUI.Dock = DockStyle.None;

            for (int i = 0; i < otherInventoryUI.Rows.Count; i++) 
            { 
                for (int j = 0; j < otherInventoryUI.Rows[i].Cells.Count; j++) 
                { 
                    otherInventoryUI.Rows[i].Cells[j].Value = emptyImage; 
                    otherInventoryUI.Rows[i].Cells[j].ToolTipText = "";
                } 
            }
            //otherInventoryUI.CellClick += new DataGridViewCellEventHandler(Game.Game.MainGameInstance.otherInventory.ItemClicked);
            #endregion


            window.Controls.Add(playerInventoryPanel);
            window.Controls.Add(otherInventoryPanel);
            //otherInventoryPanel.GotFocus += new EventHandler((object? o, EventArgs e) => window.Focus()); //neni treba, viz inventory slot - SetStyle(neco.Selectable, false); :)
            //playerInventoryPanel.GotFocus += new EventHandler((object? o, EventArgs e) => window.Focus());
            //playerInventoryUI.GotFocus += new EventHandler((object? o, EventArgs e) => window.Focus());
            //otherInventoryUI.GotFocus += new EventHandler((object? o, EventArgs e) => window.Focus());
            //for (int i = 0; i < inventory.inventorySlots.Count; i++)
            //{
            //    window.Controls.Add(inventory.inventorySlots[i]);
            //}

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
                    foreach (Collider col in debugRectangles)
                    {
                        if (!col.enabled) { continue; }
                        if(col.GetType() == typeof(BoxCollider))
                        {
                            BoxCollider box = col as BoxCollider;
                            if (box == null) { Debug.LogError("ERROR LOADING COLLIDER DEBUG RECTANGLE"); continue; }
                            graphics.DrawRectangle(new Pen(box.isTrigger ? Color.Cyan : Color.Orange, 2), box.gameObject.transform.position.x + box.offset.x - box.size.x / 2, box.gameObject.transform.position.y + box.offset.y - box.size.y / 2, box.size.x, box.size.y);
                        }else if(col.GetType() == typeof(CircleCollider))
                        {
                            CircleCollider circle = col as CircleCollider;
                            if (circle == null) { Debug.LogError("ERROR LOADING COLLIDER DEBUG RECTANGLE"); continue; }
                            //Debug.Log($"Drawing circle at ({circle.gameObject.transform.position.x + circle.offset.x - circle.radius}, {circle.gameObject.transform.position.y + circle.offset.y - circle.radius}) with size {circle.radius * 2}");
                            graphics.DrawEllipse(new Pen(circle.isTrigger ? Color.Cyan : Color.Orange, 2), circle.gameObject.transform.position.x + circle.offset.x - circle.radius, circle.gameObject.transform.position.y + circle.offset.y - circle.radius, circle.radius*2, circle.radius*2);
                        }


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
        public static void ChangeSpriteRenderOrder(GameObject GameObject, int order) { spritesToRender.Remove(GameObject); spritesToRender.Insert(order > spritesToRender.Count ? spritesToRender.Count - 1 : order, GameObject); }

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
        public static void ChangeUISpriteRenderOrder(GameObject GameObject, int order) { UISpritesToRender.Remove(GameObject); UISpritesToRender.Insert(order > UISpritesToRender.Count ? UISpritesToRender.Count-1 : order, GameObject); }

        //Debug boxes list
        public static void AddDebugRectangle(Collider Collider, int order = int.MaxValue) { if (order != int.MaxValue && order < debugRectangles.Count) { debugRectangles.Insert(order, Collider); return; } debugRectangles.Add(Collider); }
        public static void RemoveDebugRectangle(Collider Collider) { debugRectangles.Remove(Collider); }
        
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
    public class InventoryItemSlots : DataGridView
    {
        private bool alreadyTransparent;
        private Image backgroundImage;
        protected override void PaintBackground(Graphics graphics, Rectangle clipBounds, Rectangle gridBounds)
        {
            base.PaintBackground(graphics, clipBounds, gridBounds);
            
            //painting background image
            for(int i = 0; i < (int)(Parent.Width / backgroundImage.Width); i++)
            {
                for(int j = 0; j < (int)(Parent.Height / backgroundImage.Height); j++)
                {
                    graphics.DrawImage(backgroundImage, i* backgroundImage.Width, j* backgroundImage.Height, backgroundImage.Width, backgroundImage.Height);
                }
            }
            
            //making the original background transparent
            if (!alreadyTransparent) { SetCellsTransparent(); alreadyTransparent = true; }
        }
        public InventoryItemSlots()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.Selectable, false);
            backgroundImage = Image.FromFile("Assets/Inventory-Background.png");
        }

        public void SetCellsTransparent()
        {
            this.EnableHeadersVisualStyles = false;
            this.ColumnHeadersDefaultCellStyle.BackColor = Color.Transparent;
            this.RowHeadersDefaultCellStyle.BackColor = Color.Transparent;


            foreach (DataGridViewColumn col in this.Columns)
            {
                col.DefaultCellStyle.BackColor = Color.Transparent;
                col.DefaultCellStyle.SelectionBackColor = Color.Transparent;
            }
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
        [JsonIgnore] public float magnitude { get => MathF.Sqrt(x*x + y*y); }
        [JsonIgnore] public Vector2 normalized { get => magnitude == 0 ? Vector2.zero : new Vector2(x / magnitude, y / magnitude); }
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
