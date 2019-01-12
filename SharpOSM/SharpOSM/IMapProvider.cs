using System;
using System.Collections.Generic;

namespace SharpOSM
{
    interface IMapProvider : IDisposable
    {
        IEnumerable<Natural> Natural();
        IEnumerable<BuildingPart> Buildings();
        bool Load(string filename);
        bool BBox(double left, double bottom, double right, double top, out string err);
    }
}
