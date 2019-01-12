using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.WindowsForms;

namespace SharpOSMgl
{
    public partial class MapForm : Form
    {
        private const double MapHalfSize = 0.005;   // 1km x 1km
        private GMapOverlay polyOverlay;
        SharpGLForm map3d;

        public MapForm()
        {
            InitializeComponent();
        }

        private void MapForm_Load(object sender, EventArgs e)
        {
            // Initialize map:
            gmap.MapProvider = GMap.NET.MapProviders.OpenStreetMapProvider.Instance;
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerOnly;
            this.polyOverlay = new GMapOverlay("polygons");
            gmap.Overlays.Add(polyOverlay);
        }

        private void gmap_MouseDown(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    var location = gmap.FromLocalToLatLng(e.X, e.Y);
                    List<PointLatLng> points = new List<PointLatLng>();
                    points.Add(new PointLatLng(location.Lat - MapHalfSize, location.Lng - MapHalfSize));
                    points.Add(new PointLatLng(location.Lat - MapHalfSize, location.Lng + MapHalfSize));
                    points.Add(new PointLatLng(location.Lat + MapHalfSize, location.Lng + MapHalfSize));
                    points.Add(new PointLatLng(location.Lat + MapHalfSize, location.Lng - MapHalfSize));
                    GMapPolygon polygon = new GMapPolygon(points, "mypolygon");
                    polygon.Fill = new SolidBrush(Color.FromArgb(30, Color.Red));
                    polygon.Stroke = new Pen(Color.Red, 1);
                    this.polyOverlay.Clear();
                    this.polyOverlay.Polygons.Add(polygon);
                    break;
                case MouseButtons.Right:
                    break;
                case MouseButtons.Middle:
                    break;
                default:
                    break;
            }
        }

        private void gmap_MouseMove(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    var location = gmap.FromLocalToLatLng(e.X, e.Y);
                    List<PointLatLng> points = new List<PointLatLng>();
                    points.Add(new PointLatLng(location.Lat - MapHalfSize, location.Lng - MapHalfSize));
                    points.Add(new PointLatLng(location.Lat - MapHalfSize, location.Lng + MapHalfSize));
                    points.Add(new PointLatLng(location.Lat + MapHalfSize, location.Lng + MapHalfSize));
                    points.Add(new PointLatLng(location.Lat + MapHalfSize, location.Lng - MapHalfSize));
                    GMapPolygon polygon = new GMapPolygon(points, "mypolygon");
                    polygon.Fill = new SolidBrush(Color.FromArgb(30, Color.Red));
                    polygon.Stroke = new Pen(Color.Red, 1);
                    this.polyOverlay.Clear();
                    this.polyOverlay.Polygons.Add(polygon);
                    break;
                case MouseButtons.Right:
                    break;
                case MouseButtons.Middle:
                    break;
                default:
                    break;
            }
        }

        private void gmap_MouseUp(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    if (map3d == null || map3d.IsDisposed)
                    {
                        map3d = new SharpGLForm();
                    }
                    map3d.Show();
                    var bbox = this.polyOverlay.Polygons.First();
                    double left = Math.Min(bbox.Points[1].Lng, bbox.Points[3].Lng);
                    double bottom = Math.Min(bbox.Points[0].Lat, bbox.Points[2].Lat);
                    double right = Math.Max(bbox.Points[1].Lng, bbox.Points[3].Lng);
                    double top = Math.Max(bbox.Points[0].Lat, bbox.Points[2].Lat);
                    map3d.LoadMap(left, bottom, right, top);
                    break;
                case MouseButtons.Right:
                    break;
                case MouseButtons.Middle:
                    break;
                default:
                    break;
            }
        }

        private void gmap_Load(object sender, EventArgs e)
        {

        }

        private void gmap_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9)
            {
                if (map3d == null || map3d.IsDisposed)
                {
                    map3d = new SharpGLForm();
                }
                map3d.Show();
                map3d.LoadMap(SharpGLForm.SamplesPath + SharpGLForm.Samples[e.KeyCode - Keys.D1]);
            }
        }
    }
}
