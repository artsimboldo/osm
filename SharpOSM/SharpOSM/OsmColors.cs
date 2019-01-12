using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Reflection;

namespace SharpOSM
{
    /// <summary>
    /// Random color generator
    /// </summary>
    class RandomColorSelector
    {
        static readonly Color[] Colors =
            typeof(Color).GetProperties(BindingFlags.Public | BindingFlags.Static)
           .Select(propInfo => propInfo.GetValue(null, null))
           .Cast<Color>()
           .ToArray();

        static readonly string[] ColorNames =
            typeof(Color).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Select(propInfo => propInfo.Name)
            .ToArray();

        private Random rand = new Random();

        public Color GetRandomColor()
        {
            return Colors[rand.Next(0, Colors.Length)];
        }

        public string GetRandomColorName()
        {
            return ColorNames[rand.Next(0, Colors.Length)];
        }
    }

    /// <summary>
    /// Key value table for OSM keywords
    /// See http://wiki.openstreetmap.org/wiki/Key:colour
    /// </summary>
    public static class OsmColor
    {
        public static Dictionary<string, Color> Keywords = new Dictionary<string, Color>() 
        {
            {"aliceblue", ColorTranslator.FromHtml("#F0F8FF")},
            {"antiquewhite", ColorTranslator.FromHtml("#FAEBD7")},
            {"aqua", ColorTranslator.FromHtml("#00FFFF")},
        	{"aquamarine", ColorTranslator.FromHtml("#7FFFD4")},
	        {"azure", ColorTranslator.FromHtml("#F0FFFF")},
			{"beige", ColorTranslator.FromHtml("#F5F5DC")},
			{"bisque", ColorTranslator.FromHtml("#FFE4C4")},
			{"black", ColorTranslator.FromHtml("#000000")},
			{"blanchedalmond", ColorTranslator.FromHtml("#FFEBCD")},
			{"blue", ColorTranslator.FromHtml("#0000FF")},
			{"blueviolet", ColorTranslator.FromHtml("#8A2BE2")},
			{"brown", ColorTranslator.FromHtml("#A52A2A")},
			{"burlywood", ColorTranslator.FromHtml("#DEB887")},
			{"cadetblue", ColorTranslator.FromHtml("#5F9EA0")},
			{"chartreuse", ColorTranslator.FromHtml("#7FFF00")},
			{"chocolate", ColorTranslator.FromHtml("#D2691E")},
			{"coral", ColorTranslator.FromHtml("#FF7F50")},
			{"cornflowerblue", ColorTranslator.FromHtml("#6495ED")},	
			{"cornsilk", ColorTranslator.FromHtml("#FFF8DC")},
			{"crimson", ColorTranslator.FromHtml("#DC143C")},
			{"cyan", ColorTranslator.FromHtml("#00FFFF")},
			{"darkblue", ColorTranslator.FromHtml("#00008B")},	
			{"darkcyan", ColorTranslator.FromHtml("#008B8B")},
			{"darkgoldenrod", ColorTranslator.FromHtml("#B8860B")},	
			{"darkgray", ColorTranslator.FromHtml("#A9A9A9")},
			{"darkgreen", ColorTranslator.FromHtml("#006400")},
			{"darkgrey", ColorTranslator.FromHtml("#A9A9A9")},	
			{"darkkhaki", ColorTranslator.FromHtml("#BDB76B")},
			{"darkmagenta", ColorTranslator.FromHtml("#8B008B")},	
			{"darkolivegreen", ColorTranslator.FromHtml("#556B2F")},	
			{"darkorange", ColorTranslator.FromHtml("#FF8C00")},
			{"darkorchid", ColorTranslator.FromHtml("#9932CC")},
			{"darkred", ColorTranslator.FromHtml("#8B0000")},
			{"darksalmon", ColorTranslator.FromHtml("#E9967A")},	
			{"darkseagreen", ColorTranslator.FromHtml("#8FBC8F")},
			{"darkslateblue", ColorTranslator.FromHtml("#483D8B")},	
			{"darkslategray", ColorTranslator.FromHtml("#2F4F4F")},	
			{"darkslategrey", ColorTranslator.FromHtml("#2F4F4F")},	
			{"darkturquoise", ColorTranslator.FromHtml("#00CED1")},	
			{"darkviolet", ColorTranslator.FromHtml("#9400D3")},	
			{"deeppink", ColorTranslator.FromHtml("#FF1493")},
            {"deepskyblue", ColorTranslator.FromHtml("#00BFFF")},
			{"dimgray", ColorTranslator.FromHtml("#696969")},
			{"dimgrey", ColorTranslator.FromHtml("#696969")},
			{"dodgerblue", ColorTranslator.FromHtml("#1E90FF")},
			{"firebrick", ColorTranslator.FromHtml("#B22222")},
			{"floralwhite", ColorTranslator.FromHtml("#FFFAF0")},
			{"forestgreen", ColorTranslator.FromHtml("#228B22")},
			{"fuchsia", ColorTranslator.FromHtml("#FF00FF")},
			{"gainsboro", ColorTranslator.FromHtml("#DCDCDC")},
			{"ghostwhite", ColorTranslator.FromHtml("#F8F8FF")},
			{"gold", ColorTranslator.FromHtml("#FFD700")},
			{"goldenrod", ColorTranslator.FromHtml("#DAA520")},
			{"gray", ColorTranslator.FromHtml("#808080")},
			{"green", ColorTranslator.FromHtml("#008000")},
			{"greenyellow", ColorTranslator.FromHtml("#ADFF2F")},
			{"grey", ColorTranslator.FromHtml("#808080")},
			{"honeydew", ColorTranslator.FromHtml("#F0FFF0")},
			{"hotpink", ColorTranslator.FromHtml("#FF69B4")},
			{"indianred", ColorTranslator.FromHtml("#CD5C5C")},
			{"indigo", ColorTranslator.FromHtml("#4B0082")},
			{"ivory", ColorTranslator.FromHtml("#FFFFF0")},
			{"khaki", ColorTranslator.FromHtml("#F0E68C")},
			{"lavender", ColorTranslator.FromHtml("#E6E6FA")},
			{"lavenderblush", ColorTranslator.FromHtml("#FFF0F5")},
			{"lawngreen", ColorTranslator.FromHtml("#7CFC00")},
			{"lemonchiffon", ColorTranslator.FromHtml("#FFFACD")},
			{"lightblue", ColorTranslator.FromHtml("#ADD8E6")},
			{"lightcoral", ColorTranslator.FromHtml("#F08080")},
			{"lightcyan", ColorTranslator.FromHtml("#E0FFFF")},
			{"lightgoldenrodyellow", ColorTranslator.FromHtml("#FAFAD2")},
			{"lightgray", ColorTranslator.FromHtml("#D3D3D3")},
			{"lightgreen", ColorTranslator.FromHtml("#90EE90")},
			{"lightgrey", ColorTranslator.FromHtml("#D3D3D3")},
			{"lightpink", ColorTranslator.FromHtml("#FFB6C1")},
			{"lightsalmon", ColorTranslator.FromHtml("#FFA07A")},
			{"lightseagreen", ColorTranslator.FromHtml("#20B2AA")},
			{"lightskyblue", ColorTranslator.FromHtml("#87CEFA")},
			{"lightslategray", ColorTranslator.FromHtml("#778899")},
			{"lightslategrey", ColorTranslator.FromHtml("#778899")},
			{"lightsteelblue", ColorTranslator.FromHtml("#B0C4DE")},
			{"lightyellow", ColorTranslator.FromHtml("#FFFFE0")},
			{"lime", ColorTranslator.FromHtml("#00FF00")},
			{"limegreen", ColorTranslator.FromHtml("#32CD32")},
			{"linen", ColorTranslator.FromHtml("#FAF0E6")},
			{"magenta", ColorTranslator.FromHtml("#FF00FF")},
			{"maroon", ColorTranslator.FromHtml("#800000")},
			{"mediumaquamarine", ColorTranslator.FromHtml("#66CDAA")},
			{"mediumblue", ColorTranslator.FromHtml("#0000CD")},
			{"mediumorchid", ColorTranslator.FromHtml("#BA55D3")},
			{"mediumpurple", ColorTranslator.FromHtml("#9370DB")},
			{"mediumseagreen", ColorTranslator.FromHtml("#3CB371")},
			{"mediumslateblue", ColorTranslator.FromHtml("#7B68EE")},
			{"mediumspringgreen", ColorTranslator.FromHtml("#00FA9A")},
			{"mediumturquoise", ColorTranslator.FromHtml("#48D1CC")},
			{"mediumvioletred", ColorTranslator.FromHtml("#C71585")},
			{"midnightblue", ColorTranslator.FromHtml("#191970")},
			{"mintcream", ColorTranslator.FromHtml("#F5FFFA")},
			{"mistyrose", ColorTranslator.FromHtml("#FFE4E1")},
			{"moccasin", ColorTranslator.FromHtml("#FFE4B5")},
			{"navajowhite", ColorTranslator.FromHtml("#FFDEAD")},
			{"navy", ColorTranslator.FromHtml("#000080")},
			{"oldlace", ColorTranslator.FromHtml("#FDF5E6")},
			{"olive", ColorTranslator.FromHtml("#808000")},
			{"olivedrab", ColorTranslator.FromHtml("#6B8E23")},
			{"orange", ColorTranslator.FromHtml("#FFA500")},
			{"orangered", ColorTranslator.FromHtml("#FF4500")},
			{"orchid", ColorTranslator.FromHtml("#DA70D6")},
			{"palegoldenrod", ColorTranslator.FromHtml("#EEE8AA")},
			{"palegreen", ColorTranslator.FromHtml("#98FB98")},
			{"paleturquoise", ColorTranslator.FromHtml("#AFEEEE")},
			{"palevioletred", ColorTranslator.FromHtml("#DB7093")},
			{"papayawhip", ColorTranslator.FromHtml("#FFEFD5")},
			{"peachpuff", ColorTranslator.FromHtml("#FFDAB9")},
			{"peru", ColorTranslator.FromHtml("#CD853F")},
			{"pink", ColorTranslator.FromHtml("#FFC0CB")},
			{"plum", ColorTranslator.FromHtml("#DDA0DD")},
			{"powderblue", ColorTranslator.FromHtml("#B0E0E6")},
			{"purple", ColorTranslator.FromHtml("#800080")},
			{"red", ColorTranslator.FromHtml("#FF0000")},
			{"rosybrown", ColorTranslator.FromHtml("#BC8F8F")},
			{"royalblue", ColorTranslator.FromHtml("#4169E1")},
			{"saddlebrown", ColorTranslator.FromHtml("#8B4513")},
			{"salmon", ColorTranslator.FromHtml("#FA8072")},
			{"sandybrown", ColorTranslator.FromHtml("#F4A460")},
			{"seagreen", ColorTranslator.FromHtml("#2E8B57")},
			{"seashell", ColorTranslator.FromHtml("#FFF5EE")},
			{"sienna", ColorTranslator.FromHtml("#A0522D")},
			{"silver", ColorTranslator.FromHtml("#C0C0C0")},
			{"skyblue", ColorTranslator.FromHtml("#87CEEB")},
			{"slateblue", ColorTranslator.FromHtml("#6A5ACD")},
			{"slategray", ColorTranslator.FromHtml("#708090")},
			{"slategrey", ColorTranslator.FromHtml("#708090")},
			{"snow", ColorTranslator.FromHtml("#FFFAFA")},
			{"springgreen", ColorTranslator.FromHtml("#00FF7F")},
			{"steelblue", ColorTranslator.FromHtml("#4682B4")},
			{"tan", ColorTranslator.FromHtml("#D2B48C")},
			{"teal", ColorTranslator.FromHtml("#008080")},
			{"thistle", ColorTranslator.FromHtml("#D8BFD8")},
			{"tomato", ColorTranslator.FromHtml("#FF6347")},
			{"turquoise", ColorTranslator.FromHtml("#40E0D0")},
			{"violet", ColorTranslator.FromHtml("#EE82EE")},
			{"wheat", ColorTranslator.FromHtml("#F5DEB3")},
			{"white", ColorTranslator.FromHtml("#FFFFFF")},
			{"whitesmoke", ColorTranslator.FromHtml("#F5F5F5")},
			{"yellow", ColorTranslator.FromHtml("#FFFF00")},
			{"yellowgreen", ColorTranslator.FromHtml("#9ACD32")}
        };
    }
}
