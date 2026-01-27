namespace Project.Core.Settings.Ui
{
    public readonly struct SettingsUiResult
    {
        public readonly bool SelectionChanged;
        public readonly bool ModeChanged;

        public readonly bool EnteredFolder;
        public readonly bool ExitedFolderOrRoot;

        public readonly bool StartedEdit;
        public readonly bool CancelledEdit;
        public readonly bool CommittedEdit;

        public readonly bool StartedConfirmAction;
        public readonly bool CancelledConfirmAction;
        public readonly bool ConfirmedAction;

        public readonly bool ConfirmedBackItem;
        public readonly bool BackFromRoot;

        public readonly bool HandledByHooks;

        public readonly SettingsItem AffectedItem;
        public readonly SettingsItemType? AffectedItemType;

        public readonly bool ValueChanged;
        public readonly bool ToggleValue;
        public readonly int ListIndex;
        public readonly float RangeValue;

        public readonly bool ActionExecuted;

        public SettingsUiResult(
            bool selectionChanged = false,
            bool modeChanged = false,
            bool enteredFolder = false,
            bool exitedFolderOrRoot = false,
            bool startedEdit = false,
            bool cancelledEdit = false,
            bool committedEdit = false,
            bool startedConfirmAction = false,
            bool cancelledConfirmAction = false,
            bool confirmedAction = false,
            bool confirmedBackItem = false,
            bool backFromRoot = false,

            bool handledByHooks = false,
            SettingsItem affectedItem = null,
            SettingsItemType? affectedItemType = null,

            bool valueChanged = false,
            bool toggleValue = false,
            int listIndex = 0,
            float rangeValue = 0f,

            bool actionExecuted = false)
        {
            SelectionChanged = selectionChanged;
            ModeChanged = modeChanged;

            EnteredFolder = enteredFolder;
            ExitedFolderOrRoot = exitedFolderOrRoot;

            StartedEdit = startedEdit;
            CancelledEdit = cancelledEdit;
            CommittedEdit = committedEdit;

            StartedConfirmAction = startedConfirmAction;
            CancelledConfirmAction = cancelledConfirmAction;
            ConfirmedAction = confirmedAction;

            ConfirmedBackItem = confirmedBackItem;
            BackFromRoot = backFromRoot;

            HandledByHooks = handledByHooks;

            AffectedItem = affectedItem;
            AffectedItemType = affectedItemType;

            ValueChanged = valueChanged;
            ToggleValue = toggleValue;
            ListIndex = listIndex;
            RangeValue = rangeValue;

            ActionExecuted = actionExecuted;
        }

        public static SettingsUiResult None => new SettingsUiResult();
    }
}