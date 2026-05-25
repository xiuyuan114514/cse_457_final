using UnityEngine;

namespace TinyRobotEscape.Member2
{
    public class ConveyorBeltAnimator : MonoBehaviour
    {
        [SerializeField] private Vector3 localDirection = Vector3.forward;
        [SerializeField] private float arrowTravelDistance = 1.5f;
        [SerializeField] private float arrowSpeed = 2f;

        private Vector3 startLocalPosition;

        public void Configure(Vector3 direction, float travelDistance, float speed)
        {
            localDirection = direction.normalized;
            arrowTravelDistance = travelDistance;
            arrowSpeed = speed;
        }

        private void Awake()
        {
            startLocalPosition = transform.localPosition;
        }

        private void Update()
        {
            float offset = Mathf.Repeat(Time.time * arrowSpeed, arrowTravelDistance);
            transform.localPosition = startLocalPosition + localDirection.normalized * offset;
        }
    }
}
