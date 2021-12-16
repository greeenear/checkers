using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ui {
    public class RingImageChanger : MonoBehaviour {
        public List<Image> imageStorage;

        private Image imageRef;
        private int counter;

        private void Awake() {
            if (gameObject.TryGetComponent(out Image image)) {
                imageRef = image;
            } else {
                Debug.LogError("NoImageComponent");
                enabled = false;
                return;
            }
        }

        public void Change() {
            if (imageRef == null) {
                return;
            }

            imageRef.sprite = imageStorage[(counter++) % (imageStorage.Count)].sprite;
        }
    }
}
