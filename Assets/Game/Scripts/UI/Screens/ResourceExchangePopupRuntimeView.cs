using System.Collections.Generic;
using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ResourceExchangePopupView))]
    public sealed class ResourceExchangePopupRuntimeView : MonoBehaviour
    {
        [SerializeField] private ResourceExchangePopupView view;

        private readonly List<ButtonBinding> _bindings = new();
        private uint _lastAppliedVersion;
        private bool _hasAppliedVersion;

        public ResourceExchangePopupView View => view;

        private void Awake()
        {
            if (view == null)
                view = GetComponent<ResourceExchangePopupView>();
        }

        private void OnEnable()
        {
            WireButtons();
            RefreshNow(force: true);
        }

        private void Update()
        {
            RefreshNow(force: false);
        }

        private void OnDisable()
        {
            ClearBindings();
            _hasAppliedVersion = false;
            _lastAppliedVersion = 0;
        }

        public void ConfigureForTests(ResourceExchangePopupView popupView)
        {
            view = popupView;
        }

        public void RefreshNow(bool force = false)
        {
            if (view == null || !view.IsOpen)
                return;

            if (!UiShellRuntimeGateway.TryReadResourceExchange(out UiResourceExchangeModel model))
                return;

            if (!force && _hasAppliedVersion && _lastAppliedVersion == model.Version)
                return;

            view.ApplyModel(model);
            _lastAppliedVersion = model.Version;
            _hasAppliedVersion = true;
        }

        private void WireButtons()
        {
            ClearBindings();
            if (view == null)
                return;

            AddBinding(view.ExportTabButton, () => Enqueue(UiActionKind.ResourceExchangeTab, (int)UiResourceExchangeTabKind.Export));
            AddBinding(view.ImportTabButton, () => Enqueue(UiActionKind.ResourceExchangeTab, (int)UiResourceExchangeTabKind.Import));
            AddBinding(view.AmountDecreaseButton, () => Enqueue(UiActionKind.ResourceExchangeAmountDecrease, 0));
            AddBinding(view.AmountIncreaseButton, () => Enqueue(UiActionKind.ResourceExchangeAmountIncrease, 0));
            AddBinding(view.ConfirmButton, () => Enqueue(UiActionKind.ResourceExchangeConfirm, 0));
            AddBinding(view.RushAllButton, () => Enqueue(UiActionKind.ResourceExchangeRushAll, 0));
            AddBinding(view.ClearCompletedButton, () => Enqueue(UiActionKind.ResourceExchangeClearCompleted, 0));

            ResourceExchangeRecipeCardView[] cards = view.StaticRecipeCards;
            if (cards != null)
            {
                for (int i = 0; i < cards.Length; i++)
                {
                    ResourceExchangeRecipeCardView card = cards[i];
                    if (card == null || card.SelectionButton == null)
                        continue;

                    int slot = i;
                    AddBinding(card.SelectionButton, () => Enqueue(UiActionKind.ResourceExchangeRecipe, slot));
                }
            }

            ResourceExchangeQueueItemView[] rows = view.StaticQueueRows;
            if (rows != null)
            {
                for (int i = 0; i < rows.Length; i++)
                {
                    ResourceExchangeQueueItemView row = rows[i];
                    if (row == null)
                        continue;

                    AddBinding(row.RushButton, () => EnqueueQueueAction(UiActionKind.ResourceExchangeQueueRush, row));
                    AddBinding(row.CancelButton, () => EnqueueQueueAction(UiActionKind.ResourceExchangeQueueCancel, row));
                }
            }
        }

        private void AddBinding(Button button, UnityAction action)
        {
            if (button == null || action == null)
                return;

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
            _bindings.Add(new ButtonBinding(button, action));
        }

        private void ClearBindings()
        {
            for (int i = 0; i < _bindings.Count; i++)
            {
                ButtonBinding binding = _bindings[i];
                if (binding.Button != null && binding.Action != null)
                    binding.Button.onClick.RemoveListener(binding.Action);
            }

            _bindings.Clear();
        }

        private static void Enqueue(UiActionKind kind, int payloadId)
        {
            UiShellRuntimeGateway.TryEnqueueUiAction(kind, payloadId);
        }

        private static void EnqueueQueueAction(UiActionKind kind, ResourceExchangeQueueItemView row)
        {
            if (row == null || !UiShellRuntimeGateway.TryReadResourceExchange(out UiResourceExchangeModel model))
                return;

            ResourceExchangeQueueItemView[] rows = row.GetComponentInParent<ResourceExchangePopupView>()?.StaticQueueRows;
            if (rows == null)
                return;

            for (int i = 0; i < rows.Length && i < model.QueueRowCount; i++)
            {
                if (rows[i] != row)
                    continue;

                UiResourceExchangeQueueRowModel rowModel = model.GetQueueRow(i);
                Enqueue(kind, rowModel.QueueItemId);
                return;
            }
        }

        private readonly struct ButtonBinding
        {
            public readonly Button Button;
            public readonly UnityAction Action;

            public ButtonBinding(Button button, UnityAction action)
            {
                Button = button;
                Action = action;
            }
        }
    }
}
