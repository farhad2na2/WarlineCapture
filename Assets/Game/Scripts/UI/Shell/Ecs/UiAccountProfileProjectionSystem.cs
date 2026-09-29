using System;
using System.Globalization;
using Game.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    // A read-only projection of the existing save owner. Never grants or settles rewards.
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class UiAccountProfileProjectionSystem : SystemBase
    {
        private long observedVersion = -1;
        private string credits = "—", name = "Commander", portrait = "0";
        private SaveService saves;
        protected override void OnUpdate()
        {
            long version = JsonSaveRepository.ChangeVersion;
            if (saves == null || observedVersion != version)
            {
                saves ??= SaveService.CreateDefault();
                try
                {
                    if (saves.TryReadAccountProfile(out var profile))
                    {
                        credits = profile.credits.ToString("N0", CultureInfo.InvariantCulture);
                        name = profile.firstLaunchCommanderDisplayName;
                        portrait = profile.firstLaunchCommanderPortraitIndex.ToString(CultureInfo.InvariantCulture);
                    }
                    else credits = "—";
                    observedVersion = version;
                }
                catch (Exception error) when (error is System.IO.IOException || error is UnauthorizedAccessException || error is ArgumentException)
                {
                    credits = "—";
                    observedVersion = version;
                }
            }
            var resources = new UiShellMainMenuResourcesComponent { CreditsText = new FixedString32Bytes(credits) };
            foreach (var target in SystemAPI.Query<RefRW<UiShellMainMenuResourcesComponent>>())
                target.ValueRW = resources;
            var displayName = new FixedString128Bytes();
            displayName.CopyFromTruncated(name); // Protect rendering of oversized legacy/imported names; preserve the save.
            foreach (var target in SystemAPI.Query<RefRW<UiShellCommanderProfileComponent>>())
                target.ValueRW = new UiShellCommanderProfileComponent
                {
                    Name = displayName,
                    Subtitle = new FixedString64Bytes("FIELD COMMANDER"),
                    PortraitClass = new FixedString64Bytes(portrait)
                };
        }
    }
}
