using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TinyRobotEscape.Member2
{
    [RequireComponent(typeof(Rigidbody))]
    public class SimpleRobotController : MonoBehaviour
    {
        [SerializeField] private float moveForce = 22f;
        [SerializeField] private float maxSpeed = 6.5f;
        [SerializeField] private float lookSensitivity = 0.12f;
        [SerializeField] private float keyboardTurnSpeed = 85f;
        [SerializeField] private Transform cameraReference;

        private Rigidbody robotRigidbody;
        private float cameraPitch;

        public void Configure(Transform reference)
        {
            cameraReference = reference;
        }

        private void Awake()
        {
            robotRigidbody = GetComponent<Rigidbody>();
            robotRigidbody.freezeRotation = true;
        }

        private void Update()
        {
            Vector2 lookInput = ReadLookInput();
            if (lookInput.sqrMagnitude <= 0.001f)
            {
                return;
            }

            float yaw = lookInput.x * lookSensitivity;
            float pitch = lookInput.y * lookSensitivity;

            if (Mathf.Abs(lookInput.x) > 2f || Mathf.Abs(lookInput.y) > 2f)
            {
                transform.Rotate(Vector3.up, yaw, Space.World);
            }
            else
            {
                transform.Rotate(Vector3.up, lookInput.x * keyboardTurnSpeed * Time.deltaTime, Space.World);
            }

            cameraPitch = Mathf.Clamp(cameraPitch - pitch, -45f, 55f);
            if (cameraReference != null)
            {
                cameraReference.localRotation = Quaternion.Euler(cameraPitch, transform.eulerAngles.y, 0f);
            }
        }

        private void FixedUpdate()
        {
            Vector2 input = ReadMoveInput();
            if (input.sqrMagnitude <= 0.001f)
            {
                return;
            }

            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = (forward * input.y + right * input.x).normalized;
            Vector3 horizontalVelocity = new Vector3(robotRigidbody.linearVelocity.x, 0f, robotRigidbody.linearVelocity.z);

            if (horizontalVelocity.magnitude < maxSpeed)
            {
                robotRigidbody.AddForce(moveDirection * moveForce, ForceMode.Acceleration);
            }
        }

        private static Vector2 ReadMoveInput()
        {
            Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    input.x -= 1f;
                }
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    input.x += 1f;
                }
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                {
                    input.y -= 1f;
                }
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                {
                    input.y += 1f;
                }
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                input.x -= 1f;
            }
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                input.x += 1f;
            }
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                input.y -= 1f;
            }
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                input.y += 1f;
            }
#endif

            return Vector2.ClampMagnitude(input, 1f);
        }

        private static Vector2 ReadLookInput()
        {
            Vector2 look = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                look += mouse.delta.ReadValue();
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.qKey.isPressed)
                {
                    look.x -= 1f;
                }
                if (keyboard.eKey.isPressed)
                {
                    look.x += 1f;
                }
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            look.x += Input.GetAxis("Mouse X") * 8f;
            look.y += Input.GetAxis("Mouse Y") * 8f;
            if (Input.GetKey(KeyCode.Q))
            {
                look.x -= 1f;
            }
            if (Input.GetKey(KeyCode.E))
            {
                look.x += 1f;
            }
#endif

            return look;
        }
    }
}
