using UnityEngine;

namespace Skiski
{
    /// <summary>
    /// ターゲットを一定のオフセットで追いかける、動作確認用のシンプルなカメラ。
    /// 本格的なカメラワークが必要になったら Cinemachine（com.unity.cinemachine）への置き換えを推奨。
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 offset = new Vector3(0f, 5f, -8f);
        [SerializeField, Tooltip("注視点のターゲットからの高さ")] float lookAtHeight = 1f;
        [SerializeField, Min(0f)] float smoothTime = 0.15f;

        Vector3 velocity;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        public Vector3 Offset
        {
            get => offset;
            set => offset = value;
        }

        void LateUpdate()
        {
            if (target == null)
                return;

            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
            transform.LookAt(target.position + Vector3.up * lookAtHeight);
        }
    }
}
