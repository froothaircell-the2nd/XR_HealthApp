using GameResources.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameResources.Gameplay
{
    public class InteractionPanel : MonoBehaviour, ICursorInteractable
    {
        private bool _isInteractable = false;

        public bool IsInteractable
        {
            get => _isInteractable;
            private set => _isInteractable = value;
        }

        public void InitializePanel()
        {
            IsInteractable = true;
        }

        public void DeInitializePanel()
        {
            IsInteractable = false;
        }
    }
}