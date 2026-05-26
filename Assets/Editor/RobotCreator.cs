using UnityEngine;
using UnityEditor;

public class RobotCreator
{
    static float cellSize = 3f;
    static int gridW = 5;
    static int gridH = 5;
    static float wallHeight = 1.5f;
    static float wallThick = 0.2f;

    [MenuItem("Tools/Create BB8 Robot")]
    static void CreateRobot()
    {
        // Delete old objects
        DestroyIfExists("Robot");
        DestroyIfExists("Maze");
        DestroyIfExists("Ground");
        DestroyIfExists("GameUI");

        float mazeW = gridW * cellSize;
        float mazeH = gridH * cellSize;

        // === Maze parent ===
        var maze = new GameObject("Maze");
        maze.transform.position = Vector3.zero;

        // === Ground / Floor ===
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.SetParent(maze.transform);
        ground.transform.localPosition = new Vector3(mazeW / 2f, -0.05f, mazeH / 2f);
        ground.transform.localScale = new Vector3(mazeW + 2f, 0.1f, mazeH + 2f);
        SetColor(ground, new Color(0.35f, 0.35f, 0.4f));

        // === Build walls ===
        BuildMazeWalls(maze.transform, mazeW, mazeH);

        // === 3 Keys ===
        CreateKey("Key1", maze.transform, new Vector3(1.5f * cellSize, 0.5f, 1.5f * cellSize));
        CreateKey("Key2", maze.transform, new Vector3(3.5f * cellSize, 0.5f, 1.5f * cellSize));
        CreateKey("Key3", maze.transform, new Vector3(2.5f * cellSize, 0.5f, 3.5f * cellSize));

        // === Exit trigger ===
        var exit = GameObject.CreatePrimitive(PrimitiveType.Cube);
        exit.name = "Exit";
        exit.transform.SetParent(maze.transform);
        exit.transform.localPosition = new Vector3(mazeW - cellSize / 2f, 0.05f, mazeH + 0.5f);
        exit.transform.localScale = new Vector3(cellSize * 0.8f, 0.1f, 1f);
        SetColor(exit, new Color(0.2f, 0.9f, 0.2f)); // green
        exit.GetComponent<BoxCollider>().isTrigger = true;
        exit.layer = 0;

        // === Robot ===
        float bodyRadius = 0.5f;
        float headRadius = 0.35f;

        var robot = new GameObject("Robot");
        robot.transform.position = new Vector3(cellSize / 2f, bodyRadius + 0.1f, -0.5f);

        // Body
        var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Body";
        body.transform.SetParent(robot.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = Vector3.one * bodyRadius * 2f;
        SetColor(body, new Color(1f, 0.6f, 0.2f));
        var bodyRb = body.AddComponent<Rigidbody>();
        bodyRb.mass = 5f;
        bodyRb.linearDamping = 2f;
        bodyRb.angularDamping = 5f;

        // Add trigger forwarder so Body's triggers reach MazeGame on Robot
        body.AddComponent<TriggerForwarder>();

        // Head
        float headY = bodyRadius + headRadius * 0.6f;
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(robot.transform);
        head.transform.localPosition = new Vector3(0, headY, 0);
        head.transform.localScale = Vector3.one * headRadius * 2f;
        SetColor(head, new Color(0.9f, 0.9f, 0.9f));
        Object.DestroyImmediate(head.GetComponent<SphereCollider>());

        // Eyes
        CreateCarvedEye("LeftEye", head.transform, headRadius, new Vector3(-0.15f, 0.15f, 0.5f));
        CreateCarvedEye("RightEye", head.transform, headRadius, new Vector3(0.15f, 0.15f, 0.5f));

        // Arms
        CreateArm("LeftArm", robot.transform, bodyRadius, true);
        CreateArm("RightArm", robot.transform, bodyRadius, false);

        // Camera
        var camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        camObj.transform.SetParent(robot.transform);
        camObj.transform.localPosition = new Vector3(0, headY, 0);
        camObj.transform.localRotation = Quaternion.identity;
        camObj.AddComponent<Camera>();
        camObj.AddComponent<AudioListener>();

        // Also remove any other existing Main Camera in scene
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (cam.gameObject != camObj)
                Object.DestroyImmediate(cam.gameObject);
        }

        // Add scripts
        robot.AddComponent<RobotController>();
        robot.AddComponent<MazeGame>();

        Selection.activeGameObject = robot;
        Undo.RegisterCreatedObjectUndo(robot, "Create BB8 Robot");
        Undo.RegisterCreatedObjectUndo(maze, "Create Maze");
        Debug.Log("BB8 Robot + Maze created!");
    }

    static void BuildMazeWalls(Transform parent, float mazeW, float mazeH)
    {
        // Outer walls with gaps:
        // Bottom wall (south): gap at x=0 (entrance)
        CreateWall("WallS1", parent, new Vector3(cellSize / 2f + cellSize, 0, 0), new Vector3(mazeW - cellSize, wallHeight, wallThick), true);

        // Top wall (north): gap at x = mazeW - cellSize (exit)
        CreateWall("WallN1", parent, new Vector3((mazeW - cellSize) / 2f, 0, mazeH), new Vector3(mazeW - cellSize, wallHeight, wallThick), true);

        // Left wall (west): full
        CreateWall("WallW", parent, new Vector3(0, 0, mazeH / 2f), new Vector3(wallThick, wallHeight, mazeH), true);

        // Right wall (east): full
        CreateWall("WallE", parent, new Vector3(mazeW, 0, mazeH / 2f), new Vector3(wallThick, wallHeight, mazeH), true);

        // === Inner walls (at least 6) ===
        // Horizontal inner walls (along X axis)
        // Row 1 (y = 1*cellSize)
        CreateWall("InnerH1", parent, new Vector3(1.5f * cellSize, 0, 1 * cellSize), new Vector3(2 * cellSize, wallHeight, wallThick), false);
        CreateWall("InnerH2", parent, new Vector3(4 * cellSize, 0, 1 * cellSize), new Vector3(cellSize, wallHeight, wallThick), false);

        // Row 2 (y = 2*cellSize)
        CreateWall("InnerH3", parent, new Vector3(2.5f * cellSize, 0, 2 * cellSize), new Vector3(cellSize, wallHeight, wallThick), false);

        // Row 3 (y = 3*cellSize)
        CreateWall("InnerH4", parent, new Vector3(1 * cellSize, 0, 3 * cellSize), new Vector3(cellSize, wallHeight, wallThick), false);
        CreateWall("InnerH5", parent, new Vector3(3.5f * cellSize, 0, 3 * cellSize), new Vector3(cellSize, wallHeight, wallThick), false);

        // Row 4 (y = 4*cellSize)
        CreateWall("InnerH6", parent, new Vector3(2 * cellSize, 0, 4 * cellSize), new Vector3(cellSize, wallHeight, wallThick), false);

        // Vertical inner walls (along Z axis)
        CreateWall("InnerV1", parent, new Vector3(1 * cellSize, 0, 1.5f * cellSize), new Vector3(wallThick, wallHeight, cellSize), false);
        CreateWall("InnerV2", parent, new Vector3(2 * cellSize, 0, 2.5f * cellSize), new Vector3(wallThick, wallHeight, cellSize), false);
        CreateWall("InnerV3", parent, new Vector3(3 * cellSize, 0, 1.5f * cellSize), new Vector3(wallThick, wallHeight, 2 * cellSize), false);
        CreateWall("InnerV4", parent, new Vector3(4 * cellSize, 0, 3.5f * cellSize), new Vector3(wallThick, wallHeight, cellSize), false);
    }

    static void CreateWall(string name, Transform parent, Vector3 center, Vector3 size, bool isOuter)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent);
        wall.transform.localPosition = new Vector3(center.x, wallHeight / 2f, center.z);
        wall.transform.localScale = size;
        SetColor(wall, isOuter ? new Color(0.5f, 0.5f, 0.55f) : new Color(0.6f, 0.45f, 0.3f));
    }

    static void CreateKey(string name, Transform parent, Vector3 pos)
    {
        // Key visual: cylinder + sphere on top
        var keyRoot = new GameObject(name);
        keyRoot.transform.SetParent(parent);
        keyRoot.transform.localPosition = pos;

        var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.name = "Shaft";
        shaft.transform.SetParent(keyRoot.transform);
        shaft.transform.localPosition = Vector3.zero;
        shaft.transform.localScale = new Vector3(0.1f, 0.25f, 0.1f);
        SetColor(shaft, new Color(1f, 0.85f, 0.1f)); // gold
        Object.DestroyImmediate(shaft.GetComponent<CapsuleCollider>());

        var handle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        handle.name = "Handle";
        handle.transform.SetParent(keyRoot.transform);
        handle.transform.localPosition = new Vector3(0, 0.3f, 0);
        handle.transform.localScale = Vector3.one * 0.2f;
        SetColor(handle, new Color(1f, 0.85f, 0.1f));
        Object.DestroyImmediate(handle.GetComponent<SphereCollider>());

        // Trigger collider on root
        var col = keyRoot.AddComponent<SphereCollider>();
        col.radius = 0.5f;
        col.isTrigger = true;

        // Tag for detection
        keyRoot.tag = "Respawn"; // Use built-in tag; MazeGame detects by name prefix "Key"

        // Rotate animation
        keyRoot.AddComponent<KeyRotator>();
    }

    static void CreateCarvedEye(string name, Transform headTransform, float headRadius, Vector3 localPos)
    {
        float eyeSocketRadius = 0.07f;
        var socket = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        socket.name = name + "Socket";
        socket.transform.SetParent(headTransform);
        socket.transform.localPosition = localPos;
        socket.transform.localScale = Vector3.one * (eyeSocketRadius * 2f / (headRadius * 2f));
        SetColor(socket, new Color(0.15f, 0.15f, 0.15f));
        Object.DestroyImmediate(socket.GetComponent<SphereCollider>());

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

    static void DestroyIfExists(string name)
    {
        var obj = GameObject.Find(name);
        if (obj != null) Object.DestroyImmediate(obj);
    }
}
