using System;

namespace Brnkly.Operations.Raven.ServerBundle.UnitTests
{
    public class TestDocument
    {
        public string Id { get; private set; }
        public DateTimeOffset LastModifiedAtUtc { get; set; }
        public string MyProperty { get; set; }

        public TestDocument(string id)
        {
            this.Id = id;
            this.LastModifiedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}
