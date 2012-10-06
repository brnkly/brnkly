using Brnkly.Framework.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Brnkly.Framework.UnitTests.Data
{
    [TestClass]
    public class RavenStoreTests
    {
        [TestMethod]
        public void Should_return_closest_read_replica()
        {
            var store = this.GetStore();

            Assert.AreEqual("dc1raven03", store.GetClosestReplica("dc1web01"));
            Assert.AreEqual("dc1raven02", store.GetClosestReplica("dc1web02"));
            Assert.AreEqual("dc1raven03", store.GetClosestReplica("dc1web03"));
            Assert.AreEqual("dc1raven02", store.GetClosestReplica("dc1web04"));
            Assert.AreEqual("dc1raven03", store.GetClosestReplica("dc1web05"));
            Assert.AreEqual("dc1raven02", store.GetClosestReplica("dc1web06"));
            Assert.AreEqual("dc1raven03", store.GetClosestReplica("dc1internal01"));
            Assert.AreEqual("dc1raven02", store.GetClosestReplica("dc1internal02"));

            Assert.AreEqual("dc2ravenXX", store.GetClosestReplica("dc2skywebXX"));
            Assert.AreEqual("dc2raven01", store.GetClosestReplica("dc2skyweb01"));
            Assert.AreEqual("dc2ravenXX", store.GetClosestReplica("dc2skyweb02"));
            Assert.AreEqual("dc2raven01", store.GetClosestReplica("dc2skyweb03"));
            Assert.AreEqual("dc2ravenXX", store.GetClosestReplica("dc2skyweb04"));
            Assert.AreEqual("dc2raven01", store.GetClosestReplica("dc2skyweb05"));
            Assert.AreEqual("dc2ravenXX", store.GetClosestReplica("dc2skyweb06"));
            Assert.AreEqual("dc2raven01", store.GetClosestReplica("dc2rsedit01"));
            Assert.AreEqual("dc2ravenXX", store.GetClosestReplica("dc2rsedit02"));
        }

        [TestMethod]
        public void Should_return_closest_write_replica()
        {
            var store = this.GetStore();

            Assert.AreEqual("dc1raven01", store.GetClosestReplica("dc1web01", true));
            Assert.AreEqual("dc1raven04", store.GetClosestReplica("dc1web02", true));
            Assert.AreEqual("dc1raven01", store.GetClosestReplica("dc1web03", true));
            Assert.AreEqual("dc1raven04", store.GetClosestReplica("dc1web04", true));
            Assert.AreEqual("dc1raven01", store.GetClosestReplica("dc1web05", true));
            Assert.AreEqual("dc1raven04", store.GetClosestReplica("dc1web06", true));
            Assert.AreEqual("dc1raven01", store.GetClosestReplica("dc1internal01", true));
            Assert.AreEqual("dc1raven04", store.GetClosestReplica("dc1internal02", true));
        }

        [TestMethod]
        public void Should_return_null_when_no_replicas_exist()
        {
            var store = new RavenStore("Foo");

            Assert.IsNull(store.GetClosestReplica("foo123"));
            Assert.IsNull(store.GetClosestReplica("bar456", true));
        }

        private RavenStore GetStore()
        {
            var store = new RavenStore("Foo");
            store.Servers.Add(new RavenServer("dc1raven01", false, true));
            store.Servers.Add(new RavenServer("dc1raven02", true, false));
            store.Servers.Add(new RavenServer("dc1raven03", true, false));
            store.Servers.Add(new RavenServer("dc1raven04", true, true));

            store.Servers.Add(new RavenServer("dc2ravenXX", true, true));
            store.Servers.Add(new RavenServer("dc2raven01", true, true));
            store.Servers.Add(new RavenServer("dc2raven02", true, true));
            store.Servers.Add(new RavenServer("dc2raven03", true, true));
            return store;
        }
    }
}
