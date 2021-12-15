using UnityEngine;
using UnityEngine.UI;

namespace ui {
    public class ToggleImage : MonoBehaviour {
        public Image activeImage;
        public Image newImage;

        public void Change() {
            activeImage.sprite = newImage.sprite;
        }
    }
}
