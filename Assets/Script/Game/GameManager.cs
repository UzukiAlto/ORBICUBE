using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MainSystem;
using UnityEngine.PlayerLoop;

namespace Game
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameObject systemInputObject;
        [SerializeField] private GameObject playerMoveInputObject;
        [SerializeField] private LifeManager lifeManager;
        private ISystemInput systemInput;
        private IPlayerMoveInput playerMoveInput;
        public bool isGamePlaying { get; private set; } = false;
        private bool isGameOver = false;
        private bool isGameReady = false;
        private bool wasPlayingBeforePause = false; // ゲームが一時停止される前にプレイ中だったかどうか

        public event Action OnGameStarted;
        public event Action OnGameReady;
        public event Action OnAllBlocksRemoved;
        public event Action OnGameFailed;
        public event Action OnGameOver;
        public event Action OnGamePaused;
        /// <summary>
        /// ポーズ前にプレイ中ならtrue、そうでなければfalseを渡す
        /// </summary>
        public event Action<bool> OnGameUnpaused;

        void Update()
        {
#if UNITY_EDITOR
            // debug
            // デバッグ用に全ブロック削除を強制的に呼び出す
            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.N))
            {
                RaiseAllBlocksRemoved();
            }
#endif
        }


        private void Awake()
        {
            systemInput = systemInputObject.GetComponent<ISystemInput>();
            playerMoveInput = playerMoveInputObject.GetComponent<IPlayerMoveInput>();
        }

        private void OnEnable()
        {
            systemInput.OnSubmit += StartGame;
            systemInput.OnRetry += RetryGame;
            OnAllBlocksRemoved += GameState.NextStage;
            OnGameReady += GameState.Initialize;
        }
        private void OnDisable()
        {
            systemInput.OnSubmit -= StartGame;
            systemInput.OnRetry -= RetryGame;
            OnAllBlocksRemoved -= GameState.NextStage;
            OnGameReady -= GameState.Initialize;
        }
        private void InitializeGameFlags()
        {

            isGamePlaying = false;
            isGameOver = false;
            isGameReady = false;
            wasPlayingBeforePause = false;
        }

        public void PrepareGame()
        {
            OnGameReady?.Invoke();
            InitializeGameFlags();

            isGameReady = true;
        }

        public void RaiseAllBlocksRemoved()
        {
            AudioManager.PlaySE(AudioManager.SEType.Next);
            OnAllBlocksRemoved?.Invoke();
        }

        public void StartGame(bool isPressed)
        {
            if (!isPressed || isGamePlaying || !isGameReady)
            {
                return;
            }
            Debug.Log("Start Game");
            isGamePlaying = true;
            isGameReady = false;
            OnGameStarted?.Invoke();
        }

        public void FailGame()
        {
            isGamePlaying = false;
            isGameOver = lifeManager.decreaseLife();
            if (isGameOver)
            {
                OnGameOver?.Invoke();
                isGameReady = false;
            }
            else
            {
                OnGameFailed?.Invoke();
                isGameReady = true;
            }
        }

        public void RetryGame(bool isPressed)
        {
            if (!isPressed || isGamePlaying || !isGameOver)
            {
                return;
            }
            isGameOver = false;
            PrepareGame();
        }

        public void PauseGame()
        {
            if (isGamePlaying)
            {
                wasPlayingBeforePause = true;
                isGamePlaying = false;
            }
            else
            {
                wasPlayingBeforePause = false;
            }
            systemInput.ChangeInputEnableState(SystemInputType.Submit, false);
            systemInput.ChangeInputEnableState(SystemInputType.Retry, false);
            playerMoveInput.ChangeInputEnableState(false);
            OnGamePaused?.Invoke();
        }
        public void UnpauseGame()
        {
            if (wasPlayingBeforePause)
            {
                isGamePlaying = true;
            }
            else
            {
                isGamePlaying = false;
            }
            systemInput.ChangeInputEnableState(SystemInputType.Submit, true);
            systemInput.ChangeInputEnableState(SystemInputType.Retry, true);
            playerMoveInput.ChangeInputEnableState(true);
            OnGameUnpaused?.Invoke(wasPlayingBeforePause);
        }
        public void ReturnToModeSelect()
        {
            UnpauseGame();
            OnGameReady?.Invoke();
            InitializeGameFlags();
        }
    }
}