using System;
using System.Linq;
using NLog;
using Raven.Abstractions.Data;
using Raven.Bundles.Replication;
using Raven.Bundles.Replication.Plugins;
using Raven.Json.Linq;

namespace Brnkly.Operations.Raven.ServerBundle
{
    public class LastInWinsReplicationConflictResolver
        : AbstractDocumentReplicationConflictResolver
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();

        public override bool TryResolve(
            string id,
            RavenJObject metadata,
            RavenJObject document,
            JsonDocument existingDoc)
        {
            if (id.StartsWith("Raven/", StringComparison.OrdinalIgnoreCase) &&
                !id.StartsWith("Raven/Hilo/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            try
            {
                this.ResolveConflict(id, metadata, document, existingDoc);
            }
            catch (Exception exception)
            {
                this.log.ErrorException(
                    string.Format(
                        "An exception occured while attempting to resolve a replication conflict for document '{0}'. The incoming document will be saved.",
                        id),
                    exception);
            }

            return true;
        }

        private void ResolveConflict(
            string id,
            RavenJObject metadata,
            RavenJObject document,
            JsonDocument existingDoc)
        {
            if (this.ExistingDocShouldWin(metadata, document, existingDoc))
            {
                this.ReplaceValues(metadata, existingDoc.Metadata);
                this.ReplaceValues(document, existingDoc.DataAsJson);
                log.Debug("Replication conflict for '{0}' resolved by choosing existing document.", id);
            }
            else
            {
                log.Debug("Replication conflict for '{0}' resolved by choosing incoming document.", id);
            }
        }

        private bool ExistingDocShouldWin(
            RavenJObject newMetadata,
            RavenJObject newDocument,
            JsonDocument existingDoc)
        {
            if (existingDoc == null ||
                this.ExistingDocHasConflict(existingDoc) ||
                this.ExistingDocIsOlder(newMetadata, newDocument, existingDoc))
            {
                return false;
            }

            return true;
        }

        private bool ExistingDocHasConflict(JsonDocument existingDoc)
        {
            return existingDoc.Metadata[ReplicationConstants.RavenReplicationConflict] != null;
        }

        private bool ExistingDocIsOlder(
            RavenJObject newMetadata,
            RavenJObject newDocument,
            JsonDocument existingDoc)
        {
            var newBrnklyLastModifiedAtUtc = this.GetBrnklyLastModifiedAtUtc(newDocument);
            var existingBrnklyLastModifiedAtUtc = this.GetBrnklyLastModifiedAtUtc(existingDoc.DataAsJson);
            if (newBrnklyLastModifiedAtUtc.HasValue && existingBrnklyLastModifiedAtUtc.HasValue)
            {
                return existingBrnklyLastModifiedAtUtc <= newBrnklyLastModifiedAtUtc;
            }

            var newRavenLastModified = this.GetRavenLastModified(newMetadata);
            if (!existingDoc.LastModified.HasValue ||
                newRavenLastModified.HasValue &&
                existingDoc.LastModified <= newRavenLastModified)
            {
                return true;
            }

            return false;
        }

        private DateTimeOffset? GetBrnklyLastModifiedAtUtc(RavenJObject document)
        {
            var jvalue = document["LastModifiedAtUtc"] as RavenJValue;
            bool hasDateValue = jvalue != null && jvalue.Value is DateTimeOffset;
            return hasDateValue ? jvalue.Value<DateTimeOffset?>() : null;
        }

        private DateTime? GetRavenLastModified(RavenJObject metadata)
        {
            var lastModified = metadata[Constants.LastModified];

            return (lastModified == null) ?
                new DateTime?() :
                lastModified.Value<DateTime?>();
        }

        private void ReplaceValues(RavenJObject target, RavenJObject source)
        {
            var targetKeys = target.Keys.ToArray();
            foreach (var key in targetKeys)
            {
                target.Remove(key);
            }

            foreach (var key in source.Keys)
            {
                target.Add(key, source[key]);
            }
        }
    }
}