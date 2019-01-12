using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using SharpGL;

namespace csg
{
    /// <summary>
    /// The main form class.
    /// </summary>
    public partial class SharpGLForm : Form
    {
        private int mouseX = 0;
        private int mouseY = 0;
        private bool wireframe = false;
        private bool normals = false;

        private const double DefaultDistance = 20;
        private const double MouseOrbitSpeed = 0.5;
        private const double WheelOrbitSpeed = 0.05;
        private const double MousePanSpeed = 0.5;

        private Vector3 cameraPos = new Vector3(0, 0, 0);
        private Vector3 targetPos = new Vector3(0, 0, 0);
        private Vector2 DefaultAngles = new Vector2(0, 90);
        private Vector2 angles;
        private double distance;

        private List<Mesh> meshes = new List<Mesh>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="footprint"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        private Mesh ComputeBody(Polygon footprint, Color color)
        {
            // Extrude
            var minY = 0;
            var maxY = 5;
            Vector3[] vertices;
            vertices = new Vector3[footprint.Points.Count * 2];
            var i = 0;
            foreach (var point in footprint.Points)
            {
                vertices[i++] = new Vector3(point.X, minY, -point.Y);
                vertices[i++] = new Vector3(point.X, maxY, -point.Y);
            }
            // Triangulate
            var faces = new List<int>();
            for (var idx = 0; idx < vertices.Length - 2; idx += 2)
            {
                faces.Add(idx + 1);
                faces.Add(idx + 0);
                faces.Add(idx + 2);

                faces.Add(idx + 1);
                faces.Add(idx + 2);
                faces.Add(idx + 3);
            }
            return new Mesh(vertices,
                faces.ToArray(),
                Enumerable.Repeat(color, vertices.Length).ToArray());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private Polygon ReadPoly(string filename)
        {
            string[] data = File.ReadAllLines(filename);
            var contour = new List<Vector2>();
            foreach (var str in data)
            {
                try
                {
                    string[] parts = str.TrimEnd().Split(' ');
                    var _x = Double.Parse(Regex.Replace(parts[0], "[{}XY:]", string.Empty));
                    var _y = Double.Parse(Regex.Replace(parts[1], "[{}XY:]", string.Empty));
                    contour.Add(new Vector2(_x / 10, _y / 10));
                }
                catch (Exception)
                {
                    return null;
                }
            }
            return new Polygon(contour);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SharpGLForm"/> class.
        /// </summary>
        public SharpGLForm()
        {
            InitializeComponent();
            this.Text = "CSG";
            this.MouseWheel += new MouseEventHandler(openGLControl_MouseWheel);
            angles = DefaultAngles;
            distance = DefaultDistance;
            UpdateCamera(angles, distance);

            // Substract and union
            var a = CSG.cube(new Vector3(0, 1, 0), new Vector3(5, 1, 5));
            var b = CSG.cube(new Vector3(0, 1, 0), new Vector3(2, 1, 2));
            var c = CSG.cube(new Vector3(2, 1, 2), new Vector3(2, 1, 2));
            var d = CSG.sphere(new Vector3(-4, 2, -4), 2, 16, 16);

            var x = a.subtract(b);
            var y = x.subtract(c);
            var z = y.union(d);
            var mesh = z.toMesh();
            mesh.Colors = Enumerable.Repeat(Color.Red, mesh.Vertices.Length).ToArray();
            meshes.Add(mesh);

            // Intersect
            var e = CSG.cube(new Vector3(10, 1, -10), new Vector3(2, 1, 2));
            var f = CSG.sphere(new Vector3(9, 2, -9), 2, 16, 16);
            var w = e.intersect(f);
            mesh = w.toMesh();
            mesh.Colors = Enumerable.Repeat(Color.Yellow, mesh.Vertices.Length).ToArray();
            meshes.Add(mesh);

            // OSM polys issues
            var outer = ComputeBody(ReadPoly(@"..\..\samples\64955049.txt"), Color.Green);
            outer.Cap(Color.Green);

            var inner = ComputeBody(ReadPoly(@"..\..\samples\64955049-inner.txt"), Color.Green);
            inner.Cap(Color.Green);

//            outer.Substract(inner);
            meshes.Add(outer);
        }

        /// <summary>
        /// Handles the OpenGLDraw event of the openGLControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RenderEventArgs"/> instance containing the event data.</param>
        private void openGLControl_OpenGLDraw(object sender, RenderEventArgs e)
        {
            //  Get the OpenGL object.
            OpenGL gl = openGLControl.OpenGL;

            //  Clear the color and depth buffer.
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

            gl.Disable(OpenGL.GL_LIGHTING);
            gl.LineWidth(0.1f);
            const float dist = 50f;
            gl.Begin(OpenGL.GL_LINES);
            gl.Color(0.1, 0.5, 0.1);
            gl.Vertex(0, 0, 0);
            gl.Vertex(0, 1, 0);
            for (float i = -dist; i <= dist; i += 1)
            {
                if (i == 0)
                {
                    gl.Color(0.5, 0.1, 0.1);
                }
                else
                {
                    gl.Color(0.1, 0.1, 0.1);
                }

                gl.Vertex(i, -0.01, dist);
                gl.Vertex(i, -0.01, -dist);

                if (i == 0)
                {
                    gl.Color(0.1, 0.1, 0.5);
                }

                gl.Vertex(dist, -0.01, i);
                gl.Vertex(-dist, -0.01, i);
            }
            gl.End();

            gl.Enable(OpenGL.GL_LIGHTING);
            foreach (var mesh in meshes)
            {
                gl.Begin(OpenGL.GL_TRIANGLES);
//                gl.Color(mesh.Colors[0].R,mesh.Colors[0].G,mesh.Colors[0].B);
//                gl.Material(OpenGL.GL_FRONT_AND_BACK, OpenGL.GL_DIFFUSE,  new float[] { mesh.Colors[0].R, mesh.Colors[0].G, mesh.Colors[0].B, 1.0f });

                for (var idx = 0; idx < mesh.Faces.Length; idx += 3)
                {
                    var i = mesh.Faces[idx];
                    var j = mesh.Faces[idx + 1];
                    var k = mesh.Faces[idx + 2];
                    gl.Vertex(mesh.Vertices[i].X, mesh.Vertices[i].Y, mesh.Vertices[i].Z);
                    gl.Normal(mesh.Normals[i].X, mesh.Normals[i].Y, mesh.Normals[i].Z);

                    gl.Vertex(mesh.Vertices[j].X, mesh.Vertices[j].Y, mesh.Vertices[j].Z);
                    gl.Normal(mesh.Normals[j].X, mesh.Normals[j].Y, mesh.Normals[j].Z);

                    gl.Vertex(mesh.Vertices[k].X, mesh.Vertices[k].Y, mesh.Vertices[k].Z);
                    gl.Normal(mesh.Normals[k].X, mesh.Normals[k].Y, mesh.Normals[k].Z);
                }
                gl.End();

                if (normals && mesh.Normals != null)
                {
                    gl.Disable(OpenGL.GL_LIGHTING);
                    var f = 1;
                    gl.Color(0, 0, 1.0);
                    gl.Begin(OpenGL.GL_LINES);
                    for (var i = 0; i < mesh.Normals.Length; i++)
                    {
                        gl.Vertex(mesh.Vertices[i].X, mesh.Vertices[i].Y, mesh.Vertices[i].Z);
                        gl.Vertex(mesh.Vertices[i].X + f * mesh.Normals[i].X, mesh.Vertices[i].Y + f * mesh.Normals[i].Y, mesh.Vertices[i].Z + f * mesh.Normals[i].Z);
                    }
                    gl.End();
                    gl.Enable(OpenGL.GL_LIGHTING);
                }
            }
        }

        /// <summary>
        /// Handles the OpenGLInitialized event of the openGLControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void openGLControl_OpenGLInitialized(object sender, EventArgs e)
        {
            OpenGL gl = openGLControl.OpenGL;
            gl.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);

            // Setup lighting
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_POSITION, new float[] {1.0f, 2.0f, 1.0f, 0.0f});
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_AMBIENT, new float[] { 0.1f, 0.2f, 0.3f, 1.0f });
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_DIFFUSE, new float[] { 0.1f, 0.3f, 0.9f, 1.0f });
//            gl.Material(OpenGL.GL_FRONT, OpenGL.GL_AMBIENT, new float[] { 0.1f, 0.1f, 0.1f, 1.0f });
//            gl.Material(OpenGL.GL_FRONT, OpenGL.GL_DIFFUSE, new float[] { 0.75f, 0.0f, 0.0f, 1.0f });
//            gl.Material(OpenGL.GL_FRONT, OpenGL.GL_SPECULAR, new float[] { 1.0f, 1.0f, 1.0f, 1.0f });
//            gl.Material(OpenGL.GL_FRONT, OpenGL.GL_SHININESS, 100);
            gl.Enable(OpenGL.GL_LIGHT0);
        }

        /// <summary>
        /// Handles the Resized event of the openGLControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void openGLControl_Resized(object sender, EventArgs e)
        {
            var gl = openGLControl.OpenGL;
            gl.MatrixMode(OpenGL.GL_PROJECTION);
            gl.LoadIdentity();
            gl.Perspective(45.0f, (double)Width / (double)Height, 0.1, 1000.0);
            gl.MatrixMode(OpenGL.GL_MODELVIEW);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openGLControl_MouseMove(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    {
                        angles.X = (angles.X + (e.X - mouseX) * MouseOrbitSpeed) % 360;
                        angles.Y = MathHelper.Clamp(angles.Y + (e.Y - mouseY) * MouseOrbitSpeed, -90, 90);
                        UpdateCamera(angles, distance);
                    }
                    break;

                case MouseButtons.Right:
                    {
                        var df = distance / DefaultDistance;
                        targetPos.X += (e.X - mouseX) * df * MousePanSpeed;
                        targetPos.Z += (e.Y - mouseY) * df * MousePanSpeed;
                        UpdateCamera(angles, distance);
                    }
                    break;
            }

            mouseX = e.X;
            mouseY = e.Y;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openGLControl_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta != 0)
            {
                distance = MathHelper.Clamp(distance - (double)e.Delta * WheelOrbitSpeed, 0.1, 200);
                UpdateCamera(angles, distance);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="angles"></param>
        /// <param name="distance"></param>
        private void UpdateCamera(Vector2 angles, double distance)
        {
            // Calculate the camera position using the distance and angles
            var phi = MathHelper.ToRadians(angles.X);
            var theta = MathHelper.ToRadians(angles.Y);
            cameraPos.X = targetPos.X + distance * Math.Cos(phi) * Math.Cos(theta);
            cameraPos.Y = targetPos.Y + distance * Math.Sin(theta) ;
            cameraPos.Z = targetPos.Z + distance * Math.Sin(phi) * Math.Cos(theta);

            // Set camera lookat
            var gl = openGLControl.OpenGL;
            gl.LoadIdentity();
            gl.LookAt(cameraPos.X, cameraPos.Y, cameraPos.Z,   // Camera position
                      targetPos.X, targetPos.Y, targetPos.Z,   // Look at point
                      0, 1, 0);   // Up vector
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openGLControl_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F1:
                    wireframe = !wireframe;
                    if (wireframe)
                    {
                        openGLControl.OpenGL.PolygonMode(OpenGL.GL_FRONT_AND_BACK, OpenGL.GL_LINE);
                    }
                    else
                    {
                        openGLControl.OpenGL.PolygonMode(OpenGL.GL_FRONT_AND_BACK, OpenGL.GL_FILL);
                    }
                    break;

                case Keys.F2:
                    normals = !normals;
                    break;
            }
        }
    }
}
