using System;
//using System.Windows;
//using System.Drawing;
//using System.Numerics;


namespace Test_Apka_STC_Engine.Engine
{

    class Program
    {
        static void Main(string[] args)
        {
            var a = new Vector2();
            a.x = 1;
            a.y = 23;
            var b = new Vector2(454, 312);
            Console.WriteLine("a == (" + a.x + ", " + a.y + "), length: " + a.length);
            Console.WriteLine("b == (" + b.x + ", " + b.y + ")");
        }
    }
 

    public abstract class Engine
    {
        private Vector2 screenSize;
        private string title;
        //private 
    }


    class Vector2
    {
        public float x { get; set; }
        public float y { get; set; }

        public static Vector2 zero { get => new Vector2(0, 0); }
        public static Vector2 up { get => new Vector2(0, 1); }
        public static Vector2 right { get => new Vector2(1, 0); }
        public float length { get => MathF.Sqrt(x*x + y*y); }



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
    class GameObject
    {
        public Vector2 position;
        public float rotation;
        public Vector2 size;

        //public 
        //public GameObject() { }
    }

    class Sprite : GameObject
    {

    }
}
