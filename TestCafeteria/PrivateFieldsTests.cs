using Cafeteria;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestCafeteria
{
    [TestClass]
    public class PrivateFieldsTests
    {
        public class Target
        {
            private int x;
            public int y;

            private object obj;
        }

        public class TargetFields : PrivateFields<Target, TargetFields>
        {
            public IField<int> x { get; set; }
            public IField<int> y { get; set; }

            public IField<object> obj { get; set; }
        }

        [TestMethod]
        public void TestPublicField()
        {
            var target = new Target() { y = 7 };
            var targetFields = TargetFields.Of(target);

            Assert.AreEqual(targetFields.y.Value, 7);

            targetFields.y.Value = 12;

            Assert.AreEqual(target.y, 12);
        }

        [TestMethod]
        public void TestPrivateField()
        {
            var target = new Target();
            var targetFields = TargetFields.Of(target);

            targetFields.x.Value = 9;
            Assert.AreEqual(targetFields.x.Value, 9);
        }

        [TestMethod]
        public void TestRefField()
        {
            var target = new Target();
            var targetFields = TargetFields.Of(target);

            var expected = new object();
            targetFields.obj.Value = expected;
            Assert.AreEqual(targetFields.obj.Value, expected);
        }
    }
}
