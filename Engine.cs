using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STCEngine;

class Program
{
    static void Main(string[] args)
    {
        var a = Vector2.zero();
        Console.WriteLine(a);
    }
}
class Vector2
{
    float x { get; set; }
    float y { get; set; }
    

    public Vector2 zero()
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
        this.x = y;
        this.y = y;
    }

}