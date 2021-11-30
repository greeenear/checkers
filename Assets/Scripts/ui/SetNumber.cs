using UnityEngine;
using UnityEngine.UI;

namespace ui {
    public class SetNumber : MonoBehaviour {
        public Text text;
        private void OnEnable() {
            text.text = gameObject.transform.parent.childCount.ToString();
        }
    }
}
