using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace SharpOSM
{
    /*
     * See http://wiki.openstreetmap.org/wiki/Simple_3D_Buildings
     */

    public enum BuildingShape
    {
        None,
        Cylinder,
        Pyramidal
    }

    public enum RoofShape
    {
        None,
        Flat,
        Skillion,
        Gabled,
        Halfhipped,
        Hipped,
        Pyramidal,
        Gambrel,
        Mansard,
        Dome,
        Onion,
        Round,
        Saltbox
    }

    public enum RoofOrientation
    {
        None,
        Along,
        Across
    };

    public struct Surface
    {
        public Color color;
        public string material;
    }

    // TODO: buiding shape (e.g. pyramidal)
    public struct PartData
    {
        /*
         * Type of shape
         */
        public BuildingShape shape;

        /*
         * Name of that part
         */
        public string name;

        /*
         * For 3D rendering (true) or 2D
         */
        public bool volume;

        /*
         * Distance between the lowest possible position with ground contact and the top of the roof of the building, 
         * excluding antennas, spires and other equipment mounted on the roof.
         */
        public float height;

        /*
         * Approximate height below the building structure.
         * Note that when min_height is used, height is still defined as the distance from the ground to the top of the structure. 
         * So "bridge" with 3 meters height, where bottom part of the bridge is positioned 10 meters above ground level will have min_height=10, height=13. 
         */
        public float min_height;

        /*
         * Number of floors of the building above ground (without levels in the roof), to be able to texture the building in a nice way. 
         */
        public short levels;

        /*
         * Levels skipped in a building part, analogous to min_height 
         */
        public short min_level;

        /*
         * material description
         */
        public Surface surface;
    }

    public struct RoofData
    {
        /*
         * Standard catalogue of well known roof types. 
         */
        public RoofShape shape;

        /*
         * For roofs with a ridge the ridge is assumed to be parallel to the longest side of the building (roof:orientation=along). But it can be tagged explicitly with this tag.
         */
        public RoofOrientation orientation;

        /*
         * Roof height in meters
         */
        public float height;

        /*
         * Alternatively to roof:height=*, roof height can be indicated implicitly by providing the inclination of the sides (in degrees).
         */
        public float angle;

        /*
         * Number of floors within the roof, which are not already counted in building:levels=*.
         */
        public short levels;

        /*
         * Direction from back side of roof to front, i.e. the direction towards which the main face of the roof is looking
         */
        public float direction;

        /*
         * material description
         */
        public Surface surface;
    }

    public class BuildingPart : Shape
    {
        private Mesh mesh;
        private Polygon footprint;
        public PartData Body { get; protected set; }
        public RoofData Roof { get; protected set; }

        public override Mesh Mesh
        {
            get
            {
                if (this.mesh == null && this.Body.volume)
                {
                    this.mesh = ModelBuilder.CreateMesh(this);
                }
                return this.mesh;
            }
        }
            
        public Polygon Footprint
        {
            get
            {
                if (this.footprint == null)
                {
                    this.footprint = new Polygon();
                }
                return this.footprint;
            }
            internal set
            {
                this.footprint = value;
            }
        }
 
        public BuildingPart(string id, Polygon footprint, PartData body, RoofData roof, Color color)
            : base(id, color)
        {
            Footprint = footprint;
            Body = body;
            Roof = roof;
        }
    }
}
