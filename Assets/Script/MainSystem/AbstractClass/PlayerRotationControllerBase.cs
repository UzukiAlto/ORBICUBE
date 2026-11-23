using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MainSystem
{
    /// <summary>
    /// キューブの回転を制御する基底クラス
    /// 入力購読、ポインタロック制御、デルタクランプ、回転ロジックを統合管理
    /// </summary>
    public abstract class PlayerRotationControllerBase : MonoBehaviour
    {
        [SerializeField] protected ScreenBase currentScreen;
        [SerializeField] protected GameObject playerCameraObj;
        [SerializeField] protected GameObject rotateCenterObj;
        [SerializeField] protected GameObject playerCameraInputObject;
        [SerializeField] private ConfigData configData;

        [SerializeField] private float sensitivity;
        private const float MinSensitivity = 40f;
        private const float MaxSensitivity = 100f;

        // WebGL環境でのデルタスパイクによる急激な画面ワープを防ぎ、かつ滑らかに動く最大許容移動量
        private const float MaxDeltaPerFrame = 0.08f;

        protected IPlayerCameraInput playerCameraInput;
        protected bool isCameraMoving = false;
        protected int skipDeltaFrames = 0;

        protected virtual void Awake()
        {
            if (playerCameraInputObject != null)
            {
                playerCameraInput = playerCameraInputObject.GetComponent<IPlayerCameraInput>();
            }
        }

        protected virtual void OnEnable()
        {
            configData.SubscribeSensitivity(SetSensitivity);

            if (playerCameraInput != null)
            {
                playerCameraInput.OnCameraMoveStart += OnCameraMoveStart;
                playerCameraInput.OnCameraMovingDelta += OnCameraMovingDelta;
                playerCameraInput.OnCameraMoveEnd += OnCameraMoveEnd;
            }

            isCameraMoving = false;
        }

        protected virtual void OnDisable()
        {
            configData.UnsubscribeSensitivity(SetSensitivity);

            // 画面無効化時にドラッグ状態のリセットおよびカーソルロック解除を行うため
            UnlockCursor();

            if (playerCameraInput != null)
            {
                playerCameraInput.OnCameraMoveStart -= OnCameraMoveStart;
                playerCameraInput.OnCameraMovingDelta -= OnCameraMovingDelta;
                playerCameraInput.OnCameraMoveEnd -= OnCameraMoveEnd;
            }
        }

        private void SetSensitivity(float configSensitivity)
        {
            this.sensitivity = Mathf.Lerp(MinSensitivity, MaxSensitivity, configSensitivity);
        }

        protected virtual void OnCameraMoveStart(Vector2 pointerPos)
        {
            // 操作可能でない画面ではドラッグによるカメラ回転を開始させないため
            if (currentScreen != null && !currentScreen.canOperate)
            {
                return;
            }

            isCameraMoving = true;

            if (!MobileInputManager.IsMobile)
            {
                // ロック直後に発生する初期ワープデルタを破棄するため
                skipDeltaFrames = 2;
                // ドラッグ中のマウス移動量を安定して取得するためカーソルをロック
                WebGLPointerLockBridge.RequestLock();
            }
        }

        protected virtual void OnCameraMovingDelta(Vector2 delta)
        {
            // UIクリック時など、ドラッグ中でない場合は回転させないため
            if (!isCameraMoving)
            {
                return;
            }

            // ロック直後のジャンプ差分をスキップしてカメラの初期ワープを防ぐため
            if (skipDeltaFrames > 0)
            {
                skipDeltaFrames--;
                return;
            }

            // WebGLのPointer Lock特有の巨大スパイクを安全な上限に丸め、ワープとカクつき（完全停止）を両立防止
            Vector2 clampedDelta = new Vector2(
                Mathf.Clamp(delta.x, -MaxDeltaPerFrame, MaxDeltaPerFrame),
                Mathf.Clamp(delta.y, -MaxDeltaPerFrame, MaxDeltaPerFrame)
            );

            // PlayerRotationControllerBaseのRotateメソッドを呼び出してカメラを回転させる
            Rotate(clampedDelta);
        }

        protected virtual void OnCameraMoveEnd(Vector2 pointerPos)
        {
            // 操作可能状態にかかわらず、クリック終了時は確実にドラッグ状態を終了しカーソルを復帰させるため
            UnlockCursor();
        }

        protected virtual void UnlockCursor()
        {
            isCameraMoving = false;
            skipDeltaFrames = 0;

            if (!MobileInputManager.IsMobile)
            {
                WebGLPointerLockBridge.ExitLock();
            }
        }

        public void Rotate(Vector2 rotationAngle)
        {
            // 操作可能でないなら処理を行わない
            if (currentScreen != null && !currentScreen.canOperate)
            {
                return;
            }

            rotationAngle *= sensitivity;

            playerCameraObj.transform.RotateAround(rotateCenterObj.transform.position, playerCameraObj.transform.up, rotationAngle.x);
            playerCameraObj.transform.RotateAround(rotateCenterObj.transform.position, playerCameraObj.transform.right, -rotationAngle.y);
        }
    }
}
