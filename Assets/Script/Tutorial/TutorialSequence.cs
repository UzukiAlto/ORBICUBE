using System.Collections.Generic;
using UnityEngine;
using Game;
using MainSystem;

namespace Tutorial
{
    /// <summary>
    /// チュートリアルの各ステップを順番に実行・管理するシーケンサ
    /// 展示用として画面ごとの表示頻度（リセット/セッションスキップ）やゲーム開始待機を制御します
    /// </summary>
    public class TutorialSequence : MonoBehaviour
    {
        [Header("UI & Steps")]
        [SerializeField] private TutorialUI tutorialUI;
        [SerializeField] private List<TutorialStepBase> steps = new List<TutorialStepBase>();

        [Header("Game Start Timing")]
        [Tooltip("ゲーム開始イベント（GameManager.OnGameStarted）を待ってから開始するか（ゲーム画面用）")]
        [SerializeField] private bool startOnGameStarted = false;
        [SerializeField] private GameManager gameManager;

        [Header("Exhibition Settings")]
        [Tooltip("有効化されるたびに毎回チュートリアルを実行するか（モード選択画面等）")]
        [SerializeField] private bool resetOnEnable = false;

        [Tooltip("ゲーム画面用チュートリアルとしてセッション管理を行うか")]
        [SerializeField] private bool isGameScreenTutorial = false;

        // 展示会で同一プレイヤーがリトライした際に重複して案内しないよう、セッション単位で完了状態を保持
        private static bool hasCompletedGameTutorial = false;

        private int currentStepIndex = -1;
        private bool isRunning = false;

        private void Awake()
        {
            // ゲーム画面用チュートリアルの場合、ボール発射前の操作不可時に案内が始まらないよう自動設定
            if (isGameScreenTutorial)
            {
                startOnGameStarted = true;
                if (gameManager == null)
                {
                    gameManager = FindObjectOfType<GameManager>();
                }
            }

            // シーン上で編集用にActiveのまま保存されていても、実行開始時は全て確実に非アクティブ化するため
            if (steps == null)
            {
                return;
            }

            foreach (TutorialStepBase step in steps)
            {
                if (step == null || step.Visual == null)
                {
                    continue;
                }

                step.Visual.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            // Awakeの実行順によってOnEnable時点でIsMobileが未確定だった場合でも、初回描画前に確実に非表示化するため
            if (!MobileInputManager.IsMobile)
            {
                return;
            }

            StopSequence();
            if (tutorialUI != null)
            {
                tutorialUI.ForceHideImmediate();
            }
        }

        private void OnEnable()
        {
            // モード選択画面に戻った際は、次のプレイヤーのためにゲーム用チュートリアルの完了状態をリセット
            if (!isGameScreenTutorial)
            {
                ResetGameTutorialSession();
            }

            // ゲーム画面の場合、ゲーム開始前（Start待機中）は操作不能なためゲーム開始イベントを待機
            if (startOnGameStarted && gameManager != null)
            {
                gameManager.OnGameStarted += HandleGameStarted;
                gameManager.OnGameFailed += HandleGameInterrupted;
                gameManager.OnGameOver += HandleGameInterrupted;
                return;
            }

            // モード選択画面など、画面が開くたびにプレイヤー交代の可能性があるため毎回リセット
            if (resetOnEnable)
            {
                StartSequence();
                return;
            }

            // ゲーム画面の場合、同じプレイヤーがリトライ中であればスキップ
            if (isGameScreenTutorial && hasCompletedGameTutorial)
            {
                if (tutorialUI != null)
                {
                    tutorialUI.ForceHideImmediate();
                }
                return;
            }

            StartSequence();
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.OnGameStarted -= HandleGameStarted;
                gameManager.OnGameFailed -= HandleGameInterrupted;
                gameManager.OnGameOver -= HandleGameInterrupted;
            }

            // 画面遷移等で中断された際にイベント購読が残るのを防ぐため
            StopSequence();
        }

        private void HandleGameStarted()
        {
            // 展示会で同じプレイヤーがプレイ中・リトライ中であればスキップ
            if (isGameScreenTutorial && hasCompletedGameTutorial)
            {
                if (tutorialUI != null)
                {
                    tutorialUI.ForceHideImmediate();
                }
                return;
            }

            StartSequence();
        }

        private void HandleGameInterrupted()
        {
            // 失敗時やゲームオーバー時はチュートリアル案内を安全に中断し、再開時の重複案内を防ぐため完了扱いにする
            if (isGameScreenTutorial)
            {
                hasCompletedGameTutorial = true;
            }

            StopSequence();
        }

        /// <summary>
        /// チュートリアルシーケンスを先頭から開始します
        /// </summary>
        public void StartSequence()
        {
            StopSequence();

            // モバイル環境ではPC向けのキー・マウス操作チュートリアルを表示しないため
            if (MobileInputManager.IsMobile)
            {
                if (tutorialUI != null)
                {
                    tutorialUI.ForceHideImmediate();
                }

                return;
            }

            if (steps == null || steps.Count == 0)
            {
                return;
            }

            isRunning = true;
            currentStepIndex = -1;
            AdvanceToNextStep();
        }

        /// <summary>
        /// チュートリアルシーケンスを中断し、後始末を行います
        /// </summary>
        public void StopSequence()
        {
            if (!isRunning)
            {
                return;
            }

            isRunning = false;

            if (currentStepIndex >= 0 && currentStepIndex < steps.Count)
            {
                TutorialStepBase activeStep = steps[currentStepIndex];
                if (activeStep != null)
                {
                    activeStep.OnStepCompleted -= HandleStepCompleted;
                    activeStep.OnRequestShowVisual -= HandleRequestShowVisual;
                    activeStep.ExitStep();
                }
            }

            if (tutorialUI != null)
            {
                tutorialUI.ForceHideImmediate();
            }
        }

        private void AdvanceToNextStep()
        {
            // 直前のステップの後始末
            if (currentStepIndex >= 0 && currentStepIndex < steps.Count)
            {
                TutorialStepBase previousStep = steps[currentStepIndex];
                if (previousStep != null)
                {
                    previousStep.OnStepCompleted -= HandleStepCompleted;
                    previousStep.OnRequestShowVisual -= HandleRequestShowVisual;
                    previousStep.ExitStep();
                }
            }

            currentStepIndex++;

            // 全ステップ完了
            if (currentStepIndex >= steps.Count)
            {
                OnAllStepsCompleted();
                return;
            }

            TutorialStepBase currentStep = steps[currentStepIndex];
            if (currentStep == null)
            {
                Debug.LogWarning($"インデックス {currentStepIndex} のステップが null のため、次のステップへスキップします。");
                AdvanceToNextStep();
                return;
            }

            currentStep.OnStepCompleted += HandleStepCompleted;
            currentStep.OnRequestShowVisual += HandleRequestShowVisual;

            // ステップ開始時の自動表示フラグが有効な場合のみアニメーションを表示
            if (currentStep.AutoShowVisualOnEnter)
            {
                if (tutorialUI != null)
                {
                    tutorialUI.ShowVisual(currentStep.Visual);
                }
            }
            else
            {
                // 次のステップが待機状態（遅延表示）の場合は、直前のVisualを達成演出とともに閉じる
                if (tutorialUI != null)
                {
                    tutorialUI.DismissCurrentVisual();
                }
            }

            currentStep.EnterStep();
        }

        private void HandleRequestShowVisual(TutorialVisualBase visual)
        {
            if (!isRunning || tutorialUI == null)
            {
                return;
            }

            tutorialUI.ShowVisual(visual);
        }

        private void HandleStepCompleted()
        {
            if (!isRunning)
            {
                return;
            }

            if (tutorialUI != null)
            {
                tutorialUI.NotifyStepComplete();
            }

            AdvanceToNextStep();
        }

        private void OnAllStepsCompleted()
        {
            isRunning = false;

            if (isGameScreenTutorial)
            {
                hasCompletedGameTutorial = true;
            }

            if (tutorialUI != null)
            {
                tutorialUI.HideUI();
            }
        }

        /// <summary>
        /// 展示会で次のプレイヤーのためにゲーム用チュートリアルの完了状態をリセットします
        /// タイトル画面に戻った際などに呼び出します
        /// </summary>
        public static void ResetGameTutorialSession()
        {
            hasCompletedGameTutorial = false;
        }

        [ContextMenu("Reset All Tutorial Flags")]
        private void ResetAllFlagsForDebug()
        {
            hasCompletedGameTutorial = false;
            Debug.Log("[TutorialSequence] チュートリアルフラグをリセットしました。");
        }
    }
}
