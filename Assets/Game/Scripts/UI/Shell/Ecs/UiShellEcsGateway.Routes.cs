using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Configs;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.Components;
using Game.UI.Runtime;
using Game.Runtime;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static class UiShellRouteAdapter
        {
        public static bool TryEnqueueRouteRequest(UiShellRouteIntent intent, UIRoute route, bool pushHistory)
        {
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            DynamicBuffer<UiShellRouteRequestComponent> requests =
                entityManager.GetBuffer<UiShellRouteRequestComponent>(boundary);
            requests.Add(new UiShellRouteRequestComponent
            {
                Intent = intent,
                Route = route,
                PushHistory = pushHistory ? (byte)1 : (byte)0
            });
            return true;
        }

        public static bool TryConsumePresentationCommands(List<UiShellPresentationCommandModel> commands)
        {
            if (commands == null)
                return false;

            commands.Clear();
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            if (!entityManager.HasBuffer<UiShellPresentationCommandComponent>(boundary))
                return false;

            DynamicBuffer<UiShellPresentationCommandComponent> buffer =
                entityManager.GetBuffer<UiShellPresentationCommandComponent>(boundary);
            if (buffer.Length == 0)
                return false;

            for (int i = 0; i < buffer.Length; i++)
            {
                UiShellPresentationCommandComponent command = buffer[i];
                commands.Add(new UiShellPresentationCommandModel(
                    command.Kind,
                    command.Region,
                    command.Route,
                    command.TargetMode,
                    command.SequenceId,
                    command.PopupKind));
            }

            buffer.Clear();
            return commands.Count > 0;
        }

        public static bool TryEnqueueTransitionComplete(UiShellTransitionCompleteModel completion)
        {
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            if (!entityManager.HasBuffer<UiShellTransitionCompleteComponent>(boundary))
                return false;

            DynamicBuffer<UiShellTransitionCompleteComponent> completions =
                entityManager.GetBuffer<UiShellTransitionCompleteComponent>(boundary);
            completions.Add(new UiShellTransitionCompleteComponent
            {
                Kind = completion.Kind,
                Region = completion.Region,
                SequenceId = completion.SequenceId
            });
            return true;
        }


        }
    }
}
