namespace ONIAnywhereDoor
{
    /// <summary>
    /// Shared pass-through toggle logic. The buttons live in the action button
    /// area below the building details panel, next to vanilla actions such as
    /// Deconstruct and Copy Settings.
    /// </summary>
    public abstract class AnywhereDoorMenuButton : KMonoBehaviour
    {
        protected AnywhereDoor door;

        private static readonly EventSystem.IntraObjectHandler<AnywhereDoorMenuButton> OnRefreshUserMenuDelegate =
            new EventSystem.IntraObjectHandler<AnywhereDoorMenuButton>(
                delegate(AnywhereDoorMenuButton component, object data)
                {
                    component.OnRefreshUserMenu(data);
                });

        protected abstract string IconName { get; }

        protected abstract string ButtonText { get; }

        protected abstract string ButtonTooltip { get; }

        protected abstract float SortOrder { get; }

        protected abstract void OnPressed();

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            Subscribe(493375141, OnRefreshUserMenuDelegate);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            door = GetComponent<AnywhereDoor>();
            RefreshUserMenu();
        }

        private void OnRefreshUserMenu(object data)
        {
            if (door == null)
            {
                door = GetComponent<AnywhereDoor>();
            }
            if (door == null || Game.Instance == null || Game.Instance.userMenu == null)
            {
                return;
            }

            KIconButtonMenu.ButtonInfo button = new KIconButtonMenu.ButtonInfo(
                IconName,
                ButtonText,
                OnPressed,
                Action.NumActions,
                null,
                null,
                null,
                ButtonTooltip,
                true);
            Game.Instance.userMenu.AddButton(gameObject, button, SortOrder);
        }

        private void RefreshUserMenu()
        {
            if (Game.Instance != null && Game.Instance.userMenu != null)
            {
                Game.Instance.userMenu.Refresh(gameObject);
            }
        }
    }

    /// <summary>Duplicant passage toggle (allowed by default).</summary>
    public sealed class AnywhereDoorDupeButton : AnywhereDoorMenuButton
    {
        protected override string IconName
        {
            get { return "action_control"; }
        }

        protected override string ButtonText
        {
            get
            {
                bool allowed = door != null && door.AllowDuplicants;
                string state = allowed
                    ? STRINGS.UI.ONIANYWHEREDOOR.DUPE_ALLOWED
                    : STRINGS.UI.ONIANYWHEREDOOR.DUPE_FORBIDDEN;
                return string.Format(STRINGS.UI.ONIANYWHEREDOOR.DUPE, state);
            }
        }

        protected override string ButtonTooltip
        {
            get
            {
                string state = (door != null && door.AllowDuplicants)
                    ? STRINGS.UI.ONIANYWHEREDOOR.DUPE_ALLOWED
                    : STRINGS.UI.ONIANYWHEREDOOR.DUPE_FORBIDDEN;
                string format = STRINGS.UI.ONIANYWHEREDOOR.CURRENT_STATE;
                return string.Format(format + "\n{1}", state,
                                     STRINGS.UI.ONIANYWHEREDOOR.DUPE_TOOLTIP);
            }
        }

        protected override float SortOrder
        {
            get { return 2f; }
        }

        protected override void OnPressed()
        {
            if (door != null)
            {
                door.SetAllowDuplicants(!door.AllowDuplicants);
            }
        }
    }

    /// <summary>Critter passage toggle (blocked by default).</summary>
    public sealed class AnywhereDoorCritterButton : AnywhereDoorMenuButton
    {
        protected override string IconName
        {
            get { return "action_capture"; }
        }

        protected override string ButtonText
        {
            get
            {
                bool allowed = door != null && door.AllowCritters;
                string state = allowed
                    ? STRINGS.UI.ONIANYWHEREDOOR.CRITTER_ALLOWED
                    : STRINGS.UI.ONIANYWHEREDOOR.CRITTER_FORBIDDEN;
                return string.Format(STRINGS.UI.ONIANYWHEREDOOR.CRITTER, state);
            }
        }

        protected override string ButtonTooltip
        {
            get
            {
                string state = (door != null && door.AllowCritters)
                    ? STRINGS.UI.ONIANYWHEREDOOR.CRITTER_ALLOWED
                    : STRINGS.UI.ONIANYWHEREDOOR.CRITTER_FORBIDDEN;
                string format = STRINGS.UI.ONIANYWHEREDOOR.CURRENT_STATE;
                return string.Format(format + "\n{1}", state,
                                     STRINGS.UI.ONIANYWHEREDOOR.CRITTER_TOOLTIP);
            }
        }

        protected override float SortOrder
        {
            get { return 3f; }
        }

        protected override void OnPressed()
        {
            if (door != null)
            {
                door.SetAllowCritters(!door.AllowCritters);
            }
        }
    }
}
