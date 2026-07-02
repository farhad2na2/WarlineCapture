using System.Collections.Generic;
using UnityEngine;

namespace Game.UI.Contracts
{
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

    public readonly struct UiEntityHandle : System.IEquatable<UiEntityHandle>
    {
        public readonly int Index;
        public readonly int Version;

        public UiEntityHandle(int index, int version)
        {
            Index = index;
            Version = version;
        }

        public bool IsNull => Index == 0 && Version == 0;

        public static UiEntityHandle Null => default;

        public bool Equals(UiEntityHandle other)
        {
            return Index == other.Index && Version == other.Version;
        }

        public override bool Equals(object obj)
        {
            return obj is UiEntityHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Index * 397) ^ Version;
            }
        }

        public static bool operator ==(UiEntityHandle left, UiEntityHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UiEntityHandle left, UiEntityHandle right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct MatchHudSelectionPanelPassengerItemModel
    {
        public readonly UiEntityHandle Passenger;
        public readonly string DisplayName;
        public readonly string RoleText;
        public readonly string HealthText;
        public readonly float Health01;
        public readonly Sprite PortraitSprite;
        public readonly bool ExitEnabled;

        public MatchHudSelectionPanelPassengerItemModel(
            UiEntityHandle passenger,
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

    public enum MatchHudStorageChipKind
    {
        Passengers = 0,
        OilBarrels = 1,
        FuelBarrels = 2,
        OilAndFuel = 3,
        ResourceCargo = 4
    }

    public readonly struct MatchHudTransportPassengersModel
    {
        public readonly bool Visible;
        public readonly bool DrawerOpen;
        public readonly UiEntityHandle Transport;
        public readonly MatchHudStorageChipKind StorageKind;
        public readonly int PassengerCount;
        public readonly int Capacity;
        public readonly int OilCurrent;
        public readonly int OilCapacity;
        public readonly int FuelCurrent;
        public readonly int FuelCapacity;
        public readonly int SoldierPassengerCount;
        public readonly int SoldierCapacity;
        public readonly int VehiclePassengerCount;
        public readonly int VehicleCapacity;
        public readonly bool ExitAllEnabled;
        public readonly IReadOnlyList<MatchHudSelectionPanelPassengerItemModel> Passengers;

        public MatchHudTransportPassengersModel(
            bool visible,
            bool drawerOpen,
            UiEntityHandle transport,
            int passengerCount,
            int capacity,
            bool exitAllEnabled,
            IReadOnlyList<MatchHudSelectionPanelPassengerItemModel> passengers,
            int soldierPassengerCount = 0,
            int soldierCapacity = 0,
            int vehiclePassengerCount = 0,
            int vehicleCapacity = 0,
            MatchHudStorageChipKind storageKind = MatchHudStorageChipKind.Passengers,
            int oilCurrent = 0,
            int oilCapacity = 0,
            int fuelCurrent = 0,
            int fuelCapacity = 0)
        {
            Visible = visible;
            DrawerOpen = drawerOpen;
            Transport = transport;
            StorageKind = storageKind;
            PassengerCount = passengerCount;
            Capacity = capacity;
            OilCurrent = oilCurrent;
            OilCapacity = oilCapacity;
            FuelCurrent = fuelCurrent;
            FuelCapacity = fuelCapacity;
            SoldierPassengerCount = soldierPassengerCount;
            SoldierCapacity = soldierCapacity;
            VehiclePassengerCount = vehiclePassengerCount;
            VehicleCapacity = vehicleCapacity;
            ExitAllEnabled = exitAllEnabled;
            Passengers = passengers;
        }

        public static MatchHudTransportPassengersModel Hidden => new(false, false, UiEntityHandle.Null, 0, 0, false, null);
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
}
