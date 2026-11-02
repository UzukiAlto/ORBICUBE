using UnityEngine;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace MainSystem
{
    /// <summary>
    /// Joystickアセットの入力をInput Systemの仮想デバイス入力へ橋渡しするクラス
    /// </summary>
    public class JoystickInputBridge : OnScreenControl
    {
        [Tooltip("入力を取得する対象のジョイスティック")]
        [SerializeField] private Joystick joystick;

        [InputControl(layout = "Vector2")]
        [SerializeField] private string targetControlPath = "<Gamepad>/leftStick";

        private Vector2 previousDirection = Vector2.zero;

        protected override string controlPathInternal
        {
            get => targetControlPath;
            set => targetControlPath = value;
        }


        private void Update()
        {
            if (joystick == null)
            {
                return;
            }

            Vector2 currentDirection = joystick.Direction;

            // 未操作時や入力値に変化がないフレームで不要なイベントを発行し他デバイスと競合するのを防ぐため
            if (currentDirection == previousDirection)
            {
                return;
            }

            previousDirection = currentDirection;
            SendValueToControl(currentDirection);
        }

        protected override void OnDisable()
        {
            // ドラッグ中にUIが非表示化された際、OnPointerUpが呼ばれず内部値やハンドル表示が残るのを防ぐため
            if (joystick != null)
            {
                joystick.OnPointerUp(null);
            }

            if (previousDirection != Vector2.zero)
            {
                previousDirection = Vector2.zero;
                SentDefaultValueToControl();
            }

            base.OnDisable();
        }
    }
}

