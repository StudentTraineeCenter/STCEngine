using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STCEngine.Engine;

namespace STCEngine
{
    class Game : EngineClass
    {
        public GameObject player;

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
            player.AddComponent(new Sprite("Assets/Basic Enemy White 1.png", player));

            AnimationFrame[] animFrames = {
                new AnimationFrame(Image.FromFile("Assets/Basic Enemy White 1.png"), 1000),
                new AnimationFrame(Image.FromFile("Assets/Basic Enemy White 2.png"), 2000),
                new AnimationFrame(Image.FromFile("Assets/Basic Enemy White 3.png"), 3000)
            };
            Animation anim = new Animation("TestAnimation", animFrames);
            player.AddComponent(new Animator(anim));
            player.GetComponent<Animator>().Play("TestAnimation");
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
            //player.transform.position.x += 1;
        }

        /// <summary>
        /// Called every frame after graphics update
        /// </summary>
        public override void LateUpdate()
        {
            
        }
    }
}
