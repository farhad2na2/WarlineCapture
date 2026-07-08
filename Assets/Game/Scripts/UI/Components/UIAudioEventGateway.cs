using System;

namespace Game.UI.Runtime
{
    public enum UIAudioEventKind
    {
        None = 0,
        ButtonPrimaryClick = 1,
        ButtonSecondaryClick = 2,
        ButtonNegativeClick = 3,
        ButtonDisabledTap = 4,
        TabSelect = 5,
        CardSelect = 6,
        CardLocked = 7,
        ToggleOn = 8,
        ToggleOff = 9,
        SliderTick = 10,
        ScreenForward = 11,
        ScreenBack = 12,
        PopupOpen = 13,
        PopupClose = 14,
        DrawerOpen = 15,
        DrawerClose = 16
    }

    public readonly struct UIAudioEventRequest
    {
        public UIAudioEventRequest(UIAudioEventKind kind, string eventId, uint eventHash, string busId, float cooldownSeconds)
        {
            Kind = kind;
            EventId = eventId;
            EventHash = eventHash;
            BusId = busId;
            CooldownSeconds = cooldownSeconds;
        }

        public UIAudioEventKind Kind { get; }
        public string EventId { get; }
        public uint EventHash { get; }
        public string BusId { get; }
        public float CooldownSeconds { get; }
    }

    public static class UIAudioEventGateway
    {
        public static event Action<UIAudioEventRequest> AudioEventRequested;

        public static bool TryCreateRequest(UIAudioEventKind kind, out UIAudioEventRequest request)
        {
            request = default;
            if (kind == UIAudioEventKind.None)
                return false;

            string eventId = ResolveEventId(kind);
            if (string.IsNullOrEmpty(eventId))
                return false;

            request = new UIAudioEventRequest(
                kind,
                eventId,
                StableHash(eventId),
                "UI",
                ResolveCooldownSeconds(kind));
            return true;
        }

        public static bool Raise(UIAudioEventKind kind)
        {
            if (!TryCreateRequest(kind, out UIAudioEventRequest request))
                return false;

            AudioEventRequested?.Invoke(request);
            return true;
        }

        private static string ResolveEventId(UIAudioEventKind kind)
        {
            return kind switch
            {
                UIAudioEventKind.ButtonPrimaryClick => "UI.Button.Primary.Click",
                UIAudioEventKind.ButtonSecondaryClick => "UI.Button.Secondary.Click",
                UIAudioEventKind.ButtonNegativeClick => "UI.Button.Negative.Click",
                UIAudioEventKind.ButtonDisabledTap => "UI.Button.Disabled.Tap",
                UIAudioEventKind.TabSelect => "UI.Tab.Select",
                UIAudioEventKind.CardSelect => "UI.Card.Select",
                UIAudioEventKind.CardLocked => "UI.Card.Locked",
                UIAudioEventKind.ToggleOn => "UI.Toggle.On",
                UIAudioEventKind.ToggleOff => "UI.Toggle.Off",
                UIAudioEventKind.SliderTick => "UI.Slider.Tick",
                UIAudioEventKind.ScreenForward => "UI.Screen.Forward",
                UIAudioEventKind.ScreenBack => "UI.Screen.Back",
                UIAudioEventKind.PopupOpen => "UI.Popup.Open",
                UIAudioEventKind.PopupClose => "UI.Popup.Close",
                UIAudioEventKind.DrawerOpen => "UI.Drawer.Open",
                UIAudioEventKind.DrawerClose => "UI.Drawer.Close",
                _ => string.Empty
            };
        }

        private static float ResolveCooldownSeconds(UIAudioEventKind kind)
        {
            return kind switch
            {
                UIAudioEventKind.SliderTick => 0.04f,
                UIAudioEventKind.ButtonDisabledTap => 0.08f,
                UIAudioEventKind.ScreenForward => 0.08f,
                UIAudioEventKind.ScreenBack => 0.08f,
                UIAudioEventKind.PopupOpen => 0.08f,
                UIAudioEventKind.PopupClose => 0.08f,
                UIAudioEventKind.DrawerOpen => 0.08f,
                UIAudioEventKind.DrawerClose => 0.08f,
                _ => 0f
            };
        }

        private static uint StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= 16777619u;
                }

                return hash;
            }
        }
    }
}
