using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace MainSystem
{
    /// <summary>
    /// Joystickアセットの入力をInput Systemの仮想デバイス入力へ橋渡しし、マルチタッチ時の操作指固定を行うクラス
    /// </summary>
    public class JoystickInputBridge : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const int NoPointerId = int.MinValue;

        [Tooltip("入力を取得する対象のジョイスティック")]
        [SerializeField] private Joystick joystick;

        [InputControl(layout = "Vector2")]
        [SerializeField] private string targetControlPath = "<Gamepad>/leftStick";

        private Vector2 previousDirection = Vector2.zero;
        private int activePointerId = NoPointerId;

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

        public void OnPointerDown(PointerEventData eventData)
        {
            if (joystick == null || eventData == null)
            {
                return;
            }

            // 既に1本目の指でジョイスティックを操作中の場合、2本目以降の指の接触で位置がリセットされるのを防ぐため
            if (activePointerId != NoPointerId)
            {
                return;
            }

            activePointerId = eventData.pointerId;

            // 1本目の指の押下時はExecuteEventsからFloatingJoystick.OnPointerDownも直接呼ばれるため、
            // 押下直後に無効化して以降のドラッグ中・2本目の指のイベントを本クラスのみでフィルタリングする
            joystick.enabled = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (joystick == null || eventData == null)
            {
                return;
            }

            // 操作を開始した指以外のドラッグ入力が混ざるのを防ぐため
            if (eventData.pointerId != activePointerId)
            {
                return;
            }

            joystick.OnDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (joystick == null || eventData == null)
            {
                return;
            }

            // 操作中の指以外が離れた際にジョイスティックが中断されるのを防ぐため
            if (eventData.pointerId != activePointerId)
            {
                return;
            }

            activePointerId = NoPointerId;
            joystick.OnPointerUp(eventData);

            // 次の1本目のタッチを受け付けられるよう再度有効化する
            joystick.enabled = true;
        }

        protected override void OnDisable()
        {
            activePointerId = NoPointerId;

            // ドラッグ中にUIが非表示化された際、OnPointerUpが呼ばれず内部値やハンドル表示が残るのを防ぐため
            if (joystick != null)
            {
                joystick.OnPointerUp(null);
                joystick.enabled = true;
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

