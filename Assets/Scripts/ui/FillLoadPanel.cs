using UnityEngine;
using controller;
using System.IO;
using UnityEngine.UI;

namespace ui {
    public class FillLoadPanel : MonoBehaviour {
        public FillBoardImage boardImage;
        public SetImage whoseMoveImage;
        public SetText date;
        public SetText kind;
        public SetButtonEvent delete;
        public SetButtonEvent load;

        public void FillPanel(
            GameObject panel,
            SaveInfo save,
            UiResources res,
            Controller gameController,
            Button openMenu
        ) {
            boardImage.Fill(save, res);
            date.WriteText(save.saveDate.ToString("dd.MM.yyyy HH:mm:ss"));
            kind.WriteText("Checker Kind: " + save.checkerKind.ToString());
            delete.SetLisener(() => File.Delete(save.fileName));
            delete.SetLisener(() => Destroy(panel.gameObject));
            load.SetLisener(() => gameController.Load(save.fileName));
            load.SetLisener(() => openMenu.onClick?.Invoke());

            var whoseMovePref = res.whiteCheckerImage;
            if (save.whoseMove == controller.ChColor.Black) {
                whoseMovePref = res.blackCheckerImage;
            }
            whoseMoveImage.InstantiateImage(whoseMovePref);
        }
    }
}