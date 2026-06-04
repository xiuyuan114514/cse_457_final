using UnityEngine;

public class LaserRoomBootstrap : MonoBehaviour
{
    [Header("Robot")]
    public GameObject robotPrefab;
    public LaserRoomGameManager gameManager;
    public float robotHeight = 1.2f;
    public float robotSpeedMultiplier = 9.504f;

    [Header("Atmosphere")]
    public int extraStarCount = 120;
    public float guidanceBeamWidth = 0.45f;

    [Header("Materials")]
    public Material laserMaterial;
    public Material solidWallMaterial;
    public Material ghostWallMaterial;
    public Material bounceWallMaterial;
    public Material startPadMaterial;
    public Material goalZoneMaterial;

    const float FloorY = 0.08f;
    const float WallHeight = 4.5f;
    const float WallThickness = 0.65f;
    const float LaserRadius = 0.1122f;
    const float LaserLengthScale = 1.04f;
    const float LaserHeightDifferenceScale = 1.15f;
    const float CourseZMin = -46f;
    const float CourseZMax = 46f;
    const float EnvelopeZMin = -70f;
    const float EnvelopeZMax = 70f;
    static readonly Vector3 CrystalPosition = new Vector3(0f, 25f, 0f);
    static readonly Vector3 DestinationPosition = new Vector3(0f, 0.35f, 58f);

    Material runtimeLaserMaterial;
    Material runtimeSolidWallMaterial;
    Material runtimeGhostWallMaterial;
    Material runtimeBounceWallMaterial;
    Material runtimeStartPadMaterial;
    Material runtimeGoalZoneMaterial;
    Material runtimeGuidanceBeamMaterial;
    PhysicsMaterial runtimeLowFrictionMaterial;

    void Awake()
    {
        if (!Application.isPlaying)
            return;

        Time.timeScale = 1f;
        BuildRoom();
    }

    public void BuildRoom()
    {
        if (gameManager == null)
            gameManager = GetComponent<LaserRoomGameManager>();

        if (gameManager == null)
            gameManager = gameObject.AddComponent<LaserRoomGameManager>();

        gameManager.robotHeight = robotHeight;

        PrepareMaterials();

        Transform generatedRoot = CreateGeneratedRoot();
        Transform spawnPoint = CreateMarker(generatedRoot, "StartPoint", new Vector3(0f, 0.6f, -58f), Quaternion.identity);

        ApplyLowFrictionToStaticEdgeWalls();
        CreatePad(generatedRoot, "StartPad", spawnPoint.position + new Vector3(0f, -0.52f, 0f), 7f, runtimeStartPadMaterial, false);
        CreateGoal(generatedRoot, new Vector3(0f, FloorY, 58f));
        CreateCrystalGuidanceLights(generatedRoot);
        CreateCrystalGuidanceBeam(generatedRoot);
        CreateExtraStars(generatedRoot);
        CreateLaneLayout(generatedRoot);
        CreateRobot(generatedRoot, spawnPoint);
    }

    Transform CreateGeneratedRoot()
    {
        Transform existing = transform.Find("Generated_LaserGameplay");
        if (existing != null)
            Destroy(existing.gameObject);

        GameObject root = new GameObject("Generated_LaserGameplay");
        root.transform.SetParent(transform, false);
        return root.transform;
    }

    Transform CreateMarker(Transform parent, string markerName, Vector3 position, Quaternion rotation)
    {
        GameObject marker = new GameObject(markerName);
        marker.transform.SetParent(parent, false);
        marker.transform.SetPositionAndRotation(position, rotation);
        return marker.transform;
    }

    void CreateRobot(Transform parent, Transform spawnPoint)
    {
        if (robotPrefab == null)
        {
            Debug.LogError("LaserRoomBootstrap: robotPrefab is not assigned.");
            return;
        }

        GameObject robotObject = Instantiate(robotPrefab, spawnPoint.position, spawnPoint.rotation);
        robotObject.name = "LaserRoom_Robot";
        robotObject.transform.SetParent(parent, true);

        Behaviour mazeGame = robotObject.GetComponent("MazeGame") as Behaviour;
        if (mazeGame != null)
            Destroy(mazeGame);

        RobotController robot = robotObject.GetComponent<RobotController>();
        if (robot == null)
        {
            Debug.LogError("LaserRoomBootstrap: robotPrefab does not contain RobotController.");
            return;
        }

        robot.Speed *= robotSpeedMultiplier;

        Rigidbody body = robotObject.GetComponentInChildren<Rigidbody>();
        Camera robotCamera = robotObject.GetComponentInChildren<Camera>(true);
        if (robotCamera != null)
        {
            DisableNonRobotCameras(robotCamera);
            robotCamera.gameObject.SetActive(true);
            robotCamera.enabled = true;
            robotCamera.tag = "MainCamera";
            robotCamera.farClipPlane = 1200f;
            robot.CameraTransform = robotCamera.transform;
        }

        ApplyLowFrictionToRobot(robotObject);

        if (body != null)
        {
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        gameManager.RegisterRobot(robot, body, spawnPoint);
    }

    void DisableNonRobotCameras(Camera robotCamera)
    {
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera sceneCamera in cameras)
        {
            if (sceneCamera == robotCamera)
                continue;

            sceneCamera.enabled = false;

            AudioListener listener = sceneCamera.GetComponent<AudioListener>();
            if (listener != null)
                listener.enabled = false;
        }
    }

    void CreateLaneLayout(Transform parent)
    {
        Transform walls = CreateMarker(parent, "InteriorWalls", Vector3.zero, Quaternion.identity);
        Transform lasers = CreateMarker(parent, "RedLasers", Vector3.zero, Quaternion.identity);

        CreateCourseEnvelope(walls);

        CreateCurvedDivider(walls, "Divider_A_B", -30f, 4.2f, 0.2f, new[] { 2, 3, 6 });
        CreateCurvedDivider(walls, "Divider_B_C", 0f, 3.4f, 1.8f, new[] { 1, 2, 5 });
        CreateCurvedDivider(walls, "Divider_C_D", 30f, 4.6f, 3.1f, new[] { 3, 4, 7 });

        CreateIrregularWallSegment(walls, "LaneA_BounceFin_South", new Vector3(-47f, WallHeight * 0.5f, -30f),
            12f, WallThickness, WallHeight, -24f, runtimeBounceWallMaterial, false, true, 0.28f, 0.36f);
        CreateIrregularWallSegment(walls, "LaneA_BounceFin_North", new Vector3(-45f, WallHeight * 0.5f, 33f),
            13f, WallThickness, WallHeight, 21f, runtimeBounceWallMaterial, false, true, -0.22f, 0.24f);
        CreateIrregularWallSegment(walls, "LaneB_BounceFin_Mid", new Vector3(-13f, WallHeight * 0.5f, -4f),
            13f, WallThickness, WallHeight, 18f, runtimeBounceWallMaterial, false, true, -0.22f, 0.18f);
        CreateIrregularWallSegment(walls, "LaneB_BounceFin_North", new Vector3(-16f, WallHeight * 0.5f, 28f),
            10f, WallThickness, WallHeight, -29f, runtimeBounceWallMaterial, false, true, 0.18f, -0.22f);
        CreateIrregularWallSegment(walls, "LaneC_BounceFin_South", new Vector3(16f, WallHeight * 0.5f, -25f),
            12f, WallThickness, WallHeight, -31f, runtimeBounceWallMaterial, false, true, 0.18f, -0.24f);
        CreateIrregularWallSegment(walls, "LaneC_BounceFin_Mid", new Vector3(15f, WallHeight * 0.5f, 15f),
            12f, WallThickness, WallHeight, 27f, runtimeBounceWallMaterial, false, true, -0.2f, 0.26f);
        CreateIrregularWallSegment(walls, "LaneD_BounceFin_South", new Vector3(46f, WallHeight * 0.5f, -34f),
            11f, WallThickness, WallHeight, 22f, runtimeBounceWallMaterial, false, true, -0.3f, 0.28f);
        CreateIrregularWallSegment(walls, "LaneD_BounceFin_North", new Vector3(47f, WallHeight * 0.5f, 24f),
            14f, WallThickness, WallHeight, -26f, runtimeBounceWallMaterial, false, true, 0.24f, -0.2f);

        CreateIrregularWallSegment(walls, "LaneA_BounceFin_Mid", new Vector3(-51f, WallHeight * 0.5f, 4f),
            9f, WallThickness, WallHeight, 32f, runtimeBounceWallMaterial, false, true, 0.16f, -0.2f);
        CreateIrregularWallSegment(walls, "LaneB_BounceFin_South", new Vector3(-20f, WallHeight * 0.5f, -35f),
            9f, WallThickness, WallHeight, -21f, runtimeBounceWallMaterial, false, true, -0.18f, 0.16f);
        CreateIrregularWallSegment(walls, "LaneC_BounceFin_North", new Vector3(21f, WallHeight * 0.5f, 34f),
            9f, WallThickness, WallHeight, -24f, runtimeBounceWallMaterial, false, true, 0.2f, -0.18f);
        CreateIrregularWallSegment(walls, "LaneD_BounceFin_Mid", new Vector3(52f, WallHeight * 0.5f, -2f),
            9f, WallThickness, WallHeight, 28f, runtimeBounceWallMaterial, false, true, -0.16f, 0.2f);

        CreateCurvedLaneLasers(lasers, "LaneA", -48f, 7.2f, 2.9f, new[] { -38f, -18f, 2f, 22f, 40f },
            new[] { 0.62f, 1.32f, 0.74f, 1.21f, 0.83f },
            new[] { 0.79f, 1.24f, 0.96f, 1.39f, 1.02f });
        CreateCurvedLaneLasers(lasers, "LaneB", -16f, 7.2f, 1.1f, new[] { -40f, -20f, 0f, 20f, 38f },
            new[] { 0.72f, 1.38f, 0.64f, 0.93f, 1.25f },
            new[] { 0.91f, 1.22f, 0.84f, 1.13f, 1.4f });
        CreateCurvedLaneLasers(lasers, "LaneC", 16f, 7.2f, 4.2f, new[] { -37f, -17f, 3f, 23f, 41f },
            new[] { 0.68f, 0.86f, 1.31f, 0.6f, 1.18f },
            new[] { 0.88f, 1.07f, 1.4f, 0.82f, 1.34f });
        CreateCurvedLaneLasers(lasers, "LaneD", 48f, 7.2f, 2.1f, new[] { -39f, -19f, 1f, 21f, 39f },
            new[] { 1.34f, 0.66f, 0.88f, 1.29f, 0.74f },
            new[] { 1.18f, 0.83f, 1.08f, 1.4f, 0.94f });
    }

    void CreateCrystalGuidanceLights(Transform parent)
    {
        CreateCrystalSpotLight(parent, "CrystalLight_StartRegion", CrystalPosition, new Vector3(0f, 0.2f, -58f), 2.6f);
        CreateCrystalSpotLight(parent, "CrystalLight_Destination", CrystalPosition, DestinationPosition, 4.2f);

        GameObject destinationGlow = new GameObject("Destination_GreenGlow");
        destinationGlow.transform.SetParent(parent, false);
        destinationGlow.transform.position = DestinationPosition + Vector3.up * 1.2f;

        Light glow = destinationGlow.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(0.2f, 1f, 0.55f);
        glow.intensity = 5.5f;
        glow.range = 16f;
        glow.shadows = LightShadows.Soft;
    }

    void CreateCrystalSpotLight(Transform parent, string lightName, Vector3 position, Vector3 target, float intensity)
    {
        GameObject lightObject = new GameObject(lightName);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.position = position;
        lightObject.transform.rotation = Quaternion.LookRotation((target - position).normalized, Vector3.up);

        Light spot = lightObject.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.color = new Color(0.45f, 0.9f, 1f);
        spot.intensity = intensity;
        spot.range = 95f;
        spot.spotAngle = 32f;
        spot.innerSpotAngle = 16f;
        spot.shadows = LightShadows.Soft;
    }

    void CreateCrystalGuidanceBeam(Transform parent)
    {
        Transform beams = CreateMarker(parent, "CrystalGuidanceBeam", Vector3.zero, Quaternion.identity);
        CreateBeamLine(beams, "OuterBeam", guidanceBeamWidth, new Color(0.1f, 0.95f, 1f, 0.45f),
            new Color(0.15f, 1f, 0.45f, 0.7f));
        CreateBeamLine(beams, "CoreBeam", guidanceBeamWidth * 0.28f, new Color(0.85f, 1f, 1f, 0.85f),
            new Color(0.75f, 1f, 0.75f, 0.95f));
    }

    void CreateBeamLine(Transform parent, string beamName, float width, Color startColor, Color endColor)
    {
        GameObject beamObject = new GameObject(beamName);
        beamObject.transform.SetParent(parent, false);

        LineRenderer line = beamObject.AddComponent<LineRenderer>();
        line.sharedMaterial = runtimeGuidanceBeamMaterial;
        line.useWorldSpace = false;
        line.positionCount = 2;
        line.SetPosition(0, CrystalPosition);
        line.SetPosition(1, DestinationPosition);
        line.startWidth = Mathf.Max(0.04f, width);
        line.endWidth = Mathf.Max(0.04f, width * 0.7f);
        line.startColor = startColor;
        line.endColor = endColor;
        line.numCapVertices = 8;
        line.numCornerVertices = 4;
        line.textureMode = LineTextureMode.Stretch;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
    }

    void CreateExtraStars(Transform parent)
    {
        if (extraStarCount <= 0)
            return;

        Transform starRoot = CreateMarker(parent, "ExtraRuntimeStars", Vector3.zero, Quaternion.identity);
        Material starMaterial = FindSceneMaterial("Star_01");
        if (starMaterial == null)
        {
            starMaterial = MakeMaterial(null, "M1_RuntimeExtraStar",
                Color.white, new Color(3f, 3f, 3f, 1f), 0f, 0.2f, false);
        }

        Mesh starMesh = CreateStarMesh();
        for (int i = 0; i < extraStarCount; i++)
        {
            GameObject star = new GameObject($"ExtraStar_{i + 1:000}");
            star.transform.SetParent(starRoot, false);
            star.transform.localPosition = ExtraStarPosition(i);
            float scale = Mathf.Lerp(0.55f, 2.2f, PseudoRandom01(i, 3.7f));
            star.transform.localScale = Vector3.one * scale;
            star.transform.localRotation = Quaternion.Euler(
                PseudoRandom01(i, 9.1f) * 360f,
                PseudoRandom01(i, 13.4f) * 360f,
                PseudoRandom01(i, 19.8f) * 360f);

            MeshFilter filter = star.AddComponent<MeshFilter>();
            filter.sharedMesh = starMesh;

            MeshRenderer renderer = star.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = starMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    Vector3 ExtraStarPosition(int index)
    {
        float yaw = Mathf.Repeat(index * 137.508f + PseudoRandom01(index, 1.2f) * 24f, 360f);
        float pitch = Mathf.Lerp(18f, 78f, PseudoRandom01(index, 5.4f));
        float radius = Mathf.Lerp(230f, 390f, PseudoRandom01(index, 8.6f));
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        return rotation * Vector3.forward * radius;
    }

    float PseudoRandom01(int index, float seed)
    {
        float value = Mathf.Sin((index + 1) * 12.9898f + seed * 78.233f) * 43758.5453f;
        return value - Mathf.Floor(value);
    }

    float SolidWallGrowth(string wallName, int cornerIndex)
    {
        unchecked
        {
            uint hash = 2166136261u;
            for (int i = 0; i < wallName.Length; i++)
            {
                hash ^= wallName[i];
                hash *= 16777619u;
            }

            hash ^= (uint)(cornerIndex + 1) * 374761393u;
            hash *= 668265263u;
            float random01 = (hash & 0x00ffffff) / 16777215f;
            return Mathf.Lerp(0.05f, 0.25f, random01);
        }
    }

    Mesh CreateStarMesh()
    {
        Mesh mesh = new Mesh { name = "M1_RuntimeStarMesh" };
        mesh.vertices = new[]
        {
            Vector3.up,
            Vector3.down,
            Vector3.left,
            Vector3.right,
            Vector3.forward,
            Vector3.back
        };
        mesh.triangles = new[]
        {
            0, 4, 2,
            0, 3, 4,
            0, 5, 3,
            0, 2, 5,
            1, 2, 4,
            1, 4, 3,
            1, 3, 5,
            1, 5, 2
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    void CreateCourseEnvelope(Transform parent)
    {
        CreateCurvedBoundary(parent, "LeftOuterBoundary", -66f, 2.6f, 0.6f);
        CreateCurvedBoundary(parent, "RightOuterBoundary", 66f, 2.4f, 2.7f);
        CreateCap(parent, "SouthBoundaryCap", EnvelopeZMin);
        CreateCap(parent, "NorthBoundaryCap", EnvelopeZMax);
    }

    void CreateCurvedBoundary(Transform parent, string boundaryName, float baseX, float amplitude, float phase)
    {
        const int segments = 10;
        for (int i = 0; i < segments; i++)
        {
            float t0 = i / (float)segments;
            float t1 = (i + 1) / (float)segments;
            float z0 = Mathf.Lerp(EnvelopeZMin, EnvelopeZMax, t0);
            float z1 = Mathf.Lerp(EnvelopeZMin, EnvelopeZMax, t1);
            Vector3 start = new Vector3(CurvedX(baseX, amplitude, phase, z0), WallHeight * 0.5f, z0);
            Vector3 end = new Vector3(CurvedX(baseX, amplitude, phase, z1), WallHeight * 0.5f, z1);
            CreateIrregularWallBetween(parent, $"{boundaryName}_{i + 1:00}", start, end,
                WallThickness * 1.2f, WallHeight, runtimeSolidWallMaterial, false, false, 0.2f * Mathf.Sin(i), 0.18f * Mathf.Cos(i));
        }
    }

    void CreateCap(Transform parent, string capName, float z)
    {
        float[] xPoints = { -66f, -39f, -12f, 13f, 40f, 66f };
        for (int i = 0; i < xPoints.Length - 1; i++)
        {
            float zNudge = z + Mathf.Sin(i * 1.7f) * 0.8f;
            Vector3 start = new Vector3(xPoints[i], WallHeight * 0.5f, zNudge);
            Vector3 end = new Vector3(xPoints[i + 1], WallHeight * 0.5f, zNudge + Mathf.Cos(i * 1.1f) * 0.45f);
            CreateIrregularWallBetween(parent, $"{capName}_{i + 1:00}", start, end,
                WallThickness * 1.4f, WallHeight, runtimeSolidWallMaterial, false, false, -0.2f + i * 0.08f, 0.16f);
        }
    }

    void CreateCurvedDivider(Transform parent, string dividerName, float baseX, float amplitude, float phase, int[] ghostSegments)
    {
        const int segments = 8;
        for (int i = 0; i < segments; i++)
        {
            float t0 = i / (float)segments;
            float t1 = (i + 1) / (float)segments;
            float z0 = Mathf.Lerp(CourseZMin, CourseZMax, t0);
            float z1 = Mathf.Lerp(CourseZMin, CourseZMax, t1);
            bool ghost = ContainsSegment(ghostSegments, i);
            Material material = ghost ? runtimeGhostWallMaterial : runtimeSolidWallMaterial;
            float thickness = ghost ? WallThickness * 0.8f : WallThickness;

            Vector3 start = new Vector3(CurvedX(baseX, amplitude, phase, z0), WallHeight * 0.5f, z0);
            Vector3 end = new Vector3(CurvedX(baseX, amplitude, phase, z1), WallHeight * 0.5f, z1);
            CreateIrregularWallBetween(parent, $"{dividerName}_{(ghost ? "Mirror" : "Solid")}_{i + 1:00}", start, end,
                thickness, ghost ? WallHeight * 0.92f : WallHeight, material, ghost, false, 0.18f * Mathf.Cos(i + phase), 0.22f * Mathf.Sin(i));
        }
    }

    void CreateCurvedLaneLasers(Transform parent, string laneName, float centerX, float halfWidth, float phase, float[] zValues, float[] leftHeights, float[] rightHeights)
    {
        float scaledHalfWidth = halfWidth * LaserLengthScale;
        for (int i = 0; i < zValues.Length; i++)
        {
            float z = zValues[i];
            float curve = Mathf.Sin(z * 0.055f + phase) * 3.2f;
            float diagonal = (i % 2 == 0 ? 0.55f : -0.45f);
            float leftX = centerX + curve - scaledHalfWidth;
            float rightX = centerX + curve + scaledHalfWidth;
            float leftHeight = leftHeights[i];
            float rightHeight = rightHeights[i];
            ExpandLaserHeightDifference(ref leftHeight, ref rightHeight);
            CreateLaser(parent, $"{laneName}_Laser_{i + 1:00}",
                new Vector3(leftX, robotHeight * leftHeight, z - diagonal),
                new Vector3(rightX, robotHeight * rightHeight, z + diagonal));
        }
    }

    void ExpandLaserHeightDifference(ref float leftHeight, ref float rightHeight)
    {
        float center = (leftHeight + rightHeight) * 0.5f;
        float halfDelta = (rightHeight - leftHeight) * 0.5f * LaserHeightDifferenceScale;
        leftHeight = Mathf.Clamp(center - halfDelta, 0.6f, 1.4f);
        rightHeight = Mathf.Clamp(center + halfDelta, 0.6f, 1.4f);
    }

    bool ContainsSegment(int[] segments, int segmentIndex)
    {
        foreach (int segment in segments)
        {
            if (segment == segmentIndex)
                return true;
        }

        return false;
    }

    float CurvedX(float baseX, float amplitude, float phase, float z)
    {
        return baseX + Mathf.Sin(z * 0.055f + phase) * amplitude;
    }

    void CreateIrregularWallBetween(Transform parent, string wallName, Vector3 start, Vector3 end, float thickness, float height, Material material, bool passThrough, bool bounce, float skew, float topTilt)
    {
        Vector3 delta = end - start;
        float length = new Vector2(delta.x, delta.z).magnitude;
        if (length <= 0.001f)
            return;

        float yawDegrees = Mathf.Atan2(-delta.z, delta.x) * Mathf.Rad2Deg;
        Vector3 center = (start + end) * 0.5f;
        center.y = height * 0.5f;

        CreateIrregularWallSegment(parent, wallName, center, length, thickness, height, yawDegrees, material, passThrough, bounce, skew, topTilt);
    }

    void CreateIrregularWallSegment(Transform parent, string wallName, Vector3 center, float length, float thickness, float height, float yawDegrees, Material material, bool passThrough, bool bounce, float skew, float topTilt)
    {
        bool growSolidWall = !passThrough && !bounce;
        float growth0 = growSolidWall ? SolidWallGrowth(wallName, 0) : 0f;
        float growth1 = growSolidWall ? SolidWallGrowth(wallName, 1) : 0f;
        float growth2 = growSolidWall ? SolidWallGrowth(wallName, 2) : 0f;
        float growth3 = growSolidWall ? SolidWallGrowth(wallName, 3) : 0f;
        float maxGrowth = Mathf.Max(Mathf.Max(growth0, growth1), Mathf.Max(growth2, growth3));
        float colliderHeight = growSolidWall
            ? height * (1f + maxGrowth) + Mathf.Abs(topTilt) * 1.5f
            : height;
        center.y = colliderHeight * 0.5f;

        GameObject wall = new GameObject(wallName);
        wall.name = wallName;
        wall.transform.SetParent(parent, false);
        wall.transform.SetPositionAndRotation(center, Quaternion.Euler(0f, yawDegrees, 0f));

        MeshFilter meshFilter = wall.AddComponent<MeshFilter>();
        MeshRenderer renderer = wall.AddComponent<MeshRenderer>();
        bool needsCollider = !passThrough || bounce;
        BoxCollider boxCollider = needsCollider ? wall.AddComponent<BoxCollider>() : null;

        float halfLength = length * 0.5f;
        float halfThickness = thickness * 0.5f;
        float bottomY = -colliderHeight * 0.5f;
        float topY = growSolidWall ? bottomY + height : colliderHeight * 0.5f;
        float zSkew = Mathf.Clamp(skew, -0.45f, 0.45f) * thickness;
        float topShift = Mathf.Clamp(topTilt, -0.45f, 0.45f) * thickness;

        Vector3[] vertices =
        {
            new Vector3(-halfLength, bottomY, -halfThickness + zSkew),
            new Vector3(halfLength, bottomY, -halfThickness - zSkew),
            new Vector3(halfLength * 0.96f, bottomY, halfThickness - zSkew * 0.5f),
            new Vector3(-halfLength * 1.04f, bottomY, halfThickness + zSkew * 0.5f),
            new Vector3(-halfLength * 0.98f, topY + height * growth0 + topTilt, -halfThickness + zSkew + topShift),
            new Vector3(halfLength * 1.03f, topY + height * growth1 - topTilt * 0.55f, -halfThickness - zSkew + topShift * 0.35f),
            new Vector3(halfLength * 0.94f, topY + height * growth2 + topTilt * 0.4f, halfThickness - zSkew * 0.45f - topShift),
            new Vector3(-halfLength * 1.01f, topY + height * growth3 - topTilt * 0.7f, halfThickness + zSkew * 0.55f - topShift * 0.4f)
        };

        int[] triangles =
        {
            0, 2, 1, 0, 3, 2,
            4, 5, 6, 4, 6, 7,
            0, 1, 5, 0, 5, 4,
            1, 2, 6, 1, 6, 5,
            2, 3, 7, 2, 7, 6,
            3, 0, 4, 3, 4, 7
        };

        Mesh mesh = new Mesh
        {
            name = wallName + "_Mesh",
            vertices = vertices,
            triangles = triangles
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        meshFilter.sharedMesh = mesh;

        if (boxCollider != null)
        {
            boxCollider.size = new Vector3(length, colliderHeight, thickness);
            boxCollider.center = Vector3.zero;
            boxCollider.sharedMaterial = runtimeLowFrictionMaterial;
        }

        if (renderer != null)
            renderer.sharedMaterial = material;

        if (bounce)
            wall.AddComponent<BounceWall>();
    }

    void CreateLaser(Transform parent, string laserName, Vector3 start, Vector3 end)
    {
        GameObject beamObject = new GameObject(laserName);
        beamObject.transform.SetParent(parent, false);

        LaserBeam beam = beamObject.AddComponent<LaserBeam>();
        beam.Configure(laserName, start, end, LaserRadius, runtimeLaserMaterial, gameManager);

        CreateLaserEndpoint(parent, laserName + "_StartEmitter", start);
        CreateLaserEndpoint(parent, laserName + "_EndEmitter", end);
    }

    void CreateLaserEndpoint(Transform parent, string endpointName, Vector3 position)
    {
        GameObject endpoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        endpoint.name = endpointName;
        endpoint.transform.SetParent(parent, false);
        endpoint.transform.position = position;
        endpoint.transform.localScale = Vector3.one * 0.4f;

        MeshRenderer renderer = endpoint.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sharedMaterial = runtimeLaserMaterial;

        LaserPulseVisual pulseVisual = endpoint.AddComponent<LaserPulseVisual>();
        pulseVisual.Configure(endpointName, runtimeLaserMaterial);

        Collider collider = endpoint.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;
    }

    void CreatePad(Transform parent, string padName, Vector3 position, float radius, Material material, bool isGoal)
    {
        GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = padName;
        pad.transform.SetParent(parent, false);
        pad.transform.position = position;
        pad.transform.localScale = new Vector3(radius * 2f, 0.08f, radius * 2f);

        MeshRenderer renderer = pad.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;

        Collider collider = pad.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = isGoal;
            collider.isTrigger = isGoal;
        }
    }

    void CreateGoal(Transform parent, Vector3 position)
    {
        GameObject goal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        goal.name = "GoalZone";
        goal.transform.SetParent(parent, false);
        goal.transform.position = position;
        goal.transform.localScale = new Vector3(16f, 0.12f, 16f);

        MeshRenderer renderer = goal.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sharedMaterial = runtimeGoalZoneMaterial;

        Collider collider = goal.GetComponent<Collider>();
        if (collider != null)
            collider.isTrigger = true;

        GoalZone zone = goal.AddComponent<GoalZone>();
        zone.gameManager = gameManager;
    }

    void PrepareMaterials()
    {
        runtimeLowFrictionMaterial = CreateLowFrictionMaterial();

        runtimeLaserMaterial = MakeMaterial(laserMaterial, "M1_RuntimeLaserRed",
            new Color(0.95f, 0.012f, 0.004f, 1f), new Color(9.5f, 0.08f, 0.015f, 1f), 0f, 0.16f, false);
        runtimeSolidWallMaterial = MakeMaterial(solidWallMaterial, "M1_RuntimeDeepBlueWall",
            new Color(0.015f, 0.055f, 0.18f, 1f), new Color(0.01f, 0.09f, 0.22f, 1f), 0.35f, 0.72f, false);
        runtimeGhostWallMaterial = MakeMaterial(ghostWallMaterial, "M1_RuntimeGhostMirrorWall",
            new Color(0.45f, 0.85f, 1f, 0.32f), new Color(0.08f, 0.5f, 0.85f, 1f), 0.1f, 0.95f, true);
        runtimeBounceWallMaterial = MakeMaterial(bounceWallMaterial, "M1_RuntimeBounceGoldWall",
            new Color(1f, 0.68f, 0.16f, 1f), new Color(0.55f, 0.24f, 0.02f, 1f), 0.7f, 0.82f, false);
        runtimeStartPadMaterial = MakeMaterial(startPadMaterial, "M1_RuntimeStartPad",
            new Color(0.05f, 0.9f, 0.95f, 1f), new Color(0.02f, 0.5f, 0.62f, 1f), 0.05f, 0.55f, true);
        runtimeGoalZoneMaterial = MakeMaterial(goalZoneMaterial, "M1_RuntimeGoalZone",
            new Color(0.2f, 1f, 0.48f, 0.78f), new Color(0.08f, 2.6f, 0.7f, 1f), 0.05f, 0.7f, true);
        runtimeGuidanceBeamMaterial = MakeMaterial(null, "M1_RuntimeGuidanceBeam",
            new Color(0.12f, 1f, 0.68f, 0.48f), new Color(0.12f, 4f, 2.4f, 1f), 0f, 0.9f, true);
        runtimeGuidanceBeamMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 20;
    }

    PhysicsMaterial CreateLowFrictionMaterial()
    {
        return new PhysicsMaterial("M1_RuntimeLowFriction")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
    }

    Material FindSceneMaterial(string objectName)
    {
        GameObject sceneObject = GameObject.Find(objectName);
        if (sceneObject == null)
            return null;

        Renderer renderer = sceneObject.GetComponent<Renderer>();
        return renderer != null ? renderer.sharedMaterial : null;
    }

    void ApplyLowFrictionToStaticEdgeWalls()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider roomCollider in colliders)
        {
            if (!roomCollider.isTrigger && roomCollider.gameObject.name.StartsWith("EdgeWall"))
                roomCollider.sharedMaterial = runtimeLowFrictionMaterial;
        }
    }

    void ApplyLowFrictionToRobot(GameObject robotObject)
    {
        Collider[] colliders = robotObject.GetComponentsInChildren<Collider>(true);
        foreach (Collider robotCollider in colliders)
        {
            if (!robotCollider.isTrigger)
                robotCollider.sharedMaterial = runtimeLowFrictionMaterial;
        }
    }

    Material MakeMaterial(Material source, string materialName, Color baseColor, Color emission, float metallic, float smoothness, bool transparent)
    {
        Shader shader = source != null ? source.shader : Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = source != null ? new Material(source) : new Material(shader);
        material.name = materialName;
        material.SetColor("_BaseColor", baseColor);
        material.SetColor("_Color", baseColor);
        material.SetColor("_EmissionColor", emission);
        material.EnableKeyword("_EMISSION");

        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);

        if (transparent)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 1f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        return material;
    }
}
