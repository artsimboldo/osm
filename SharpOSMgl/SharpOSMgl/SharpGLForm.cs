using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SharpGL;
using SharpOSM;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace SharpOSMgl
{
    /// <summary>
    /// The main form class.
    /// </summary>
    /// 
    public partial class SharpGLForm : Form
    {
        private int mouseX = 0;
        private int mouseY = 0;
        private bool wireframe = false;
        private bool normals = false;
        private bool colorcodes = false;

        private Vector2 DefaultAngles = new Vector2(0, 90);
        private const double DefaultDistance = 500;
        private const double MouseOrbitSpeed = 0.5;
        private const double MousePanSpeed = 2;
        private const double WheelOrbitSpeed = 0.5;

        private Vector3 cameraPos = new Vector3(0, 0, 0);
        private Vector3 targetPos = new Vector3(0, 0, 0);
        private Vector2 angles;
        private double distance;

        private CancellationTokenSource tokenSource;
        private Task task;
        private BlockingCollection<Shape> shapeQueue;

        internal const string SamplesPath = @"..\..\..\samples\";

        internal static List<string> Samples = new List<string>() 
        {
            "nycplaza2.xml",
//            "nycBrooklynBridgePlaza.xml",
            "multipolygon.xml",
            "museum.xml",
            "eiffel.xml",
            "nyc2.xml",
            "ny.xml",
//            "london.xml",
//            "trocadero.xml",
            "ecole-militaire.xml",
            "ecole-militaire2.xml",
            "invalides2.xml",
//            "invalides.xml"
//            "la.xml"
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        public void LoadMap(string filename)
        {
            shapeQueue = new BlockingCollection<Shape>();
            tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token; ;
            task = Task.Factory.StartNew(() =>
            {
                try
                {
                    string err = "";
                    using (var map = new OsmProvider())
                    {
                        if (map.Load(filename))
                        {
                            foreach (var building in map.Buildings())
                            {
                                if (token.IsCancellationRequested || building == null)
                                {
                                    break;
                                }
                                shapeQueue.Add(building);
                            }

                            foreach (var plant in map.Natural())
                            {
                                if (token.IsCancellationRequested || plant == null)
                                {
                                    break;
                                }
                                shapeQueue.Add(plant);
                            }
 
                        }
                        else
                        {
                            MessageBox.Show(err, "OSM reader error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "OSM reader unexpected error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }, token);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="bottom"></param>
        /// <param name="right"></param>
        /// <param name="top"></param>
        public void LoadMap(double left, double bottom, double right, double top)
        {
            shapeQueue = new BlockingCollection<Shape>();
            Task.Factory.StartNew(() =>
            {
                try
                {
                    string err = "";
                    using (var map = new OsmProvider())
                    {
                        if (map.BBox(left, bottom, right, top, out err))
                        {
                            foreach (var building in map.Buildings())
                            {
                                shapeQueue.Add(building);
                            }

                            foreach (var plant in map.Natural())
                            {
                                shapeQueue.Add(plant);
                            }
                        }
                        else
                        {
                            MessageBox.Show(err, "OSM reader error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "OSM reader unexpected error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SharpGLForm"/> class.
        /// </summary>
        public SharpGLForm()
        {
            InitializeComponent();
            this.Text = "SharpOSM";
            this.MouseWheel += new MouseEventHandler(openGLControl_MouseWheel);
            angles = DefaultAngles;
            distance = DefaultDistance;
            UpdateCamera(angles, distance);
        }

        /// <summary>
        /// Handles the OpenGLDraw event of the openGLControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RenderEventArgs"/> instance containing the event data.</param>
        private void openGLControl_OpenGLDraw(object sender, RenderEventArgs e)
        {
            //  Get the OpenGL object.
            var gl = openGLControl.OpenGL;

            //  Clear the color and depth buffer.
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

            // Grid
            float dist = 1500f;
            gl.Color(0.95, 0.95, 0.95);
            gl.Begin(OpenGL.GL_LINES);
            for (float i = -dist; i <= dist; i += 100)
            {
                gl.Vertex(i, -0.01, dist);
                gl.Vertex(i, -0.01, -dist);
                gl.Vertex(dist, -0.01, i);
                gl.Vertex(-dist, -0.01, i);
            }
            gl.End();

            // Shapes
            foreach (var shape in shapeQueue)
            {
                var mesh = shape.Mesh;
                if (mesh != null)
                {
                    gl.LineWidth(1);
                    gl.Begin(OpenGL.GL_TRIANGLES);
                    for (var idx = 0; idx < mesh.Faces.Length; idx += 3)
                    {
                        var i = mesh.Faces[idx];
                        var j = mesh.Faces[idx + 1];
                        var k = mesh.Faces[idx + 2];
                        var ci = colorcodes ? shape.ColorCode : mesh.Colors[i];
                        var cj = colorcodes ? shape.ColorCode : mesh.Colors[j];
                        var ck = colorcodes ? shape.ColorCode : mesh.Colors[k];
                        gl.Color(ci.R, ci.G, ci.B, ci.A);
                        gl.Vertex(mesh.Vertices[i].X, mesh.Vertices[i].Y, mesh.Vertices[i].Z);
                        gl.Color(cj.R, cj.G, cj.B, cj.A);
                        gl.Vertex(mesh.Vertices[j].X, mesh.Vertices[j].Y, mesh.Vertices[j].Z);
                        gl.Color(ck.R, ck.G, ck.B, ck.A);
                        gl.Vertex(mesh.Vertices[k].X, mesh.Vertices[k].Y, mesh.Vertices[k].Z);
                    }
                    gl.End();

                    if (normals)
                    {
                        var f = 20;
                        gl.LineWidth(2);
                        gl.Color(0, 0, 0.9);
                        gl.Begin(OpenGL.GL_LINES);
                        for (var i = 0; i < mesh.Normals.Length; i++)
                        {
                            gl.Vertex(mesh.Vertices[i].X, mesh.Vertices[i].Y, mesh.Vertices[i].Z);
                            gl.Vertex(mesh.Vertices[i].X + f * mesh.Normals[i].X, mesh.Vertices[i].Y + f * mesh.Normals[i].Y, mesh.Vertices[i].Z + f * mesh.Normals[i].Z);
                        }
                        gl.End();
                    }
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
            openGLControl.OpenGL.ClearColor(1, 1, 1, 0);
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
            gl.Perspective(45.0f, (double)Width / (double)Height, 0.1, 5000.0);
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
                        angles.Y = MathHelper.Clamp(angles.Y + (e.Y - mouseY) * MouseOrbitSpeed, 0, 90);
                        UpdateCamera(angles, distance);
                    }
                    break;

                case MouseButtons.Right:
                    {
                        var dx = (double)(e.X - mouseX);
                        var dy = (double)(e.Y - mouseY);
                        var df = distance / DefaultDistance;
                        targetPos.X += dx * df * MousePanSpeed;
                        targetPos.Z += dy * df * MousePanSpeed;
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
                distance = MathHelper.Clamp(distance - (double)e.Delta * WheelOrbitSpeed, 10, 5000);
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
            var gl = openGLControl.OpenGL;
            // Calculate the camera position using the distance and angles
            var phi = MathHelper.ToRadians(angles.X);
            var theta = MathHelper.ToRadians(angles.Y);
            cameraPos.X = targetPos.X + distance * Math.Cos(phi) * Math.Cos(theta);
            cameraPos.Y = targetPos.Y + distance * Math.Sin(theta);
            cameraPos.Z = targetPos.Z + distance * Math.Sin(phi) * Math.Cos(theta);
            // Set camera lookat
            gl.LoadIdentity();
            gl.LookAt(cameraPos.X, cameraPos.Y, cameraPos.Z,   // Camera position
                      targetPos.X, targetPos.Y, targetPos.Z,    // Look at point
                      0, 1, 0);   // Up vector 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openGLControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9)
            {
                if (tokenSource != null)
                {
                    // ongoing thread working, terminate it:
                    tokenSource.Cancel();
                    task.Wait();
                    tokenSource.Dispose();
                }
                LoadMap(SamplesPath + Samples[e.KeyCode - Keys.D1]);
            }
            else
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

                    case Keys.F3:
                        colorcodes = !colorcodes;
                        break;
                }
            }
        }
    }
}
