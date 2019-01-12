using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace SharpOSM
{

    /// <summary>
    /// Mesh class
    /// </summary>
    public class Mesh
    {
        public Vector3[] Vertices { get; set; }
        public Vector3[] Normals { get; set; }
        public int[] Faces { get; set; }
        public Color[] Colors { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        private static Vector3 ComputeFaceNormal(Vector3 p0, Vector3 p1, Vector3 p2)
        {
            return Vector3.Normalize(Vector3.Cross(p1 - p0, p2 - p0));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="faces"></param>
        /// <returns></returns>
        private static Vector3[] ComputeNormals(Vector3[] vertices, int[] faces)
        {
            if (vertices == null || faces == null)
            {
                return null;
            }

            var normals = new Vector3[vertices.Length];
            for (var idx = 0; idx < faces.Length; idx += 3)
            {
                var i = faces[idx + 2];
                var j = faces[idx + 1];
                var k = faces[idx + 0];
                var normal = ComputeFaceNormal(vertices[i], vertices[j], vertices[k]);
                normals[i] += normal;
                normals[j] += normal;
                normals[k] += normal;
            }

            for (var i = 0; i < normals.Length; i++)
            {
                normals[i] = Vector3.Normalize(normals[i]);
            }

            return normals;
        }

        /// <summary>
        /// Ctors
        /// </summary>
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
