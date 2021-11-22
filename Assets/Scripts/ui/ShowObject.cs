using System;
using System.Threading.Tasks;
using UnityEngine;

namespace ui {
    public class ShowObject : MonoBehaviour {
        public int waitingTime;

        public async void Show() {
            gameObject.SetActive(true);
            await Task.Delay(TimeSpan.FromSeconds(waitingTime));
            gameObject.SetActive(false);
        }
    }
}