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
        public GameObject? player;
        public GameObject? tilemap;
        private Animator? playerAnim;
        private float movementSpeed = 10;

        private float horizontalInput, verticalInput;

        //starts the game
        public Game() : base(new Vector2(512, 512), "Hraaa :)") { } 

        /// <summary>
        /// Called upon starting the game
        /// </summary>
        public override void OnLoad() 
        {
            Debug.LogInfo("Game started");
            backgroundColor = Color.Black;

            //spawn player
            player = new GameObject("Player",new Transform(new Vector2(10, 10), 0, new Vector2(0.6f, 0.6f)));
            player.AddComponent(new Sprite("Assets/Basic Enemy White 1.png"));

            AnimationFrame[] animFrames = {
                new AnimationFrame(Image.FromFile("Assets/Basic Enemy White 1.png"), 100),
                new AnimationFrame(Image.FromFile("Assets/Basic Enemy White 2.png"), 100),
                new AnimationFrame(Image.FromFile("Assets/Basic Enemy White 3.png"), 100)
            };
            Animation anim = new Animation("TestAnimation", animFrames);
            playerAnim = player.AddComponent(new Animator(anim)) as Animator;

            tilemap = new GameObject("Tilemap", new Vector2(50, 100));
            tilemap.AddComponent(new Tilemap("Assets/Tilemap.json"));
        }

        /// <summary>
        /// Called upon exiting the game
        /// </summary>
        public override void OnExit()
        {
            gameLoopThread.Interrupt();
            Debug.LogInfo("Application Quit");
        }

        /// <summary>
        /// Called every frame before graphics update
        /// </summary>
        public override void Update()
        {
            if (horizontalInput != 0 || verticalInput != 0)
            {
                player.transform.position += new Vector2(horizontalInput, verticalInput).normalized * movementSpeed;

                if (!playerAnim.isPlaying) { playerAnim.Play("TestAnimation"); }
            }
            else if (playerAnim.isPlaying)
            {
                playerAnim.Stop();
            }
        }

        /// <summary>
        /// Called every frame after graphics update
        /// </summary>
        public override void LateUpdate()
        {
            
        }

        private bool up = false, down = false, left = false, right = false;
        /// <summary>
        /// Called the frame a key is pressed down
        /// </summary>
        /// <param name="e"></param>
        public override void GetKeyDown(KeyEventArgs e)
        {
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
