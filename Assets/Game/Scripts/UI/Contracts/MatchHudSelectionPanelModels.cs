using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public enum SelectionSummaryPortraitKind
{
    None = 0,
    GenericSquad = 1,
    Soldiers = 2,
    Vehicles = 3,
    Aircraft = 4,
    Transports = 5,
    Buildings = 6,
    MixedForce = 7,
    MixedSoldierVehicle = 8,
    MixedSoldierAircraft = 9,
    MixedVehicleAircraft = 10,
    MixedSoldierVehicleAircraft = 11
}

public readonly struct MatchHudSelectionPanelPassengerItemModel
{
    public readonly Entity Passenger;
    public readonly string DisplayName;
    public readonly string RoleText;
    public readonly string HealthText;
    public readonly float Health01;
    public readonly Sprite PortraitSprite;
    public readonly bool ExitEnabled;

    public MatchHudSelectionPanelPassengerItemModel(
        Entity passenger,
        string displayName,
        string roleText,
        string healthText,
        float health01,
        Sprite portraitSprite,
        bool exitEnabled)
    {
        Passenger = passenger;
        DisplayName = displayName;
        RoleText = roleText;
        HealthText = healthText;
        Health01 = health01;
        PortraitSprite = portraitSprite;
        ExitEnabled = exitEnabled;
    }
}

public readonly struct MatchHudTransportPassengersModel
{
    public readonly bool Visible;
    public readonly bool DrawerOpen;
    public readonly Entity Transport;
    public readonly int PassengerCount;
    public readonly int Capacity;
    public readonly bool ExitAllEnabled;
    public readonly IReadOnlyList<MatchHudSelectionPanelPassengerItemModel> Passengers;

    public MatchHudTransportPassengersModel(
        bool visible,
        bool drawerOpen,
        Entity transport,
        int passengerCount,
        int capacity,
        bool exitAllEnabled,
        IReadOnlyList<MatchHudSelectionPanelPassengerItemModel> passengers)
    {
        Visible = visible;
        DrawerOpen = drawerOpen;
        Transport = transport;
        PassengerCount = passengerCount;
        Capacity = capacity;
        ExitAllEnabled = exitAllEnabled;
        Passengers = passengers;
    }

    public static MatchHudTransportPassengersModel Hidden => new(false, false, Entity.Null, 0, 0, false, null);
}

public readonly struct MatchHudSelectionPanelModel
{
    public readonly bool Visible;
    public readonly string Title;
    public readonly string Subtitle;
    public readonly string CurrentOrder;
    public readonly string HealthText;
    public readonly float Health01;
    public readonly Sprite PortraitSprite;
    public readonly SelectionSummaryPortraitKind PortraitKind;
    public readonly bool BadgeVisible;
    public readonly Sprite BadgeSprite;
    public readonly bool ReturnEnabled;
    public readonly bool DestroyEnabled;
    public readonly bool BoardEnabled;

    public MatchHudSelectionPanelModel(
        bool visible,
        string title,
        string subtitle,
        string currentOrder,
        string healthText,
        float health01,
        Sprite portraitSprite,
        bool badgeVisible,
        Sprite badgeSprite,
        bool returnEnabled,
        bool destroyEnabled,
        bool boardEnabled)
        : this(
            visible,
            title,
            subtitle,
            currentOrder,
            healthText,
            health01,
            portraitSprite,
            SelectionSummaryPortraitKind.GenericSquad,
            badgeVisible,
            badgeSprite,
            returnEnabled,
            destroyEnabled,
            boardEnabled)
    {
    }

    public MatchHudSelectionPanelModel(
        bool visible,
        string title,
        string subtitle,
        string currentOrder,
        string healthText,
        float health01,
        Sprite portraitSprite,
        SelectionSummaryPortraitKind portraitKind,
        bool badgeVisible,
        Sprite badgeSprite,
        bool returnEnabled,
        bool destroyEnabled,
        bool boardEnabled)
    {
        Visible = visible;
        Title = title;
        Subtitle = subtitle;
        CurrentOrder = currentOrder;
        HealthText = healthText;
        Health01 = health01;
        PortraitSprite = portraitSprite;
        PortraitKind = portraitKind;
        BadgeVisible = badgeVisible;
        BadgeSprite = badgeSprite;
        ReturnEnabled = returnEnabled;
        DestroyEnabled = destroyEnabled;
        BoardEnabled = boardEnabled;
    }

    public static MatchHudSelectionPanelModel Hidden => new(
        false,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        0f,
        null,
        false,
        null,
        false,
        false,
        false);
}
