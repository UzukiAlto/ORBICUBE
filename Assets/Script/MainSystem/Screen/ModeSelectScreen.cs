using System;
using UnityEngine;

namespace MainSystem
{
    /// <summary>
    /// モード選択画面の管理を行うクラス
    /// </summary>
    public class ModeSelectScreen : ScreenBase
    {
        public event Action<bool> OnOperationEnableChanged;

        public override void Hide()
        {
            base.Hide();
            screenCanvas.SetActive(false);
            SetEnableOperation(false);
        }

        public override void Show()
        {
            base.Show();
            screenCanvas.SetActive(true);
            SetEnableOperation(true);
        }

        public override void SetEnableOperation(bool enable)
        {
            base.SetEnableOperation(enable);
            // サブ画面（Config・Score）への遷移時を含め、モード選択操作の有効状態にUI表示を追従させるため
            OnOperationEnableChanged?.Invoke(enable);
        }
    }
}
