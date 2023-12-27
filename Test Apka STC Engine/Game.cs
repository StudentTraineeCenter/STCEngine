using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STCEngine.Engine;
using System.Text.Json;
using STCEngine.Components;

namespace STCEngine.Game
{
    /// <summary>
    /// Class responsible for handling all the game logic
    /// </summary>
    class Game : EngineClass
    {
        public static Game MainGameInstance;
        public bool twoInventoriesOpen { get => otherInventory != null; }
        public Inventory? otherInventory;
        public bool npcDialogueOpen { get => activeNPC != null; }
        public NPC? activeNPC;

        private static readonly Vector2 windowSize = new Vector2(2560, 1440);

        //watch out when creating references to objects, make sure they get cleared/overwritten in OnLoad method!
        public GameObject? player;
        public CombatStats playerStats;
        private Animator? playerAnim;
        public Sprite playerSprite;
        public Inventory? playerInventory;
        public BoxCollider? playerCol;
        public BoxCollider? playerTopCol, playerBotCol, playerLeftCol, playerRightCol; //hitboxes used for wall collision detection
        public BoxCollider? playerAttackHurtbox;

        public List<GameObject> enemiesToMove = new List<GameObject>();
        public List<GameObject> idleEnemies = new List<GameObject>();
        public GameObject? pauseScreen;
        public GameObject? pressEGameObject;

        private float horizontalInput, verticalInput;
        private GameObject? interactingGameObject, highlightedGameObject;

        //private Inventory testInventory;

        /// <summary>
        /// Starts the game
        /// </summary>
        public Game() : base(windowSize, "Hraaa :)") { }


        public override async void OnLoad(bool initializeUIs = true)
        {
            //:)
            Debug.LogInfo("OnLoad started");
            if (MainGameInstance == null) //first time loading
            {
                backgroundColor = Color.FromArgb(120, 120, 120);//Color.Black;//Color.FromArgb(139, 195, 74);
                MainGameInstance = this;
                await LoadLevel("Assets/Level");
            }

            if (initializeUIs)
            {
                InitializePauseScreenButtonsUI();
                InitializeNPCUI();
                InitializeInventoriesUI();
            }

            player = GameObject.Find("Player");
            cameraPosition = player.transform.position;
            playerAnim = player.GetComponent<Animator>();
            if(playerInventory != null) { playerInventoryUI.CellClick -= playerInventory.ItemClicked; } 
            playerInventory = player.GetComponent<Inventory>(); playerInventoryUI.CellClick += playerInventory.ItemClicked; //MUST BE HERE!
            playerSprite = player.GetComponent<Sprite>();
            playerStats = player.GetComponent<CombatStats>();
            var playerColliders = player.GetComponents<BoxCollider>();
            foreach (BoxCollider boxCol in playerColliders)
            {
                switch (boxCol.tag)
                {
                    case "player":
                        playerCol = boxCol;
                        break;
                    case "playerWalkUp":
                        playerTopCol = boxCol;
                        break;
                    case "playerWalkDown":
                        playerBotCol = boxCol;
                        break;
                    case "playerWalkRight":
                        playerRightCol = boxCol;
                        break;
                    case "playerWalkLeft":
                        playerLeftCol = boxCol;
                        break;
                    case "playerAttackHurtbox":
                        playerAttackHurtbox = boxCol;
                        break;
                }

            }

            //configure wall detection for the player (so that you don't have to do it manually in json...)
            playerTopCol.size = new Vector2(playerCol.size.x, playerStats.movementSpeed); playerTopCol.offset = Vector2.up * (playerCol.size.y / 2 + playerStats.movementSpeed / 2 + 1);
            playerBotCol.offset = -playerTopCol.offset; playerBotCol.size = playerTopCol.size;
            playerRightCol.size = new Vector2(playerStats.movementSpeed, playerCol.size.y); playerRightCol.offset = Vector2.right * (playerCol.size.x / 2 + playerStats.movementSpeed / 2 + 1);
            playerLeftCol.offset = -playerRightCol.offset; playerLeftCol.size = playerRightCol.size;

            idleEnemies.Clear(); enemiesToMove.Clear();
            idleEnemies.AddRange(GameObject.FindAll("Enemy"));

            pauseScreen = GameObject.Find("Pause Screen");
            pressEGameObject = GameObject.Find("Press E GameObject");

            //playerStats.SerializeToJSON("Assets");

            #region Old scene setup (inactive)
            //player = new GameObject("Player", new Transform(new Vector2(100, 100), 0, new Vector2(0.6f, 0.6f)));
            //playerInventory = player.AddComponent(new Inventory(true));

            //quitButton.Click += new EventHandler(QuitButton);
            //resumeButton.Click += new EventHandler(ResumeButton);


            //#region Player component setup
            ////spawn player
            //player.AddComponent(new Sprite("Assets/Basic Enemy White 1.png"));
            //playerCol = player.AddComponent(new BoxCollider(Vector2.one * 100, "player", Vector2.zero, false, true)) as BoxCollider;

            //var hitboxWidth = 1f; //values lower than 1 might cause walking into walls
            //playerTopCol = player.AddComponent(new BoxCollider(Vector2.up * movementSpeed * hitboxWidth + Vector2.right * 100, "playerWalk", Vector2.up * (51 + movementSpeed / 2 * hitboxWidth), true, true)) as BoxCollider;
            //playerBotCol = player.AddComponent(new BoxCollider(Vector2.up * movementSpeed * hitboxWidth + Vector2.right * 100, "playerWalk", -Vector2.up * (51 + movementSpeed / 2 * hitboxWidth), true, true)) as BoxCollider;
            //playerRightCol = player.AddComponent(new BoxCollider(Vector2.right * movementSpeed * hitboxWidth + Vector2.up * 100, "playerWalk", Vector2.right * (51 + movementSpeed / 2 * hitboxWidth), true, true)) as BoxCollider;
            //playerLeftCol = player.AddComponent(new BoxCollider(Vector2.right * movementSpeed * hitboxWidth + Vector2.up * 100, "playerWalk", -Vector2.right * (51 + movementSpeed / 2 * hitboxWidth), true, true)) as BoxCollider;

            //AnimationFrame[] animFrames = {
            //    new AnimationFrame("Assets/Basic Enemy White 1.png", 100),
            //    new AnimationFrame("Assets/Basic Enemy White 2.png", 100),
            //    new AnimationFrame("Assets/Basic Enemy White 3.png", 100)
            //};

            //Animation anim = new Animation("TestAnimation", animFrames, true);
            //playerAnim = player.AddComponent(new Animator(anim)) as Animator;

            //#endregion

            ////tilemap = new GameObject("Tilemap", new Vector2(0, 0));
            ////tilemap.AddComponent(new Tilemap("Assets/Level/Tilemap.json"));
            ////tilemap.transform.position = new Vector2(tilemap.GetComponent<Tilemap>().tileMapImage.Width / 2, tilemap.GetComponent<Tilemap>().tileMapImage.Height / 2);

            //pressEGameObject = new GameObject("Press E GameObject", new Transform(Vector2.zero, 0, Vector2.one * 1.5f), false);
            //pressEGameObject.AddComponent(new Sprite("Assets/PressEImage.png"));

            //string fileName = "Assets/Level/playerZed.json";
            //testGameObject2 = GameObject.CreateGameObjectFromJSON(fileName);
            //testGameObject2.RemoveComponent<BoxCollider>();
            //testGameObject2.AddComponent(new CircleCollider(50, "a", Vector2.zero, false, true));
            //testGameObject2.GetComponent<Animator>().Play("TestAnimation2");
            //testGameObject2.AddComponent(new Inventory());

            //testGameObject3 = GameObject.CreateGameObjectFromJSON("Assets/Level/Chest1.json");

            //#region test npc setup
            //testGameObject4 = new GameObject("Test Dvere", new List<Component> { new Transform(new Vector2(300, 200), 0, Vector2.one), new BoxCollider(Vector2.one * 50, "wall", Vector2.zero, false, true), new ToggleCollider(), new Sprite("Assets/Basic Wall.png") });
            //Dialogue startingTestDialogue = new Dialogue("start", new DialoguePart[] { new DialoguePart("Ahoj!", 3000), new DialoguePart("Jak se dneska máš?", 5000) }, new Response[] { new Response("dobre", "Dobře :)"), new Response("blbe", "Blbě :(") });
            //List<Dialogue> testDialogues = new List<Dialogue>
            //{
            //    new Dialogue("dobre", new DialoguePart[] { new DialoguePart("Ah, to rád slyším!", 3000), new DialoguePart("Jen tak dál :)", 3000)}),
            //    new Dialogue("blbe", new DialoguePart[] { new DialoguePart("Ah, to nerad slyším...", 3000), new DialoguePart("Co se stalo?", 3000)}, new Response[]{new Response("nic", "Nic..."), new Response("kocka", "Umřela mi kočka..."), new Response("spageti", "Moc špagety kódu...")}),
            //    new Dialogue("kocka", new DialoguePart[] { new DialoguePart("To je velmi smutné", 3000), new DialoguePart("Tak přežívej", 3000)}),
            //    new Dialogue("spageti", new DialoguePart[] { new DialoguePart("Sám si ho napsal...", 3000), new DialoguePart("Je to jen tvoje chyba ;)", 3000)}),
            //    new Dialogue("nic", new DialoguePart[] { new DialoguePart("Ok, nebudu se vyptávat", 3000), new DialoguePart("Snad se to brzy vyřeší", 3000)})
            //};
            //NPC testNPC = new NPC("Test npc", startingTestDialogue, testDialogues);

            //testGameObject5 = new GameObject("Test NPC", new List<Component> { new Transform(new Vector2(400, 250), 0, Vector2.one), new BoxCollider(Vector2.one * 50, "wall", Vector2.zero, false, true), testNPC, new Sprite("Assets/Basic Wall.png") });
            //#endregion

            //pauseScreen = new GameObject("Pause Screen", new Transform(Vector2.zero, 0, Vector2.one));
            //pauseScreen.AddComponent(new UISprite("Assets/PauseScreenOverlayBG.png", UISprite.ScreenAnchor.MiddleCentre));
            //pauseScreen.transform.size = new Vector2(windowSize.x / pauseScreen.GetComponent<UISprite>().image.Width, windowSize.y / pauseScreen.GetComponent<UISprite>().image.Height);
            //pauseScreen.isActive = false;

            //playerInventory.AddItem(new ItemInInventory[] { new ItemInInventory("mec", 1, "Assets/Items/Item-Test_Sword.png"), new ItemInInventory("Slimeball", 5, "Assets/Items/Slimeball.png") });

            //var randomDroppedItem = new GameObject("dropped item", new Transform(Vector2.one * 200, 0, Vector2.one));
            //randomDroppedItem.AddComponent(new Sprite("Assets/Items/Slimeball.png"));
            //randomDroppedItem.AddComponent(new DroppedItem(new ItemInInventory("Slimeball", 3, "Assets/Items/Slimeball.png")));

            //pressEGameObject.GetComponent<Sprite>().orderInLayer = int.MaxValue;
            #endregion

            Debug.LogInfo("\nOnLoad Complete\n");
        }

        public override void OnExit()
        {
            Debug.LogInfo("\nApplication Quit");
        }

        public override void Update()
        {
            #region Player movement logic
            if ((horizontalInput != 0 || verticalInput != 0) && playerAnim.currentlyPlayingAnimation?.name != "AttackAnimation") //moves the player according to user input
            {
                MovePlayer(); 
            }
            else if(playerAnim.currentlyPlayingAnimation?.name != "AttackAnimation" && playerAnim.currentlyPlayingAnimation?.name != "IdleAnimation") //if the player isnt moving or attacking, play idle animation
            {
                playerAnim.Play("IdleAnimation");
            }
            #endregion

            #region Interaction logic
            //collecting dropped items
            Collider droppedCol; if (playerCol.IsColliding("droppedItem", true, out droppedCol)) { droppedCol.gameObject.GetComponent<DroppedItem>().CollectItem(); }
            Collider? interactCol = null; float closestDistance = float.MaxValue;
            foreach (Collider col in playerCol.OverlapCollider(true))
            {
                if (col.tag == "Interactible")
                {
                    if ((col.gameObject.transform.position - player.transform.position).magnitude < closestDistance)
                    {
                        closestDistance = (col.gameObject.transform.position - player.transform.position).magnitude;
                        interactCol = col;
                    }
                }
            }

            if (interactCol != null)
            {
                if (interactCol.gameObject.name != (highlightedGameObject != null ? highlightedGameObject.name : "")) //prisel bliz k jinemu interactible objektu
                {
                    if (interactingGameObject != null) { interactingGameObject.components.OfType<IInteractibleGameObject>().FirstOrDefault().StopInteract(); interactingGameObject = null; } //prestane interagovat stary
                                                                                                                                                                                             //if(highlightedGameObject != null) { highlightedGameObject.components.OfType<IInteractibleGameObject>().FirstOrDefault().StopHighlight(); } //prestane highlightovat stary - neni treba, jen vypne pressEGameObject a pak ho zase zapne ;)

                    interactCol.gameObject.components.OfType<IInteractibleGameObject>().FirstOrDefault().Highlight(); //highlightne novy
                    highlightedGameObject = interactCol.gameObject;
                }
            }
            else if (highlightedGameObject != null) //odesel od interactible objektu
            {
                if (interactingGameObject != null)
                {
                    interactingGameObject.components.OfType<IInteractibleGameObject>().FirstOrDefault().StopInteract(); //prestane interagovat
                }
                highlightedGameObject.components.OfType<IInteractibleGameObject>().FirstOrDefault().StopHighlight();
                highlightedGameObject = null;
                interactingGameObject = null;
            }
            #endregion

            #region Combat logic

            //moving enemies
            MoveEnemies();

            //hit detection
            if (playerCol.IsColliding("EnemyHurtbox", true, out Collider? enemyHurtbox, registeredEnemyHurtboxes)) //player hit by an enemy
            {
                if (playerStats.TakeDamage(enemyHurtbox.gameObject.GetComponent<CombatStats>().damage)) { PlayerDeath(); }//deals damage and checks whether the player died
                //change health bar
            }
            if (playerAttackHurtbox.enabled) //--------------------------------------------------------------- player hitting an enemy
            {
                if (playerAttackHurtbox.IsColliding("EnemyHitbox", true, out Collider? enemyHitbox, registeredEnemyHitboxes))
                {
                    CombatStats enemyStats = enemyHitbox.gameObject.GetComponent<CombatStats>();

                    if (enemyStats.TakeDamage(playerStats.damage)) { NPCDeath(enemyStats); } //deals damage and checks whether the entity died
                }
            }


            #endregion
        }

        /// <summary>
        /// Called every frame after graphics update
        /// </summary>
        public override void LateUpdate()
        {

        }

        #region Movement functions
        public void MovePlayer()
        {
            if (horizontalInput != 0 && ((playerSprite.flipX == true ? -1 : 1) != MathF.Sign(horizontalInput))) { playerSprite.flipX = horizontalInput > 0 ? false : true; } //flips the sprite according to where the player is running

            var modifiedMovementInput = new Vector2(horizontalInput, verticalInput);

            //prevents the player from walking into walls
            if (horizontalInput > 0) { if (playerRightCol.OverlapCollider().Length > 0) { modifiedMovementInput.x = 0; } }
            else if (horizontalInput < 0) { if (playerLeftCol.OverlapCollider().Length > 0) { modifiedMovementInput.x = 0; } }
            if (verticalInput > 0) { if (playerTopCol.OverlapCollider().Length > 0) { modifiedMovementInput.y = 0; } }
            else if (verticalInput < 0) { if (playerBotCol.OverlapCollider().Length > 0) { modifiedMovementInput.y = 0; } }

            player.transform.position += modifiedMovementInput.normalized * playerStats.movementSpeed; //moves the player
            cameraPosition = player.transform.position; //moves the camera to follow the player

            if (playerAnim.currentlyPlayingAnimation?.name != "RunAnimation") { playerAnim.Play("RunAnimation"); } //plays the run animation
        }

        public void MoveEnemies()
        {
            var enemiesToWake = new List<GameObject>();
            foreach(GameObject idleEnemy in idleEnemies)
            {
                if((idleEnemy.transform.position - player.transform.position).magnitude < idleEnemy.GetComponent<CombatStats>().agroRange) { enemiesToWake.Add(idleEnemy); }
            }
            enemiesToWake.ForEach(enemy => { enemiesToMove.Add(enemy); idleEnemies.Remove(enemy); enemy.GetComponent<Animator>().Play("MoveAnimation"); });

            var enemiesToSleep = new List<GameObject>();
            foreach (GameObject enemy in enemiesToMove)
            {
                var stats = enemy.GetComponent<CombatStats>();
                //if ((enemy.transform.position - player.transform.position).magnitude && !stats.agroed) { continue; } //only moves enemies that are agroed
                if((enemy.transform.position - player.transform.position).magnitude > stats.deagroRange) { enemiesToSleep.Add(enemy); continue; }
                enemy.transform.position += (player.transform.position - enemy.transform.position).normalized * enemy.GetComponent<CombatStats>().movementSpeed;
            }
            enemiesToSleep.ForEach(enemy => { enemiesToMove.Remove(enemy); idleEnemies.Add(enemy); enemy.GetComponent<Animator>().Play("IdleAnimation"); });
        }
        #endregion

        #region Combat help functions
        public async void PlayerDeath()
        {
            Debug.LogInfo("\n----------------------------------------------------------------------");
            Debug.LogInfo("\n THE PLAYER HAS DIED, RELOADING STARTING SCENE IN 1 SECOND");
            Debug.LogInfo("\n----------------------------------------------------------------------\n");
            await ClearScene();
            await LoadLevel("Assets/Level");
            OnLoad(false);
        }
        public void NPCDeath(CombatStats enemyStats)
        {
            Debug.LogInfo($"Enemy NPC {enemyStats.gameObject.name} has died");
            enemiesToMove.Remove(enemyStats.gameObject);
            enemyStats.gameObject.DestroySelf();
        }
        public void PlayerAttack()
        {
            playerAnim.Play("AttackAnimation");
            playerAttackHurtbox.offset.x = (playerSprite.flipX ? -1 : 1) * (playerCol.size.x / 2 + playerAttackHurtbox.size.x / 2);
            playerAttackHurtbox.enabled = true;
            Task.Delay(playerAnim.animations.TryGetValue("AttackAnimation", out Animation anim) ? anim.duration+10 : throw new Exception("Error getting attack animation duration")).ContinueWith(t => playerAttackHurtbox.enabled = false);
        }
        #endregion

        #region Interact help functions
        /// <summary>
        /// Called when E is pressed when near an interactible GameObject
        /// </summary>
        public void InteractButtonPressed()
        {
            Collider interactCol;
            if (highlightedGameObject != null) { highlightedGameObject.components.OfType<IInteractibleGameObject>().FirstOrDefault().Interact(); interactingGameObject = (interactingGameObject == null ? highlightedGameObject : null); }
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
            inventory.gameObject.GetComponent<Sprite>().image = Image.FromFile("Assets/ChestOpened.png");
            otherInventory.ShowInventory();
            otherInventoryPanel.Visible = true;
        }
        public void CloseOtherInventory()
        {
            try //nekdy se to rozbije 
            {
                otherInventoryUI.CellClick -= new DataGridViewCellEventHandler(otherInventory.ItemClicked);
            }
            catch { }
            otherInventory.gameObject.GetComponent<Sprite>().image = Image.FromFile("Assets/ChestClosed.png");
            otherInventory = null;
            otherInventoryPanel.Visible = false;
        }
        #endregion

        private bool up = false, down = false, left = false, right = false;
        public override void GetKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.I) //inventory
            {
                if (playerInventoryPanel.Visible) { ClosePlayerInventory(); if (otherInventoryPanel.Visible) { CloseOtherInventory(); } }
                else { OpenPlayerInventory(); }

            }
            if (e.KeyCode == Keys.Escape) //pause
            {
                if (paused) { Unpause(); }
                else { Pause(); }
            }
            if (e.KeyCode == Keys.E) { InteractButtonPressed(); }

            //movement inputs
            if (e.KeyCode == Keys.A || e.KeyCode == Keys.Left)
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
        public override void GetMouseClick(MouseEventArgs eventArgs)
        {
            //Debug.Log($"{eventArgs.Button}, {eventArgs.Location}, {eventArgs.Clicks}, {eventArgs.X}, {eventArgs.Y}" );
            if(eventArgs.Button == MouseButtons.Left && !playerAttackHurtbox.enabled)
            {
                PlayerAttack();
            }
        }
        /// <summary>
        /// Pauses the game
        /// </summary>
        public override void Pause()
        {
            if (playerInventoryPanel.Visible) { ClosePlayerInventory(); }
            if (otherInventoryPanel.Visible) { CloseOtherInventory(); }
            //if (npcDialogueOpen) { activeNPC.EndDialogue(); } //rozbije to, ale asi to nejak bude treba implementovat
            paused = true;
            //pauseScreen.isActive = true;
            PausePanel.Visible = true;

        }
        /// <summary>
        /// Unpauses the game
        /// </summary>
        public override void Unpause()
        {
            paused = false;
            //pauseScreen.isActive = false;
            PausePanel.Visible = false;
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
