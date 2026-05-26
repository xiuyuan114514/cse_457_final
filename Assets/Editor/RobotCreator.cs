using UnityEngine;
using UnityEditor;

public class RobotCreator
{
    [MenuItem("Tools/Create BB8 Robot")]
    static void CreateRobot()
    {
        // Find the "Robot" object in scene, or create one
        // Delete old Robot if exists, then create fresh
        GameObject old = GameObject.Find("Robot");
        if (old != null)
            Object.DestroyImmediate(old);

        var robot = new GameObject("Robot");
        robot.transform.position = new Vector3(0, 10f, 0);

        float bodyRadius = 0.5f;
        float headRadius = 0.35f;

        // === Body (big sphere at bottom) ===
        var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Body";
        body.transform.SetParent(robot.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = Vector3.one * bodyRadius * 2f;
        SetColor(body, new Color(1f, 0.6f, 0.2f)); // orange
        var bodyRb = body.AddComponent<Rigidbody>();
        bodyRb.mass = 5f;
        bodyRb.linearDamping = 2f;
        bodyRb.angularDamping = 5f;

        // === Head (child of Robot, follows Body position via script) ===
        float headY = bodyRadius + headRadius * 0.6f;
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(robot.transform);
        head.transform.localPosition = new Vector3(0, headY, 0);
        head.transform.localScale = Vector3.one * headRadius * 2f;
        SetColor(head, new Color(0.9f, 0.9f, 0.9f)); // light gray
        Object.DestroyImmediate(head.GetComponent<SphereCollider>());

        // === Eyes carved into head ===
        CreateCarvedEye("LeftEye", head.transform, headRadius,
            new Vector3(-0.15f, 0.15f, 0.5f));
        CreateCarvedEye("RightEye", head.transform, headRadius,
            new Vector3(0.15f, 0.15f, 0.5f));

        // === Arms (children of Robot, follow Body position via script) ===
        CreateArm("LeftArm", robot.transform, bodyRadius, true);
        CreateArm("RightArm", robot.transform, bodyRadius, false);

        // === Ground plane ===
        if (GameObject.Find("Ground") == null)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0, -0.05f, 0);
            ground.transform.localScale = new Vector3(50f, 0.1f, 50f);
            SetColor(ground, new Color(0.4f, 0.4f, 0.4f));
        }

        // Add controller script
        robot.AddComponent<RobotController>();

        Selection.activeGameObject = robot;
        Undo.RegisterCreatedObjectUndo(robot, "Create BB8 Robot");
        Debug.Log("BB8 Robot created under 'Robot' GameObject!");
    }

    static void CreateCarvedEye(string name, Transform headTransform, float headRadius, Vector3 localPos)
    {
        float eyeSocketRadius = 0.07f;

        // Dark eye socket
        var socket = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        socket.name = name + "Socket";
        socket.transform.SetParent(headTransform);
        socket.transform.localPosition = localPos;
        socket.transform.localScale = Vector3.one * (eyeSocketRadius * 2f / (headRadius * 2f));
        SetColor(socket, new Color(0.15f, 0.15f, 0.15f)); // dark gray
        Object.DestroyImmediate(socket.GetComponent<SphereCollider>());

        // Bright pupil/iris dot inside socket
        float pupilRadius = 0.03f;
        var pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pupil.name = name + "Pupil";
        pupil.transform.SetParent(socket.transform);
        pupil.transform.localPosition = new Vector3(0, 0, 0.3f);
        pupil.transform.localScale = Vector3.one * (pupilRadius / eyeSocketRadius);
        SetColor(pupil, Color.white);
        Object.DestroyImmediate(pupil.GetComponent<SphereCollider>());
    }

    static void CreateArm(string name, Transform parent, float bodyRadius, bool isLeft)
    {
        float armLength = 0.3f;
        float armThickness = 0.06f;
        float handRadius = 0.07f;
        Color armColor = new Color(0.6f, 0.6f, 0.6f);
        float side = isLeft ? -1f : 1f;

        var armRoot = new GameObject(name);
        armRoot.transform.SetParent(parent);
        armRoot.transform.localPosition = new Vector3(side * bodyRadius, 0.05f, 0);

        var arm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        arm.name = "Forearm";
        arm.transform.SetParent(armRoot.transform);
        arm.transform.localPosition = new Vector3(side * armLength * 0.5f, 0, 0);
        arm.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        arm.transform.localScale = new Vector3(armThickness, armLength * 0.5f, armThickness);
        SetColor(arm, armColor);
        Object.DestroyImmediate(arm.GetComponent<CapsuleCollider>());

        var hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hand.name = "Hand";
        hand.transform.SetParent(armRoot.transform);
        hand.transform.localPosition = new Vector3(side * armLength, 0, 0);
        hand.transform.localScale = Vector3.one * handRadius * 2f;
        SetColor(hand, armColor);
        Object.DestroyImmediate(hand.GetComponent<SphereCollider>());
    }

    static void SetColor(GameObject go, Color color)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            renderer.sharedMaterial = mat;
        }
    }
}
