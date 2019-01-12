using System;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Drawing;
using System.Threading.Tasks;

namespace SharpOSM
{
    public class TileBound
    {
        public Vector2 TopLeft { get; private set; }
        public Vector2 BottomRight { get; private set; }
        public Vector2 Center { get; private set; }

        public TileBound(Vector2 topleft, Vector2 botttomright)
        {
            TopLeft = topleft;
            BottomRight = botttomright;
            double width = botttomright.X - topleft.X;
            double height = topleft.Y - botttomright.Y;
            Center = new Vector2(topleft.X + width * 0.5, botttomright.Y + height * 0.5);
        }
    }

    public enum CompoundType
    {
        None,
        Building,
        MultipolygonBuilding,
        MultipolygonBuildingPart,
    }

    public enum PartRole
    {
        None,
        Outline,
        Part,
        Outer,
        Inner
    }

    public class Reference
    {
        public readonly string Id;
        public XElement Data { get; set;}

        public Reference(string id)
        {
            Id = id;
        }

        public Reference(string id, XElement data)
        {
            Id = id;
            Data = data;
        }
    }

    public class Simple : Reference
    {
        public Color Color { get; set; }
        public Polygon Polygon { get; set; }

        public Simple(string id) : base(id)
        { }

        public Simple(string id, XElement data, Polygon poly, Color color)
            : base(id, data)
        {
            Polygon = poly;
            Color = color;
        }
    }

    public class Structure : Reference
    {
        public readonly PartRole Role;
        public MultiPolygon Polygon { get; set; }
        public bool Unassigned { get; set; }

        public Structure(string id, string role)
            : base(id)
        {
            Unassigned = true;
            switch (role.ToLower())
            { 

                case "outline":
                    Role = PartRole.Outline;
                    break;

                case "part":
                    Role = PartRole.Part;
                    break;

                case "outer":
                    Role = PartRole.Outer;
                    break;

                case "inner":
                    Role = PartRole.Inner;
                    break;
            }
        }
    }

    public class Compound
    {
        public readonly CompoundType Type;
        public readonly List<Structure> Structures;
        public readonly List<Reference> Relations;
        public XElement Data { get; set; }

        public Compound(CompoundType type, XElement data)
        {
            Type = type;
            Data = data;
            Structures = new List<Structure>();
            Relations = new List<Reference>();
            foreach (var member in data.Descendants("member"))
            {
                switch(member.Attribute("type").Value.ToLower())
                {
                    case "way":
                        Structures.Add(new Structure(member.Attribute("ref").Value, member.Attribute("role").Value));
                        break;

                    case "relation":
                        if (member.Attribute("role").Value == "part")
                        {
                            Relations.Add(new Reference(member.Attribute("ref").Value));
                        }
                        break;
                }
            }
        }
    }

    /// <summary>
    /// TODO:
    /// ParseSimple: clip building parts with buildings
    /// Buildings:
    /// - Building:shape i.e. pyramidal
    /// - Better height estimation? complement with other data sources?
    /// - Roof shapes
    /// 
    /// Natural:
    /// - Remaining trees properties
    /// - Lands, water, etc.
    /// 
    /// Routes:
    /// - ...
    /// 
    /// General:
    /// - Retry system if connection errors
    /// 
    /// </summary>
    public class OsmProvider : IMapProvider
    {
        protected static readonly string OsmMapUrl = "http://api.openstreetmap.org/api/0.6/map?";
        protected static float DefaultTreeCircumference = 6.0f;
        protected static float DefaultTreeHeight = 10.0f;
        protected static Color DefaultBuildingColor = Color.LightSlateGray;
        protected static Color DefaultRoofColor = Color.LightGray;
        protected static float DefaultBuildingHeight = 15.0f;
        protected static float DefaultFloorHeight = 3.5f;
        protected static float DefaultBuildingMinHeight = 0.01f;
        protected XDocument Xdoc { get; private set; }
        private bool absoluteAnchor = true;
        public TileBound Tile { get; private set; }
        private List<string> ways = new List<string>();

        /// <summary>
        /// Candidate for memoization?
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private XElement FindNodeById(string id)
        {
            return (from node in Xdoc.Descendants("node") where node.Attribute("id").Value == id select node).FirstOrDefault();
        }

        /// <summary>
        /// Candidate for memoization?
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private XElement FindWayById(string id)
        {
            return (from node in Xdoc.Descendants("way") where node.Attribute("id").Value == id select node).FirstOrDefault();
        }

        /// <summary>
        /// NATURAL
        /// Support for Natural like map elements
        /// TODO: 
        /// - Trees: rows, woods.
        /// - other naturals
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Natural> Natural()
        {
            var nodes = (from e in Xdoc.Descendants("node")
                        where e.Attribute("visible").Value == "true"
                        from t in e.Descendants("tag")
                        where t.Attribute("k").Value == "natural" && t.Attribute("v").Value == "tree"
                        select new
                        {
                            Id = e.Attribute("id").Value,
                            Name = t.Attribute("v").Value,
                            Lat = Convert.ToDouble(e.Attribute("lat").Value),
                            Long = Convert.ToDouble(e.Attribute("lon").Value),
                            Element = e
                        });

            foreach (var node in nodes)
            {
                NaturalData data = default(NaturalData);
                data.circumference = DefaultTreeCircumference;
                data.height = DefaultTreeHeight;
                foreach (var tag in node.Element.Descendants("tag"))
                {
                    switch (tag.Attribute("k").Value.ToLower())
                    {
                        case "genus":
                            data.genus = tag.Attribute("v").Value;
                            break;
                        case "leaf_type":
                            data.leaf_type = tag.Attribute("v").Value;
                            break;
                        case "circumference":
                            MatchFloat(tag.Attribute("v").Value, out data.circumference);
                            break;
                        case "height":
                            MatchFloat(tag.Attribute("v").Value, out data.height);
                            break;
                    }
                }
                var pos = MercatorHelper.ToMeters(node.Lat, node.Long);
                yield return new Natural(node.Id, new Vector2(pos.X - Tile.Center.X, pos.Y - Tile.Center.Y), data);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<BuildingPart> Buildings()
        {
            ways.Clear();
            var compounds = ParseCompounds();

            // Build first multipolygon building parts of complex buildings
            foreach (var building in Compounds(compounds, CompoundType.MultipolygonBuildingPart))
            {
                yield return building;
            }

            // Second are remaining multipolygon of complex buildings
            foreach (var building in Compounds(compounds, CompoundType.MultipolygonBuilding))
            {
                yield return building;
            }

            // Third are remaining simpler parts of complex buildings
            foreach (var building in Compounds(compounds, CompoundType.Building))
            {
                yield return building;
            }

            // Fourth remaining parts of simple buildings
            foreach (var building in ParseSimples())
            {
                yield return new BuildingPart(building.Id, building.Polygon, ParsePart(building.Data), ParseRoof(building.Data), building.Color);
            }

#if DEBUG
            Console.Clear();
            var count = 0;
            foreach (var way in Xdoc.Descendants("way"))
            {
                var id = way.Attribute("id").Value;
                if (way.Attribute("visible").Value != "true")
                {
                    continue;
                }

                if (ways.Contains(id))
                {
                    continue;
                }

                var build = from tag in way.Descendants("tag")
                                 where tag.Attribute("k").Value == "building" || tag.Attribute("k").Value == "building:part"
                                 select tag;

                if (build.Any())
                {
                    yield return new BuildingPart(id, CreateFootprint(way), ParsePart(way), ParseRoof(way), Color.Red);
                    count++;
                    Console.WriteLine("Way {0}: {1} = {2}", id, build.First().Attribute("k").Value, build.First().Attribute("v").Value);
                }
            }
            Console.WriteLine("{0}\nTotal remaining building structures = {1}", new String('-', 40), count);
#endif
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private IEnumerable<Simple> ParseSimple(string key, Color color)
        {
            foreach (var way in Xdoc.Descendants("way"))
            {
                var id = way.Attribute("id").Value;

                if (way.Attribute("visible").Value != "true")
                {
                    continue;
                }

                if (ways.Contains(id))
                {
                    continue;
                }

                var isKey = (from tag in way.Descendants("tag")
                              where tag.Attribute("k").Value == key && tag.Attribute("v").Value != "no"
                              select tag).Any();

                if (isKey)
                {
                    ways.Add(id);
                    yield return new Simple(id, way, CreateFootprint(way), color);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Simple> ParseSimples()
        {
            var parts = new List<Simple>();
            foreach (var part in ParseSimple("building:part", Color.LightBlue))
            {
                yield return part;
                parts.Add(part);
            }

            foreach (var building in ParseSimple("building", Color.Blue))
            {
                var ignore = false;
//                var poly = new Polygon(building.Polygon.Points);
                foreach (var part in parts)
                {
                    var inter = building.Polygon.Intersects(part.Polygon);
                    if (inter != 0)
                    {
                        ignore = true;
                        break;
                    }
/*
                    var inter = poly.Intersects(part.Polygon);
                    if (inter < 0)
                    {
                        ignore = true;
                        break;
                    }
                    else if (inter > 0)
                    {
                        var diff = poly.Clip(part.Polygon, PolygonOps.Difference);
                        if (diff != null)
                        {
                            poly.Points = diff;
                        }
                    }
 */
                }
                if (!ignore)
                {
//                    building.Polygon = poly;
                    yield return building;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, Compound> ParseCompounds()
        {
            var compounds = new Dictionary<string, Compound>();
            foreach (var relation in Xdoc.Descendants("relation"))
            {
                if (relation.Attribute("visible").Value != "true")
                {
                    continue;
                }

                var id = relation.Attribute("id").Value;

                var type = (from tag in relation.Descendants("tag")
                            where tag.Attribute("k").Value == "type"
                            select tag.Attribute("v").Value).FirstOrDefault();

                switch (type)
                {
                    case "building":
                        compounds.Add(id, new Compound(CompoundType.Building, relation));
                        break;

                    case "multipolygon":
                        {
                            var subtype = (from tag in relation.Descendants("tag")
                                           where tag.Attribute("k").Value == "building" || tag.Attribute("k").Value == "building:part"
                                           select tag.Attribute("k").Value).FirstOrDefault();

                            switch (subtype)
                            {
                                case "building":
                                    compounds.Add(id, new Compound(CompoundType.MultipolygonBuilding, relation));
                                    break;

                                case "building:part":
                                    compounds.Add(id, new Compound(CompoundType.MultipolygonBuildingPart, relation));
                                    break;
                            }
                        }
                        break;

                    default:
                        break;
                }
            }
            return compounds;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compound"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private IEnumerable<BuildingPart> Compound(Compound compound, Dictionary<string, Compound> compounds, CompoundType type)
        {
            switch (type)
            {
                case CompoundType.MultipolygonBuilding:
                case CompoundType.MultipolygonBuildingPart:
                    foreach (var part in Multipolygon(compound, type))
                    {
                        yield return part;
                    }
                    break;

                case CompoundType.Building:
                    foreach (var part in Parts(compound))
                    {
                        yield return part;
                    }
                    break;
            }

            foreach (var relation in compound.Relations)
            {
                Compound newCompound;
                if (compounds.TryGetValue(relation.Id, out newCompound))
                {
                    foreach (var part in Compound(newCompound, compounds, type))
                    {
                        yield return part;
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compounds"></param>
        /// <returns></returns>
        private IEnumerable<BuildingPart> Compounds(Dictionary<string, Compound> compounds, CompoundType type)
        {
            foreach (var compound in compounds.Values)
            {
                if (compound.Type == type)
                {
                    foreach (var part in Compound(compound, compounds, type))
                    {
                        yield return part;
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compound"></param>
        /// <returns></returns>
        IEnumerable<BuildingPart> Parts(Compound compound)
        {
            var partData = ParsePart(compound.Data);
            var roofData = ParseRoof(compound.Data);
            foreach (var part in compound.Structures)
            {
                if (part.Role != PartRole.Part)
                {
                    continue;
                }

                if (ways.Contains(part.Id))
                {
                    continue;
                }

                var way = FindWayById(part.Id);
                if (way == null)
                {
                    continue;
                }

                part.Unassigned = false;
                ways.Add(part.Id);
                var newPartData = partData;
                Parse(way, ref newPartData);
                var newRoofData = roofData;
                Parse(way, ref newRoofData);
                yield return new BuildingPart(part.Id, CreateFootprint(way), newPartData, newRoofData, Color.Green);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="compound"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        IEnumerable<BuildingPart> Multipolygon(Compound compound, CompoundType type)
        {
            var partData = ParsePart(compound.Data);
            var roofData = ParseRoof(compound.Data);
            // Merge and combine Outer and Inner parts
            List<Polygon> inners = new List<Polygon>();
            foreach (var structure in Merge(compound, PartRole.Inner))
            {
                inners.Add(structure.Polygon);
            }

            foreach (var structure in Merge(compound, PartRole.Outer))
            {
                var newPartData = partData;
                Parse(structure.Data, ref newPartData);
                var newRoofData = roofData;
                Parse(structure.Data, ref newRoofData);
                var outer = structure.Polygon as MultiPolygon;
                foreach (var inner in inners)
                {
                    if (outer.Intersects(inner) < 0)
                    {
                        outer.Holes.Add(inner);
                    }
                }
                yield return new BuildingPart(structure.Id, outer, newPartData, newRoofData, Color.LightGreen);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="compound"></param>
        /// <returns></returns>
        private IEnumerable<Structure> Merge(Compound compound, PartRole role)
        {
            /// Ring assignments
            while (true)
            {
                // For each unassigned structure
                var structure = (from node in compound.Structures
                                 where node.Unassigned && node.Role == role
                                 select node).FirstOrDefault();

                if (structure == null)
                {
                    break;
                }

                if (ways.Contains(structure.Id))
                {
                    break;
                }

                var way = FindWayById(structure.Id);
                if (way == null)
                {
                    break;
                }
                ways.Add(structure.Id);

                // Merge other fitting structures 
                structure.Unassigned = false;
                MultiPolygon ring = new MultiPolygon(CreateFootprint(way).Points);
                while (true)
                {
                    if (ring.IsClosed())
                    {
                        // Ring is closed, return merged structure
                        structure.Data = way;
                        structure.Polygon = ring;
                        yield return structure;
                        break;
                    }
                    else
                    {
                        Vector2 first = ring.Points.First();
                        Vector2 last = ring.Points.Last();
                        foreach (var str in compound.Structures)
                        {
                            if (!str.Unassigned || str.Role != role)
                            {
                                continue;
                            }

                            var w = FindWayById(str.Id);
                            if (w == null)
                            {
                                continue;
                            }
                            ways.Add(str.Id);

                            var poly = CreateFootprint(w);

                            if (last == poly.Points.First())
                            {
                                ring.Points.Remove(last);
                                ring.Points.AddRange(poly.Points);
                                str.Unassigned = false;
                                break;
                            }
                            else if (last == poly.Points.Last())
                            {
                                var newPoly = poly.Points.Reverse<Vector2>().ToList();
                                ring.Points.Remove(last);
                                ring.Points.AddRange(newPoly);
                                str.Unassigned = false;
                                break;
                            }
                            else if (first == poly.Points.First())
                            {
                                ring.Points.Remove(first);
                                var oldRing = ring;
                                ring = new MultiPolygon(poly.Points.Reverse<Vector2>().ToList());
                                ring.Points.AddRange(oldRing.Points);
                                str.Unassigned = false;
                                break;
                            }
                            else if (first == poly.Points.Last())
                            {
                                ring.Points.Remove(first);
                                var oldRing = ring;
                                ring = new MultiPolygon(poly.Points);
                                ring.Points.AddRange(oldRing.Points);
                                str.Unassigned = false;
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="way"></param>
        /// <returns></returns>
        private Polygon CreateFootprint(XElement way)
        {
            var footprint = new Polygon();
            foreach (var elt in way.Descendants("nd"))
            {
                // find node that matches this ways' nodes ref
                var node = FindNodeById(elt.Attribute("ref").Value);
                if (node != null)
                {
                    var pos = MercatorHelper.ToMeters(
                        Convert.ToDouble(node.Attribute("lat").Value),
                        Convert.ToDouble(node.Attribute("lon").Value));
                    footprint.Points.Add(new Vector2(pos.X - Tile.Center.X, pos.Y - Tile.Center.Y));
                }
            }
            return footprint;
        }

        /// <summary>
        /// Parse building data
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected PartData ParsePart(XElement node)
        {
            // Part data in node
            PartData part = default(PartData);
            part.volume = true;
            part.shape = BuildingShape.None;
            part.surface = default(Surface);
            part.surface.color = DefaultBuildingColor;
            Parse(node, ref part);
            return part;
        }

        /// <summary>
        /// Parse roof data in node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected RoofData ParseRoof(XElement node)
        {
            // roof data
            RoofData roof = default(RoofData);
            roof.shape = RoofShape.None;
            roof.surface = default(Surface);
            roof.surface.color = DefaultRoofColor;
            Parse(node, ref roof);
            return roof;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        private string filter(string tag)
        {
            // TODO match building:*:colour instead with regex
            // TODO match building:*:material instead with regex
            // TODO match building:*:shape instead with regex
            return tag.ToLower();
        }

        /// <summary>
        /// Parse Part data ref
        /// </summary>
        /// <param name="node"></param>
        /// <param name="part"></param>
        protected void Parse(XElement node, ref PartData part)
        {
            foreach (var tag in node.Descendants("tag"))
            {
                switch (filter(tag.Attribute("k").Value))
                {
                    case "name":
                        part.name = tag.Attribute("v").Value;
                        break;

                    case "height":
                    case "building:height":
                        MatchFloat(tag.Attribute("v").Value, out part.height);
                        break;

                    case "min_height":
                    case "building:min_height":
                        MatchFloat(tag.Attribute("v").Value, out part.min_height);
                        break;

                    case "levels":
                    case "building:levels":
                    case "building:levels:aboveground":
                        MatchShort(tag.Attribute("v").Value, out part.levels);
                        break;

                    case "min_levels":
                    case "building:min_levels":
                        MatchShort(tag.Attribute("v").Value, out part.min_level);
                        break;

                    case "building:colour":
                    case "building:facade:colour":
                        part.surface.color = MatchColor(tag.Attribute("v").Value);
                        break;

                    case "building:material":
                        part.surface.material = tag.Attribute("v").Value;
                        break;

                    case "building:shape":
                        Enum.TryParse(tag.Attribute("v").Value, true, out part.shape);
                        break;
                }
            }

            // Height estimation rule
            if (part.volume && part.height == 0)
            {
                var levels = part.min_level + part.levels;
                if (levels > 0)
                {
                    part.height = levels * DefaultFloorHeight;
                }
                else
                {
                    var rnd = new Random();
                    part.height = rnd.Next((int)(DefaultBuildingHeight - DefaultFloorHeight), (int)(DefaultBuildingHeight + DefaultFloorHeight));
                }
            }
        }

        /// <summary>
        /// Parse Roof data ref
        /// </summary>
        /// <param name="node"></param>
        /// <param name="roof"></param>
        protected void Parse(XElement node, ref RoofData roof)
        {
            foreach (var tag in node.Descendants("tag"))
            {
                switch (tag.Attribute("k").Value.ToLower())
                {
                    case "roof:shape":
                        Enum.TryParse(tag.Attribute("v").Value, true, out roof.shape);
                        break;

                    case "roof:colour":
                        roof.surface.color = MatchColor(tag.Attribute("v").Value);
                        break;

                    case "roof:material":
                        roof.surface.material = tag.Attribute("v").Value;
                        break;

                    // TODO: other roof data
                }
            }
        }

        /// <summary>
        /// Match ARGB color code or OSM color name to Color
        /// </summary>
        /// <param name="colorStr"></param>
        /// <returns></returns>
        internal static Color MatchColor(string str)
        {
            if (str.StartsWith("#"))
            {
                return (Color)ColorTranslator.FromHtml(str);
            }
            else
            {
                string keyword = Regex.Replace(str, " ", "").ToLower();
                if (OsmColor.Keywords.ContainsKey(keyword))
                {
                    return OsmColor.Keywords[keyword];
                }
            }
            return DefaultBuildingColor;
        }

        /// <summary>
        /// Convert string expressions to float value
        /// </summary>
        /// <param name="exp"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static bool MatchFloat(string exp, out float value)
        {
            if (exp == null)
            {
                value = 0.0f;
                return false;
            }
            var exp2 = Regex.Replace(exp, @"[^0-9-,.]", "");
            var result = float.TryParse(exp2, NumberStyles.Any, CultureInfo.GetCultureInfo("fr-FR"), out value);
            if (!result)
            {
                result = float.TryParse(exp2, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
            }
            return result;
        }

        /// <summary>
        /// Convert string expressions to short value
        /// </summary>
        /// <param name="exp"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static bool MatchShort(string exp, out short value)
        {
            return short.TryParse(Regex.Replace(exp, @"[^0-9-]", ""), out value);
        }

        /// <summary>
        /// Request handles URI loading
        /// </summary>
        /// <param name="requestUrl"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        protected XDocument Request(string requestUrl, out string err)
        {
            // TODO: retry if connection aborts
            err = "";
            try
            {
                var xdoc = XDocument.Load(requestUrl);
                return (xdoc);
            }
            catch (WebException ex)
            {
                // TODO better process error status code with details
                err = ex.Message;
                return null;
            }
            catch (Exception ex)
            {
                err = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// Load reads map information from a xml file
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool Load(string filename)
        {
            Xdoc = XDocument.Load(filename);
            if (!Xdoc.Equals(null))
            {
                double left = Convert.ToDouble(Xdoc.Root.Element("bounds").Attribute("minlon").Value);
                double top = Convert.ToDouble(Xdoc.Root.Element("bounds").Attribute("maxlat").Value);
                double right = Convert.ToDouble(Xdoc.Root.Element("bounds").Attribute("maxlon").Value);
                double bottom = Convert.ToDouble(Xdoc.Root.Element("bounds").Attribute("minlat").Value);
                Tile = new TileBound(MercatorHelper.ToMeters(top, left), MercatorHelper.ToMeters(bottom, right));
                return true;
            }
            return false;
        }

        /// <summary>
        /// BBox gets map information for the geo location
        /// </summary>
        /// <param name="left"></param>
        /// <param name="bottom"></param>
        /// <param name="right"></param>
        /// <param name="top"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public bool BBox(double left, double bottom, double right, double top, out string err)
        {
            var mapRequest = OsmMapUrl + "bbox=" + left.ToString() + "," + bottom.ToString() + "," + right.ToString() + "," + top.ToString();
            Xdoc = Request(mapRequest, out err);
            if (Xdoc != null)
            {
                if (absoluteAnchor)
                {
                    Tile = new TileBound(MercatorHelper.ToMeters(top, left), MercatorHelper.ToMeters(bottom, right));
                    absoluteAnchor = false;
                }
                return true;
            }
            return false;
        }

        public OsmProvider()
        {
            absoluteAnchor = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
            }
            // free native resources if there are any.
        }
    }
}
