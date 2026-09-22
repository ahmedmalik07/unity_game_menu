using System;
using Aetherfall.Audio;
using UnityEngine.UIElements;

namespace Aetherfall.UI
{
    /// <summary>
    /// Modal yes/no dialog (ConfirmDialog.uxml). While open, the screen underneath is disabled so
    /// keyboard and gamepad focus cannot wander behind the dialog.
    /// </summary>
    public class ConfirmDialog
    {
        readonly VisualElement root;
        readonly Label kicker, title, message;
        readonly Button confirm, cancel;
        Action onConfirm;
        VisualElement blocked;
        Focusable previousFocus;

        public bool IsOpen { get; private set; }

        public ConfirmDialog(VisualElement root)
        {
            this.root = root;
            kicker = root.Q<Label>("dialog-kicker");
            title = root.Q<Label>("dialog-title");
            message = root.Q<Label>("dialog-message");
            confirm = root.Q<Button>("dialog-confirm");
            cancel = root.Q<Button>("dialog-cancel");

            UIUtil.Bind(confirm, Confirm);
            UIUtil.Bind(cancel, Cancel, UISound.Back);
            UIUtil.SetVisible(root, false, instant: true);
        }

        public void Show(string kickerText, string titleText, string messageText, string confirmText,
            Action confirmAction, VisualElement screenBehind, bool destructive = false, string cancelText = "CANCEL")
        {
            kicker.text = kickerText;
            title.text = titleText;
            message.text = messageText;
            confirm.text = confirmText;
            cancel.text = cancelText;
            confirm.EnableInClassList("pill-button--danger", destructive);
            onConfirm = confirmAction;

            previousFocus = root.focusController?.focusedElement;
            blocked = screenBehind;
            blocked?.SetEnabled(false);

            IsOpen = true;
            UIUtil.SetVisible(root, true);
            // Destructive actions default to the safe choice.
            UIUtil.FocusLater(destructive ? cancel : confirm);
        }

        public void Cancel() => Close(null);

        void Confirm() => Close(onConfirm);

        void Close(Action then)
        {
            if (!IsOpen) return;
            IsOpen = false;
            blocked?.SetEnabled(true);
            UIUtil.SetVisible(root, false);
            if (then == null && previousFocus is VisualElement element) UIUtil.FocusLater(element, 10);
            then?.Invoke();
        }
    }
}
