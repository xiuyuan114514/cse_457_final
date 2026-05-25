using UnityEngine;

namespace TinyRobotEscape.Member2
{
    public class FollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0.62f, 0.35f);
        [SerializeField] private float followSharpness = 20f;

        public void Configure(Transform followTarget, Vector3 cameraOffset)
        {
            target = followTarget;
            offset = cameraOffset;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = target.TransformPoint(offset);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSharpness);
        }
    }
}
