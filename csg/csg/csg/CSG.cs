using System;
using System.Collections;
using System.Collections.Generic;

// Constructive Solid Geometry (CSG) is a modeling technique that uses Boolean
// operations like union and intersection to combine 3D solids. This library
// implements CSG operations on meshes elegantly and concisely using BSP trees,
// and is meant to serve as an easily understandable implementation of the
// algorithm. All edge cases involving overlapping coplanar polygons in both
// solids are correctly handled.
// 
// Example usage:
// 
//     var cube = CSG.cube();
//     var sphere = CSG.sphere({ radius: 1.3 });
//     var polygons = cube.subtract(sphere).toPolygons();
// 
// ## Implementation Details
// 
// All CSG operations are implemented in terms of two functions, `clipTo()` and
// `invert()`, which remove parts of a BSP tree inside another BSP tree and swap
// solid and empty space, respectively. To find the union of `a` and `b`, we
// want to remove everything in `a` inside `b` and everything in `b` inside `a`,
// then combine polygons from `a` and `b` into one solid:
// 
//     a.clipTo(b);
//     b.clipTo(a);
//     a.build(b.allPolygons());
// 
// The only tricky part is handling overlapping coplanar polygons in both trees.
// The code above keeps both copies, but we need to keep them in one tree and
// remove them in the other tree. To remove them from `b` we can clip the
// inverse of `b` against `a`. The code for union now looks like this:
// 
//     a.clipTo(b);
//     b.clipTo(a);
//     b.invert();
//     b.clipTo(a);
//     b.invert();
//     a.build(b.allPolygons());
// 
// Subtraction and intersection naturally follow from set operations. If
// union is `A | B`, subtraction is `A - B = ~(~A | B)` and intersection is
// `A & B = ~(~A | ~B)` where `~` is the complement operator.
// 
// ## License
// 
// Original ActionScript version copyright (c) 2011 Evan Wallace (http://madebyevan.com/), under the MIT license.
// Ported to C# / Unity by Andrew Perry, 2013.

namespace csg
{
    /// <summary>
    /// Holds a binary space partition tree representing a 3D solid. Two solids can
    /// be combined using the `union()`, `subtract()`, and `intersect()` methods.
    /// </summary>
    public class CSG
    {
        public List<CsgPolygon> polygons;
//        private Bounds bounds = new Bounds();

        /// <summary>
        /// Constuctor
        /// </summary>
        public CSG()
        {
            this.polygons = new List<CsgPolygon>();
        }

        /// <summary>
        /// Clone
        /// </summary>
        /// <returns></returns>
        public CSG clone()
        {
            CSG csg = new CSG();
            foreach (CsgPolygon p in this.polygons)
            {
                csg.polygons.Add(p.clone());
            }
            return csg;
        }

        public CSG inverse()
        {
            CSG csg = this.clone();
            foreach (CsgPolygon p in csg.polygons)
            {
                p.flip();
            }
            return csg;
        }

        public List<CsgPolygon> toPolygons()
        {
            return this.polygons;
        }

        public Mesh toMesh()
        {
            List<CsgPolygon> trisFromPolygons = new List<CsgPolygon>();
            // triangulate polygons
            for (int i = this.polygons.Count - 1; i >= 0; i--)
            {
                if (this.polygons[i].vertices.Length > 3)
                {
                    for (int vi = 1; vi < this.polygons[i].vertices.Length - 1; vi++)
                    {
                        IVertex[] tri = new IVertex[] {
                            this.polygons[i].vertices[0], 
                            this.polygons[i].vertices[vi], 
                            this.polygons[i].vertices[vi+1]
                        };
                        trisFromPolygons.Add(new CsgPolygon(tri));
                    }
                    // the original polygon is replaced by a set of triangles
                    this.polygons.RemoveAt(i);
                }
            }
            this.polygons.AddRange(trisFromPolygons);

            // TODO: Simplify mesh - the boolean CSG algorithm leaves lots of coplanar
            //       polygons that share an edge that could be simplified.

            // At this point, we have a soup of triangles without regard for shared vertices.
            // We index these to combine any vertices with identical positions & normals 
            // (and maybe later UVs & vertex colors)

            List<Vertex> vertices = new List<Vertex>();
            int[] tris = new int[this.polygons.Count * 3];
            for (int pi = 0; pi < this.polygons.Count; pi++)
            {
                CsgPolygon tri = this.polygons[pi];
                for (int vi = 0; vi < 3; vi++)
                {
                    Vertex vertex = tri.vertices[vi] as Vertex;
                    bool equivalentVertexAlreadyInList = false;
                    for (int i = 0; i < vertices.Count; i++)
                    {
                        if (MathHelper.AlmostEqual(vertices[i].pos, vertex.pos) 
                            && MathHelper.AlmostEqual(vertices[i].normal, vertex.normal))
                        {
                            equivalentVertexAlreadyInList = true;
                            vertex.index = vertices[i].index;
                        }
                    }
                    if (!equivalentVertexAlreadyInList)
                    {
                        vertices.Add(vertex);
                        vertex.index = vertices.Count - 1;
                    }
                    tris[(pi * 3) + vi] = vertex.index;
                }
            }

            Vector3[] verts = new Vector3[this.polygons.Count * 3];
            Vector3[] normals = new Vector3[this.polygons.Count * 3];
            Mesh m = new Mesh();
            for (int i = 0; i < vertices.Count; i++)
            {
                verts[i] = vertices[i].pos;
                normals[i] = vertices[i].normal;
            }
            m.Vertices = verts;
            m.Normals = normals;
            m.Faces = tris;
            //m.RecalculateBounds();
            //m.RecalculateNormals();
            //m.Optimize();
            return m;
        }


//        public static CSG fromMesh(Mesh m, Transform tf)
        public static CSG fromMesh(Mesh m)
        {
            List<CsgPolygon> triangles = new List<CsgPolygon>();
            int[] tris = m.Faces;
            for (int t = 0; t < tris.Length; t += 3)
            {
                Vertex[] vs = new Vertex[3];
/*
                vs[0] = TranslateVertex(m, tf, tris[t]);
                vs[1] = TranslateVertex(m, tf, tris[t + 1]);
                vs[2] = TranslateVertex(m, tf, tris[t + 2]);
*/
                vs[0] = new Vertex(m.Vertices[tris[t]], m.Normals[tris[t]]);
                vs[1] = new Vertex(m.Vertices[tris[t + 1]], m.Normals[tris[t + 1]]);
                vs[2] = new Vertex(m.Vertices[tris[t + 2]], m.Normals[tris[t + 2]]);
                triangles.Add(new CsgPolygon(vs));
            }
            return CSG.fromPolygons(triangles);
        }

/*
        // Transform local to worldspace
        private static Vertex TranslateVertex(Mesh m, Transform tf, int tri)
        {
            Vector3 pos = tf.TransformPoint(m.vertices[tri]);
            Vector3 norm = tf.TransformDirection(m.normals[tri]);
            return new Vertex(pos, norm);
        }
*/
        /// <summary>
        /// Return a new CSG solid representing space in either this solid or in the
        /// solid `csg`. Neither this solid nor the solid `csg` are modified.
        /// </summary>
        /// <remarks>
        ///    A.union(B)
        ///
        ///    +-------+            +-------+
        ///    |       |            |       |
        ///    |   A   |            |       |
        ///    |    +--+----+   =   |       +----+
        ///    +----+--+    |       +----+       |
        ///         |   B   |            |       |
        ///         |       |            |       |
        ///         +-------+            +-------+
        /// </summary>
        /// <param name="csg"></param>
        /// <returns></returns>
        public CSG union(CSG csg)
        {
            Node a = new Node(this.polygons);
            Node b = new Node(csg.polygons);
            a.clipTo(b);
            b.clipTo(a);
            //b.invert();
            //b.clipTo(a);
            //b.invert();
            a.build(b.allPolygons());
            return CSG.fromPolygons(a.allPolygons());
        }

        /// <summary>
        /// Return a new CSG solid representing space in this solid but not in the
        /// solid `csg`. Neither this solid nor the solid `csg` are modified.
        /// </summary>
        /// <remarks>
        /// A.subtract(B)
        ///    +-------+            +-------+
        ///    |       |            |       |
        ///    |   A   |            |       |
        ///    |    +--+----+   =   |    +--+
        ///    +----+--+    |       +----+
        ///         |   B   |
        ///         |       |
        ///         +-------+
        /// </remarks>
        /// <param name="csg"></param>
        /// <returns></returns>
        public CSG subtract(CSG csg)
        {
            Node a = new Node(this.polygons);
            Node b = new Node(csg.polygons);
            a.invert();
            a.clipTo(b);
            b.clipTo(a);
            b.invert();
            b.clipTo(a);
            b.invert();
            a.build(b.allPolygons());
            a.invert();
            return CSG.fromPolygons(a.allPolygons());
        }

        /// <summary>
        /// Return a new CSG solid representing space both this solid and in the
        /// solid `csg`. Neither this solid nor the solid `csg` are modified.
        /// </summary>
        /// <remarks>
        /// A.intersect(B)
        /// 
        ///    +-------+
        ///    |       |
        ///    |   A   |
        ///    |    +--+----+   =   +--+
        ///    +----+--+    |       +--+
        ///         |   B   |
        ///         |       |
        ///         +-------+
        /// </remarks>
        /// <param name="csg"></param>
        /// <returns>CSG of the intersection</returns>
        public CSG intersect(CSG csg)
        {
            Node a = new Node(this.polygons);
            Node b = new Node(csg.polygons);
            a.invert();
            b.invert();
            a.clipTo(b);
            b.clipTo(a);
            a.build(b.allPolygons());
            return CSG.fromPolygons(a.allPolygons()).inverse();
        }

        /// <summary>
        /// Cube function, Untested but compiles
        /// </summary>
        /// <param name="center">world space center of the cube</param>
        /// <param name="radius">size of the cube created at center</param>
        /// <returns></returns>
        public static CSG cube(Vector3? center, Vector3? radius)
        {
            Vector3 c = center.GetValueOrDefault(Vector3.Zero);
            Vector3 r = radius.GetValueOrDefault(Vector3.One);

            //TODO: Test if this works
            CsgPolygon[] polygons = new CsgPolygon[6];
            int[][][] data = new int[][][] {
                new int[][]{new int[]{0, 4, 6, 2}, new int[]{-1, 0, 0}},
                new int[][]{new int[]{1, 3, 7, 5}, new int[]{1, 0, 0}},
                new int[][]{new int[]{0, 1, 5, 4}, new int[]{0, -1, 0}},
                new int[][]{new int[]{2, 6, 7, 3}, new int[]{0, 1, 0}},
                new int[][]{new int[]{0, 2, 3, 1}, new int[]{0, 0, -1}},
                new int[][]{new int[]{4, 5, 7, 6}, new int[]{0, 0, 1}}
            };
            for (int x = 0; x < 6; x++)
            {
                int[][] v = data[x];
                Vector3 normal = new Vector3(v[1][0], v[1][1], v[1][2]);

                IVertex[] verts = new IVertex[4];
                for (int i = 0; i < 4; i++)
                {
                    verts[i] = new Vertex(
                        new Vector3(
                            c.X + (r.X * (2 * (((v[0][i] & 1) > 0) ? 1 : 0) - 1)),
                            c.Y + (r.Y * (2 * (((v[0][i] & 2) > 0) ? 1 : 0) - 1)),
                            c.Z + (r.Z * (2 * (((v[0][i] & 4) > 0) ? 1 : 0) - 1))),
                            normal
                        );
                }
                polygons[x] = new CsgPolygon(verts);
            }
            return CSG.fromPolygons(polygons);
        }

        private static void makeSphereVertex(ref List<IVertex> vxs, Vector3 center, double r, double theta, double phi)
        {
            theta *= Math.PI * 2;
            phi *= Math.PI;
            Vector3 dir = new Vector3(Math.Cos(theta) * Math.Sin(phi),
                                      Math.Cos(phi),
                                      Math.Sin(theta) * Math.Sin(phi)
            );
            Vector3 sdir = dir;
            sdir *= r;
            vxs.Add(new Vertex(center + sdir, dir));
        }

        public static CSG sphere(Vector3 center, double radius = 1, double slices = 16, double stacks = 8f)
        {
            double r = radius;
            List<CsgPolygon> polygons = new List<CsgPolygon>();
            List<IVertex> vertices;

            for (int i = 0; i < slices; i++)
            {
                for (int j = 0; j < stacks; j++)
                {
                    vertices = new List<IVertex>();
                    makeSphereVertex(ref vertices, center, r, i / slices, j / stacks);

                    if (j > 0)
                    {
                        makeSphereVertex(ref vertices, center, r, (i + 1) / slices, j / stacks);
                    }

                    if (j < stacks - 1)
                    {
                        makeSphereVertex(ref vertices, center, r, (i + 1) / slices, (j + 1) / stacks);
                    }

                    makeSphereVertex(ref vertices, center, r, i / slices, (j + 1) / stacks);
                    polygons.Add(new CsgPolygon(vertices));
                }
            }
            return CSG.fromPolygons(polygons);
        }

        /// <summary>
        /// Construct a CSG solid from a list of `Polygon` instances.
        /// The polygons are cloned
        /// </summary>
        /// <param name="polygons"></param>
        /// <returns></returns>
        public static CSG fromPolygons(List<CsgPolygon> polygons)
        {
            //TODO: Optimize polygons to share vertices
            CSG csg = new CSG();
            foreach (CsgPolygon p in polygons)
            {
                csg.polygons.Add(p.clone());
            }
            return csg;
        }

        /// <summary>
        /// Create CSG from array, does not clone the polygons
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        private static CSG fromPolygons(CsgPolygon[] polygons)
        {
            //TODO: Optimize polygons to share vertices
            CSG csg = new CSG();
            csg.polygons.AddRange(polygons);
            return csg;
        }
    }
}
