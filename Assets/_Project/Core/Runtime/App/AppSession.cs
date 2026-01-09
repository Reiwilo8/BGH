namespace Project.Core.App
{
    public enum HubReturnTarget
    {
        Main,
        GameSelect
    }

    public sealed class AppSession
    {
        public string SelectedGameId { get; private set; }
        public string SelectedModeId { get; private set; }

        public HubReturnTarget HubTarget { get; private set; } = HubReturnTarget.Main;

        public void SelectGame(string gameId)
        {
            SelectedGameId = gameId;
            SelectedModeId = null;
        }

        public void SelectMode(string modeId)
        {
            SelectedModeId = modeId;
        }

        public void SetHubTarget(HubReturnTarget target)
        {
            HubTarget = target;
        }

        public void Clear()
        {
            SelectedGameId = null;
            SelectedModeId = null;
            HubTarget = HubReturnTarget.Main;
        }
    }
}