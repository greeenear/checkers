using System;
using System.Threading.Tasks;
using UnityEngine;

namespace ui {
    public class DelayTimer : MonoBehaviour {
        public async Task Timer(int waitingTime) {
            await Task.Delay(TimeSpan.FromSeconds(waitingTime));
        }
    }
}
