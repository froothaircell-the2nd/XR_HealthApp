using CoreResources.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class App2_MenuView : UIView<App2_MenuView>
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
