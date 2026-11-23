using UnityEngine;

namespace MainSystem
{
    /// <summary>
    /// スクリーンの抽象クラス
    /// </summary>
    public abstract class ScreenBase : MonoBehaviour
    {
        public bool canOperate { get; private set; } = true;

        protected virtual void Awake()
        {
            defaultCameraPosition = cameraObject.transform.position;
            defaultCameraRotation = cameraObject.transform.rotation;
        }

        public virtual void Show()
        {
            cameraObject.transform.SetPositionAndRotation(defaultCameraPosition, defaultCameraRotation);
            Debug.Log("Show " + name);
        }

        public virtual void Hide()
        {
            Debug.Log("Hide " + name);
        }

        // インスペクター上で設定できる読み取り専用プロパティ
        [field: SerializeField] public GameObject cameraObject { get; private set; }
        private Vector3 defaultCameraPosition { get; set; }
        private Quaternion defaultCameraRotation { get; set; }
        [field: SerializeField] public GameObject screenCanvas { get; private set; }
        [field: SerializeField] public Camera screenCamera { get; private set; }

        public virtual void SetEnableOperation(bool enable)
        {
            canOperate = enable;
        }
    }
}
