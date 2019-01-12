using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csg
{
    public class MultiPolygon : Polygon
    {
        private List<Polygon> holes;

        public List<Polygon> Holes
        {
            get
            {
                if (this.holes == null)
                {
                    this.holes = new List<Polygon>();
                }
                return this.holes;
            }
            set
            {
                this.holes = value;
            }
        }

        public MultiPolygon() 
        { }

        public MultiPolygon(List<Vector2> initial)
            : base(initial)
        { }
    }

    public class Polygon
    {
        protected List<Vector2> points;

        public List<Vector2> Points
        {
            get
            {
                if (this.points == null)
                {
                    this.points = new List<Vector2>();
                }
                return this.points;
            }
            set
            {
                this.points = value;
            }
        }

        public bool IsClosed()
        {
            return (points == null || points.Count < 3) ? false : points.First() == points.Last();
        }

        public bool Contains(Polygon other)
        {
            foreach(var p in other.Points)
            {
                if (!Contains(p))
                {
                    return false;
                }
            }
            return true;
        }

        public bool Contains(Vector2 p)
        {
            double minX = points[0].X;
            double maxX = points[0].X;
            double minY = points[0].Y;
            double maxY = points[0].Y;
            foreach(var q in points)
            {
                minX = Math.Min(q.X, minX);
                maxX = Math.Max(q.X, maxX);
                minY = Math.Min(q.Y, minY);
                maxY = Math.Max(q.Y, maxY);
            }

            if (p.X < minX || p.X > maxX || p.Y < minY || p.Y > maxY)
            {
                return false;
            }

            // http://www.ecse.rpi.edu/Homepages/wrf/Research/Short_Notes/pnpoly.html
            bool inside = false;
            for (int i = 0, j = points.Count - 1; i < points.Count; j = i++)
            {
                if ((points[i].Y > p.Y) != (points[j].Y > p.Y) &&
                     p.X < (points[j].X - points[i].X) * (p.Y - points[i].Y) / (points[j].Y - points[i].Y) + points[i].X)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        public Polygon() 
        {
        }

        public Polygon(List<Vector2> initial)
        {
            points = new List<Vector2>(initial);
        }
    }
}
