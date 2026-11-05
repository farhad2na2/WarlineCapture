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
        internal static void PreviewOnly(Context context, BuildDrawerCatalogItem model) => BindDetail(context, model, false);

        private static void BindDetail(Context context, BuildDrawerCatalogItem model, bool selected = true)
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
                selected ? FormatRequirements(context.TextResolver, model)
                    : context.TextResolver.Format("hud.build.preview_hint", "Select {0} to begin.", UiLocalizedText.CatalogLabel(model.DisplayName)),
                model.ActionPortrait,
                model.CardPortrait,
                model.ActionLabel,
                selected && failure == BuildingUiCommandFailure.None);
        }
    }
}
