using UnityEngine;
using UnityEngine.InputSystem;

public class RobotController : MonoBehaviour
{
    [Header("Movement")]
    public float Speed = 10f;
    public float JumpForce = 5f;
    public float JumpDelay = 1f;

    [Header("Ground")]
    public float GroundY = -10f;

    [Header("References (auto-found if empty)")]
    public GameObject Body;
    public GameObject Head;
    public GameObject LeftArm;
    public GameObject RightArm;
    public Transform CameraTransform;

    Rigidbody bodyRb;
    bool grounded;
    float lastJumpTime = -999f;
    float lateralSpeed;
    float forwardSpeed;
    bool jumping;

    // Arm swing
    float armSwingAngle;

    // Offsets for head/arms relative to body (translate only, no rotation)
    Vector3 headOffset;
    Vector3 leftArmOffset;
    Vector3 rightArmOffset;

    // Eye position offset relative to head (for camera)
    Vector3 eyeOffsetLocal;

    // Mouse look
    float mouseYaw;

    void Start()
    {
        // Auto-find children by name if not assigned
        if (Body == null) Body = FindChild("Body");
        if (Head == null) Head = FindChild("Head");
        if (LeftArm == null) LeftArm = FindChild("LeftArm");
        if (RightArm == null) RightArm = FindChild("RightArm");

        if (Body == null)
        {
            Debug.LogError("RobotController: Could not find 'Body' child object!");
            enabled = false;
            return;
        }

        bodyRb = Body.GetComponent<Rigidbody>();

        // Record initial offsets from Body (for translate-only following)
        if (Head != null) headOffset = Head.transform.position - Body.transform.position;
        if (LeftArm != null) leftArmOffset = LeftArm.transform.position - Body.transform.position;
        if (RightArm != null) rightArmOffset = RightArm.transform.position - Body.transform.position;

        // Auto-find camera if not assigned
        if (CameraTransform == null && Camera.main != null)
            CameraTransform = Camera.main.transform;

        // Calculate eye center position in head's local space
        // Eyes are at (-0.15, 0.15, 0.5) and (0.15, 0.15, 0.5), center = (0, 0.15, 0.5)
        eyeOffsetLocal = new Vector3(0f, 0.15f, 0.5f);
    }

    GameObject FindChild(string name)
    {
        var t = transform.Find(name);
        if (t != null) return t.gameObject;
        // Search deeper (e.g. LeftArm is under Body)
        foreach (Transform child in transform.GetComponentsInChildren<Transform>())
        {
            if (child.name == name) return child.gameObject;
        }
        return null;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float h = 0f, v = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;

        lateralSpeed = h * Speed;
        forwardSpeed = v * Speed;
        jumping = kb.spaceKey.isPressed && (Time.time - lastJumpTime > JumpDelay);

        // Mouse look: rotate head left/right with mouse X
        var mouse = Mouse.current;
        if (mouse != null)
        {
            float mouseX = mouse.delta.x.ReadValue() * 0.15f;
            mouseYaw += mouseX;
        }
    }

    void FixedUpdate()
    {
        if (bodyRb == null) return;

        // Clamp body position so it doesn't fall below GroundY
        if (Body.transform.position.y < GroundY)
        {
            Body.transform.position = new Vector3(
                Body.transform.position.x, GroundY, Body.transform.position.z);
            bodyRb.linearVelocity = new Vector3(bodyRb.linearVelocity.x, 0, bodyRb.linearVelocity.z);
        }

        // Movement based on head facing direction (first person)
        Vector3 moveForce = Vector3.zero;

        if (Head != null)
        {
            var forward = new Vector3(Head.transform.forward.x, 0, Head.transform.forward.z).normalized;
            var right = new Vector3(Head.transform.right.x, 0, Head.transform.right.z).normalized;
            moveForce = forward * forwardSpeed + right * lateralSpeed;
        }
        else
        {
            moveForce = Vector3.forward * forwardSpeed + Vector3.right * lateralSpeed;
        }

        moveForce = Vector3.ClampMagnitude(moveForce, Speed);

        if (jumping)
        {
            moveForce += Vector3.up * JumpForce;
            lastJumpTime = Time.time;
        }

        bodyRb.AddForce(moveForce);

        // No input -> brake quickly
        if (Mathf.Approximately(lateralSpeed, 0f) && Mathf.Approximately(forwardSpeed, 0f))
        {
            Vector3 brakeVel = bodyRb.linearVelocity;
            brakeVel.x *= 0.85f;
            brakeVel.z *= 0.85f;
            bodyRb.linearVelocity = brakeVel;
        }

        // Head/Arms follow Body position
        Vector3 bodyPos = Body.transform.position;
        if (Head != null) Head.transform.position = bodyPos + headOffset;

        // Head rotation controlled by mouse yaw
        if (Head != null)
        {
            Head.transform.rotation = Quaternion.Euler(0, mouseYaw, 0);
        }

        // Arms follow head rotation (position + orientation stay fixed relative to head)
        Quaternion headRot = Head != null ? Head.transform.rotation : Quaternion.identity;
        if (LeftArm != null)
        {
            LeftArm.transform.position = bodyPos + headRot * leftArmOffset;
            LeftArm.transform.rotation = headRot;
        }
        if (RightArm != null)
        {
            RightArm.transform.position = bodyPos + headRot * rightArmOffset;
            RightArm.transform.rotation = headRot;
        }

        // Arm swing animation
        AnimateArms();
    }

    void LateUpdate()
    {
        // Camera follows eye position on head
        if (CameraTransform == null || Head == null) return;

        // Position camera at eye center (between two eyes in head local space)
        CameraTransform.position = Head.transform.TransformPoint(eyeOffsetLocal);
        CameraTransform.rotation = Head.transform.rotation;
    }

    void AnimateArms()
    {
        if (LeftArm == null || RightArm == null) return;

        float speed = bodyRb.linearVelocity.magnitude;
        if (speed > 0.5f)
        {
            armSwingAngle += Time.fixedDeltaTime * speed * 3f;
            float swing = Mathf.Sin(armSwingAngle) * 30f;
            LeftArm.transform.localRotation = Quaternion.Euler(swing, 0, 0);
            RightArm.transform.localRotation = Quaternion.Euler(-swing, 0, 0);
        }
        else
        {
            // Return to rest
            LeftArm.transform.localRotation = Quaternion.Slerp(
                LeftArm.transform.localRotation, Quaternion.identity, Time.fixedDeltaTime * 5f);
            RightArm.transform.localRotation = Quaternion.Slerp(
                RightArm.transform.localRotation, Quaternion.identity, Time.fixedDeltaTime * 5f);
        }
    }

}
