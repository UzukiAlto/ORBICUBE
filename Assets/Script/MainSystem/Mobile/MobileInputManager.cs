using System.Runtime.InteropServices;
using UnityEngine;

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

            // モバイル専用UI（ジョイスティック等）の初期表示状態を適用
            if (mobileUIRoot != null)
            {
                mobileUIRoot.SetActive(IsMobile);
            }
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
    }
}