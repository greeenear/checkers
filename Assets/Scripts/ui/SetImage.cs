using UnityEngine;
using UnityEngine.UI;

namespace ui {
    public class SetImage : MonoBehaviour {
        public void SpawnImage(RawImage image) {
            Instantiate(image, gameObject.transform);
        }
    }
}
