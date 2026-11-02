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

        [Tooltip("ゲームプレイ中にモバイル時のみアクティブにするUIルートオブジェクト")]
        [SerializeField] private GameObject gameUIRoot;

        [Tooltip("モード選択中にモバイル時のみアクティブにするUIルートオブジェクト")]
        [SerializeField] private GameObject modeSelectUIRoot;

        [SerializeField] private GameManager gameManager;
        [SerializeField] private ModeSelectScreen modeSelectScreen;

        // 1フレーム待機中に表示状態が再度切り替わった場合の不整合を防ぐための目標状態フラグ
        private bool shouldShowGameUI = false;
        private bool shouldShowModeSelectUI = false;

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
            shouldShowModeSelectUI = false;

            if (gameUIRoot != null)
            {
                gameUIRoot.SetActive(false);
            }

            if (modeSelectUIRoot != null)
            {
                modeSelectUIRoot.SetActive(false);
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

            gameManager.OnGameStarted += ShowGameMobileUI;
            gameManager.OnGameUnpaused += HandleGameUnpaused;

            gameManager.OnGameReady += HideGameMobileUI;
            gameManager.OnGamePaused += HideGameMobileUI;
            gameManager.OnGameFailed += HideGameMobileUI;
            gameManager.OnGameOver += HideGameMobileUI;
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

            modeSelectScreen.OnOperationEnableChanged += SetModeSelectMobileUIActive;
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

            gameManager.OnGameStarted -= ShowGameMobileUI;
            gameManager.OnGameUnpaused -= HandleGameUnpaused;

            gameManager.OnGameReady -= HideGameMobileUI;
            gameManager.OnGamePaused -= HideGameMobileUI;
            gameManager.OnGameFailed -= HideGameMobileUI;
            gameManager.OnGameOver -= HideGameMobileUI;
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

            modeSelectScreen.OnOperationEnableChanged -= SetModeSelectMobileUIActive;
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

        private void HandleGameUnpaused(bool wasPlayingBeforePause)
        {
            // ポーズ前にプレイ中でなかった場合（発射待機中など）はゲーム用モバイルUIを表示しないため
            if (!wasPlayingBeforePause)
            {
                return;
            }

            ShowGameMobileUI();
        }

        private void ShowGameMobileUI()
        {
            shouldShowGameUI = true;

            if (gameUIRoot == null)
            {
                return;
            }

            gameUIRoot.SetActive(true);
        }

        private void HideGameMobileUI()
        {
            shouldShowGameUI = false;
            DeactivateGameUINextFrameAsync().Forget();
        }

        private void SetModeSelectMobileUIActive(bool isActive)
        {
            shouldShowModeSelectUI = isActive;

            if (isActive)
            {
                if (modeSelectUIRoot == null)
                {
                    return;
                }

                modeSelectUIRoot.SetActive(true);
                return;
            }

            DeactivateModeSelectUINextFrameAsync().Forget();
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

        private async UniTaskVoid DeactivateModeSelectUINextFrameAsync()
        {
            // Input Systemのイベント通知ループ途中でOnScreenControlが無効化され仮想デバイスが破棄されるのを防ぐため
            await UniTask.NextFrame(this.GetCancellationTokenOnDestroy());

            // 待機中に再度表示要求が来ていた場合は非表示処理を中断するため
            if (shouldShowModeSelectUI)
            {
                return;
            }

            if (modeSelectUIRoot == null)
            {
                return;
            }

            modeSelectUIRoot.SetActive(false);
        }
    }
}
