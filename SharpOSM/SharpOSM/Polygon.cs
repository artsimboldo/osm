using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClipperLib;

namespace SharpOSM
{
    using Path = List<IntPoint>;
    using Paths = List<List<IntPoint>>;

    public enum PolygonOps
    {
        Difference = ClipType.ctDifference,
        Intersection = ClipType.ctIntersection,
        Union = ClipType.ctUnion, 
        Xor = ClipType.ctXor
    }

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

        /// <summary>
        /// returns true is the polygon is closed
        /// </summary>
        /// <returns></returns>
        public bool IsClosed()
        {
            return (points == null || points.Count < 3) ? false : points.First() == points.Last();
        }

        /// <summary>
        /// Clipper interface
        /// </summary>
        const double Scale = 1000000;

        private static Func<List<Vector2>, Path> ToClipper = (List<Vector2> points) =>
        {
            var newpoints = new Path();
            points.ForEach(point => newpoints.Add(new IntPoint(point.X * Scale, point.Y * Scale)));
            return newpoints;
        };

        private static Func<Path, List<Vector2>> FromClipper = (Path points) =>
        {
            var newpoints = new List<Vector2>();
            points.ForEach(point => newpoints.Add(new Vector2(Convert.ToDouble(point.X) / Scale, Convert.ToDouble(point.Y) / Scale)));
            return newpoints;
        };

        /// <summary>
        /// process the difference between this and other
        /// </summary>
        /// <param name="other"></param>
        /// <param name="type">http://www.angusj.com/delphi/clipper/documentation/Docs/Units/ClipperLib/Types/ClipType.htm</param>
        /// <returns></returns>
        public List<Vector2> Clip(Polygon other, PolygonOps op)
        {
            var type = (ClipType)op;
            var subj = new Paths(1);
            subj.Add(ToClipper(this.points));
            var clip = new Paths(1);
            clip.Add(ToClipper(other.Points));
            Paths solution = new Paths();
            Clipper clp = new Clipper();
            clp.AddPaths(subj, PolyType.ptSubject, true);
            clp.AddPaths(clip, PolyType.ptClip, true);
            clp.Execute(type, solution, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);
            return solution.Count > 0 ? FromClipper(solution[0]) : null;
        }

        /// <summary>
        /// Verify intersection of this with other
        /// </summary>
        /// <param name="other"></param>
        /// <returns> 0: no intersection; -1: this contains other; +1: this intersects other  </returns>
        public int Intersects(Polygon other)
        {
            var count = 0;
            foreach (var p in other.Points)
            {
                if (Contains(p))
                {
                    count++;
                }
            }
            return count == 0 ? 0 : count == other.Points.Count ? -1 : 1;
        }

        /// <summary>
        /// Check a point is inside  polygon
        /// Ref: http://www.ecse.rpi.edu/Homepages/wrf/Research/Short_Notes/pnpoly.html
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public bool Contains(Vector2 p)
        {
            double minX = points[0].X;
            double maxX = points[0].X;
            double minY = points[0].Y;
            double maxY = points[0].Y;
            foreach (var q in points)
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

        /// <summary>
        /// Constructors
        /// </summary>
        public Polygon() { }

        public Polygon(List<Vector2> initial)
        {
            points = new List<Vector2>(initial);
        }
    }
}
