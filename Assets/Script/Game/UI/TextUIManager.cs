using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MainSystem;
using TMPro;

namespace Game
{
    public class TextUIManager : MonoBehaviour, IInitializable
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private TMP_Text startText;
        [SerializeField] private TMP_Text stageText;
        [SerializeField] private TMP_Text gameSpeedText;
        [SerializeField] private TMP_Text retryText;
        [SerializeField] private TMP_Text highScoreText;

        private void Start()
        {
            if (MobileInputManager.IsMobile)
            {
                // モバイル環境では画面タップによる発射・リトライ操作となるため、対応する案内文言へ設定する
                if (startText != null)
                {
                    startText.text = "Tap to Start";
                }

                if (retryText != null)
                {
                    retryText.text = "Tap to Retry";
                }

                return;
            }

            // PC環境ではキーボードによる発射・リトライ操作となるため、対応する案内文言へ設定する
            if (startText != null)
            {
                startText.text = "Space to \nStart";
            }

            if (retryText != null)
            {
                retryText.text = "R to Retry";
            }
        }

        public void OnEnable()
        {
            gameManager.OnGameStarted += OnGameStarted;
            gameManager.OnGameStarted += UpdateText;
            gameManager.OnGameFailed += OnGameFailed;
            gameManager.OnGameOver += OnGameOver;
            gameManager.OnAllBlocksRemoved += UpdateText;
        }
        public void OnDisable()
        {
            gameManager.OnGameStarted -= OnGameStarted;
            gameManager.OnGameStarted -= UpdateText;
            gameManager.OnGameFailed -= OnGameFailed;
            gameManager.OnGameOver -= OnGameOver;
            gameManager.OnAllBlocksRemoved -= UpdateText;
        }
        public void Initialize()
        {
            startText.enabled = true;
            stageText.enabled = false;
            gameSpeedText.enabled = false;
            retryText.enabled = false;
            highScoreText.enabled = false;
        }

        private void OnGameStarted()
        {
            startText.enabled = false;
            stageText.enabled = true;
            gameSpeedText.enabled = true;
            retryText.enabled = false;
            highScoreText.enabled = false;
        }

        private void OnGameFailed()
        {
            startText.enabled = true;
            stageText.enabled = false;
            highScoreText.enabled = false;
            gameSpeedText.enabled = false;
        }

        private void OnGameOver()
        {
            stageText.enabled = false;
            gameSpeedText.enabled = false;
            retryText.enabled = true;
            stageText.enabled = true;
        }
        private void UpdateText()
        {
            stageText.text = $"Stage: {GameState.currentStage}";
            float roundedGameSpeed = (float)Math.Round(GameState.gameSpeed, 2, MidpointRounding.AwayFromZero);

            gameSpeedText.text = $"Speed: x{roundedGameSpeed.ToString("F2")}";
            if (GameState.currentStage >= scoreManager.ScoreList[0])
            {
                highScoreText.enabled = true;
            }
        }

    }
}
