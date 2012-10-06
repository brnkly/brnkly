using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Brnkly.Framework.Data
{
    public class RavenStore
    {
        private static Random random = new Random();

        public string Name { get; private set; }
        public Collection<RavenServer> Servers { get; private set; }

        public RavenStore(string name)
        {
            StoreName.ValidateName(name);
            this.Name = name;
            this.Servers = new Collection<RavenServer>();
        }

        public string GetClosestReplica(string fromMachine, bool isForWriting = false)
        {
            CodeContract.ArgumentNotNullOrWhitespace("fromMachine", fromMachine);
            var availableReplicas = this.Servers
                .Where(replica => isForWriting ? replica.AllowWrites : replica.AllowReads)
                .Select(replica => replica.Name);

            var longestMatches = fromMachine.GetLongestMatches(availableReplicas);
            if (longestMatches != null && longestMatches.Count() > 1)
            {
                return
                    ChooseReplicaByNumberSuffix(fromMachine, longestMatches) ??
                    ChooseRandomReplicaOnTie(longestMatches);
            }

            return longestMatches.FirstOrDefault();
        }

        private static string ChooseReplicaByNumberSuffix(string fromMachine, IEnumerable<string> matches)
        {
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


        private static string ChooseRandomReplicaOnTie(IEnumerable<string> matches)
        {
            int randomIdx = random.Next(0, matches.Count());

            return matches.ElementAt(randomIdx);
        }
    }
}
