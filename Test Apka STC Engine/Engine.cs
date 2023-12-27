using System;
using System.Windows.Forms;
using System.Threading;
//using System.Windows;
//using System.Drawing;
//using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using STCEngine.Components;


namespace STCEngine.Engine
{
    /// <summary>
    /// The main class of the engine, handles most of the lower-level logic; Game class is a derivative of this class
    /// </summary>
    public abstract class EngineClass
    {
        public readonly bool testDebug = true;

        public Vector2 screenSize { get => new Vector2(window.ClientSize.Width, window.ClientSize.Height); set { RescaleUI(window, oldWindowSize, value); window.ClientSize = value; oldWindowSize = window.ClientSize; } }
        private Size oldWindowSize;
        private string title;
        private Canvas window;
        //public Thread gameLoopThread;
        private static System.Windows.Forms.Timer animationTimer = new System.Windows.Forms.Timer();
        private static System.Windows.Forms.Timer gameLoopTimer = new System.Windows.Forms.Timer();
        public static bool paused;
        private bool changingScene { get => _changingScene; set { if (loadingSceneText.InvokeRequired) { loadingSceneText.Invoke(new Action(() => loadingSceneText.Visible = value)); Debug.Log("Showing text"); } else { loadingSceneText.Visible = value; } _changingScene = value; } }//has to be like this because of threading
        private bool _changingScene;

        public static InventoryItemSlots playerInventoryUI = new InventoryItemSlots(), otherInventoryUI = new InventoryItemSlots();
        public Panel playerInventoryPanel = new Panel(), otherInventoryPanel = new Panel();
        public static Panel NPCDialoguePanel = new Panel(); public static UITextBox NPCDialogueText; public static UITextBox NPCDialogueName;
        public static Button NPCDialogueResponse1; public static Button NPCDialogueResponse2; public static Button NPCDialogueResponse3;
        public static Panel PausePanel = new Panel();
        public static UITextBox loadingSceneText = new UITextBox();
        private Button resumeButton, quitButton, saveButton, loadButton;

        public static Dictionary<string, GameObject> registeredGameObjects { get; private set; } = new Dictionary<string, GameObject>();
        public static List<GameObject> spritesToRender { get; private set; } = new List<GameObject>();
        public static List<GameObject> UISpritesToRender { get; private set; } = new List<GameObject>();

        public static List<Collider> debugRectangles { get; private set; } = new List<Collider>();
        public static List<Animation> runningAnimations { get; private set; } = new List<Animation>();
        private static List<Animation> animationsToStop = new List<Animation>(); private static List<Animation> animationsToRun = new List<Animation>();

        public static List<Collider> registeredColliders { get; private set; } = new List<Collider>();

        public static List<Collider> registeredEnemyHitboxes { get; private set; } = new List<Collider>();
        public static List<Collider> registeredEnemyHurtboxes { get; private set; } = new List<Collider>();
        //make sure to clear list in ClearScene when adding a new static list 

        public Color backgroundColor;
        public static readonly Bitmap emptyImage = new Bitmap(1, 1);

        public Vector2 cameraPosition = Vector2.zero;

        /// <summary>
        /// Starts the game loop and the game window
        /// </summary>
        public EngineClass(Vector2 screenSize, string title)
        {
            window = new Canvas();
            this.oldWindowSize = screenSize;
            this.screenSize = screenSize;
            this.title = title;

            //Rendering
            //window.Size = new Size((int)this.screenSize.x, (int)this.screenSize.y); already done by changing screen size above
            window.Text = this.title;
            window.Paint += Renderer;
            window.SizeChanged += new EventHandler((object? o, EventArgs e) => { this.screenSize = new Vector2(window.ClientSize.Width, window.ClientSize.Height); });
            

            //Animations
            animationTimer.Tick += new EventHandler(RunAnimations);
            animationTimer.Interval = 10;
            animationTimer.Start();

            //Input
            window.KeyDown += Window_KeyDown;
            window.KeyUp += Window_KeyUp;
            window.MouseClick += Window_MouseClick;

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
        public abstract void Pause();
        public abstract void Unpause();

        #region UI initialization, scaling
        /// <summary>
        /// Recursively rescales all the UI upon changing the window size
        /// </summary>
        /// <param name="oldSize"></param>
        /// <param name="newSize"></param>
        private void RescaleUI(Control control, Size oldSize, Size newSize)
        {
            if(((float)newSize.Width / (float)oldSize.Width == 1) && ((float)newSize.Height / (float)oldSize.Height == 1)) { return; }
            foreach(Control childControl in control.Controls)
            {
                int width = newSize.Width - oldSize.Width;
                childControl.Left += (childControl.Left * width) / oldSize.Width;
                childControl.Width += (childControl.Width * width) / oldSize.Width;

                int height = newSize.Height - oldSize.Height;
                childControl.Top += (childControl.Top * height) / oldSize.Height;
                childControl.Height += (childControl.Height * height) / oldSize.Height;

                //special resizing for the inventory table...
                if (childControl.GetType() == typeof(InventoryItemSlots))
                {
                    var invSlots = childControl as InventoryItemSlots;
                    for (int i = 0; i < invSlots.RowCount; i++)
                    {
                        (invSlots.Rows[i] as DataGridViewRow).Height += (invSlots.Rows[i].Height * height) / oldSize.Height;
                    }
                    for (int j = 0; j < invSlots.ColumnCount; j++)
                    {
                        (invSlots.Columns[j] as DataGridViewColumn).Width += (invSlots.Columns[j].Width * width) / oldSize.Width;
                    }
                }
                
                if(childControl.Controls.Count > 0) //recursively rescale all UI
                {
                    RescaleUI(childControl, oldSize, newSize);
                }
            }
        }
        /// <summary>
        /// Creates the pause screen Quit and Resume buttons, must be called ONCE upon loading to use the pause screen
        /// </summary>
        public void InitializePauseScreenButtonsUI()
        {
            PausePanel.BackColor = Color.FromArgb(100, 255, 255, 255);
            PausePanel.BackgroundImageLayout = ImageLayout.Zoom;
            System.Reflection.PropertyInfo aProp = typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            aProp.SetValue(PausePanel, true, null);
            PausePanel.BackgroundImage = Image.FromFile("Assets/Engine Resources/PauseScreenOverlayBG.png");
            PausePanel.Dock = DockStyle.Fill;

            quitButton = new Button();
            quitButton.BackColor = Color.White;
            quitButton.ForeColor = Color.Black;
            quitButton.Text = "Quit";
            quitButton.Font = new Font(quitButton.Font.FontFamily, 30);
            quitButton.Size = new Size((int)screenSize.x / 7, (int)screenSize.y / 10);
            quitButton.Location = new Point((int)screenSize.x / 2 - (int)screenSize.x / 10 - quitButton.Size.Width / 2, (int)screenSize.y / 3 * 2 - (int)screenSize.y / 8);
            quitButton.MouseEnter += new EventHandler((object? o, EventArgs e) => quitButton.BackColor = Color.SteelBlue);
            quitButton.MouseLeave += new EventHandler((object? o, EventArgs e) => quitButton.BackColor = Color.White);
            quitButton.Click += new EventHandler((object? o, EventArgs e) => Application.Exit());
            PausePanel.Controls.Add(quitButton);

            resumeButton = new Button();
            resumeButton.BackColor = Color.White;
            resumeButton.ForeColor = Color.Black;
            resumeButton.Text = "Resume";
            resumeButton.Font = new Font(resumeButton.Font.FontFamily, 30);
            resumeButton.Size = new Size((int)screenSize.x / 7, (int)screenSize.y / 10);
            resumeButton.Location = new Point((int)screenSize.x / 2 + (int)screenSize.x / 10 - resumeButton.Size.Width / 2, (int)screenSize.y / 3 * 2- (int)screenSize.y / 8);
            resumeButton.MouseEnter += new EventHandler((object? o, EventArgs e) => resumeButton.BackColor = Color.SteelBlue);
            resumeButton.MouseLeave += new EventHandler((object? o, EventArgs e) => resumeButton.BackColor = Color.White);
            resumeButton.Click += new EventHandler((object? o, EventArgs e) => { Unpause(); window.Focus(); });
            PausePanel.Controls.Add(resumeButton);

            loadButton = new Button()
            {
                BackColor = Color.White,
                ForeColor = Color.Black,
                Text = "Load Save",
                Font = new Font(resumeButton.Font.FontFamily, 30),
                Size = new Size((int)screenSize.x / 7, (int)screenSize.y / 10),
                Location = new Point((int)screenSize.x / 2 + (int)screenSize.x / 10 - resumeButton.Size.Width / 2, (int)screenSize.y / 3 * 2)
            };
            loadButton.MouseEnter += new EventHandler((object? o, EventArgs e) => loadButton.BackColor = Color.SteelBlue);
            loadButton.MouseLeave += new EventHandler((object? o, EventArgs e) => loadButton.BackColor = Color.White);
            loadButton.Click += new EventHandler((object? o, EventArgs e) =>
            { //source: https://stackoverflow.com/questions/17762037/current-thread-must-be-set-to-single-thread-apartment-sta-error-in-copy-stri
                Thread thread = new Thread(() => LoadSaveFile());
                thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
                thread.Start();
                thread.Join(); //Wait for the thread to end
            }); 
            PausePanel.Controls.Add(loadButton);

            saveButton = new Button()
            {
                BackColor = Color.White,
                ForeColor = Color.Black,
                Text = "Save Game",
                Font = new Font(resumeButton.Font.FontFamily, 30),
                Size = new Size((int)screenSize.x / 7, (int)screenSize.y / 10),
                Location = new Point((int)screenSize.x / 2 - (int)screenSize.x / 10 - resumeButton.Size.Width / 2, (int)screenSize.y / 3 * 2)
            };
            saveButton.MouseEnter += new EventHandler((object? o, EventArgs e) => saveButton.BackColor = Color.SteelBlue);
            saveButton.MouseLeave += new EventHandler((object? o, EventArgs e) => saveButton.BackColor = Color.White);
            saveButton.Click += new EventHandler((object? o, EventArgs e) => 
            {
                Thread thread = new Thread(() => CreateSaveFile());
                thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
                thread.Start();
                thread.Join(); //Wait for the thread to end
            });
            PausePanel.Controls.Add(saveButton);

            loadingSceneText = new UITextBox()
            {
                Font = new Font(resumeButton.Font.FontFamily, 100),

                Text = "Loading scene, please wait...",
                TextColor = Color.Black,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Location = new Point((int)screenSize.x / 4, (int)screenSize.y / 4),
                Size = new Size((int)screenSize.x/2, (int)screenSize.y/2)
            };

            window.Controls.Add(loadingSceneText);
            window.Controls.Add(PausePanel);

            loadingSceneText.Visible = false;
            PausePanel.Visible = false;

        }

        /// <summary>
        /// Creates the inventory UI, must be called ONCE upon loading to use the inventory system
        /// </summary>
        public void InitializeInventoriesUI()
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
            playerInventoryUI.AllowUserToAddRows = false; playerInventoryUI.AllowUserToDeleteRows = false; playerInventoryUI.AllowUserToOrderColumns = false; playerInventoryUI.AllowUserToResizeRows = false; playerInventoryUI.AllowUserToResizeColumns = false;

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

            for (int i = 0; i < playerInventoryUI.Rows.Count; i++)
            {
                for (int j = 0; j < playerInventoryUI.Rows[i].Cells.Count; j++)
                {
                    playerInventoryUI.Rows[i].Cells[j].Value = emptyImage;
                    playerInventoryUI.Rows[i].Cells[j].ToolTipText = "";
                }
            }
            
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

            otherInventoryPanel.Visible = false;
            playerInventoryPanel.Visible = false;

            //otherInventoryPanel.GotFocus += new EventHandler((object? o, EventArgs e) => window.Focus()); //neni treba, viz inventory slot - SetStyle(ControlStyles.Selectable, false); :)
            //playerInventoryPanel.GotFocus += new EventHandler((object? o, EventArgs e) => window.Focus());
            //playerInventoryUI.GotFocus += new EventHandler((object? o, EventArgs e) => window.Focus());
            //otherInventoryUI.GotFocus += new EventHandler((object? o, EventArgs e) => window.Focus());
            //for (int i = 0; i < inventory.inventorySlots.Count; i++)
            //{
            //    window.Controls.Add(inventory.inventorySlots[i]);
            //}

        }

        /// <summary>
        /// Creates the NPC dialogue UI, must be called ONCE upon loading to use the NPC dialogue system
        /// </summary>
        public void InitializeNPCUI()
        {
            NPCDialoguePanel.ForeColor = Color.Azure;
            NPCDialoguePanel.BackColor = Color.Azure;
            //NPCDialoguePanel.Dock = DockStyle.Bottom;
            NPCDialoguePanel.Top = (int)(screenSize.y * 2 / 3);
            NPCDialoguePanel.Height = (int)(screenSize.y / 3);
            NPCDialoguePanel.Width = (int)screenSize.x;
            System.Reflection.PropertyInfo aProp = typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            aProp.SetValue(NPCDialoguePanel, true, null);

            // Main dialogue text box
            NPCDialogueText = new UITextBox
            {
                TextColor = Color.Black,
                Font = new Font("Arial", 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Size = new Size((int)(screenSize.x * 4 / 5), (int)(screenSize.y * 4 / 15)),
                Location = new Point(0, (int)(NPCDialoguePanel.Height / 5)),
            };

            NPCDialogueText.Text =
                "Ralof: Hey, you. You're finally awake. You were trying to cross the border, right? Walked right into that Imperial ambush, same as us, and that thief over there. " +
                "Lokir: Damn you Stormcloaks. Skyrim was fine until you came along. Empire was nice and lazy. If they hadn't been looking for you, I could've stolen that horse and been half way to Hammerfell. " +
                    "You there. You and me -- we should be here. It's these Stormcloaks the Empire wants. " +
                "Ralof: We're all brothers and sisters in binds now, thief. " +
                "Imperial Soldier: Shut up back there! " +
                "Lokir: And what's wrong with him? " +
                "Ralof: Watch your tongue! You're speaking to Ulfric Stormcloak, the true High King. " +
                "Lokir: Ulfric? The Jarl of Windhelm? You're the leader of the rebellion. But if they captured you... Oh gods, where are they taking us? " +
                "Ralof: I don't know where we're going, but Sovngarde awaits. Lokir: No, this can't be happening. This isn't happening. Ralof: Hey, what village are you from, horse thief? " +
                "Lokir: Why do you care? " +
                "Ralof: A Nord's last thoughts should be of home. " +
                "Lokir: Rorikstead. I'm...I'm from Rorikstead. " +
                "Imperial Soldier: General Tullius, sir! The headsman is waiting! " +
                "General Tullius: Good. Let's get this over with. " +
                "Lokir: Shor, Mara, Dibella, Kynareth, Akatosh. Divines, please help me. Ralof: Look at him, General Tullius the Military Governor. And it looks like the Thalmor are with him. Damn elves. " +
                    "I bet they had something to do with this. This is Helgen. I used to be sweet on a girl from here. Wonder if Vilod is still making that mead with juniper berries mixed in. " +
                    "Funny...when I was a boy, Imperial walls and towers used to make me feel so safe. " +
                "Haming: Who are they, daddy? Where are they going? Torolf: You need to go inside, little cub. " +
                "Haming: Why? I want to watch the soldiers. " +
                "Torolf: Inside the house. Now. " +
                "Imperial Soldier: Whoa. " +
                "Lokir: Why are they stopping? " +
                "Ralof: Why do you think? End of the line."; //:>


            //NPC name text box
            NPCDialogueName = new UITextBox
            {
                TextColor = Color.Black,
                Font = new Font("Arial", 35),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Size = new Size((int)(screenSize.x * 4 / 5), (int)(screenSize.y / 15)),
                Location = new Point(0, 0),
            };
            NPCDialogueName.Text = "Blender21";

            // Response buttons
            NPCDialogueResponse1 = new Button
            {
                Text = "Option 1",
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Size = new Size((int)(screenSize.x / 5), (int)(NPCDialoguePanel.Height / 3)),
                Location = new Point((int)(screenSize.x / 10), 0),
                Left = (int)(NPCDialoguePanel.Width * 4 / 5),
                BackColor = Color.Aqua,
                ForeColor = Color.Black
            };
            NPCDialogueResponse2 = new Button
            {
                Text = "Option 1",
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Size = new Size((int)(screenSize.x / 5), (int)(NPCDialoguePanel.Height / 3)),
                Location = new Point((int)(screenSize.x / 10), (int)(NPCDialoguePanel.Height / 3)),
                Left = (int)(NPCDialoguePanel.Width * 4 / 5),
                BackColor = Color.Aqua,
                ForeColor = Color.Black
            };
            NPCDialogueResponse3 = new Button
            {
                Text = "Option 1",
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Size = new Size((int)(screenSize.x / 5), (int)(NPCDialoguePanel.Height / 3)),
                Location = new Point((int)(screenSize.x / 10), (int)(NPCDialoguePanel.Height / 1.5f)),
                Left = (int)(NPCDialoguePanel.Width * 4 / 5),
                BackColor = Color.Aqua,
                ForeColor = Color.Black
            };

            // Event handlers for response buttons
            NPCDialogueResponse1.Click += new EventHandler((sender, e) => NPCResponseCallback.Invoke(0));
            NPCDialogueResponse2.Click += new EventHandler((sender, e) => NPCResponseCallback.Invoke(1));
            NPCDialogueResponse3.Click += new EventHandler((sender, e) => NPCResponseCallback.Invoke(2));
            NPCDialogueResponse1.GotFocus += new EventHandler((sender, e) => window.Focus());
            NPCDialogueResponse2.GotFocus += new EventHandler((sender, e) => window.Focus());
            NPCDialogueResponse3.GotFocus += new EventHandler((sender, e) => window.Focus());

            // Add controls to the form
            NPCDialoguePanel.Controls.Add(NPCDialogueResponse1);
            NPCDialoguePanel.Controls.Add(NPCDialogueResponse2);
            NPCDialoguePanel.Controls.Add(NPCDialogueResponse3);
            NPCDialoguePanel.Controls.Add(NPCDialogueName);
            NPCDialoguePanel.Controls.Add(NPCDialogueText);
            window.Controls.Add(NPCDialoguePanel);
            NPCDialoguePanel.GotFocus += new EventHandler((sender, e) => window.Focus());

            NPCDialogueResponse1.Visible = false;
            NPCDialogueResponse2.Visible = false;
            NPCDialogueResponse3.Visible = false;
            NPCDialoguePanel.Visible = false;

            window.Focus();
            Debug.LogInfo("Finished setting up NPC dialogue UI");

        }
        public delegate void NPCResponseCallbacks(int index);
        public static NPCResponseCallbacks NPCResponseCallback;
        #endregion

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
            if (changingScene) { return; }
            try
            {
                Graphics graphics = args.Graphics;
                
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                graphics.Clear(backgroundColor);
                //graphics.TranslateTransform(cameraPosition.x - screenSize.x / 2, cameraPosition.y - screenSize.y / 2); //(asi) dela to stejne co cameraRenderingOffset, jen jsem do toho dal spatne hodnoty :p
                Vector2 cameraRenderingOffset = -cameraPosition + screenSize / 2 - Vector2.up * 50;

                foreach (GameObject gameObject in spritesToRender)
                {
                    if (gameObject == null) { continue; } //kdo vi proc se to stava, ale tohle zabrani crashi ;)
                    Sprite? sprite = gameObject.GetComponent<Sprite>();
                    if (sprite != null && sprite.enabled && gameObject.isActive)
                    {
                        var relativeDistance = gameObject.transform.position - cameraPosition;
                        if (!((Math.Abs(relativeDistance.x) < (sprite.image.Width * gameObject.transform.size.x + screenSize.x) / 2) && (Math.Abs(relativeDistance.y) < (sprite.image.Height * gameObject.transform.size.y + screenSize.y) / 2))) { continue; } //skips images out of the players screen
                        //foreach(Sprite sprite in gameObject.GetComponents<Sprite>()) //pro vice spritu na jednom objektu by to teoreticky fungovat mohlo, ale pak by nesel odstranit specificky sprite
                        //{
                        graphics.DrawImage(sprite.image, 
                            gameObject.transform.position.x - sprite.image.Width * gameObject.transform.size.x / 2 + cameraRenderingOffset.x,
                            gameObject.transform.position.y - sprite.image.Height * gameObject.transform.size.y / 2 + cameraRenderingOffset.y,
                            sprite.image.Width * gameObject.transform.size.x,
                            sprite.image.Height * gameObject.transform.size.y);
                        //}
                    }
                    Tilemap? tilemap = gameObject.GetComponent<Tilemap>();
                    if (tilemap != null && tilemap.enabled && gameObject.isActive)
                    {
                        //Bitmap newImage = tilemap.tileMapImage as Bitmap;
                        //Bitmap bitmap = new Bitmap((int)screenSize.x, (int)screenSize.y);
                        //using (Graphics g = Graphics.FromImage(bitmap))
                        //{
                        //    Debug.Log($"{(int)(gameObject.transform.position.x)} - {(screenSize.x / 2)}, {(int)(gameObject.transform.position.y)} - {(screenSize.y / 2)}, {(int)screenSize.x}, {(int)screenSize.y}");
                        //    Rectangle section = new Rectangle((int)(gameObject.transform.position.x - screenSize.x / 2), (int)(gameObject.transform.position.y - screenSize.y / 2), (int)screenSize.x, (int)screenSize.y);
                        //    //g.DrawImage(tilemap.tileMapImage, 0, 0, section, GraphicsUnit.Pixel);
                        //    //graphics.DrawImage(tilemap.tileMapImage, -screenSize.x / 2, -screenSize.y / 2, section, GraphicsUnit.Pixel);
                        //}

                        //Rectangle screenRectangle = new Rectangle(cameraPosition - screenSize / 2 + cameraRenderingOffset, screenSize);
                        //Debug.Log(cameraRenderingOffset +  cameraPosition);
                        //Rectangle worldSpaceScreenRectangle = new Rectangle(cameraPosition - screenSize/2, screenSize);
                        //x...world pozice stredu obrazovky, y...velikost obrazovky 
                        //graphics.DrawRectangle(new Pen(Color.Orange, 10), new Rectangle(tilemap.GetActiveTileMapImage(cameraPosition, screenSize) + cameraRenderingOffset, Vector2.one * 10));
                        //graphics.DrawRectangle(new Pen(Color.Blue, 10), worldSpaceScreenRectangle);
                        //tilemap.GetActiveTileMapImage(cameraPosition, screenSize);
                        //graphics.DrawImage(bitmap, 0, 0);

                        graphics.DrawImage(tilemap.tileMapImage,
                            gameObject.transform.position.x - tilemap.tileMapImage.Width * gameObject.transform.size.x / 2 + cameraRenderingOffset.x,
                            gameObject.transform.position.y - tilemap.tileMapImage.Height * gameObject.transform.size.y / 2 + cameraRenderingOffset.y,
                            tilemap.tileMapImage.Width * gameObject.transform.size.x,
                            tilemap.tileMapImage.Height * gameObject.transform.size.y);

                        //graphics.DrawImageUnscaled(tilemap.tileMapImage,
                        //    (int)(gameObject.transform.position.x - tilemap.tileMapImage.Width * gameObject.transform.size.x / 2 + cameraRenderingOffset.x),
                        //    (int)(gameObject.transform.position.y - tilemap.tileMapImage.Height * gameObject.transform.size.y / 2 + cameraRenderingOffset.y),
                        //    (int)(tilemap.tileMapImage.Width * gameObject.transform.size.x),
                        //    (int)(tilemap.tileMapImage.Height * gameObject.transform.size.y));



                        //for (int y = 0; y < tilemap.mapSize.y; y++)
                        //{
                        //    for (int x = 0; x < tilemap.mapSize.x; x++)
                        //    {
                        //        graphics.DrawImage(tilemap.tiles[x, y], gameObject.transform.position.x - tilemap.tileSize.x/2 + x*, gameObject.transform.position.y - tilemap.tileSize.y / 2, tilemap.tileMapImage.Width, tilemap.tileMapImage.Height);//(tilemap.tiles[x, y], gameObject.transform.position.x + x * tilemap.tileSize.x, gameObject.transform.position.y + y * tilemap.tileSize.y, tilemap.tileSize.x, tilemap.tileSize.x);
                        //    }
                        //}
                        //Vector2 generalOffset = gameObject.transform.position - new Vector2(tilemap.tileSize.x / 2 * gameObject.transform.size.x, tilemap.tileSize.y / 2 * gameObject.transform.size.y) + cameraRenderingOffset - new Vector2(tilemap.tileSize.x * tilemap.mapSize.x, tilemap.tileSize.y * tilemap.mapSize.y)/2;
                        //for (int i = 0; i < tilemap.mapSize.x; i++)
                        //{
                        //    for (int j = 0; j < tilemap.mapSize.y; j++)
                        //    {
                        //        Vector2 tilePosition = new Vector2(generalOffset.x + i * tilemap.tileSize.x, generalOffset.y + j * tilemap.tileSize.y);
                        //        var relativeDistance = tilePosition - cameraPosition;
                        //        if (!((Math.Abs(relativeDistance.x) < (tilemap.tileSize.x + screenSize.x) / 2) && (Math.Abs(relativeDistance.y) < (tilemap.tileSize.y + screenSize.y) / 2))) { continue; } //skips images out of the players screen
                        //        graphics.DrawImage(tilemap.tiles[i, j], generalOffset.x + i * tilemap.tileSize.x, generalOffset.y + j * tilemap.tileSize.y);
                        //    }
                        //}
                    }
                }
                foreach (GameObject gameObject in UISpritesToRender) //DOESNT USE CAMERA RENDERING OFFSET! (overlay)
                {
                    if (gameObject == null) { continue; }
                    UISprite? UISprite = gameObject.GetComponent<UISprite>();
                    if (UISprite != null && UISprite.enabled && gameObject.isActive)
                    {
                        //foreach(Sprite sprite in gameObject.GetComponents<Sprite>()) //pro vice spritu na jednom objektu by to teoreticky fungovat mohlo, ale pak by nesel odstranit specificky sprite
                        //{
                        //Debug.Log($"graphics.DrawImage({gameObject.transform.position.x - UISprite.image.Width * gameObject.transform.size.x / 2 + UISprite.offset.x + UISprite.screenAnchorOffset.x}, {gameObject.transform.position.y - UISprite.image.Height * gameObject.transform.size.y / 2 + UISprite.offset.y + UISprite.screenAnchorOffset.y}, {UISprite.image.Width * gameObject.transform.size.x}, {UISprite.image.Height * gameObject.transform.size.y}");
                        graphics.DrawImage(UISprite.image, gameObject.transform.position.x - UISprite.image.Width * gameObject.transform.size.x / 2 + UISprite.offset.x + UISprite.screenAnchorOffset.x, gameObject.transform.position.y - UISprite.image.Height * gameObject.transform.size.y / 2 + UISprite.offset.y + UISprite.screenAnchorOffset.y, UISprite.image.Width * gameObject.transform.size.x, UISprite.image.Height * gameObject.transform.size.y);
                        //}
                    }
                }
                //DEBUG ---------
                if (testDebug)
                {
                    foreach (Collider col in debugRectangles)
                    {
                        if (col == null) { continue; }
                        if (!col.enabled) { continue; }
                        if (col.GetType() == typeof(BoxCollider))
                        {
                            BoxCollider box = col as BoxCollider;
                            if (box == null) { Debug.LogError("ERROR LOADING COLLIDER DEBUG RECTANGLE"); continue; }
                            graphics.DrawRectangle(new Pen(box.isTrigger ? Color.Cyan : Color.Orange, 2), box.gameObject.transform.position.x + box.offset.x - box.size.x / 2 + cameraRenderingOffset.x, box.gameObject.transform.position.y + box.offset.y - box.size.y / 2 + cameraRenderingOffset.y, box.size.x, box.size.y);
                        }
                        else if (col.GetType() == typeof(CircleCollider))
                        {
                            CircleCollider circle = col as CircleCollider;
                            if (circle == null) { Debug.LogError("ERROR LOADING COLLIDER DEBUG RECTANGLE"); continue; }
                            //Debug.Log($"Drawing circle at ({circle.gameObject.transform.position.x + circle.offset.x - circle.radius}, {circle.gameObject.transform.position.y + circle.offset.y - circle.radius}) with size {circle.radius * 2}");
                            graphics.DrawEllipse(new Pen(circle.isTrigger ? Color.Cyan : Color.Orange, 2), circle.gameObject.transform.position.x + circle.offset.x - circle.radius + cameraRenderingOffset.x, circle.gameObject.transform.position.y + circle.offset.y - circle.radius + cameraRenderingOffset.y, circle.radius * 2, circle.radius * 2);
                        }


                    }
                    foreach (KeyValuePair<string, GameObject> gameObject in registeredGameObjects)
                    {
                        //graphics.DrawRectangle(new Pen(Color.Black), gameObject.Value.transform.position.x, gameObject.Value.transform.position.y, 2, 2);
                        graphics.DrawString(gameObject.Value.name, new Font("Arial", 11, FontStyle.Regular), new SolidBrush(Color.Black), gameObject.Value.transform.position.x - gameObject.Value.name.Length * 4f + cameraRenderingOffset.x, gameObject.Value.transform.position.y - 5.5f + cameraRenderingOffset.y);
                    }
                }
                //graphics.TranslateTransform(cameraPosition.x + screenSize.x/2, cameraPosition.y + screenSize.y/2);
                //Debug.Log(cameraPosition);
            }
            catch (Exception e) { Debug.LogError("Error running renderer, error message: " + e.Message); }
        }
        /// <summary>
        /// Calls the RunAnimation function in all currently playing Animator components
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void RunAnimations(object? sender, EventArgs eventArgs)
        {
            if (changingScene) { return; }
            if (paused) { return; }
            if(animationsToRun.Count > 0) { runningAnimations.AddRange(animationsToRun); animationsToRun.Clear(); }
            if (animationsToStop.Count > 0) { foreach (Animation a in animationsToStop) { runningAnimations.Remove(a); } animationsToStop.Clear(); } //neexistuje equivalent list.AddRange pro odstaneni... :D
            try
            {
                foreach (Animation anim in runningAnimations)
                {
                    anim.RunAnimation();
                }
            }
            catch (Exception e) { Debug.LogError("Error running animations, error message: " + e.Message); }
        }
        #endregion

        #region JSON help functions
        /// <summary>
        /// Creates a save file to be loaded later using the Load Game button
        /// </summary>
        public void CreateSaveFile()
        {

            using(SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                Stream myStream;

                saveFileDialog.Filter = "jsonSave files|*.jsonSave";
                saveFileDialog.InitialDirectory = "Assets/Saves";
                saveFileDialog.Title = "Select the save to overwrite / create new one";
                saveFileDialog.DefaultExt = ".jsonFile";
                saveFileDialog.FileName = "Main Save";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    if ((myStream = saveFileDialog.OpenFile()) != null)
                    {
                        Debug.LogInfo($"Creating save file \"{saveFileDialog.FileName}\"");
                        string jsonSaveString = "";
                        foreach(GameObject gameObject in registeredGameObjects.Values)
                        {
                            jsonSaveString += gameObject.SerializeGameObject() + "\n|||||\n";
                        }
                        // Code to write the stream goes here.
                        using (StreamWriter writer = new StreamWriter(myStream))
                        {
                            writer.Write(jsonSaveString);
                        }

                        myStream.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Shows the dialogue box to select a file to load, upon selection loads it
        /// </summary>
        public async void LoadSaveFile()
        {
            
            //source: Dotnet documentation - https://learn.microsoft.com/cs-cz/dotnet/api/system.windows.forms.openfiledialog?view=windowsdesktop-8.0
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "jsonSave files|*.jsonSave";
                openFileDialog.InitialDirectory = "Assets/Saves";
                openFileDialog.Title = "Select the save load";
                openFileDialog.DefaultExt = ".jsonFile";
                openFileDialog.FileName = "Main Save";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    Debug.LogInfo($"Loading save file \"{openFileDialog.FileName}\"");
                    //Get the path of specified file
                    var filePath = openFileDialog.FileName;

                    //Read the contents of the file into a stream
                    var fileStream = openFileDialog.OpenFile();

                    string saveString;
                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        saveString = reader.ReadToEnd();
                    }
                    
                    string[] splitSaveString = saveString.Split("|||||");
                    
                    
                    try
                    {
                        await Task.Run(() => ClearScene());
                        //await ClearScene();
                        
                        changingScene = true;
                        for (int i = 0; i < splitSaveString.Length - 1; i++) //the last entry is empty
                        {
                            GameObject.CreateGameObjectFromJSONString(splitSaveString[i]);
                        }
                        
                        while (registeredGameObjects.Count < splitSaveString.Length - 1) //wait until loaded
                        {
                            await Task.Delay(25);
                        }

                    }catch(Exception e)
                    {
                        Debug.LogError("Error loading save file! Error message: " + e.Message);
                        Debug.LogError("Loading base level instead...");
                        await Task.Run(() => ClearScene());
                        await Task.Run(() => LoadLevel("Assets/Level"));
                    }
                    changingScene = false; 
                    OnLoad(false);
                }
            }
        }

        /// <summary>
        /// Clears the scene from all GameObjects -> Make sure you clear your local variables in Game.OnLoad!
        /// </summary>
        public async Task ClearScene()
        {

            changingScene = true;
            while (registeredGameObjects.Count + spritesToRender.Count + UISpritesToRender.Count + debugRectangles.Count + runningAnimations.Count + registeredColliders.Count + registeredEnemyHitboxes.Count 
                + registeredEnemyHurtboxes.Count + animationsToStop.Count + animationsToRun.Count > 0) //wait until loaded
            {
                await Task.Delay(5);
                foreach(GameObject g in registeredGameObjects.Values) { g.DestroySelf(); }
                //Debug.Log(registeredGameObjects.Count +", "+ spritesToRender.Count + ", " + UISpritesToRender.Count + ", " + debugRectangles.Count + ", " + runningAnimations.Count + ", " + registeredColliders.Count + ", " + registeredEnemyHitboxes.Count
                //+ ", " + registeredEnemyHurtboxes.Count + ", " + animationsToStop.Count + ", " + animationsToRun.Count);
                registeredGameObjects.Clear();
                spritesToRender.Clear();
                UISpritesToRender.Clear();
                debugRectangles.Clear();
                registeredColliders.Clear();
                registeredEnemyHitboxes.Clear();
                registeredEnemyHurtboxes.Clear();
                runningAnimations.Clear();
                animationsToRun.Clear();
                animationsToStop.Clear();
                Debug.Log("Attempting to clear scene...");
            }
            Debug.LogInfo("Scene Cleared");
            changingScene = false;


        }
        /// <summary>
        /// Loads all the json GameObject files in the given directory
        /// </summary>
        /// <param name="directory">Path to the directory with all the GameObjects to load</param>
        public async Task LoadLevel(string directory)
        {
            changingScene = true;
            DirectoryInfo dir = new DirectoryInfo(directory);
            var userFiles = dir.GetFiles("*.json"); var engineFiles = new DirectoryInfo("Assets/Engine Resources").GetFiles("*.json");
            foreach (FileInfo file in userFiles)
            {
                GameObject.CreateGameObjectFromJSONFile(file.FullName);
            }
            foreach (FileInfo engineFile in engineFiles) 
            {
                GameObject.CreateGameObjectFromJSONFile(engineFile.FullName);
            }
            while (registeredGameObjects.Count < userFiles.Length + engineFiles.Length) //wait until loaded
            {
                await Task.Delay(25);
            }
            Debug.Log($"Level {directory} Succesfully loaded");
            changingScene = false;
            
        }

        #endregion

        #region Game logic classes
        /// <summary>
        /// Called every frame before updating graphics
        /// </summary>
        public abstract void Update();
        /// <summary>
        /// Called every frame after updating graphics
        /// </summary>
        public abstract void LateUpdate();
        /// <summary>
        /// Called upon loading before running the first Update
        /// </summary>
        /// <param name="initializeUIs">Whether the UIs should be initialized</param>
        public abstract void OnLoad(bool initializeUIs = true);
        /// <summary>
        /// Called upon exiting the application
        /// </summary>
        public abstract void OnExit();
        private void Window_KeyDown(object? sender, KeyEventArgs e) { GetKeyDown(e); }
        /// <summary>
        /// Called when the window registers a key press input
        /// </summary>
        /// <param name="eventArgs"></param>
        public abstract void GetKeyDown(KeyEventArgs eventArgs);
        private void Window_MouseClick(object? sender, MouseEventArgs e) { GetMouseClick(e); }
        /// <summary>
        /// Called when the window registers a mouse click
        /// </summary>
        /// <param name="eventArgs"></param>
        public abstract void GetMouseClick(MouseEventArgs eventArgs);
        private void Window_KeyUp(object? sender, KeyEventArgs e) { GetKeyUp(e); }
        /// <summary>
        /// Called when the window registers a key release input
        /// </summary>
        /// <param name="eventArgs"></param>
        public abstract void GetKeyUp(KeyEventArgs eventArgs);
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
        public static void ChangeUISpriteRenderOrder(GameObject GameObject, int order) { UISpritesToRender.Remove(GameObject); UISpritesToRender.Insert(order > UISpritesToRender.Count ? UISpritesToRender.Count - 1 : order, GameObject); }

        //Debug boxes list
        public static void AddDebugRectangle(Collider Collider, int order = int.MaxValue) { if (order != int.MaxValue && order < debugRectangles.Count) { debugRectangles.Insert(order, Collider); return; } debugRectangles.Add(Collider); }
        public static void RemoveDebugRectangle(Collider Collider) { debugRectangles.Remove(Collider); }

        /// <summary>
        /// Registers the Animation to the animation queue
        /// </summary>
        public static void AddSpriteAnimation(Animation Animation) { animationsToRun.Add(Animation); }
        /// <summary>
        /// Unregisters the Animation from the animation queue
        /// </summary>
        public static void RemoveSpriteAnimation(Animation Animation) { animationsToStop.Add(Animation); }
        #endregion
        #region Colliders
        /// <summary>
        /// Registers the collider to the list of colliders participating in collisions
        /// </summary>
        /// <param name="Collider"></param>
        public static void RegisterCollider(Collider Collider) { registeredColliders.Add(Collider); }
        /// <summary>
        /// Unregisters the collider to the list of colliders participating in collisions
        /// </summary>
        /// <param name="Collider"></param>
        public static void UnregisterCollider(Collider Collider) { registeredColliders.Remove(Collider); }
        /// <summary>
        /// Registers the collider to the list of colliders participating in combat-related player-damaging collisions
        /// </summary>
        /// <param name="Collider"></param>
        public static void RegisterEnemyHurtbox(Collider HurtboxCollider) { registeredEnemyHurtboxes.Add(HurtboxCollider); }
        /// <summary>
        /// Unregisters the collider to the list of colliders participating in combat-related player-damaging collisions
        /// </summary>
        /// <param name="Collider"></param>
        public static void UnregisterEnemyHurtbox(Collider HurtboxCollider) { registeredEnemyHurtboxes.Remove(HurtboxCollider); }
        /// <summary>
        /// Registers the collider to the list of colliders participating in combat-related damage-recieving collision
        /// </summary>
        /// <param name="Collider"></param>
        public static void RegisterEnemyHitbox(Collider HitboxCollider) { registeredEnemyHitboxes.Add(HitboxCollider); }
        /// <summary>
        /// Unregisters the collider to the list of colliders participating in combat-related damage-recieving collision
        /// </summary>
        /// <param name="Collider"></param>
        public static void UnregisterEnemyHitbox(Collider HitboxCollider) { registeredEnemyHitboxes.Remove(HitboxCollider); }
        #endregion
        #endregion

        /// <summary>
        /// Destroys the given GameObject
        /// </summary>
        /// <param name="GameObject">The GameObject to be destroyed</param>
        public static void Destroy(GameObject GameObject) { GameObject.DestroySelf(); UnregisterGameObject(GameObject); }
        /// <summary>
        /// Destroys the given Component
        /// </summary>
        /// <param name="Component">The Component to be destroyed</param>
        public static void Destroy(Component Component) { Component.DestroySelf(); }
    }

    /// <summary>
    /// Just a DoubleBuffered form
    /// </summary>
    public class Canvas : Form
    {
        public Canvas()
        {
            this.DoubleBuffered = true;
        }
    }
    /// <summary>
    /// A forms DataGridView used for inventory slots with transparent background and an image to show the icon of the item
    /// </summary>
    public class InventoryItemSlots : DataGridView
    {
        private bool alreadyTransparent;
        private Image backgroundImage;
        protected override void PaintBackground(Graphics graphics, Rectangle clipBounds, Rectangle gridBounds)
        {
            base.PaintBackground(graphics, clipBounds, gridBounds);

            //painting background image
            for (int i = 0; i < ColumnCount; i++) //(int)(Parent.Width / backgroundImage.Width)
            {
                for (int j = 0; j < RowCount; j++) //(int)(Parent.Height / backgroundImage.Height)
                {
                    graphics.DrawImage(backgroundImage, i * Parent.Width / ColumnCount, j * Parent.Height / RowCount, Parent.Width / ColumnCount, Parent.Height / RowCount);
                }
            }

            //making the original background transparent
            if (!alreadyTransparent) { SetCellsTransparent(); alreadyTransparent = true; }
        }
        public InventoryItemSlots()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.Selectable, false);
            backgroundImage = Image.FromFile("Assets/Engine Resources/Inventory-Background.png");
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

    /// <summary>
    /// A forms control with a text attached
    /// </summary>
    public class UITextBox : Control
    {
        public string Text { get; set; }
        public Font Font { get; set; } = new Font("Arial", 11, FontStyle.Regular);
        public Color TextColor { get; set; }
        public UITextBox()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.Selectable, false);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            //base.OnPaint(e);

            //Source: ChatGPT + dokumentace :)
            if (!string.IsNullOrEmpty(this.Text))
            {
                // Create a rectangle that defines the area for text drawing
                Rectangle textRectangle = new Rectangle(0, 0, this.Width, this.Height);

                // Draw the text within the specified rectangle with word wrapping
                e.Graphics.DrawString(this.Text, this.Font, new SolidBrush(TextColor), textRectangle);
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
        [JsonIgnore] public float magnitude { get => MathF.Sqrt(x * x + y * y); }
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
        public static implicit operator Size(Vector2 v) => new Size((int)v.x, (int)v.y);
        public static implicit operator Point(Vector2 v) => new Point((int)v.x, (int)v.y);
    }

}
