using System;
using System.Linq;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Diagnostics;
using XnaGeometry;

namespace SharpOSM
{
    public class SharpOsm : IDisposable
    {
        private const string OsmMapUrl = "http://api.openstreetmap.org/api/0.6/map?";
        protected XDocument Xdoc { get; private set; }
        public Cache<string, BuildingPart> Cache { get; private set; }

        private Func<XElement, string> Name = (elt) =>
        {
            return (from e in elt.Descendants("tag") where (string)e.Attribute("k") == "name" select (string)e.Attribute("v")).FirstOrDefault();
        };

        public TileBound Tile { get; private set; }

//        public IEnumerable<Building> Buildings()
        private void Buildings()
        {
            var ways = (from e in Xdoc.Descendants("way")
                        where !Cache.Exist((string)e.Attribute("id"))
                        where (string)e.Attribute("visible") == "true"
                        from t in e.Descendants("tag")
                        // warning: some nodes could contain both k,v pairs, use Distinct() since we don't want key duplicates
                        where (string)t.Attribute("k") == "building" || (string)t.Attribute("k") == "building:part"
                        select new
                        {
                            Key = (string)e.Attribute("id"),
                            Value = e
                        }).Distinct().ToDictionary(pair => pair.Key, pair => pair.Value);

            var relations = from e in Xdoc.Descendants("relation")
                            where (string)e.Attribute("visible") == "true"
                            from t in e.Descendants("tag")
                            where (string)t.Attribute("k") == "type" && (string)t.Attribute("v") == "building"
                            select new
                            {
                                Name = Name(e),
                                Element = e
                            };

            // Process building parts from relations
            foreach (var rel in relations)
            {
                var building = new Building(rel.Name);
                XElement way;
                foreach (var m in rel.Element.Descendants("member"))
                {
                    if ((string)m.Attribute("type") == "way")
                    {
                        string id = (string)m.Attribute("ref");
                        if (ways.TryGetValue(id, out way))
                        {
                            var part = BuildPart(way, (string)m.Attribute("role") == "part" ? true : false);
                            building.Parts.Add(part);
                            ways.Remove(id);
                            Cache.AddOrUpdate(id, part);
                        }
                    }
                }
//                yield return building;
            }

            // Process remaining standalone building
            foreach (var way in ways)
            {
                var part = BuildPart(way.Value);
                Cache.AddOrUpdate(way.Key, part);
//                yield return new Building(Name(way.Value), part);
            }
        }

        public BuildingPart BuildPart(XElement way, bool volume = true)
        {
            var tags = way.Descendants("tag")
                .ToDictionary(
                tag => tag.Attribute("k").Value,  // add key
                tag => tag.Attribute("v").Value); // add value

            // Building creation
            var buildingPart = new BuildingPart(
                tags.ContainsKey("name") ? tags["name"] : "",
                Convert.ToInt64(way.Attribute("id").Value));

            // Outline construction
            // TODO check if closed?
            foreach (var elt in way.Descendants("nd"))
            {
                // find node that matches ways' node ref
                var query = (from node in Xdoc.Descendants("node")
                             where (string)node.Attribute("id") == (string)elt.Attribute("ref")
                             select node).First();

                var pos = Mercator.ToMeters(Convert.ToDouble(query.Attribute("lat").Value), Convert.ToDouble(query.Attribute("lon").Value));
                var x = pos.X - Tile.Center.X;
                var y = pos.Y - Tile.Center.Y;
                buildingPart.Outline.Add(new Vector2(x, y));
            }

            // Part data
            PartData part;
            part.volume = volume;
            part.height = float.Parse(tags.ContainsKey("height") ? Regex.Match(tags["height"], @"\d+").Value : "-1", CultureInfo.InvariantCulture);
            part.min_height = float.Parse(tags.ContainsKey("min_height") ? Regex.Match(tags["min_height"], @"\d+").Value : "-1", CultureInfo.InvariantCulture);
            part.levels = Convert.ToInt16(tags.ContainsKey("building:levels") ? tags["building:levels"] : "-1");
            part.min_level = Convert.ToInt16(tags.ContainsKey("building:min_level") ? tags["building:min_level"] : "-1");
            buildingPart.Data = part;

            Surface surf;
            surf.colour = tags.ContainsKey("building:colour") ? tags["building:colour"] : "";
            surf.material = tags.ContainsKey("building:material") ? tags["building:material"] : "";
            buildingPart.PartSurface = surf;

            return buildingPart;
        }

        protected XDocument MakeRequest(string requestUrl)
        {
            // TODO: retry if connection aborts
            try
            {
                var xdoc = XDocument.Load(requestUrl);
                // TODO: check version API
                return (xdoc);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.Read();
                return null;
            }
        }

        public bool BBox(double left, double bottom, double right, double top)
        {
            var mapRequest = OsmMapUrl + "bbox=" + left.ToString() + "," + bottom.ToString() + "," + right.ToString() + "," + top.ToString();
            Xdoc = MakeRequest(mapRequest);
            if (!Xdoc.Equals(null))
            {
                Tile = new TileBound(Mercator.ToMeters(top, left), Mercator.ToMeters(bottom, right));
                Buildings();
                return true;
            }
            return false;
        }

        public SharpOsm()
        {
            Cache = new Cache<string, BuildingPart>(1000);
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
