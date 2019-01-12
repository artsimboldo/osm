using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace SharpOSM
{
    public struct NaturalData
    {

        /*
         * genus - scientific name of the genus (first part of the scientific name). If you add species=* this is not necessary. 
         */
        public string genus;

        /*
         * species - scientific name of the species (popularly known as the Latin name). Please use the namespaces for local languages (see examples).
         */
        public string species;

        /*
         * leaf_type = broadleaved / needleleaved - describes the type of leaves
         */
        public string leaf_type;

        /*
         * taxon - scientific name describing any taxonomic level e.g. order, family, genus, species, sub-species or cultivar
         */

        /*
         * sex = male / female. Some species are dioecious, meaning that an individual has only male or only female flowers. A good known example are all willows (Salix).
         */

        /* 
         * circumference - for the circumference of the trunk (measured in a height of 1 metre above ground). If no unit is given metres are assumed.
         */
        public float circumference;

        /*
         * height - for the height
         */
        public float height;

        /*
         * name - for individual trees which have a name, usually these are either individual trees with a historical or traditional name or trees with a name given in memory of special events (> memorial tree). The usual rules for name=* apply. This tag should not be used for a description of the species.
         */

        /*
         * leaf_cycle = deciduous / evergreen / semi_deciduous / semi_evergreen - describes the phenology of leaves.
         */

    }

    public class Natural : Shape
    {
        private Mesh mesh;
        public NaturalData Data { get; internal set;}
        public Vector2 Anchor { get; internal set; }

        public override Mesh Mesh
        {
            get
            {
                if (this.mesh == null)
                {
                    this.mesh = ModelBuilder.CreateMesh(this);
                }
                return this.mesh;
            }
        }

        public Natural(string id)
            : base(id, Color.Green)
        { }

        public Natural(string id, Vector2 anchor, NaturalData data)
            : base(id, Color.Green)
        {
            Anchor = anchor;
            Data = data;
        }
    }
}
