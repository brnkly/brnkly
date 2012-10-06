using System;
using Brnkly.Framework.ServiceBus;

namespace Brnkly.Framework.Data
{
    public class RavenDocumentChanged : Message
    {
        public static bool IsTransactional = false;
        public static TimeSpan TimeToLive = TimeSpan.FromSeconds(30);

        public string StoreName { get; set; }
        public string Id { get; set; }
        public Guid? Etag { get; set; }

        public override string ToString()
        {
            return string.Format(
                "StoreName='{0}', Id='{1}', Etag='{2}'.",
                this.StoreName,
                this.Id,
                this.Etag);
        }
    }
}
