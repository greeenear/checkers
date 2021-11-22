using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ui {
    public class ShowObject : MonoBehaviour {
        public DelayTimer timer;

        public async void SuccessfulSaving() {
            gameObject.SetActive(true);
            await timer.Timer(2);
            gameObject.SetActive(false);
        }
    }
}