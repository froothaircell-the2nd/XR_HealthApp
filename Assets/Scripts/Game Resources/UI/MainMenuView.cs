using CoreResources.UI;
using UnityEngine.UI;

namespace GameResources.UI
{
    public class MainMenuView : UIView<MainMenuView>
    {
        public Button PlayButton;
        public Button QuitButton;

        public override void InitializeViewElements()
        {
        }

        public override void DeInitializeViewElements()
        {
            PlayButton.onClick.RemoveAllListeners();
            QuitButton.onClick.RemoveAllListeners();
        }
    }
}