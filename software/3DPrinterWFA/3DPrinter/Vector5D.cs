using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlUSB
{
    [Serializable()]
    public struct Vector5D
    {
        public decimal X, Y, Z, E, F;

        public Vector5D(decimal _X, decimal _Y, decimal _Z, decimal _E, decimal _F)
        {
            this.X = _X;
            this.Y = _Y;
            this.Z = _Z;
            this.E = _E;
            this.F = _F;
        }

       

        public static Vector5D operator +(Vector5D v1, Vector5D v2)
        {
            return new Vector5D(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z, v1.E + v2.E, v1.F + v2.F);
        }
        
        public static Vector5D operator -(Vector5D v1, Vector5D v2)
        {
            return new Vector5D(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z, v1.E - v2.E, v1.F - v2.F);
        }
        
        public static Vector5D operator *(Vector5D v1, Vector5D v2)
        {
            return new Vector5D(v1.X * v2.X, v1.Y * v2.Y, v1.Z * v2.Z, v1.E * v2.E, v1.F * v2.F);
        }

        /*public static Vector5D Abs(Vector5D v)
       {
           v.X = Math.Abs(v.X);
           v.Y = Math.Abs(v.Y);
           v.Z = Math.Abs(v.Z);
           v.E = Math.Abs(v.E);
           v.F = Math.Abs(v.F);
           return v;
       }*/

        public static Vector5D Abs(Vector5D v)
        {
            return new Vector5D(Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z), Math.Abs(v.E), Math.Abs(v.F));
        }

        public override string ToString()
        {
            return String.Format("<{0}, {1}, {2}, {3}, {4}>", this.X, this.Y, this.Z, this.E, this.F);
        }
    }
}
