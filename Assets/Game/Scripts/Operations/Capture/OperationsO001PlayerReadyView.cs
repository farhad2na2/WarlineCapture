using Game.Operations.Content;
using Game.Operations.Loop;
using UnityEngine;

namespace Game.Operations.Capture
{
    /// <summary>
    /// Play Mode shell for O001. Every action is a button that calls
    /// <see cref="OperationsO001PlayerShell.Press"/>. Mission time also advances
    /// one second at a time through the same wait control the button uses.
    /// </summary>
    public sealed class OperationsO001PlayerReadyView : MonoBehaviour
    {
        OperationsO001PlayerShell _shell;
        OperationsTacticalWorldShell _world;
        Vector2 _scroll;
        float _clock;

        public static OperationsO001PlayerReadyView Begin(string directory)
        {
            var host = new GameObject("OperationsO001PlayerReady");
            DontDestroyOnLoad(host);
            OperationsO001PlayerReadyView view = host.AddComponent<OperationsO001PlayerReadyView>();
            view.Bind(directory);
            return view;
        }

        public void Bind(string directory)
        {
            _shell = OperationsO001PlayerShell.Bind(directory, OperationsO001PlayerShell.RegularEnSeed);
            _world = OperationsTacticalWorldShell.Ensure(transform);
            SyncWorld();
        }

        void Update()
        {
            if (_shell == null)
                return;
            SyncWorld();
            OperationsPlayerShellFrame frame = _shell.Read();
            if (!frame.HasMission || frame.Terminal || frame.Phase != OperationsLoopPhase.Active)
                return;
            _clock += Time.unscaledDeltaTime;
            if (_clock < 1f)
                return;
            _clock -= 1f;
            _shell.PumpOneSecond();
            SyncWorld();
        }

        void OnGUI()
        {
            if (_shell == null)
                return;

            OperationsPlayerShellFrame frame = _shell.Read();
            float width = Mathf.Clamp(Screen.width * 0.34f, 280f, 440f);
            GUILayout.BeginArea(new Rect(16f, 16f, width, Screen.height - 32f));
            GUILayout.Label(Copy("operations.hud.shell", "OPERATIONS"));
            GUILayout.Label(Copy(frame.TitleKey, frame.MissionId));
            GUILayout.Label(Copy(frame.ObjectiveKey, string.Empty));
            if (frame.Route == OperationsShellNames.MissionBriefing)
                GUILayout.Label(Copy(frame.BriefKey, string.Empty));
            if (frame.O001Victory)
            {
                GUILayout.Label(Copy("operations.hud.victory", "VICTORY"));
                GUILayout.Label(Copy("operations.hud.reward_credits", "Credits") + " " + frame.Credits);
                GUILayout.Label(Copy("operations.hud.reward_xp", "Commander XP") + " " + frame.CommanderXp);
            }
            else if (frame.HasMission)
            {
                GUILayout.Label(Copy("operations.hud.in_progress", "IN PROGRESS"));
            }

            DrawObjectives();
            _scroll = GUILayout.BeginScrollView(_scroll);
            OperationsPlayerControl[] controls = frame.Controls ?? System.Array.Empty<OperationsPlayerControl>();
            for (int index = 0; index < controls.Length; index++)
            {
                OperationsPlayerControl control = controls[index];
                string text = Copy(control.LabelKey, control.Label);
                if (control.Id == OperationsO001PlayerShell.SelectId(_shell.SelectedUnitId))
                    text = Copy("operations.hud.active", "active") + " " + text;
                if (!control.Enabled)
                {
                    GUILayout.Label(text);
                    continue;
                }

                if (GUILayout.Button(text, GUILayout.Height(48f)))
                {
                    _shell.Press(control.Id);
                    SyncWorld();
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void DrawObjectives()
        {
            if (_shell.Session == null || !_shell.Session.TryReadHud(out OperationsHudFrame hud) || hud.Required == null)
                return;
            for (int index = 0; index < hud.Required.Length && index < 6; index++)
            {
                OperationsHudObjective row = hud.Required[index];
                string label = Copy("operations.objective." + row.NodeId, row.NodeId);
                string status = row.Complete
                    ? Copy("operations.hud.done", "done")
                    : row.Active
                        ? Copy("operations.hud.active", "active")
                        : Copy("operations.hud.locked", "locked");
                GUILayout.Label(label + " — " + status);
            }
        }

        void SyncWorld()
        {
            if (_world == null || _shell == null)
                return;
            string focus = _shell.SelectedUnitId;
            if (string.IsNullOrEmpty(focus) && _shell.Session.TryReadHud(out OperationsHudFrame hud))
                focus = hud.CameraFocus;
            _world.Sync(_shell.Session, focus);
        }

        static string Copy(string key, string fallback)
        {
            if (!string.IsNullOrEmpty(key) &&
                OperationsLocalizedCopy.TryGet(key, "en", out string value) &&
                !string.IsNullOrEmpty(value))
                return value;
            return fallback ?? string.Empty;
        }
    }
}
