using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;
using Game;

namespace MainSystem
{
    /// <summary>
    /// プレイヤーのカメラ回転をInput Systemで処理するクラス
    /// </summary>
    public class PlayerCameraInputManager : MonoBehaviour, IPlayerCameraInput
    {
        private const int NoTouchId = -1;

        // IPlayerCameraInputのイベント
        public event Action<Vector2> OnCameraMoveStart;
        public event Action<Vector2> OnCameraMoving;
        public event Action<Vector2> OnCameraMoveEnd;
        public event Action<Vector2> OnCameraMovingDelta;

        // Input Systemのアクション
        private PlayerInputActions inputActions;
        private bool isMoving = false;
        private int activeCameraTouchId = NoTouchId;
        private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

        private void Awake()
        {
            inputActions = new PlayerInputActions();
        }

        private void OnEnable()
        {
            inputActions.Enable();
            // Input Systemのアクションにイベントハンドラーを登録
            inputActions.Player.PointerPress.started += HandleCameraMoveStarted;
            inputActions.Player.PointerPosition.performed += HandleCameraMovePerformed;
            inputActions.Player.PointerPress.canceled += HandleCameraMoveCanceled;
            inputActions.Player.PointerDelta.performed += HandleCameraMoveDeltaPerformed;
        }

        private void OnDisable()
        {
            // Input Systemのアクションからイベントハンドラーを解除
            inputActions.Player.PointerPress.started -= HandleCameraMoveStarted;
            inputActions.Player.PointerPosition.performed -= HandleCameraMovePerformed;
            inputActions.Player.PointerPress.canceled -= HandleCameraMoveCanceled;
            inputActions.Player.PointerDelta.performed -= HandleCameraMoveDeltaPerformed;

            activeCameraTouchId = NoTouchId;
            isMoving = false;
            inputActions.Disable();
        }

        private void Update()
        {
            UpdateTouchCameraInput();
        }

        private void UpdateTouchCameraInput()
        {
            if (Touchscreen.current == null)
            {
                return;
            }

            // カメラ回転用の指がまだ割り当てられていない場合は、新規タッチからUI外の指を探す
            if (activeCameraTouchId == NoTouchId)
            {
                TryStartCameraTouch();
                return;
            }

            ProcessActiveCameraTouch();
        }

        private void TryStartCameraTouch()
        {
            if (CurrentScreen.current == null)
            {
                return;
            }

            foreach (TouchControl touch in Touchscreen.current.touches)
            {
                UnityEngine.InputSystem.TouchPhase phase = touch.phase.ReadValue();
                if (phase != UnityEngine.InputSystem.TouchPhase.Began)
                {
                    continue;
                }

                Vector2 screenPos = touch.position.ReadValue();

                // ジョイスティックやボタン等のUIに触れた指はカメラ回転に使わず、2本目以降の指でカメラを回せるようにするため
                if (IsTouchOverInteractiveUI(screenPos))
                {
                    continue;
                }

                activeCameraTouchId = touch.touchId.ReadValue();
                isMoving = true;

                Vector2 startViewportPos = CurrentScreen.current.screenCamera.ScreenToViewportPoint(screenPos);
                OnCameraMoveStart?.Invoke(startViewportPos);
                return;
            }
        }

        private void ProcessActiveCameraTouch()
        {
            TouchControl trackedTouch = FindTouchById(activeCameraTouchId);
            if (trackedTouch == null)
            {
                activeCameraTouchId = NoTouchId;
                isMoving = false;
                return;
            }

            UnityEngine.InputSystem.TouchPhase phase = trackedTouch.phase.ReadValue();
            Vector2 screenPos = trackedTouch.position.ReadValue();

            if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                phase == UnityEngine.InputSystem.TouchPhase.Canceled ||
                !trackedTouch.isInProgress)
            {
                activeCameraTouchId = NoTouchId;
                isMoving = false;

                if (CurrentScreen.current == null)
                {
                    return;
                }

                Vector2 endViewportPos = CurrentScreen.current.screenCamera.ScreenToViewportPoint(screenPos);
                OnCameraMoveEnd?.Invoke(endViewportPos);
                return;
            }

            if (phase != UnityEngine.InputSystem.TouchPhase.Moved)
            {
                return;
            }

            Vector2 rawDelta = trackedTouch.delta.ReadValue();
            if (rawDelta == Vector2.zero)
            {
                return;
            }

            if (CurrentScreen.current != null)
            {
                Vector2 currentViewportPos = CurrentScreen.current.screenCamera.ScreenToViewportPoint(screenPos);
                OnCameraMoving?.Invoke(currentViewportPos);
            }

            Vector2 normalizedDelta = rawDelta / Screen.height;
            OnCameraMovingDelta?.Invoke(normalizedDelta);
        }

        private TouchControl FindTouchById(int touchId)
        {
            if (Touchscreen.current == null)
            {
                return null;
            }

            foreach (TouchControl touch in Touchscreen.current.touches)
            {
                if (touch.touchId.ReadValue() == touchId)
                {
                    return touch;
                }
            }

            return null;
        }

        private bool IsTouchOverInteractiveUI(Vector2 screenPos)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = screenPos
            };

            raycastResults.Clear();
            EventSystem.current.RaycastAll(pointerData, raycastResults);

            foreach (RaycastResult result in raycastResults)
            {
                if (result.gameObject == null)
                {
                    continue;
                }

                // 操作系UI上のタッチをカメラ回転から除外し、同時押し時に各UI操作とカメラ回転を独立させるため
                if (result.gameObject.GetComponentInParent<Joystick>() != null ||
                    result.gameObject.GetComponentInParent<OnScreenControl>() != null ||
                    result.gameObject.GetComponentInParent<Selectable>() != null ||
                    result.gameObject.GetComponentInParent<EventTrigger>() != null ||
                    result.gameObject.GetComponentInParent<MenuCube>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        // カメラ移動開始時のスクリーン座標を取得し、ビューポート座標に変換してイベント発火
        private void HandleCameraMoveStarted(InputAction.CallbackContext context)
        {
            // タッチ入力はマルチタッチ対応のためUpdateTouchCameraInput側で個別の指ごとに処理する
            if (context.control.device is Touchscreen)
            {
                return;
            }

            if (CurrentScreen.current == null)
            {
                Debug.LogWarning("CurrentScreen が null です。カメラ移動を開始できません。");
                return;
            }

            isMoving = true;
            Vector2 startScreenPos = inputActions.Player.PointerPosition.ReadValue<Vector2>();
            Vector2 startViewportPos = CurrentScreen.current.screenCamera.ScreenToViewportPoint(startScreenPos);
            OnCameraMoveStart?.Invoke(startViewportPos);
        }

        // カメラ移動中のスクリーン座標を取得し、ビューポート座標に変換してイベント発火
        private void HandleCameraMovePerformed(InputAction.CallbackContext context)
        {
            if (context.control.device is Touchscreen)
            {
                return;
            }

            if (CurrentScreen.current == null)
            {
                return;
            }

            if (isMoving == false)
            {
                return;
            }

            Vector2 currentScreenPos = context.ReadValue<Vector2>();
            Vector2 currentViewportPos = CurrentScreen.current.screenCamera.ScreenToViewportPoint(currentScreenPos);

            OnCameraMoving?.Invoke(currentViewportPos);
        }

        // カメラ移動終了時のスクリーン座標を取得し、ビューポート座標に変換してイベント発火
        private void HandleCameraMoveCanceled(InputAction.CallbackContext context)
        {
            if (context.control.device is Touchscreen)
            {
                return;
            }

            if (CurrentScreen.current == null)
            {
                return;
            }

            isMoving = false;
            Vector2 endScreenPos = inputActions.Player.PointerPosition.ReadValue<Vector2>();
            Vector2 endViewportPos = CurrentScreen.current.screenCamera.ScreenToViewportPoint(endScreenPos);
            OnCameraMoveEnd?.Invoke(endViewportPos);
        }

        // カメラ移動の移動量を取得してイベント発火
        private void HandleCameraMoveDeltaPerformed(InputAction.CallbackContext context)
        {
            if (context.control.device is Touchscreen)
            {
                return;
            }

            if (isMoving == false)
            {
                return;
            }

            Vector2 delta = context.ReadValue<Vector2>() / Screen.height; // 画面の高さで割って正規化
            OnCameraMovingDelta?.Invoke(delta);
        }
    }
}