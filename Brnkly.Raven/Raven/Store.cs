using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Brnkly.Raven
{
    public class Store
    {
        private static Random random = new Random();

        public string Name { get; private set; }
        public Collection<Instance> Instances { get; private set; }

        public Store(string name)
        {
            name.Ensure("name").IsNotNullOrWhiteSpace();

            this.Name = name;
            this.Instances = new Collection<Instance>();
        }

        public override string ToString()
        {
            return string.Format(
                "{0}: {1} instances",
                Name,
                Instances == null ? 0 : Instances.Count);
        }

        public Instance GetClosestReplica(string fromMachine, bool isForWriting = false)
        {
            fromMachine.Ensure("fromMachine").IsNotNullOrWhiteSpace();

            var availableReplicas = this.Instances
                .Where(replica => isForWriting ? replica.AllowWrites : replica.AllowReads);
            if (!availableReplicas.Any())
            {
                return null;
            }

            var closestHosts = fromMachine.GetLongestMatches(
                availableReplicas.Select(r => r.Url.Host));
            if (closestHosts.Count() > 1)
            {
                var host =
                    ChooseHostByNumberSuffix(fromMachine, closestHosts) ??
                    ChooseRandomHostOnTie(closestHosts);
                return availableReplicas.First(
                    r => r.Url.Host.Equals(host, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                return availableReplicas.First();
            }
        }

        private static string ChooseHostByNumberSuffix(
            string fromMachine,
            IEnumerable<string> matches)
        {
            // TODO: Select using mod based on number of servers.
            var fromIsEven = IsEven(fromMachine);
            return matches.Where(s => IsEven(s) == fromIsEven).FirstOrDefault();
        }

        private static bool IsEven(string machineName)
        {
            var lastCharacter = machineName.Substring(machineName.Length - 1)[0];
            if (!char.IsDigit(lastCharacter))
            {
                return true;
            }

            return (int)(lastCharacter) % 2 == 0;
        }


        private static string ChooseRandomHostOnTie(IEnumerable<string> matches)
        {
            int randomIdx = random.Next(0, matches.Count());

            return matches.ElementAt(randomIdx);
        }
    }
}
