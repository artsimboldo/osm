using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace SharpOSM
{
    public abstract class Shape
    {
        public string Id { get; internal set; }
        public Color ColorCode { get; set; }
        public abstract Mesh Mesh { get; }

        public Shape(string id, Color color)
        {
            Id = id;
            ColorCode = color;
        }
    }
}
