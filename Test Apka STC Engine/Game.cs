﻿using System;
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
        public GameObject? pressEGameObject;

        private float horizontalInput, verticalInput;
        private GameObject? interactingGameObject, highlightedGameObject;

        private GameObject testGameObject3, testGameObject2, testGameObject4;
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

            playerInventory = player.AddComponent(new Inventory(true));
            #endregion

            tilemap = new GameObject("Tilemap", new Vector2(0, 0));
            tilemap.AddComponent(new Tilemap("Assets/Level/Tilemap.json"));
            tilemap.transform.position = new Vector2(tilemap.GetComponent<Tilemap>().tileMapImage.Width / 2, tilemap.GetComponent<Tilemap>().tileMapImage.Height / 2);

            pressEGameObject = new GameObject("Press E GameObject",new Transform(Vector2.zero, 0, Vector2.one * 1.5f), false);
            pressEGameObject.AddComponent(new Sprite("Assets/PressEImage.png"));

            string fileName = "Assets/Level/playerZed.json";
            testGameObject2 = GameObject.CreateGameObjectFromJSON(fileName);
            testGameObject2.RemoveComponent<BoxCollider>();
            testGameObject2.AddComponent(new CircleCollider(50, "a", Vector2.zero, false, true));
            testGameObject2.GetComponent<Animator>().Play("TestAnimation2");
            testGameObject2.AddComponent(new Inventory());

            testGameObject3 = GameObject.CreateGameObjectFromJSON("Assets/Level/Chest1.json");

            testGameObject4 = new GameObject("Test Dvere", new List<Component> { new Transform(new Vector2(300, 200), 0, Vector2.one), new BoxCollider(Vector2.one * 50, "wall", Vector2.zero, false, true), new ToggleCollider(), new Sprite("Assets/Basic Wall.png") });

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

            pressEGameObject.GetComponent<Sprite>().orderInLayer = int.MaxValue;

            Debug.LogInfo("\nOnLoad Complete\n");
        }

        /// <summary>
        /// Called upon exiting the game
        /// </summary>
        public override void OnExit()
        {
            Debug.LogInfo("\nApplication Quit");
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
            Collider droppedCol; if (playerCol.IsColliding("droppedItem", true, out droppedCol)) { droppedCol.gameObject.GetComponent<DroppedItem>().CollectItem(); }
            Collider? interactCol = null; float closestDistance = float.MaxValue;
            foreach(Collider col in playerCol.OverlapCollider(true))
            {
                if(col.tag == "Interactible")
                {
                    if((col.gameObject.transform.position - player.transform.position).magnitude < closestDistance)
                    {
                        closestDistance = (col.gameObject.transform.position - player.transform.position).magnitude;
                        interactCol = col;
                    }
                }
            }

            if (interactCol != null) 
            { 
                if(interactCol.gameObject.name != (highlightedGameObject != null ? highlightedGameObject.name : "")) //prisel bliz k jinemu interactible objektu
                { 
                    if(interactingGameObject != null) { interactingGameObject.components.OfType<IInteractibleGameObject>().FirstOrDefault().StopInteract(); interactingGameObject = null; } //prestane interagovat stary
                    //if(highlightedGameObject != null) { highlightedGameObject.components.OfType<IInteractibleGameObject>().FirstOrDefault().StopHighlight(); } //prestane highlightovat stary - neni treba, jen vypne pressEGameObject a pak ho zase zapne ;)
                        
                    interactCol.gameObject.components.OfType<IInteractibleGameObject>().FirstOrDefault().Highlight(); //highlightne novy
                    highlightedGameObject = interactCol.gameObject;
                }
            }
            else if(highlightedGameObject != null) //odesel od interactible objektu
            {
                if (interactingGameObject != null)
                {
                    interactingGameObject.components.OfType<IInteractibleGameObject>().FirstOrDefault().StopInteract(); //prestane interagovat
                }
                highlightedGameObject.components.OfType<IInteractibleGameObject>().FirstOrDefault().StopHighlight();
                highlightedGameObject = null;
                interactingGameObject = null;
            }
        }

        /// <summary>
        /// Called every frame after graphics update
        /// </summary>
        public override void LateUpdate()
        {
            
        }

        public void InteractButtonPressed()
        {
            Collider interactCol;
            if (highlightedGameObject != null) { highlightedGameObject.components.OfType<IInteractibleGameObject>().FirstOrDefault().Interact(); interactingGameObject = (interactingGameObject == null ? highlightedGameObject : null); }
        }

        public void Pause()
        {
            if (playerInventoryPanel.Visible) { ClosePlayerInventory(); }
            if (otherInventoryPanel.Visible) { CloseOtherInventory(); }
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
                if (playerInventoryPanel.Visible) { ClosePlayerInventory(); if (otherInventoryPanel.Visible) { CloseOtherInventory(); } }
                else { OpenPlayerInventory(); }
                
            }
            //if (e.KeyCode == Keys.O) //inventory
            //{
            //    if (otherInventoryPanel.Visible) { CloseOtherInventory(); }
            //    else { OpenOtherInventory(testInventory); }
            //}
            if (e.KeyCode == Keys.Escape) //pause
            {
                if (paused) { Unpause(); }
                else { Pause(); }
            }
            if(e.KeyCode == Keys.E) { InteractButtonPressed(); }

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
