namespace ONIAnywhereDoor
{
    /// <summary>侧边栏按钮的公共基类。</summary>
    public abstract class AnywhereDoorButton : KMonoBehaviour, ISidescreenButtonControl
    {
        protected AnywhereDoor door;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            door = GetComponent<AnywhereDoor>();
        }

        public virtual string SidescreenTitle
        {
            get { return STRINGS.UI.ONIANYWHEREDOOR.TITLE; }
        }

        public abstract string SidescreenButtonText { get; }

        public abstract string SidescreenButtonTooltip { get; }

        public virtual void SetButtonTextOverride(ButtonMenuTextOverride textOverride)
        {
        }

        public virtual bool SidescreenEnabled()
        {
            return true;
        }

        public virtual bool SidescreenButtonInteractable()
        {
            return door != null;
        }

        public abstract void OnSidescreenButtonPressed();

        public virtual int HorizontalGroupID()
        {
            return -1;
        }

        public abstract int ButtonSideScreenSortOrder();
    }

    /// <summary>复制人通行开关。</summary>
    public sealed class AnywhereDoorDupeButton : AnywhereDoorButton
    {
        public override string SidescreenButtonText
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

        public override string SidescreenButtonTooltip
        {
            get { return STRINGS.UI.ONIANYWHEREDOOR.DUPE_TOOLTIP; }
        }

        public override void OnSidescreenButtonPressed()
        {
            if (door != null)
            {
                door.SetAllowDuplicants(!door.AllowDuplicants);
            }
        }

        public override int ButtonSideScreenSortOrder()
        {
            return 10;
        }
    }

    /// <summary>小动物通行开关。</summary>
    public sealed class AnywhereDoorCritterButton : AnywhereDoorButton
    {
        public override string SidescreenButtonText
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

        public override string SidescreenButtonTooltip
        {
            get { return STRINGS.UI.ONIANYWHEREDOOR.CRITTER_TOOLTIP; }
        }

        public override void OnSidescreenButtonPressed()
        {
            if (door != null)
            {
                door.SetAllowCritters(!door.AllowCritters);
            }
        }

        public override int ButtonSideScreenSortOrder()
        {
            return 20;
        }
    }
}
