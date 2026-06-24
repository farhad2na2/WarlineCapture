using System.Collections.Generic;

public sealed class RuntimeBuildingEntityLinkRegistry
{
    private readonly List<RuntimeBuildingEntityLink> links = new();

    public void Register(RuntimeBuildingEntityLink link)
    {
        if (link == null || links.Contains(link))
            return;

        links.Add(link);
    }

    public void Unregister(RuntimeBuildingEntityLink link)
    {
        if (link == null)
            return;

        links.Remove(link);
    }

    public void SyncLinks()
    {
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
