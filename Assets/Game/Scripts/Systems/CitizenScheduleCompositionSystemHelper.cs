internal sealed class CitizenScheduleCompositionSystemHelper
{
    private const float WeekdayWorkStartHour = 8f;
    private const float WeekdayWorkEndHour = 17f;
    private const float WeekdayShoppingStartHour = 11f;
    private const float WeekdayShoppingEndHour = 13f;
    private const float WeekdayLunchStartHour = 12f;
    private const float WeekdayLunchEndHour = 13f;
    private const float WeekdayEveningWalkStartHour = 17.5f;
    private const float WeekdayEveningWalkEndHour = 18.5f;
    private const float WeekendShoppingStartHour = 10f;
    private const float WeekendShoppingEndHour = 13f;
    private const float WeekendCityHallStartHour = 13f;
    private const float WeekendCityHallEndHour = 15f;
    private const float RefugeeMorningWalkStartHour = 8.5f;
    private const float RefugeeMorningWalkEndHour = 11.5f;
    private const float RefugeeLunchShelterStartHour = 11.5f;
    private const float RefugeeLunchShelterEndHour = 13.5f;
    private const float RefugeeEveningWalkStartHour = 16f;
    private const float RefugeeEveningWalkEndHour = 18.5f;

    public static CitizenStatus GetScheduledStatus(
        CitizenScheduleCompositionSystemHelper system,
        CitizenPopulationStateCompositionSystemHelper state,
        DayNightSystem dayNightSystem,
        CitizenRecordComponent citizen)
    {
        return system != null
            ? system.GetScheduledStatus(state, dayNightSystem, citizen)
            : GetScheduledStatusState(state, dayNightSystem, citizen);
    }

    public CitizenStatus GetScheduledStatus(
        CitizenPopulationStateCompositionSystemHelper state,
        DayNightSystem dayNightSystem,
        CitizenRecordComponent citizen)
    {
        return GetScheduledStatusState(state, dayNightSystem, citizen);
    }

    public static int GetScheduledTargetBuildingId(
        CitizenScheduleCompositionSystemHelper system,
        CitizenPopulationStateCompositionSystemHelper state,
        DayNightSystem dayNightSystem,
        CitizenRecordComponent citizen,
        CitizenStatus status)
    {
        return system != null
            ? system.GetScheduledTargetBuildingId(state, dayNightSystem, citizen, status)
            : GetScheduledTargetBuildingIdState(state, dayNightSystem, citizen, status);
    }

    public int GetScheduledTargetBuildingId(
        CitizenPopulationStateCompositionSystemHelper state,
        DayNightSystem dayNightSystem,
        CitizenRecordComponent citizen,
        CitizenStatus status)
    {
        return GetScheduledTargetBuildingIdState(state, dayNightSystem, citizen, status);
    }

    public static int GetSchedulePhase(CitizenScheduleCompositionSystemHelper system, DayNightSystem dayNightSystem)
    {
        return system != null
            ? system.GetSchedulePhase(dayNightSystem)
            : GetSchedulePhaseState(dayNightSystem);
    }

    public int GetSchedulePhase(DayNightSystem dayNightSystem)
    {
        return GetSchedulePhaseState(dayNightSystem);
    }

    private static CitizenStatus GetScheduledStatusState(
        CitizenPopulationStateCompositionSystemHelper state,
        DayNightSystem dayNightSystem,
        CitizenRecordComponent citizen)
    {
        if (citizen.LifeState == CitizenLifeState.Dead)
            return CitizenStatus.Dead;

        if (state.TryGetHousehold(citizen.HouseholdId, out CitizenHouseholdRecordComponent household) &&
            household.IsDisplaced != 0 &&
            household.RefugeeTentBuildingId != 0)
        {
            if (dayNightSystem == null || IsNightSchedule(dayNightSystem))
                return CitizenStatus.AtRefugeeTent;

            float refugeeHour = dayNightSystem.CurrentHour;
            bool morningWalk = refugeeHour >= RefugeeMorningWalkStartHour && refugeeHour < RefugeeMorningWalkEndHour;
            bool lunchShelter = refugeeHour >= RefugeeLunchShelterStartHour && refugeeHour < RefugeeLunchShelterEndHour;
            bool eveningWalk = refugeeHour >= RefugeeEveningWalkStartHour && refugeeHour < RefugeeEveningWalkEndHour;

            if ((morningWalk || eveningWalk) &&
                !lunchShelter &&
                citizen.PreferredWalkBuildingId != 0)
            {
                return CitizenStatus.GoingForWalk;
            }

            return CitizenStatus.AtRefugeeTent;
        }

        if (dayNightSystem == null || IsNightSchedule(dayNightSystem))
            return CitizenStatus.AtHome;

        bool isWeekend = IsWeekend(GetDayOfWeek(dayNightSystem));
        float currentHour = dayNightSystem.CurrentHour;
        if (isWeekend)
        {
            if (currentHour >= WeekendShoppingStartHour && currentHour < WeekendShoppingEndHour && citizen.PreferredShopBuildingId != 0)
                return CitizenStatus.AtShop;
            if (currentHour >= WeekendCityHallStartHour && currentHour < WeekendCityHallEndHour && citizen.PreferredCityHallBuildingId != 0)
                return CitizenStatus.GoingToCityHall;
            return CitizenStatus.AtHome;
        }

        if (currentHour >= WeekdayEveningWalkStartHour &&
            currentHour < WeekdayEveningWalkEndHour &&
            citizen.PreferredWalkBuildingId != 0)
        {
            return CitizenStatus.GoingForWalk;
        }

        if (citizen.Gender == CitizenGender.Male &&
            citizen.LunchShopBuildingId != 0 &&
            currentHour >= WeekdayLunchStartHour &&
            currentHour < WeekdayLunchEndHour)
        {
            return CitizenStatus.AtShop;
        }

        if (citizen.Gender == CitizenGender.Male &&
            citizen.WorkBuildingId != 0 &&
            currentHour >= WeekdayWorkStartHour &&
            currentHour < WeekdayWorkEndHour)
        {
            return CitizenStatus.AtWork;
        }

        if (citizen.Gender == CitizenGender.Female &&
            citizen.PreferredShopBuildingId != 0 &&
            currentHour >= WeekdayShoppingStartHour &&
            currentHour < WeekdayShoppingEndHour &&
            ShouldCitizenShopOnWeekday(dayNightSystem, citizen))
        {
            return CitizenStatus.AtShop;
        }

        return CitizenStatus.AtHome;
    }

    private static int GetScheduledTargetBuildingIdState(
        CitizenPopulationStateCompositionSystemHelper state,
        DayNightSystem dayNightSystem,
        CitizenRecordComponent citizen,
        CitizenStatus status)
    {
        if (state.TryGetHousehold(citizen.HouseholdId, out CitizenHouseholdRecordComponent household) &&
            household.IsDisplaced != 0 &&
            household.RefugeeTentBuildingId != 0)
        {
            return status switch
            {
                CitizenStatus.GoingForWalk => citizen.PreferredWalkBuildingId != 0 ? citizen.PreferredWalkBuildingId : household.RefugeeTentBuildingId,
                CitizenStatus.AtRefugeeTent => household.RefugeeTentBuildingId,
                _ => household.RefugeeTentBuildingId
            };
        }

        return status switch
        {
            CitizenStatus.AtWork => citizen.WorkBuildingId != 0 ? citizen.WorkBuildingId : citizen.HomeBuildingId,
            CitizenStatus.AtShop => ResolveShopTarget(dayNightSystem, citizen),
            CitizenStatus.GoingToCityHall => citizen.PreferredCityHallBuildingId != 0 ? citizen.PreferredCityHallBuildingId : citizen.HomeBuildingId,
            CitizenStatus.GoingForWalk => citizen.PreferredWalkBuildingId != 0 ? citizen.PreferredWalkBuildingId : citizen.HomeBuildingId,
            _ => citizen.HomeBuildingId
        };
    }

    private static int GetSchedulePhaseState(DayNightSystem dayNightSystem)
    {
        if (dayNightSystem == null || IsNightSchedule(dayNightSystem))
            return 0;

        bool isWeekend = IsWeekend(GetDayOfWeek(dayNightSystem));
        float currentHour = dayNightSystem.CurrentHour;
        if (isWeekend)
        {
            if (currentHour >= WeekendShoppingStartHour && currentHour < WeekendShoppingEndHour)
                return 1;
            if (currentHour >= WeekendCityHallStartHour && currentHour < WeekendCityHallEndHour)
                return 2;
            return 3;
        }

        if (currentHour >= WeekdayEveningWalkStartHour && currentHour < WeekdayEveningWalkEndHour)
            return 4;
        if (currentHour >= WeekdayLunchStartHour && currentHour < WeekdayLunchEndHour)
            return 2;
        if (currentHour >= WeekdayWorkStartHour && currentHour < WeekdayWorkEndHour)
            return 1;
        return 3;
    }

    private static int GetDayOfWeek(DayNightSystem dayNightSystem)
    {
        if (dayNightSystem == null)
            return 1;

        return ((dayNightSystem.DayCount - 1) % 7) + 1;
    }

    private static bool IsNightSchedule(DayNightSystem dayNightSystem)
    {
        return dayNightSystem == null || dayNightSystem.IsNightTime;
    }

    private static bool IsWeekend(int dayOfWeek)
    {
        return dayOfWeek == 6 || dayOfWeek == 7;
    }

    private static bool ShouldCitizenShopOnWeekday(DayNightSystem dayNightSystem, CitizenRecordComponent citizen)
    {
        if (dayNightSystem == null)
            return false;

        return ((citizen.HouseholdId + dayNightSystem.DayCount) & 1) == 0;
    }

    private static int ResolveShopTarget(DayNightSystem dayNightSystem, CitizenRecordComponent citizen)
    {
        if (dayNightSystem != null &&
            !IsWeekend(GetDayOfWeek(dayNightSystem)) &&
            citizen.Gender == CitizenGender.Male &&
            citizen.LunchShopBuildingId != 0 &&
            dayNightSystem.CurrentHour >= WeekdayLunchStartHour &&
            dayNightSystem.CurrentHour < WeekdayLunchEndHour)
        {
            return citizen.LunchShopBuildingId;
        }

        return citizen.PreferredShopBuildingId != 0 ? citizen.PreferredShopBuildingId : citizen.HomeBuildingId;
    }
}
