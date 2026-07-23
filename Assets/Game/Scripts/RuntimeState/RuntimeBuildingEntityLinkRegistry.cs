using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    public sealed class RuntimeBuildingEntityLinkRegistry
    {
        private const double SyncIntervalSeconds = 0.2d;
        private readonly List<RuntimeBuildingEntityLink> links = new();
        private double nextSyncAt;

        public int Count => links.Count;

        public void Register(RuntimeBuildingEntityLink link)
        {
            if (link == null || links.Contains(link))
                return;

            links.Add(link);
            link.SyncNow();
        }

        public void Unregister(RuntimeBuildingEntityLink link)
        {
            if (link == null)
                return;

            links.Remove(link);
        }

        public void SyncLinks()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (now < nextSyncAt)
                return;

            nextSyncAt = now + SyncIntervalSeconds;
            for (int i = links.Count - 1; i >= 0; i--)
            {
                RuntimeBuildingEntityLink link = links[i];
                if (link == null)
                {
                    links.RemoveAt(i);
                    continue;
                }

                link.SyncNow();
            }
        }
    }
}
