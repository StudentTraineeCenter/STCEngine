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
            player = new GameObject("Player",new Transform(new Vector2(10, 10), 0, new Vector2(10, 10)));
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
            player.transform.position.x += 1;
        }

        /// <summary>
        /// Called every frame after graphics update
        /// </summary>
        public override void LateUpdate()
        {
            
        }
    }
}
