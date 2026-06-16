// Author: Michal Petr
// Created: 25.06.2026

using System;
using System.Collections.Generic;

namespace EDIVE.ServiceHub.SaveData
{
    [Serializable]
    public class NewestWinsConflictResolver : ISaveDataConflictResolver
    {
        public int Resolve(IReadOnlyList<SaveDataCandidate> candidates)
        {
            var winner = -1;
            DateTime? best = null;

            foreach (var candidate in candidates)
            {
                if (winner >= 0 && !IsNewer(candidate.Timestamp, best)) 
                    continue;
                winner = candidate.StoreIndex;
                best = candidate.Timestamp;
            }

            return winner;
        }

        private static bool IsNewer(DateTime? candidate, DateTime? current)
        {
            if (candidate == null) return false;
            if (current == null) return true;
            return candidate.Value > current.Value;
        }
    }
}
