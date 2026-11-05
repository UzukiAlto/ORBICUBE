using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Game;

namespace MainSystem
{
    /// <summary>
    /// モバイル端末（スマホ・タブレット）の検知および関連UIの表示制御を行うクラス
    /// </summary>
    public class MobileInputManager : MonoBehaviour
    {
        [Tooltip("エディタ上でスマホ動作をテストするための強制フラグ")]
        [SerializeField] private bool debugForceMobile = false;

        [Tooltip("ゲームプレイ中にモバイル時のみアクティブにするUIルートオブジェクト（ジョイスティック・面切替ボタン等）")]
        [SerializeField] private GameObject gameUIRoot;

        [Tooltip("SystemInput系ボタン（Submit・Retry）をまとめるUIルートオブジェクト")]
        [SerializeField] private GameObject systemInputUIRoot;

        [Tooltip("モード決定・ゲーム開始（Submit）用のボタンオブジェクト")]
        [SerializeField] private GameObject submitButton;

        [Tooltip("ゲームオーバー時のリトライ（Retry）用のボタンオブジェクト")]
        [SerializeField] private GameObject retryButton;

        [SerializeField] private GameManager gameManager;
        [SerializeField] private ModeSelectScreen modeSelectScreen;

        // 1フレーム待機中に表示状態が再度切り替わった場合の不整合を防ぐための目標状態フラグ
        private bool shouldShowGameUI = false;
        private bool shouldShowSubmitButton = false;
        private bool shouldShowRetryButton = false;

        // ポーズ解除時の復帰先や画面遷移時の遅れイベント判定のためにゲーム側の待機状態を保持
        private bool isWaitingForGameStart = false;
        private bool isGameOverState = false;

        /// <summary>
        /// 現在の実行環境がモバイル端末かどうか
        /// </summary>
        public static bool IsMobile { get; private set; }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern bool WebGL_IsMobileBrowser();
#endif

        private void Awake()
        {
            IsMobile = CheckIsMobile();

            // 初期状態では各画面のイベント通知が来るまでモバイルUIを即座に非表示にしておくため
            InitializeMobileUI();
        }

        private void InitializeMobileUI()
        {
            shouldShowGameUI = false;
            shouldShowSubmitButton = false;
            shouldShowRetryButton = false;

            if (gameUIRoot != null)
            {
                gameUIRoot.SetActive(false);
            }

            if (systemInputUIRoot != null)
            {
                // SystemInputUIRoot自体はモバイル時のみ有効化し、配下の各ボタンで個別に表示を切り替えるため
                systemInputUIRoot.SetActive(IsMobile);
            }

            if (submitButton != null)
            {
                submitButton.SetActive(false);
            }

            if (retryButton != null)
            {
                retryButton.SetActive(false);
            }
        }

        private void OnEnable()
        {
            SubscribeGameEvents();
            SubscribeModeSelectEvents();
        }

        private void OnDisable()
        {
            UnsubscribeGameEvents();
            UnsubscribeModeSelectEvents();
        }

        private void SubscribeGameEvents()
        {
            // PC環境ではモバイルUIの表示切り替えを行う必要がないため
            if (!IsMobile)
            {
                return;
            }

            if (gameManager == null)
            {
                return;
            }

            gameManager.OnGameReady += HandleGameReady;
            gameManager.OnGameStarted += HandleGameStarted;
            gameManager.OnGameFailed += HandleGameFailed;
            gameManager.OnGameOver += HandleGameOver;
            gameManager.OnGamePaused += HandleGamePaused;
            gameManager.OnGameUnpaused += HandleGameUnpaused;
        }

        private void SubscribeModeSelectEvents()
        {
            // PC環境ではモバイルUIの表示切り替えを行う必要がないため
            if (!IsMobile)
            {
                return;
            }

            if (modeSelectScreen == null)
            {
                return;
            }

            modeSelectScreen.OnOperationEnableChanged += HandleModeSelectOperationChanged;
        }

        private void UnsubscribeGameEvents()
        {
            if (!IsMobile)
            {
                return;
            }

            if (gameManager == null)
            {
                return;
            }

            gameManager.OnGameReady -= HandleGameReady;
            gameManager.OnGameStarted -= HandleGameStarted;
            gameManager.OnGameFailed -= HandleGameFailed;
            gameManager.OnGameOver -= HandleGameOver;
            gameManager.OnGamePaused -= HandleGamePaused;
            gameManager.OnGameUnpaused -= HandleGameUnpaused;
        }

        private void UnsubscribeModeSelectEvents()
        {
            if (!IsMobile)
            {
                return;
            }

            if (modeSelectScreen == null)
            {
                return;
            }

            modeSelectScreen.OnOperationEnableChanged -= HandleModeSelectOperationChanged;
        }

        /// <summary>
        /// 実行環境に応じたモバイル判定を実行
        /// </summary>
        private bool CheckIsMobile()
        {
#if UNITY_EDITOR
            // エディタ上でのスマホUIや挙動の動作確認を容易にするため
            if (debugForceMobile)
            {
                return true;
            }
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL環境ではブラウザのUserAgentおよびタッチ機能から判定
            return WebGL_IsMobileBrowser();
#elif UNITY_IOS || UNITY_ANDROID
            // スマホネイティブビルド時は常にtrue
            return true;
#else
            // デスクトップやエディタ通常時のフォールバック
            return Application.isMobilePlatform;
#endif
        }

        private void HandleModeSelectOperationChanged(bool isEnabled)
        {
            if (isEnabled)
            {
                isWaitingForGameStart = false;
                isGameOverState = false;
                SetGameMobileUIActive(false);
                SetSubmitButtonActive(true);
                SetRetryButtonActive(false);
                return;
            }

            // ゲーム画面への遷移アニメーション完了時に遅れてModeSelectScreen.Hide()が呼ばれた際、
            // すでにゲーム開始待機中として表示されているSubmitボタンを誤って消さないようにするため
            if (isWaitingForGameStart)
            {
                return;
            }

            SetSubmitButtonActive(false);
        }

        private void HandleGameReady()
        {
            isWaitingForGameStart = true;
            isGameOverState = false;

            SetGameMobileUIActive(false);
            SetSubmitButtonActive(true);
            SetRetryButtonActive(false);
        }

        private void HandleGameStarted()
        {
            isWaitingForGameStart = false;
            isGameOverState = false;

            SetGameMobileUIActive(true);
            SetSubmitButtonActive(false);
            SetRetryButtonActive(false);
        }

        private void HandleGameFailed()
        {
            // 残機が残っている状態でミスした場合は次のボール発射（Submit）待機になるため
            isWaitingForGameStart = true;
            isGameOverState = false;

            SetGameMobileUIActive(false);
            SetSubmitButtonActive(true);
            SetRetryButtonActive(false);
        }

        private void HandleGameOver()
        {
            isWaitingForGameStart = false;
            isGameOverState = true;

            SetGameMobileUIActive(false);
            SetSubmitButtonActive(false);
            SetRetryButtonActive(true);
        }

        private void HandleGamePaused()
        {
            // ポーズ中はポーズパネルの操作と重ならないよう全てのモバイル操作UIを隠すため
            SetGameMobileUIActive(false);
            SetSubmitButtonActive(false);
            SetRetryButtonActive(false);
        }

        private void HandleGameUnpaused(bool wasPlayingBeforePause)
        {
            if (wasPlayingBeforePause)
            {
                SetGameMobileUIActive(true);
                return;
            }

            if (isGameOverState)
            {
                SetRetryButtonActive(true);
                return;
            }

            if (isWaitingForGameStart)
            {
                SetSubmitButtonActive(true);
            }
        }

        private void SetGameMobileUIActive(bool isActive)
        {
            shouldShowGameUI = isActive;

            if (isActive)
            {
                if (gameUIRoot == null)
                {
                    return;
                }

                gameUIRoot.SetActive(true);
                return;
            }

            DeactivateGameUINextFrameAsync().Forget();
        }

        private void SetSubmitButtonActive(bool isActive)
        {
            shouldShowSubmitButton = isActive;

            if (isActive)
            {
                if (submitButton == null)
                {
                    return;
                }

                submitButton.SetActive(true);
                return;
            }

            DeactivateSubmitButtonNextFrameAsync().Forget();
        }

        private void SetRetryButtonActive(bool isActive)
        {
            shouldShowRetryButton = isActive;

            if (isActive)
            {
                if (retryButton == null)
                {
                    return;
                }

                retryButton.SetActive(true);
                return;
            }

            DeactivateRetryButtonNextFrameAsync().Forget();
        }

        private async UniTaskVoid DeactivateGameUINextFrameAsync()
        {
            // Input Systemのイベント通知ループ途中でOnScreenControlが無効化され仮想デバイスが破棄されるのを防ぐため
            await UniTask.NextFrame(this.GetCancellationTokenOnDestroy());

            // 待機中に再度表示要求が来ていた場合は非表示処理を中断するため
            if (shouldShowGameUI)
            {
                return;
            }

            if (gameUIRoot == null)
            {
                return;
            }

            gameUIRoot.SetActive(false);
        }

        private async UniTaskVoid DeactivateSubmitButtonNextFrameAsync()
        {
            // Input Systemのイベント通知ループ途中でOnScreenControlが無効化され仮想デバイスが破棄されるのを防ぐため
            await UniTask.NextFrame(this.GetCancellationTokenOnDestroy());

            // 待機中に再度表示要求が来ていた場合は非表示処理を中断するため
            if (shouldShowSubmitButton)
            {
                return;
            }

            if (submitButton == null)
            {
                return;
            }

            submitButton.SetActive(false);
        }

        private async UniTaskVoid DeactivateRetryButtonNextFrameAsync()
        {
            // Input Systemのイベント通知ループ途中でOnScreenControlが無効化され仮想デバイスが破棄されるのを防ぐため
            await UniTask.NextFrame(this.GetCancellationTokenOnDestroy());

            // 待機中に再度表示要求が来ていた場合は非表示処理を中断するため
            if (shouldShowRetryButton)
            {
                return;
            }

            if (retryButton == null)
            {
                return;
            }

            retryButton.SetActive(false);
        }
    }
}
