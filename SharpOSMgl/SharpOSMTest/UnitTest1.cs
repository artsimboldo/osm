using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOSM;
using System.Text.RegularExpressions;
using System.Globalization;

namespace SharpOSMTest
{
    [TestClass]
    public class UnitTest1
    {
/*
        [TestMethod]
        public void TestMethod1()
        {
            Cache<int, string> cache = new Cache<int, string>(5);
            cache.AddOrUpdate(1, "1");
            cache.AddOrUpdate(2, "2");
            cache.AddOrUpdate(3, "3");
            cache.AddOrUpdate(4, "4");
            cache.AddOrUpdate(5, "5");
            // Should evict "1"
            cache.AddOrUpdate(6, "6");
            Assert.AreEqual(cache.Count, 5);
            Assert.IsFalse(cache.Exist(1));
            Assert.IsTrue(cache.Exist(6));
        }

        [TestMethod]
        public void TestMethod2()
        {
            Cache<int, string> cache = new Cache<int, string>(2);
            cache.AddOrUpdate(1, "1");
            cache.AddOrUpdate(2, "2");
            // Should update 2 to "3" and put "3" as first
            cache.AddOrUpdate(2, "3");
            Assert.AreEqual(cache.Count, 2);
            Assert.AreEqual(cache.First.Value.Value, "3");
        }
*/

        internal static bool MatchFloat(string exp, out float value)
        {
            var exp2 = Regex.Replace(exp, @"[^0-9-,.]", "");
            var result = float.TryParse(exp2, NumberStyles.Any, CultureInfo.GetCultureInfo("fr-FR"), out value);
            if (!result)
            {
                result = float.TryParse(exp2, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
            }
            return result;
        }

        [TestMethod]
        public void TestMethod3()
        {
            float value;
            Assert.IsTrue(MatchFloat("0.1", out value));
            Assert.AreEqual(value, 0.1, 1e-6);
            Assert.IsTrue(MatchFloat("-1.0", out value));
            Assert.AreEqual(value, -1.0, 1e-6);
            Assert.IsTrue(MatchFloat("1 m", out value));
            Assert.AreEqual(value, 1.0, 1e-6);
            Assert.IsTrue(MatchFloat("0,1", out value));
            Assert.AreEqual(value, 0.1, 1e-6);
        }

        internal static bool MatchShort(string exp, out short value)
        {
            return short.TryParse(Regex.Replace(exp, @"[^0-9-]", ""), out value);
        }

        [TestMethod]
        public void TestMethod4()
        {
            short value;
            Assert.IsTrue(MatchShort("1", out value));
            Assert.AreEqual(value, 1);
            Assert.IsTrue(MatchShort("-1", out value));
            Assert.AreEqual(value, -1);
            Assert.IsTrue(MatchShort("1 m", out value));
            Assert.AreEqual(value, 1);
        }

        [TestMethod]
        public void TestMethod5()
        {
            Polygon p1 = new Polygon();
            p1.Points.Add(new Vector2(-10, -10));
            p1.Points.Add(new Vector2(10, -10));
            p1.Points.Add(new Vector2(10, 10));
            p1.Points.Add(new Vector2(-10, 10));

            Polygon p2 = new Polygon();
            p2.Points.Add(new Vector2(-5, -5));
            p2.Points.Add(new Vector2(5, -5));
            p2.Points.Add(new Vector2(5, 5));
            p2.Points.Add(new Vector2(-5, 5));

            Polygon p3 = new Polygon();
            p3.Points.Add(new Vector2(-5, -5));
            p3.Points.Add(new Vector2(15, -5));
            p3.Points.Add(new Vector2(15, 5));
            p3.Points.Add(new Vector2(-5, 5));

            Assert.IsTrue(p1.Intersects(p2) < 0);
            Assert.IsFalse(p2.Intersects(p1) < 0);
            Assert.IsFalse(p1.Intersects(p3) < 0);
        }
    }
}