using System;

namespace Engine
{

    class Program
    {
        static void Main(string[] args)
        {
            var a = new Vector2();
            a.x = 1;
            a.y = 23;
            var b = new Vector2(454, 312);
            Console.WriteLine("a == (" + a.x + ", " + a.y + ")");
            Console.WriteLine("b == (" + b.x + ", " + b.y + ")");
        }
    }


    class Vector2
    {
        public float x { get; set; }
        public float y { get; set; }

        public static Vector2 Zero()
        {
            return new Vector2(0, 0);
        }

        public Vector2()
        {
            x = 0;
            y = 0;
        }
        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

    }

}
