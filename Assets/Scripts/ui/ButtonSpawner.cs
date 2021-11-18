using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ui {
    public class ButtonSpawner : MonoBehaviour
    {
        public ChangeActive changeActive;
        public LoadTemplate loadTemplate;

        private HashSet<LoadTemplate> a;

        private void OnEnable() {
            var template = Instantiate(loadTemplate);
            template.loadButton.onClick.AddListener(() => changeActive.ChangeActiveObject());
            a.Add(template);
        }

        private void OnDisable() {
            foreach (var i in a) {
                Destroy(i.gameObject);
            }

            a.Clear();
        }
    }
}