using UnityEngine;

namespace Game
{
    /// <summary>
    /// ボールの予測線を表示するクラス
    /// </summary>
    public class HitPredictionRenderer : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private GameObject ballObject;
        [SerializeField] private GameObject predictionBall;
        [SerializeField] private LayerMask ballTargetLayerMask;

        private Material predictionBallMaterial;

        private float predictionLength = 5f;
        private float maxAlpha = 0.7f;

        private void Awake()
        {
            if (predictionBall == null)
            {
                return;
            }

            if (predictionBall.TryGetComponent(out Renderer predictionRenderer))
            {
                predictionBallMaterial = predictionRenderer.material;
            }
        }

        private void OnEnable()
        {
            gameManager.OnGameReady += DisablePredictionBall;
            gameManager.OnGameStarted += EnablePredictionBall;
            gameManager.OnGameFailed += DisablePredictionBall;
            gameManager.OnGameOver += DisablePredictionBall;
        }

        private void OnDisable()
        {
            gameManager.OnGameReady -= DisablePredictionBall;
            gameManager.OnGameStarted -= EnablePredictionBall;
            gameManager.OnGameFailed -= DisablePredictionBall;
            gameManager.OnGameOver -= DisablePredictionBall;
        }

        private void OnDestroy()
        {
            if (predictionBallMaterial != null)
            {
                Destroy(predictionBallMaterial);
            }
        }

        private void Update()
        {
            Debug.DrawRay(ballObject.transform.position, ballObject.transform.forward * 100);

            if (!gameManager.isGamePlaying)
            {
                return;
            }

            if (Physics.SphereCast(ballObject.transform.position, 0.5f, ballObject.transform.forward, out RaycastHit hit, Mathf.Infinity, ballTargetLayerMask))
            {
                SetTrajectory(hit);
            }
        }

        private void SetTrajectory(RaycastHit hit)
        {
            predictionBall.transform.position = hit.point;
            float ballAlpha;

            // 対象との距離が近いほど予測を濃く表示するため
            if (hit.distance <= predictionLength)
            {
                float constA = predictionLength + 1 / (1 + maxAlpha);
                float constB = 1 / constA + maxAlpha;
                ballAlpha = Mathf.Max(0, 1 / (hit.distance - constA) + constB);
            }
            else
            {
                ballAlpha = 0f;
            }

            if (predictionBallMaterial == null)
            {
                return;
            }

            if (hit.collider.gameObject.TryGetComponent(out IBallTarget target))
            {
                target.OnHitBallRaycast(predictionBallMaterial, ballAlpha);
            }
        }

        private void EnablePredictionBall()
        {
            predictionBall.SetActive(true);
        }

        private void DisablePredictionBall()
        {
            predictionBall.SetActive(false);
        }
    }
}
