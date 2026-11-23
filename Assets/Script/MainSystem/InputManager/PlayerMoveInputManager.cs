using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MainSystem
{
    /// <summary>
    /// プレイヤーの移動をInput Systemで処理するクラス
    /// </summary>
    public class PlayerMoveInputManager : MonoBehaviour, IPlayerMoveInput
    {
        
        public event Action<Vector2> OnMoveStart;
        public event Action<Vector2> OnMoving;
        public event Action<Vector2> OnMoveEnd;
        public event Action<bool> OnChangePanel;

        private PlayerInputActions inputActions;
        private bool isMoving = false;
        [SerializeField]private Vector2 moveInput;

        private void Awake()
        {
            inputActions = new PlayerInputActions();
        }
        public void ChangeInputEnableState(bool isEnabled)
        {
            if (isEnabled)
            {
                inputActions.Player.Enable();
            }
            else
            {
                inputActions.Player.Disable();
            }
        }
        private void OnEnable()
        {
            inputActions.Player.Enable();

            inputActions.Player.Move.performed += HandleMovePerformed;
            inputActions.Player.Move.canceled += HandleMoveCanceled;
            // boolを返すイベントハンドラーはperformedとcanceledの両方に登録して、押下と離したときの両方を検知できるようにする
            inputActions.Player.ChangePanel.performed += HandleChangePanel;
            inputActions.Player.ChangePanel.canceled += HandleChangePanel;
        }

        private void OnDisable()
        {            
            inputActions.Player.Move.performed -= HandleMovePerformed;
            inputActions.Player.Move.canceled -= HandleMoveCanceled;
            inputActions.Player.ChangePanel.performed -= HandleChangePanel;
            inputActions.Player.ChangePanel.canceled -= HandleChangePanel;

            inputActions.Player.Disable();
        }

        private void Update()
        {
            HandleMoveOngoing();
        }


        private void HandleMoveOngoing()
        {
            if (isMoving == false) return;
            OnMoving?.Invoke(moveInput);
        }

        private void HandleMovePerformed(InputAction.CallbackContext context)
        {
            bool wasMoving = isMoving;
            isMoving = true;
            moveInput = context.ReadValue<Vector2>();

            // スティックの傾き変化などでperformedが連続発火した際に開始イベントが重複発火するのを防ぐため
            if (wasMoving)
            {
                return;
            }

            OnMoveStart?.Invoke(moveInput);
        }
        private void HandleMoveCanceled(InputAction.CallbackContext context)
        {
            isMoving = false;
            moveInput = context.ReadValue<Vector2>();
            OnMoveEnd?.Invoke(moveInput);
        }

        private void HandleChangePanel(InputAction.CallbackContext context)
        {
            bool isPressed = context.ReadValueAsButton();
            OnChangePanel?.Invoke(isPressed);
        }

    }
}