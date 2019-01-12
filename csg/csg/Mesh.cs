using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Poly2Tri;
using Poly2TriPolygon = Poly2Tri.Triangulation.Polygon;
using SharpOSM.Csg;

namespace SharpOSM
{
    /// <summary>
    /// TODO:
    /// - Boolean difference
    /// </summary>
    public class Mesh
    {
        private class PolygonPointExt : Poly2TriPolygon.PolygonPoint
        {
            public int Idx { get; private set; }
            public PolygonPointExt(double x, double y, int idx)
                : base(x, y)
            {
                Idx = idx;
            }
        }

        public Vector3[] Vertices { get; set; }
        public Vector3[] Normals { get; set; }
        public int[] Faces { get; set; }
        public Color[] Colors { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mesh"></param>
        public void Substract(Mesh mesh)
        {
            var a = CSG.fromMesh(this);
            var b = CSG.fromMesh(mesh);
            var c = a.subtract(b);
            var result = c.toMesh();
            var color = Color.Red; //  Colors[0];
            Colors = Enumerable.Repeat(color, result.Vertices.Length).ToArray();
            Vertices = result.Vertices;
            Normals = result.Normals;
            Faces = result.Faces;
        }

        /// <summary>
        /// Cap mesh
        /// </summary>
        /// <param name="color"></param>
        public void Cap(Color color)
        {
            if (Vertices == null || Vertices.Length < 6)
            {
                return;
            }

            // Convert to PolygonPointExt (2D + index)
            // Convention: bottom vertices start at 0, top at 1 - continue every 2 vertices
            var points = new List<PolygonPointExt>();
            for (var idx = 0; idx < Vertices.Length; idx += 2)
            {
                points.Add(new PolygonPointExt(Vertices[idx].X, Vertices[idx].Z, idx));
            }

            var poly = new Poly2TriPolygon.Polygon(points);

            try
            {
                P2T.Triangulate(poly);

                // Get resulting faces
                var faces = new List<int>(Faces);
                foreach (var tri in poly.Triangles)
                {
                    var i0 = tri.Points[0] as PolygonPointExt;
                    var i1 = tri.Points[1] as PolygonPointExt;
                    var i2 = tri.Points[2] as PolygonPointExt;
                    // apply to bottom
                    faces.Add(i0.Idx);
                    faces.Add(i1.Idx);
                    faces.Add(i2.Idx);
                    // apply to top
                    faces.Add(i0.Idx + 1);
                    faces.Add(i2.Idx + 1);
                    faces.Add(i1.Idx + 1);
                    // Add roof color
                    Colors[i0.Idx + 1] = color;
                    Colors[i2.Idx + 1] = color;
                    Colors[i1.Idx + 1] = color;
                }

                Faces = faces.ToArray();
                Normals = ComputeNormals(Vertices, Faces);
            }
            catch (Exception ex)
            {
                // TODO
                Console.WriteLine(ex.Message);
            }
        }

        private static Vector3 ComputeFaceNormal(Vector3 p0, Vector3 p1, Vector3 p2)
        {
            return Vector3.Normalize(Vector3.Cross(p1 - p0, p2 - p0));
        }

        private static Vector3[] ComputeNormals(Vector3[] vertices, int[] faces)
        {
            if (vertices == null || faces == null)
            {
                return null;
            }

            var normals = new Vector3[vertices.Length];
            for (var idx = 0; idx < faces.Length; idx += 3)
            {
                var i = faces[idx];
                var j = faces[idx + 1];
                var k = faces[idx + 2];
                var normal = ComputeFaceNormal(vertices[i], vertices[j], vertices[k]);
                normals[i] = normals[i] == null ? normal : normals[i] + normal;
                normals[j] = normals[j] == null ? normal : normals[j] + normal;
                normals[k] = normals[k] == null ? normal : normals[k] + normal;
            }

            for (var i = 0; i < normals.Length; i++)
            {
                normals[i] = Vector3.Normalize(normals[i]);
            }

            return normals;
        }

        public Mesh()
        { }

        public Mesh(Vector3[] vertices, int[] faces)
        {
            Vertices = vertices;
            Faces = faces;
            Normals = ComputeNormals(Vertices, Faces);
        }

        public Mesh(Vector3[] vertices, int[] faces, Color[] colors)
        {
            Vertices = vertices;
            Faces = faces;
            Colors = colors;
            Normals = ComputeNormals(Vertices, Faces);
        }

    }
}
