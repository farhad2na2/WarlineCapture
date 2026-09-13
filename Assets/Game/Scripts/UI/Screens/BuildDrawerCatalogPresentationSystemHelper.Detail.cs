using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Catalog.Contracts;
using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    internal static partial class BuildDrawerCatalogPresentationSystemHelper
    {
        private static void BindDetail(Context context, BuildDrawerCatalogItem model)
        {
            BuildingUiCommandFailure failure = context.GetCampRequestFailure(model, out _);
            context.View.BindCreditCost(model.CreditsCost);
            context.View.BindDetail(
                model.DisplayName,
                model.TypeLabel,
                model.Description,
                FormatPrice(model.MaterialsCost),
                model.CreditsCost > 0 ? FormatPrice(model.CreditsCost) : "",
                FormatDuration(model),
                FormatPlacement(model),
                FormatRequirements(context.TextResolver, model),
                model.ActionPortrait,
                model.CardPortrait,
                model.ActionLabel,
                failure == BuildingUiCommandFailure.None);
        }
    }
}
