using System.Runtime.InteropServices;
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

        [Tooltip("モバイル時のみアクティブにするUIルートオブジェクト（ジョイスティック等）")]
        [SerializeField] private GameObject mobileUIRoot;
        [SerializeField] private GameManager gameManager;

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

            // ゲーム開始前の初期状態ではモバイルUIを表示しないため
            HideMobileUI();
        }

        private void OnEnable()
        {
            SubscribeGameEvents();
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

            gameManager.OnGameStarted += ShowMobileUI;
            gameManager.OnGameUnpaused += HandleGameUnpaused;

            gameManager.OnGameReady += HideMobileUI;
            gameManager.OnGamePaused += HideMobileUI;
            gameManager.OnGameFailed += HideMobileUI;
            gameManager.OnGameOver += HideMobileUI;
        }

        private void OnDisable()
        {
            UnbscribeGameEvents();
        }
        private void UnbscribeGameEvents()
        {
            if (!IsMobile)
            {
                return;
            }

            if (gameManager == null)
            {
                return;
            }

            gameManager.OnGameStarted -= ShowMobileUI;
            gameManager.OnGameUnpaused -= HandleGameUnpaused;

            gameManager.OnGameReady -= HideMobileUI;
            gameManager.OnGamePaused -= HideMobileUI;
            gameManager.OnGameFailed -= HideMobileUI;
            gameManager.OnGameOver -= HideMobileUI;
        }

        /// <summary>
        /// 実行環境に応じたモバイル判定を実行
        /// </summary>
        private bool CheckIsMobile()
        {
            // エディタ上でのスマホUIや挙動の動作確認を容易にするため
            if (debugForceMobile)
            {
                return true;
            }

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
            // ポーズ前にプレイ中でなかった場合（発射待機中など）はジョイスティックを表示しないため
            if (!wasPlayingBeforePause)
            {
                return;
            }

            ShowMobileUI();
        }

        private void ShowMobileUI()
        {
            if (mobileUIRoot == null)
            {
                return;
            }

            mobileUIRoot.SetActive(true);
        }

        private void HideMobileUI()
        {
            if (mobileUIRoot == null)
            {
                return;
            }

            mobileUIRoot.SetActive(false);
        }
    }
}
