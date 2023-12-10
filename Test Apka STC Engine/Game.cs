using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STCEngine.Engine;
using System.Text.Json;

namespace STCEngine.Game
{
    class Game : EngineClass
    {
        public static Game MainGameInstance;
        public bool twoInventoriesOpen { get => otherInventory != null; }
        public Inventory? otherInventory;

        private static Vector2 windowSize = new Vector2(1920, 1080);
        public GameObject? player;
        public GameObject? tilemap;
        public GameObject? pauseScreen;
        private Animator? playerAnim;
        public Inventory? playerInventory;
        private float movementSpeed = 10;
        public BoxCollider playerCol;
        public BoxCollider playerTopCol, playerBotCol, playerLeftCol, playerRightCol; //hitboxes used for wall collision detection

        private float horizontalInput, verticalInput;

        private GameObject testKamenaStena, testGameObject2;
        private Inventory testInventory;
        //starts the game
        public Game() : base(windowSize, "Hraaa :)") {  }

        /// <summary>
        /// Quits the application, called upon clicking the quit button when the game is paused
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void QuitButton(object? sender, EventArgs e)
        {
            Application.Exit();
        }
        /// <summary>
        /// Resumes the game, called upon clicking the resume button when the game is paused
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResumeButton(object? sender, EventArgs e)
        {
            Unpause();
        }

        /// <summary>
        /// Called upon starting the game
        /// </summary>
        public override void OnLoad() 
        {
            MainGameInstance = this;
            Debug.LogInfo("Game started");
            backgroundColor = Color.Black;

            CreatePauseScreenButtons();
            quitButton.Click += new EventHandler(QuitButton);
            resumeButton.Click += new EventHandler(ResumeButton);


            #region Player component setup
            //spawn player
            player = new GameObject("Player",new Transform(new Vector2(100, 100), 0, new Vector2(0.6f, 0.6f)));
            player.AddComponent(new Sprite("Assets/Basic Enemy White 1.png"));
            playerCol = player.AddComponent(new BoxCollider(Vector2.one * 100, "player", Vector2.zero, false, true)) as BoxCollider;

            var hitboxWidth = 1f; //values lower than 1 might cause walking into walls
            playerTopCol = player.AddComponent(new BoxCollider(Vector2.up * movementSpeed *hitboxWidth+ Vector2.right * 100, "playerWalk", Vector2.up * (51 + movementSpeed / 2 * hitboxWidth), true, true)) as BoxCollider;
            playerBotCol = player.AddComponent(new BoxCollider(Vector2.up * movementSpeed * hitboxWidth + Vector2.right * 100, "playerWalk", -Vector2.up * (51 + movementSpeed / 2 * hitboxWidth), true, true)) as BoxCollider;
            playerRightCol = player.AddComponent(new BoxCollider(Vector2.right * movementSpeed * hitboxWidth + Vector2.up * 100, "playerWalk", Vector2.right * (51 + movementSpeed / 2 * hitboxWidth), true, true)) as BoxCollider;
            playerLeftCol = player.AddComponent(new BoxCollider(Vector2.right * movementSpeed * hitboxWidth + Vector2.up * 100, "playerWalk", -Vector2.right * (51 + movementSpeed / 2 * hitboxWidth), true, true)) as BoxCollider;

            AnimationFrame[] animFrames = {
                new AnimationFrame("Assets/Basic Enemy White 1.png", 100),
                new AnimationFrame("Assets/Basic Enemy White 2.png", 100),
                new AnimationFrame("Assets/Basic Enemy White 3.png", 100)
            };
            
            Animation anim = new Animation("TestAnimation", animFrames, true);
            playerAnim = player.AddComponent(new Animator(anim)) as Animator;

            playerInventory = player.AddComponent(new Inventory());
            playerInventory.isPlayerInventory = true;
            #endregion

            tilemap = new GameObject("Tilemap", new Vector2(0, 0));
            tilemap.AddComponent(new Tilemap("Assets/Level/Tilemap.json"));
            tilemap.transform.position = new Vector2(tilemap.GetComponent<Tilemap>().tileMapImage.Width / 2, tilemap.GetComponent<Tilemap>().tileMapImage.Height / 2);

            string fileName = "Assets/Level/playerZed.json";
            testGameObject2 = GameObject.CreateGameObjectFromJSON(fileName);
            testGameObject2.GetComponent<Animator>().Play("TestAnimation2");

            pauseScreen = new GameObject("Pause Screen", new Transform(Vector2.zero, 0, Vector2.one));
            pauseScreen.AddComponent(new UISprite("Assets/PauseScreenOverlayBG.png", UISprite.ScreenAnchor.MiddleCentre));
            pauseScreen.transform.size = new Vector2(windowSize.x / pauseScreen.GetComponent<UISprite>().image.Width, windowSize.y / pauseScreen.GetComponent<UISprite>().image.Height);
            pauseScreen.isActive = false;

            InitializeInventories();
            otherInventoryPanel.Visible = false;
            playerInventoryPanel.Visible = false;
            playerInventory.AddItem(new ItemInInventory[] { new ItemInInventory("mec", 1, "Assets/Items/Item-Test_Sword.png"), new ItemInInventory("Slimeball", 5, "Assets/Items/Slimeball.png") });

            var randomDroppedItem = new GameObject("dropped item", new Transform(Vector2.one * 200, 0, Vector2.one));
            randomDroppedItem.AddComponent(new Sprite("Assets/Items/Slimeball.png"));
            randomDroppedItem.AddComponent(new DroppedItem(new ItemInInventory("Slimeball", 3, "Assets/Items/Slimeball.png")));

            testInventory = new Inventory();
            //testKamenaStena = new GameObject("Kamena stena", new Transform(new Vector2(700, 75), 0, Vector2.one));
            //testKamenaStena.AddComponent(new Sprite("Assets/Basic Wall.png"));
            //testKamenaStena.AddComponent(new BoxCollider(Vector2.one * 64, Vector2.zero, false, true));
            //Debug.Log(testKamenaStena.GetComponent<Sprite>().orderInLayer.ToString());
        }

        /// <summary>
        /// Called upon exiting the game
        /// </summary>
        public override void OnExit()
        {

            Debug.LogInfo("Application Quit");
        }

        /// <summary>
        /// Called every frame before graphics update
        /// </summary>
        public override void Update()
        {
            if (horizontalInput != 0 || verticalInput != 0)
            {
                var modifiedMovementInput = new Vector2(horizontalInput, verticalInput);

                //prevents the player from walking into walls
                if (horizontalInput > 0) { if (playerRightCol.OverlapCollider().Length > 0) { modifiedMovementInput.x = 0; } }
                else if (horizontalInput < 0) { if (playerLeftCol.OverlapCollider().Length > 0) { modifiedMovementInput.x = 0; } }
                if (verticalInput > 0) { if (playerTopCol.OverlapCollider().Length > 0) { modifiedMovementInput.y = 0; } }
                else if (verticalInput < 0) { if (playerBotCol.OverlapCollider().Length > 0) { modifiedMovementInput.y = 0; } }

                player.transform.position += modifiedMovementInput.normalized * movementSpeed;

                if (!playerAnim.isPlaying) { playerAnim.Play("TestAnimation"); }
            }
            else if (playerAnim.isPlaying)
            {
                playerAnim.Stop();
            }

            //collecting dropped items
            Collider col; if (playerCol.IsColliding("droppedItem", out col)) { col.gameObject.GetComponent<DroppedItem>().CollectItem(); }
        }

        /// <summary>
        /// Called every frame after graphics update
        /// </summary>
        public override void LateUpdate()
        {
            
        }

        public void Pause()
        {
            paused = true;
            pauseScreen.isActive = true;
        }
        public void Unpause()
        {
            paused = false;
            pauseScreen.isActive = false;
        }
        public void OpenPlayerInventory()
        {
            playerInventory.ShowInventory();
            playerInventoryPanel.Visible = true;
        }
        public void ClosePlayerInventory()
        {
            playerInventoryPanel.Visible = false;
        }
        public void OpenOtherInventory(Inventory inventory)
        {
            otherInventory = inventory;
            otherInventoryUI.CellClick += new DataGridViewCellEventHandler(otherInventory.ItemClicked);
            otherInventory.ShowInventory();
            otherInventoryPanel.Visible = true;
        }
        public void CloseOtherInventory()
        {
            otherInventoryUI.CellClick -= new DataGridViewCellEventHandler(otherInventory.ItemClicked);
            otherInventory = null;
            otherInventoryPanel.Visible = false;
        }

        private bool up = false, down = false, left = false, right = false;
        /// <summary>
        /// Called the frame a key is pressed down
        /// </summary>
        /// <param name="e"></param>
        public override void GetKeyDown(KeyEventArgs e)
        {
            if(e.KeyCode == Keys.I) //inventory
            {
                if (playerInventoryPanel.Visible) { ClosePlayerInventory(); }
                else { OpenPlayerInventory(); }
                if (otherInventoryPanel.Visible) { CloseOtherInventory(); }
            }
            if (e.KeyCode == Keys.O) //inventory
            {
                if (otherInventoryPanel.Visible) { CloseOtherInventory(); }
                else { OpenOtherInventory(testInventory); }
            }
            if (e.KeyCode == Keys.Escape) //pause
            {
                if (paused) { Unpause(); }
                else { Pause(); }
            }

            //movement inputs
            if(e.KeyCode == Keys.A || e.KeyCode == Keys.Left)
            {
                left = true;
            }
            if (e.KeyCode == Keys.D || e.KeyCode == Keys.Right)
            {
                right = true;
            }
            if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up)
            {
                up = true;
            }
            if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down)
            {
                down = true;
            }
            if ((left && right) || (!left && !right)) { horizontalInput = 0; }
            else { horizontalInput = left ? -1 : 1; }
            if ((up && down) || (!up && !down)) { verticalInput = 0; }
            else { verticalInput = down ? 1 : -1; }
        }


        /// <summary>
        /// Called the frame a key is released
        /// </summary>
        /// <param name="e"></param>
        public override void GetKeyUp(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A || e.KeyCode == Keys.Left)
            {
                left = false;
            }
            if (e.KeyCode == Keys.D || e.KeyCode == Keys.Right)
            {
                right = false;
            }
            if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up)
            {
                up = false;
            }
            if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down)
            {
                down = false;
            }
            if ((left && right) || (!left && !right)) { horizontalInput = 0; }
            else { horizontalInput = left ? -1 : 1; }
            if ((up && down) || (!up && !down)) { verticalInput = 0; }
            else { verticalInput = down ? 1 : -1; }
        }

    }
    

}
