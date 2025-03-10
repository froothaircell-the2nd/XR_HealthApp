using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;


namespace CoreResources.UI
{
    public class SelectionButton_ColorAndSprite : UIButton_PersistentPress , IPointerClickHandler
    {
        [Header("Required button esentials")]
        [SerializeField]
        private TextMeshProUGUI ButtonText;
        [SerializeField]
        private Image HoverImage;
        [SerializeField]
        private Image ButtonImage;
        private Button ButtonElement;

        [Header("Color States")]
        [SerializeField]
        private Color _selectedColor;
        [SerializeField]
        private Color _unSelectedColor;

        [Header("Sprite States")]
        [SerializeField]
        private Sprite _selectedSprite;
        [SerializeField]
        private Sprite _unSelectedSprite;
        [SerializeField]
        private Sprite _disabledSprite;


        private void OnEnable()
        {
            ButtonElement = this.GetComponent<Button>();
            if (ButtonElement.interactable)
            {
                if (HoverImage != null)
                    HoverImage.sprite = _unSelectedSprite;
                if (ButtonText != null)
                    ButtonText.color = new Color(_unSelectedColor.r, _unSelectedColor.g, _unSelectedColor.b, 1);
            }
        }

        #region Pointer Listeners
     
        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);

            if (ButtonElement.interactable)
            {
                if (HoverImage != null)
                    HoverImage.sprite = _selectedSprite;
                if (ButtonText != null)
                    ButtonText.color = new Color(_selectedColor.r, _selectedColor.g, _selectedColor.b, 1);
            }
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);

            if (ButtonElement.interactable)
            {
                if (HoverImage != null)
                    HoverImage.sprite = _unSelectedSprite;
                if (ButtonText != null)
                    ButtonText.color = new Color(_unSelectedColor.r, _unSelectedColor.g, _unSelectedColor.b, 1);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (ButtonElement.interactable)
            {
                if (HoverImage != null)
                    HoverImage.sprite = _selectedSprite;
                if (ButtonText != null)
                    ButtonText.color = new Color(_selectedColor.r, _selectedColor.g, _selectedColor.b, 1);
            }
        }

        #endregion
    }
}

