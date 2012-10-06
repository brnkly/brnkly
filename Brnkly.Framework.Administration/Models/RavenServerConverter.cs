using System.Linq;
using AutoMapper;
using Brnkly.Framework.Data;

namespace Brnkly.Framework.Administration.Models
{
    public class RavenServerConverter
        : ITypeConverter<RavenServer, RavenServerEditModel>,
        ITypeConverter<RavenServerEditModel, RavenServer>
    {
        RavenServerEditModel ITypeConverter<RavenServer, RavenServerEditModel>.Convert(
            ResolutionContext context)
        {
            var source = (RavenServer)context.SourceValue;

            var model = new RavenServerEditModel
            {
                Name = source.Name,
                AllowReads = new TrackedChange<bool>() { Value = source.AllowReads },
                AllowWrites = new TrackedChange<bool>() { Value = source.AllowWrites },
            };
            foreach (var destination in source.ReplicationDestinations)
            {
                model.ReplicationDestinations.Add(
                    new RavenReplicationDestinationEditModel
                    {
                        Name = destination.ServerName,
                        Enabled = new TrackedChange<bool>(true),
                        IsTransitive = new TrackedChange<bool>(destination.IsTransitive)
                    });
            }

            return model;
        }

        RavenServer ITypeConverter<RavenServerEditModel, RavenServer>.Convert(
            ResolutionContext context)
        {
            var model = (RavenServerEditModel)context.SourceValue;
            model.AllowReads = model.AllowReads ?? new TrackedChange<bool>();
            model.AllowWrites = model.AllowWrites ?? new TrackedChange<bool>();

            var store = new RavenServer(model.Name, model.AllowReads.Value, model.AllowWrites.Value);
            var activeDestinations = model.ReplicationDestinations
                .Where(d => d.Enabled.Value)
                .OrderBy(d => d.Name);
            foreach (var destination in activeDestinations)
            {
                store.ReplicationDestinations.Add(
                    new RavenReplicationDestination(
                        destination.Name,
                        destination.IsTransitive.Value));
            }

            return store;
        }
    }
}
