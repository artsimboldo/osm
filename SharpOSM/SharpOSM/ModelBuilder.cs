using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using System.IO;
//using Assimp;
//using Assimp.Unmanaged;
using Poly2Tri;
using Poly2TriPolygon = Poly2Tri.Triangulation.Polygon;

namespace SharpOSM
{
    /// <summary>
    /// TODO:
    /// - Compute roofs
    /// </summary>
    public static class ModelBuilder
    {
        /// <summary>
        /// 
        /// </summary>
        private class PolygonPointExt : Poly2TriPolygon.PolygonPoint
        {
            public int Idx { get; private set; }
            public PolygonPointExt(double x, double y, int idx)
                : base(x, y)
            {
                Idx = idx;
            }
        }

        /// <summary>
        /// Create builidng esh
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        public static Mesh CreateMesh(BuildingPart building)
        {
            var minY = building.Body.min_height;
            var maxY = building.Body.height;
            var poly = building.Footprint;
            var total = poly.Points.Count;
            if (poly is MultiPolygon)
            {
                (poly as MultiPolygon).Holes.ForEach(x => total += x.Points.Count);
            }

            // Build mesh
            var vertices = new Vector3[total * 2];
            var faces = new List<int>();
            var colors = Enumerable.Repeat(building.Body.surface.color, total * 2).ToArray();

            // Create vertices and polygon for triangulation
            var idx = 0;
            var P2Tpoints = new List<PolygonPointExt>();
            foreach (var point in poly.Points)
            {
                vertices[idx] = new Vector3(point.X, minY, -point.Y);
                vertices[idx + total] = new Vector3(point.X, maxY, -point.Y);
                P2Tpoints.Add(new PolygonPointExt(point.X, point.Y, idx++));
            }
            var P2Tpoly = new Poly2TriPolygon.Polygon(P2Tpoints);

            // add faces = 2 triangles
            for (var i = 0; i < poly.Points.Count - 1; i++)
            {
                faces.Add(i + total);
                faces.Add(i);
                faces.Add(i + 1);
                faces.Add(i + total);
                faces.Add(i + 1);
                faces.Add(i + total + 1);
            }

            if (poly is MultiPolygon)
            {
                (poly as MultiPolygon).Holes.ForEach(hole =>
                    {
                        // Create holes vertices and holes for triangulation
                        var P2Thole = new List<PolygonPointExt>();
                        for (var i = 0; i < hole.Points.Count; i++)
                        {
                            vertices[idx + i] = new Vector3(hole.Points[i].X, minY, -hole.Points[i].Y);
                            vertices[idx + i + total] = new Vector3(hole.Points[i].X, maxY, -hole.Points[i].Y);
                            P2Thole.Add(new PolygonPointExt(hole.Points[i].X, hole.Points[i].Y, idx + i));
                        }

                        // Create holes faces = 2 triangles
                        for (var i = 0; i < hole.Points.Count - 1; i++)
                        {
                            faces.Add(idx + i + total);
                            faces.Add(idx + i);
                            faces.Add(idx + i + 1);
                            faces.Add(idx + i + total);
                            faces.Add(idx + i + 1);
                            faces.Add(idx + i + total + 1);
                        }

                        P2Tpoly.AddHole(new Poly2TriPolygon.Polygon(P2Thole));
                        idx += hole.Points.Count;
                    });
            }

            try
            {
                P2T.Triangulate(P2Tpoly);

                // Add triangulated top faces
                foreach (var tri in P2Tpoly.Triangles)
                {
                    var i0 = tri.Points[0] as PolygonPointExt;
                    var i1 = tri.Points[1] as PolygonPointExt;
                    var i2 = tri.Points[2] as PolygonPointExt;

                    // apply to bottom
                    //                    faces.Add(i2.Idx);
                    //                    faces.Add(i1.Idx);
                    //                    faces.Add(i0.Idx);

                    // apply to top
                    faces.Add(i2.Idx + total);
                    faces.Add(i1.Idx + total);
                    faces.Add(i0.Idx + total);

                    // Add roof color
                    colors[i0.Idx + total] = building.Roof.surface.color;
                    colors[i2.Idx + total] = building.Roof.surface.color;
                    colors[i1.Idx + total] = building.Roof.surface.color;
                }
                return new Mesh(vertices, faces.ToArray(), colors);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Create 3D natural
        /// </summary>
        /// <param name="natural"></param>
        /// <returns></returns>
        public static Mesh CreateMesh(Natural natural)
        {
//            return LoadModel("tree.obj");
            var x = natural.Anchor.X;
            var z = -natural.Anchor.Y;
            var h = natural.Data.height;
            var r = natural.Data.circumference * 0.5;

            var vertices = new Vector3[] { 
                new Vector3(x - r, 1, z),
                new Vector3(x, h, z),
                new Vector3(x + r, 1, z),
                new Vector3(x, 1, z - r),
                new Vector3(x, h, z),
                new Vector3(x, 1, z + r)
            };

            return new Mesh(vertices, 
                new int[] { 0, 1, 2, 3, 4, 5 }, 
                Enumerable.Repeat(Color.Green, vertices.Length).ToArray());
        }

        /// <summary>
        /// TODO: Assimp support for loading geometries
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private static Mesh LoadModel(string filename)
        {
            return null;
        }
    }
}