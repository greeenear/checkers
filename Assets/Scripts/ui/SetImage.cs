using UnityEngine;
using UnityEngine.UI;

namespace ui {
    public class SetImage : MonoBehaviour {
        public void InstantiateImage(RawImage image) {
            Instantiate(image, gameObject.transform);
        }
    }
}