using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using STCEngine.Engine;

namespace STCEngine
{
    class Game : Engine.Engine
    {
        public Game() : base(new Vector2(512, 512), "Hraaa :)") { } 
        public override void OnLoad() 
        {
            Console.WriteLine("game started");
            backgroundColor = Color.Black;
        }
        public override void OnExit()
        {
            gameLoopThread.Interrupt();
            Console.WriteLine("game quit");
        }

        int frame = 0;
        public override void Update()
        {
            Console.WriteLine($"Update {frame}");
            frame += 1;
        }

        public override void LateUpdate()
        {
            //Console.WriteLine("LateUpdate");
        }
    }
}
