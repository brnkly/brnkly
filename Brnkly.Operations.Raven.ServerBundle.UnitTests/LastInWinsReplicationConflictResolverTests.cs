using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Raven.Abstractions.Data;
using Raven.Bundles.Replication;
using Raven.Json.Linq;

namespace Brnkly.Operations.Raven.ServerBundle.UnitTests
{
    [TestClass]
    public class LastInWinsReplicationConflictResolverTests
    {
        private const string DefaultId = "foo/123";

        [TestMethod]
        public void Should_not_resolve_conflict_for_system_document()
        {
            var existingDoc = this.GetJsonDocument(
                id: "Raven/foo",
                ravenLastModified: DateTime.UtcNow.AddMinutes(-1));

            var incomingDoc = this.GetJsonDocument(
                id: "Raven/foo",
                myProperty: "Incoming value",
                ravenLastModified: DateTime.UtcNow);

            var resolved = RunResolver(existingDoc, incomingDoc);

            Assert.IsFalse(resolved);
        }

        [TestMethod]
        public void Should_choose_incoming_document_if_existing_has_conflict()
        {
            var existingDoc = this.GetJsonDocument(
                ravenLastModified: DateTime.UtcNow.AddMinutes(1),
                hasConflict: true);

            var incomingDoc = this.GetJsonDocument(
                myProperty: "Incoming value",
                ravenLastModified: DateTime.UtcNow);

            var resolved = RunResolver(existingDoc, incomingDoc);

            Assert.IsTrue(resolved);
            Assert.AreEqual("Incoming value", incomingDoc.DataAsJson["MyProperty"]);
        }

        [TestMethod]
        public void Should_choose_incoming_document_with_newer_LastModifiedAtUtc()
        {
            var existingDoc = this.GetJsonDocument(
                lastModifiedAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1),
                ravenLastModified: DateTime.UtcNow.AddMinutes(1));

            var incomingDoc = this.GetJsonDocument(
                myProperty: "Incoming value",
                lastModifiedAtUtc: DateTimeOffset.UtcNow,
                ravenLastModified: DateTime.UtcNow);

            var resolved = RunResolver(existingDoc, incomingDoc);

            Assert.IsTrue(resolved);
            Assert.AreEqual("Incoming value", incomingDoc.DataAsJson["MyProperty"]);
        }

        [TestMethod]
        public void Should_choose_existing_document_with_newer_LastModifiedAtUtc()
        {
            var existingDoc = this.GetJsonDocument(
                lastModifiedAtUtc: DateTimeOffset.UtcNow.AddMinutes(1),
                ravenLastModified: DateTime.UtcNow.AddMinutes(-1));

            var incomingDoc = this.GetJsonDocument(
                myProperty: "Incoming value",
                lastModifiedAtUtc: DateTimeOffset.UtcNow,
                ravenLastModified: DateTime.UtcNow);

            var resolved = RunResolver(existingDoc, incomingDoc);

            Assert.IsTrue(resolved);
            Assert.AreEqual("Existing value", incomingDoc.DataAsJson["MyProperty"]);
        }

        [TestMethod]
        public void Should_choose_using_Raven_LastModified_when_a_LastModifiedAtUtc_is_invalid()
        {
            var existingDoc = this.GetJsonDocument(
                ravenLastModified: DateTime.UtcNow.AddMinutes(-1));
            existingDoc.DataAsJson["LastModifiedAtUtc"] = "Invalid DateTimeOffset";

            var incomingDoc = this.GetJsonDocument(
                myProperty: "Incoming value",
                lastModifiedAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1),
                ravenLastModified: DateTime.UtcNow);

            var resolved = RunResolver(existingDoc, incomingDoc);

            Assert.IsTrue(resolved);
            Assert.AreEqual("Incoming value", incomingDoc.DataAsJson["MyProperty"]);
        }

        [TestMethod]
        public void Should_choose_using_Raven_LastModified_when_existing_LastModifiedAtUtc_is_null()
        {
            var existingDoc = this.GetJsonDocument(
                ravenLastModified: DateTime.UtcNow.AddMinutes(-1));
            existingDoc.DataAsJson.Remove("LastModifiedAtUtc");

            var incomingDoc = this.GetJsonDocument(
                myProperty: "Incoming value",
                lastModifiedAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1),
                ravenLastModified: DateTime.UtcNow);

            var resolved = RunResolver(existingDoc, incomingDoc);

            Assert.IsTrue(resolved);
            Assert.AreEqual("Incoming value", incomingDoc.DataAsJson["MyProperty"]);
        }

        [TestMethod]
        public void Should_choose_using_Raven_LastModified_when_incoming_LastModifiedAtUtc_is_null()
        {
            var existingDoc = this.GetJsonDocument(
                lastModifiedAtUtc: DateTimeOffset.UtcNow.AddMinutes(1),
                ravenLastModified: DateTime.UtcNow.AddMinutes(-1));

            var incomingDoc = this.GetJsonDocument(
                myProperty: "Incoming value",
                ravenLastModified: DateTime.UtcNow);
            incomingDoc.DataAsJson.Remove("LastModifiedAtUtc");

            var resolved = RunResolver(existingDoc, incomingDoc);

            Assert.IsTrue(resolved);
            Assert.AreEqual("Incoming value", incomingDoc.DataAsJson["MyProperty"]);
        }

        [TestMethod]
        public void Should_choose_incoming_document_with_newer_Raven_LastModified()
        {
            var existingDoc = this.GetJsonDocument(
                ravenLastModified: DateTime.UtcNow.AddMinutes(-1));
            existingDoc.DataAsJson.Remove("LastModifiedAtUtc");

            var incomingDoc = this.GetJsonDocument(
                myProperty: "Incoming value",
                ravenLastModified: DateTime.UtcNow);
            incomingDoc.DataAsJson.Remove("LastModifiedAtUtc");

            var resolved = RunResolver(existingDoc, incomingDoc);

            Assert.IsTrue(resolved);
            Assert.AreEqual("Incoming value", incomingDoc.DataAsJson["MyProperty"]);
        }

        [TestMethod]
        public void Should_choose_existing_document_with_newer_Raven_LastModified()
        {
            var existingDoc = this.GetJsonDocument(
                ravenLastModified: DateTime.UtcNow.AddMinutes(1));
            existingDoc.DataAsJson.Remove("LastModifiedAtUtc");

            var incomingDoc = this.GetJsonDocument(
                myProperty: "Incoming value",
                ravenLastModified: DateTime.UtcNow);
            incomingDoc.DataAsJson.Remove("LastModifiedAtUtc");

            var resolved = RunResolver(existingDoc, incomingDoc);

            Assert.IsTrue(resolved);
            Assert.AreEqual("Existing value", incomingDoc.DataAsJson["MyProperty"]);
        }

        private JsonDocument GetJsonDocument(
            string id = DefaultId,
            string myProperty = "Existing value",
            DateTimeOffset? lastModifiedAtUtc = null,
            DateTime? ravenLastModified = null,
            bool hasConflict = false)
        {
            var doc = new TestDocument(id)
            {
                MyProperty = myProperty,
                LastModifiedAtUtc = lastModifiedAtUtc ?? DateTimeOffset.UtcNow
            };

            var metadata = new RavenJObject { { Constants.LastModified, ravenLastModified } };
            if (hasConflict)
            {
                metadata[ReplicationConstants.RavenReplicationConflict] = true;
            }

            return new JsonDocument
            {
                Key = id,
                DataAsJson = RavenJObject.FromObject(doc),
                Metadata = metadata,
                LastModified = ravenLastModified
            };
        }

        private static bool RunResolver(JsonDocument existingDoc, JsonDocument incomingDoc)
        {
            var resolver = new LastInWinsReplicationConflictResolver();
            return resolver.TryResolve(
                incomingDoc.Key,
                incomingDoc.Metadata,
                incomingDoc.DataAsJson,
                existingDoc);
        }
    }
}
