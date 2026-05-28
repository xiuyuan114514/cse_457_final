using System.IO;
using TinyRobotEscape.Member2;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TinyRobotEscape.Member2.Editor
{
    public static class ConveyorRoomSceneBuilder
    {
        private const string RootPath = "Assets/Member2_ConveyorRoom";
        private const string MaterialsPath = RootPath + "/Materials";
        private const string ScenePath = RootPath + "/Scenes/ConveyorChallengeRoom.unity";

        [MenuItem("Tiny Robot Escape/Build Member 2 Conveyor Room")]
        public static void BuildScene()
        {
            EnsureFolders();
            EnsurePlayerTag();

            Material floor = CreateMaterial("M2_Floor", new Color(0.17f, 0.2f, 0.24f));
            Material wall = CreateMaterial("M2_Wall", new Color(0.38f, 0.42f, 0.48f));
            Material conveyor = CreateMaterial("M2_ConveyorBlue", new Color(0.03f, 0.5f, 0.85f));
            Material platform = CreateMaterial("M2_PlatformYellow", new Color(0.96f, 0.77f, 0.25f));
            Material hazard = CreateMaterial("M2_HazardRed", new Color(0.95f, 0.14f, 0.13f));
            Material goal = CreateMaterial("M2_GoalGreen", new Color(0.12f, 0.75f, 0.3f));
            Material robot = CreateMaterial("M2_RobotWhite", new Color(0.9f, 0.94f, 0.96f));
            Material arrow = CreateMaterial("M2_ArrowWhite", Color.white);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "ConveyorChallengeRoom";

            GameObject root = new GameObject("Member2_ConveyorRoom");

            BuildEnvironment(root.transform, floor, wall);
            Transform spawnPoint = CreateMarker("SpawnPoint", new Vector3(0f, 1f, -10f), root.transform);
            GameObject player = BuildPlayer(spawnPoint, robot, root.transform);
            Camera camera = BuildCamera(player.transform, root.transform);
            player.GetComponent<SimpleRobotController>().Configure(camera.transform);
            ChallengeHud hud = BuildHud(root.transform);

            BuildConveyor("ConveyorBelt_A_SidePush", new Vector3(0f, 0.18f, -5.8f), new Vector3(8f, 0.35f, 4f), Vector3.right, 8f, conveyor, arrow, root.transform);
            BuildConveyor("ConveyorBelt_B_BackPush", new Vector3(0f, 0.18f, -0.8f), new Vector3(7f, 0.35f, 4f), Vector3.back, 7f, conveyor, arrow, root.transform);
            BuildMovingPlatform(new Vector3(0f, 0.45f, 3.7f), new Vector3(3.2f, 0.35f, 3f), new Vector3(5f, 0f, 0f), 2.2f, platform, root.transform);
            BuildMovingHazard(new Vector3(-2.7f, 1.05f, 7.7f), new Vector3(1.1f, 1.1f, 1.1f), new Vector3(5.4f, 0f, 0f), 1.5f, hazard, hud, root.transform);
            BuildGoal(new Vector3(0f, 0.3f, 10.5f), new Vector3(4f, 0.45f, 2f), goal, hud, root.transform);
            BuildFailZone(new Vector3(0f, -2f, 0f), new Vector3(14f, 1f, 28f), hud, root.transform);
            BuildLights(root.transform);

            Selection.activeGameObject = root;
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Built Member 2 conveyor challenge scene at {ScenePath}");
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(MaterialsPath);
            Directory.CreateDirectory(RootPath + "/Scenes");
        }

        private static void EnsurePlayerTag()
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tags = tagManager.FindProperty("tags");

            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == "Player")
                {
                    return;
                }
            }

            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = "Player";
            tagManager.ApplyModifiedProperties();
        }

        private static Material CreateMaterial(string name, Color color)
        {
            string path = $"{MaterialsPath}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void BuildEnvironment(Transform parent, Material floor, Material wall)
        {
            CreateCube("Floor", new Vector3(0f, -0.05f, 0f), new Vector3(13f, 0.1f, 24f), floor, parent);
            CreateCube("LeftWall", new Vector3(-6.55f, 1.4f, 0f), new Vector3(0.25f, 2.8f, 24f), wall, parent);
            CreateCube("RightWall", new Vector3(6.55f, 1.4f, 0f), new Vector3(0.25f, 2.8f, 24f), wall, parent);
            CreateCube("BackWall", new Vector3(0f, 1.4f, -12.1f), new Vector3(13f, 2.8f, 0.25f), wall, parent);
            CreateCube("ExitFrame", new Vector3(0f, 1.4f, 11.95f), new Vector3(13f, 2.8f, 0.25f), wall, parent);
            CreateCube("SafePlatform_A", new Vector3(0f, 0.18f, -8.7f), new Vector3(5f, 0.35f, 2f), floor, parent);
            CreateCube("SafePlatform_B", new Vector3(0f, 0.18f, 1.7f), new Vector3(5f, 0.35f, 1.4f), floor, parent);
            CreateCube("GoalApproach", new Vector3(0f, 0.18f, 8.4f), new Vector3(4f, 0.35f, 2f), floor, parent);
        }

        private static GameObject BuildPlayer(Transform spawnPoint, Material robotMaterial, Transform parent)
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            player.name = "Robot_Player_Test";
            player.tag = "Player";
            player.transform.SetParent(parent);
            player.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            player.transform.localScale = Vector3.one * 0.85f;
            player.GetComponent<Renderer>().sharedMaterial = robotMaterial;

            Rigidbody rigidbody = player.AddComponent<Rigidbody>();
            rigidbody.mass = 1.1f;
            rigidbody.linearDamping = 0.35f;
            rigidbody.angularDamping = 0.2f;
            rigidbody.freezeRotation = true;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

            player.AddComponent<SimpleRobotController>();
            PlayerRespawn respawn = player.AddComponent<PlayerRespawn>();
            respawn.SetSpawnPoint(spawnPoint);
            return player;
        }

        private static Camera BuildCamera(Transform player, Transform parent)
        {
            GameObject cameraObject = new GameObject("Member2_FollowCamera");
            cameraObject.transform.SetParent(parent);
            cameraObject.transform.position = player.TransformPoint(new Vector3(0f, 0.62f, 0.35f));
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 72f;
            cameraObject.AddComponent<AudioListener>();
            FollowCamera follow = cameraObject.AddComponent<FollowCamera>();
            follow.Configure(player, new Vector3(0f, 0.62f, 0.35f));
            return camera;
        }

        private static ChallengeHud BuildHud(Transform parent)
        {
            GameObject canvasObject = new GameObject("Member2_HUD");
            canvasObject.transform.SetParent(parent);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            Text objective = CreateText("ObjectiveText", canvasObject.transform, new Vector2(24f, -24f), 20, TextAnchor.UpperLeft);
            Text status = CreateText("StatusText", canvasObject.transform, new Vector2(24f, -58f), 18, TextAnchor.UpperLeft);
            Text centerMessage = CreateCenterText("CenterMessageText", canvasObject.transform);

            ChallengeHud hud = canvasObject.AddComponent<ChallengeHud>();
            hud.Configure(status, objective, centerMessage);
            return hud;
        }

        private static Text CreateText(string name, Transform parent, Vector2 anchoredPosition, int size, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent);
            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(760f, 40f);
            return text;
        }

        private static Text CreateCenterText(string name, Transform parent)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent);
            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 34;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.enabled = false;

            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(900f, 180f);
            return text;
        }

        private static void BuildConveyor(string name, Vector3 position, Vector3 scale, Vector3 localDirection, float speed, Material conveyor, Material arrow, Transform parent)
        {
            GameObject belt = CreateCube(name, position, scale, conveyor, parent);

            GameObject trigger = new GameObject("PushTrigger");
            trigger.transform.SetParent(belt.transform);
            trigger.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            BoxCollider triggerCollider = trigger.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector3(1f, 1.1f, 1f);
            ConveyorBelt conveyorBelt = trigger.AddComponent<ConveyorBelt>();
            conveyorBelt.Configure(localDirection, speed, 8f);

            for (int i = -1; i <= 1; i++)
            {
                GameObject arrowMarker = CreateCube($"DirectionArrow_{i + 2}", Vector3.zero, new Vector3(0.22f, 0.08f, 0.8f), arrow, belt.transform);
                arrowMarker.transform.localPosition = localDirection.normalized * i * 0.9f + Vector3.up * 0.58f;
                arrowMarker.transform.localRotation = Quaternion.LookRotation(localDirection.normalized, Vector3.up);
                Object.DestroyImmediate(arrowMarker.GetComponent<Collider>());
                ConveyorBeltAnimator animator = arrowMarker.AddComponent<ConveyorBeltAnimator>();
                animator.Configure(localDirection, 1.6f, 1.5f);
            }
        }

        private static void BuildMovingPlatform(Vector3 position, Vector3 scale, Vector3 offset, float duration, Material material, Transform parent)
        {
            GameObject platform = CreateCube("MovingPlatform_A", position, scale, material, parent);
            Rigidbody rigidbody = platform.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            MovingPlatform movingPlatform = platform.AddComponent<MovingPlatform>();
            movingPlatform.Configure(offset, duration);
        }

        private static void BuildMovingHazard(Vector3 position, Vector3 scale, Vector3 offset, float duration, Material material, ChallengeHud hud, Transform parent)
        {
            GameObject hazard = CreateCube("MovingObstacle_A", position, scale, material, parent);
            Collider collider = hazard.GetComponent<Collider>();
            collider.isTrigger = true;
            MovingHazard movingHazard = hazard.AddComponent<MovingHazard>();
            movingHazard.Configure(offset, duration, hud);
        }

        private static void BuildGoal(Vector3 position, Vector3 scale, Material material, ChallengeHud hud, Transform parent)
        {
            GameObject goal = CreateCube("GoalZone", position, scale, material, parent);
            Collider collider = goal.GetComponent<Collider>();
            collider.isTrigger = true;
            ChallengeGoal challengeGoal = goal.AddComponent<ChallengeGoal>();
            challengeGoal.Configure(hud);
        }

        private static void BuildFailZone(Vector3 position, Vector3 scale, ChallengeHud hud, Transform parent)
        {
            GameObject failZone = new GameObject("FailZone");
            failZone.transform.SetParent(parent);
            failZone.transform.position = position;
            BoxCollider collider = failZone.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = scale;
            FailZone fail = failZone.AddComponent<FailZone>();
            fail.Configure(hud);
        }

        private static void BuildLights(Transform parent)
        {
            RenderSettings.ambientLight = new Color(0.22f, 0.25f, 0.3f);

            GameObject directional = new GameObject("Room_KeyLight");
            directional.transform.SetParent(parent);
            directional.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            Light keyLight = directional.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.1f;

            CreatePointLight("BlueConveyorLight", new Vector3(-3f, 3f, -4f), new Color(0.1f, 0.65f, 1f), parent);
            CreatePointLight("RedHazardLight", new Vector3(0f, 3f, 7.5f), new Color(1f, 0.15f, 0.1f), parent);
            CreatePointLight("GreenGoalLight", new Vector3(0f, 3f, 10.5f), new Color(0.25f, 1f, 0.35f), parent);
        }

        private static void CreatePointLight(string name, Vector3 position, Color color, Transform parent)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = 7f;
            light.intensity = 2.2f;
        }

        private static Transform CreateMarker(string name, Vector3 position, Transform parent)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(parent);
            marker.transform.position = position;
            return marker.transform;
        }

        private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }
    }
}
