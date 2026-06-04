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

            Material floor = CreateMaterial("M2_Floor", new Color(0.035f, 0.055f, 0.085f), new Color(0f, 0.025f, 0.045f));
            Material wall = CreateMaterial("M2_Wall", new Color(0.09f, 0.12f, 0.18f), new Color(0f, 0.04f, 0.08f));
            Material conveyor = CreateMaterial("M2_ConveyorBlue", new Color(0.015f, 0.18f, 0.38f), new Color(0f, 0.35f, 0.95f));
            Material platform = CreateMaterial("M2_PlatformYellow", new Color(0.92f, 0.66f, 0.2f), new Color(0.75f, 0.42f, 0.08f));
            Material hazard = CreateMaterial("M2_HazardRed", new Color(0.86f, 0.08f, 0.07f), new Color(1.35f, 0.05f, 0.03f));
            Material goal = CreateMaterial("M2_GoalGreen", new Color(0.04f, 0.72f, 0.32f), new Color(0f, 1.3f, 0.42f));
            Material robot = CreateMaterial("M2_RobotWhite", new Color(0.84f, 0.92f, 1f), new Color(0.1f, 0.18f, 0.25f));
            Material trim = CreateMaterial("M2_TrimDark", new Color(0.018f, 0.027f, 0.04f), new Color(0f, 0.015f, 0.028f));
            Material cyanGlow = CreateMaterial("M2_CyanGlow", new Color(0.04f, 0.8f, 1f), new Color(0f, 1.8f, 2.4f));
            Material amberGlow = CreateMaterial("M2_AmberGlow", new Color(1f, 0.68f, 0.22f), new Color(1.6f, 0.72f, 0.1f));
            Material voidPanel = CreateMaterial("M2_VoidPanel", new Color(0.005f, 0.008f, 0.014f), new Color(0f, 0.02f, 0.045f));
            Material pathTile = CreateMaterial("M2_PathTile", new Color(0.045f, 0.17f, 0.26f), new Color(0f, 0.18f, 0.35f));
            Material beltStripe = CreateSolidDecalMaterial("M2_BeltMotionStripe", new Color(0.32f, 0.9f, 1f, 0.45f));
            Material arrowDecal = CreateArrowDecalMaterial("M2_ArrowDecal", new Color(0.64f, 0.78f, 0.86f, 0.92f));
            Material pathArrowDecal = CreateArrowDecalMaterial("M2_PathArrowDecal", new Color(0.06f, 0.95f, 1f, 0.98f));
            Material redArrowDecal = CreateArrowDecalMaterial("M2_RedArrowDecal", new Color(1f, 0.13f, 0.08f, 0.98f));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "ConveyorChallengeRoom";

            GameObject root = new GameObject("Member2_ConveyorRoom");

            BuildEnvironment(root.transform, floor, wall, trim, cyanGlow, amberGlow, voidPanel);
            Transform spawnPoint = CreateMarker("SpawnPoint", new Vector3(0f, 1f, -6.52f), root.transform);
            GameObject player = BuildPlayer(spawnPoint, robot, root.transform);
            Camera camera = BuildCamera(player.transform, root.transform);
            player.GetComponent<SimpleRobotController>().Configure(camera.transform);
            ChallengeHud hud = BuildHud(root.transform);

            BuildConveyorGrid(conveyor, pathTile, beltStripe, arrowDecal, pathArrowDecal, redArrowDecal, cyanGlow, amberGlow, hazard, hud, root.transform);
            BuildGoal(new Vector3(0f, 0.34f, 8.2f), new Vector3(2.6f, 0.46f, 1.35f), goal, cyanGlow, hud, root.transform);
            BuildFailZone(new Vector3(0f, -1.35f, 0f), new Vector3(18f, 2.2f, 24f), hud, root.transform);
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

        private static Material CreateMaterial(string name, Color color, Color emission)
        {
            string path = $"{MaterialsPath}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            material.SetColor("_BaseColor", color);
            material.SetColor("_EmissionColor", emission);
            material.EnableKeyword("_EMISSION");
            material.SetFloat("_Smoothness", 0.72f);
            material.SetFloat("_Metallic", 0.08f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateArrowDecalMaterial(string name, Color tint)
        {
            string texturePath = $"{MaterialsPath}/M2_ArrowDecalTexture.png";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                texture = BuildArrowTexture(256);
                File.WriteAllBytes(texturePath, texture.EncodeToPNG());
                AssetDatabase.ImportAsset(texturePath);

                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            }

            string materialPath = $"{MaterialsPath}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.shader = Shader.Find("Sprites/Default");
            material.SetTexture("_MainTex", texture);
            material.SetColor("_Color", tint);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateSolidDecalMaterial(string name, Color tint)
        {
            string texturePath = $"{MaterialsPath}/M2_SolidDecalTexture.png";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
                for (int y = 0; y < texture.height; y++)
                {
                    for (int x = 0; x < texture.width; x++)
                    {
                        texture.SetPixel(x, y, Color.white);
                    }
                }

                texture.Apply();
                File.WriteAllBytes(texturePath, texture.EncodeToPNG());
                AssetDatabase.ImportAsset(texturePath);
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            }

            string materialPath = $"{MaterialsPath}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.shader = Shader.Find("Sprites/Default");
            material.SetTexture("_MainTex", texture);
            material.SetColor("_Color", tint);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D BuildArrowTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(1f, 1f, 1f, 0f);
            Color solid = Color.white;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }

            Vector2[] arrow =
            {
                new Vector2(0.42f, 0.12f),
                new Vector2(0.58f, 0.12f),
                new Vector2(0.58f, 0.58f),
                new Vector2(0.78f, 0.58f),
                new Vector2(0.5f, 0.9f),
                new Vector2(0.22f, 0.58f),
                new Vector2(0.42f, 0.58f)
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                    if (IsInsidePolygon(point, arrow))
                    {
                        texture.SetPixel(x, y, solid);
                    }
                }
            }

            texture.Apply();
            return texture;
        }

        private static bool IsInsidePolygon(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                bool intersects = polygon[i].y > point.y != polygon[j].y > point.y
                    && point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x;
                if (intersects)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static void BuildEnvironment(Transform parent, Material floor, Material wall, Material trim, Material cyanGlow, Material amberGlow, Material voidPanel)
        {
            CreateCube("VoidFloor_VisualOnly", new Vector3(0f, -1.8f, 0f), new Vector3(17f, 0.12f, 23f), voidPanel, parent, false);
            CreateCube("GridUnderlay", new Vector3(0f, 0.02f, 0f), new Vector3(15f, 0.12f, 17f), floor, parent);
            CreateCube("LeftWall", new Vector3(-7.8f, 1.45f, 0f), new Vector3(0.35f, 2.9f, 20f), wall, parent);
            CreateCube("RightWall", new Vector3(7.8f, 1.45f, 0f), new Vector3(0.35f, 2.9f, 20f), wall, parent);
            CreateCube("BackWall", new Vector3(0f, 1.45f, -9.95f), new Vector3(15.8f, 2.9f, 0.35f), wall, parent);
            CreateCube("ExitFrame", new Vector3(0f, 1.45f, 9.95f), new Vector3(15.8f, 2.9f, 0.35f), wall, parent);
            CreateCube("StartGateGlow", new Vector3(0f, 1.2f, -8.95f), new Vector3(3.2f, 0.12f, 0.14f), cyanGlow, parent, false);
            CreateCube("ExitGateGlow", new Vector3(0f, 1.2f, 8.95f), new Vector3(3.2f, 0.12f, 0.14f), amberGlow, parent, false);
            BuildWallPanels(parent, trim, cyanGlow, amberGlow);
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
            camera.backgroundColor = new Color(0.002f, 0.004f, 0.01f);
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

            Text objective = CreateText("ObjectiveText", canvasObject.transform, new Vector2(34f, -30f), 20, FontStyle.Bold, new Color(0.74f, 0.95f, 1f), TextAnchor.UpperLeft);
            Text status = CreateText("StatusText", canvasObject.transform, new Vector2(34f, -62f), 16, FontStyle.Normal, new Color(0.86f, 0.9f, 0.98f), TextAnchor.UpperLeft);
            Text centerMessage = CreateCenterText("CenterMessageText", canvasObject.transform);

            ChallengeHud hud = canvasObject.AddComponent<ChallengeHud>();
            hud.Configure(status, objective, centerMessage);
            return hud;
        }

        private static Text CreateText(string name, Transform parent, Vector2 anchoredPosition, int size, FontStyle style, Color color, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent);
            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
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
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.93f, 1f, 0.94f);
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

        private static void BuildConveyorGrid(Material conveyor, Material pathTile, Material beltStripe, Material arrowDecal, Material pathArrowDecal, Material redArrowDecal, Material cyanGlow, Material amberGlow, Material hazard, ChallengeHud hud, Transform parent)
        {
            const int gridSize = 9;
            const float tileSize = 1.55f;
            const float tileGap = 0.08f;
            const float y = 0.24f;
            float step = tileSize + tileGap;

            Vector2Int[] solution =
            {
                new Vector2Int(0, -4),
                new Vector2Int(0, -3),
                new Vector2Int(1, -3),
                new Vector2Int(2, -3),
                new Vector2Int(2, -2),
                new Vector2Int(1, -2),
                new Vector2Int(1, -1),
                new Vector2Int(1, 0),
                new Vector2Int(2, 0),
                new Vector2Int(3, 0),
                new Vector2Int(3, 1),
                new Vector2Int(2, 1),
                new Vector2Int(2, 2),
                new Vector2Int(2, 3),
                new Vector2Int(1, 3),
                new Vector2Int(0, 3),
                new Vector2Int(0, 4)
            };

            Vector2Int[] hazards =
            {
                new Vector2Int(-2, -1),
                new Vector2Int(4, -2),
                new Vector2Int(-3, 2),
                new Vector2Int(4, 3)
            };

            for (int row = -4; row <= 4; row++)
            {
                for (int col = -4; col <= 4; col++)
                {
                    Vector2Int cell = new Vector2Int(col, row);
                    bool isPath = Contains(solution, cell);
                    bool isHazard = Contains(hazards, cell);
                    Vector3 direction = isPath ? DirectionForPath(solution, cell) : DirectionForFiller(col, row);
                    Material tileMaterial = isPath ? pathTile : conveyor;
                    Material arrowMaterial = isHazard ? redArrowDecal : isPath ? pathArrowDecal : arrowDecal;
                    Vector3 position = new Vector3(col * step, y, row * step);

                    BuildConveyorTile($"GridTile_{col + 4}_{row + 4}", position, tileSize, direction, isPath ? 6.4f : 4.6f, tileMaterial, beltStripe, arrowMaterial, parent);

                    if (isHazard)
                    {
                        BuildHazardMarker($"HazardTile_{col + 4}_{row + 4}", position + Vector3.up * 0.62f, hazard, hud, parent);
                    }
                }
            }

            BuildGridFrame(gridSize, step, tileSize, cyanGlow, amberGlow, parent);
        }

        private static void BuildConveyorTile(string name, Vector3 position, float tileSize, Vector3 localDirection, float speed, Material tileMaterial, Material stripeMaterial, Material arrowMaterial, Transform parent)
        {
            GameObject tile = CreateCube(name, position, new Vector3(tileSize, 0.3f, tileSize), tileMaterial, parent);

            GameObject trigger = new GameObject("PushTrigger");
            trigger.transform.SetParent(tile.transform);
            trigger.transform.localPosition = new Vector3(0f, 0.58f, 0f);
            BoxCollider triggerCollider = trigger.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector3(1f, 1.1f, 1f);
            ConveyorBelt conveyorBelt = trigger.AddComponent<ConveyorBelt>();
            conveyorBelt.Configure(localDirection, speed, 7.4f);

            BuildMotionStripes($"{name}_Motion", position + Vector3.up * 0.17f, localDirection, tileSize, stripeMaterial, parent);
            BuildArrowGraphic($"{name}_Arrow", position + Vector3.up * 0.18f, localDirection, tileSize, arrowMaterial, parent);
        }

        private static void BuildMotionStripes(string name, Vector3 position, Vector3 direction, float tileSize, Material material, Transform parent)
        {
            Vector3 normalized = direction.normalized;
            Quaternion rotation = Quaternion.LookRotation(Vector3.up, normalized);
            float[] offsets = { -0.48f, -0.16f, 0.16f, 0.48f };

            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Quad);
                stripe.name = $"{name}_Stripe_{i + 1}";
                stripe.transform.SetParent(parent);
                stripe.transform.position = position + normalized * tileSize * offsets[i];
                stripe.transform.rotation = rotation;
                stripe.transform.localScale = new Vector3(tileSize * 0.9f, tileSize * 0.1f, 1f);
                stripe.GetComponent<Renderer>().sharedMaterial = material;

                Collider collider = stripe.GetComponent<Collider>();
                if (collider != null)
                {
                    Object.DestroyImmediate(collider);
                }

                ConveyorBeltAnimator animator = stripe.AddComponent<ConveyorBeltAnimator>();
                animator.Configure(normalized, tileSize * 1.08f, 1.35f);
            }
        }

        private static void BuildArrowGraphic(string name, Vector3 position, Vector3 direction, float tileSize, Material material, Transform parent)
        {
            Vector3 normalized = direction.normalized;
            GameObject decal = GameObject.CreatePrimitive(PrimitiveType.Quad);
            decal.name = name;
            decal.transform.SetParent(parent);
            decal.transform.position = position;
            decal.transform.rotation = Quaternion.LookRotation(Vector3.up, normalized);
            decal.transform.localScale = Vector3.one * (tileSize * 0.78f);
            decal.GetComponent<Renderer>().sharedMaterial = material;

            Collider collider = decal.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static void BuildHazardMarker(string name, Vector3 position, Material hazard, ChallengeHud hud, Transform parent)
        {
            GameObject marker = CreateCube(name, position, new Vector3(0.55f, 0.48f, 0.55f), hazard, parent);
            Collider collider = marker.GetComponent<Collider>();
            collider.isTrigger = true;
            MovingHazard movingHazard = marker.AddComponent<MovingHazard>();
            movingHazard.Configure(Vector3.zero, 1f, hud);
        }

        private static void BuildGridFrame(int gridSize, float step, float tileSize, Material cyanGlow, Material amberGlow, Transform parent)
        {
            float width = gridSize * step;
            float half = width * 0.5f - step * 0.5f;
            float edge = half + tileSize * 0.62f;
            float doorwayWidth = step * 2.05f;
            float sideWidth = (width - doorwayWidth) * 0.5f;
            float sideOffset = doorwayWidth * 0.5f + sideWidth * 0.5f;

            CreateCube("GridFrame_Left", new Vector3(-edge, 0.52f, 0f), new Vector3(0.16f, 0.45f, width), cyanGlow, parent);
            CreateCube("GridFrame_Right", new Vector3(edge, 0.52f, 0f), new Vector3(0.16f, 0.45f, width), cyanGlow, parent);
            CreateCube("GridFrame_Back_Left", new Vector3(-sideOffset, 0.52f, -edge), new Vector3(sideWidth, 0.45f, 0.16f), cyanGlow, parent);
            CreateCube("GridFrame_Back_Right", new Vector3(sideOffset, 0.52f, -edge), new Vector3(sideWidth, 0.45f, 0.16f), cyanGlow, parent);
            CreateCube("GridFrame_Exit_Left", new Vector3(-sideOffset, 0.52f, edge), new Vector3(sideWidth, 0.45f, 0.16f), amberGlow, parent);
            CreateCube("GridFrame_Exit_Right", new Vector3(sideOffset, 0.52f, edge), new Vector3(sideWidth, 0.45f, 0.16f), amberGlow, parent);
            CreateCube("GridFrame_StartDoorGlow", new Vector3(0f, 0.62f, -edge), new Vector3(doorwayWidth, 0.08f, 0.08f), cyanGlow, parent, false);
            CreateCube("GridFrame_ExitDoorGlow", new Vector3(0f, 0.62f, edge), new Vector3(doorwayWidth, 0.08f, 0.08f), amberGlow, parent, false);
        }

        private static Vector3 DirectionForPath(Vector2Int[] path, Vector2Int cell)
        {
            for (int i = 0; i < path.Length - 1; i++)
            {
                if (path[i] == cell)
                {
                    Vector2Int delta = path[i + 1] - path[i];
                    return new Vector3(delta.x, 0f, delta.y);
                }
            }

            return Vector3.forward;
        }

        private static Vector3 DirectionForFiller(int col, int row)
        {
            int selector = Mathf.Abs(col * 17 + row * 31) % 4;
            switch (selector)
            {
                case 0:
                    return Vector3.forward;
                case 1:
                    return Vector3.right;
                case 2:
                    return Vector3.back;
                default:
                    return Vector3.left;
            }
        }

        private static bool Contains(Vector2Int[] cells, Vector2Int target)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] == target)
                {
                    return true;
                }
            }

            return false;
        }

        private static void BuildMovingPlatform(Vector3 position, Vector3 scale, Vector3 offset, float duration, Material material, Transform parent)
        {
            GameObject platform = CreateCube("MovingPlatform_A", position, scale, material, parent);
            Rigidbody rigidbody = platform.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            MovingPlatform movingPlatform = platform.AddComponent<MovingPlatform>();
            movingPlatform.Configure(offset, duration);
        }

        private static void BuildMovingPlatform(Vector3 position, Vector3 scale, Vector3 offset, float duration, Material material, Material glow, Transform parent)
        {
            GameObject platform = CreateCube("MovingPlatform_A", position, scale, material, parent);
            GameObject centerGlow = CreateCube("MovingPlatform_A_CenterGlow", position + Vector3.up * 0.23f, new Vector3(scale.x * 0.7f, 0.06f, 0.14f), glow, parent, false);
            centerGlow.AddComponent<MovingPlatform>().Configure(offset, duration);
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

        private static void BuildGoal(Vector3 position, Vector3 scale, Material material, Material glow, ChallengeHud hud, Transform parent)
        {
            GameObject goal = CreateCube("GoalZone", position, scale, material, parent);
            CreateCube("GoalZone_HologramRing", position + Vector3.up * 0.45f, new Vector3(scale.x * 0.75f, 0.06f, 0.08f), glow, parent, false);
            CreateCube("GoalZone_HologramRing_Back", position + new Vector3(0f, 0.45f, 0.52f), new Vector3(scale.x * 0.75f, 0.06f, 0.08f), glow, parent, false);
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
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.035f, 0.05f, 0.085f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.01f, 0.015f, 0.025f);
            RenderSettings.fogDensity = 0.018f;

            GameObject directional = new GameObject("Room_KeyLight");
            directional.transform.SetParent(parent);
            directional.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            Light keyLight = directional.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(0.72f, 0.86f, 1f);
            keyLight.intensity = 0.62f;

            CreatePointLight("BlueConveyorLight", new Vector3(-3f, 3f, -4f), new Color(0.05f, 0.55f, 1f), 3.2f, parent);
            CreatePointLight("RedHazardLight", new Vector3(0f, 3f, 7.5f), new Color(1f, 0.08f, 0.05f), 3.8f, parent);
            CreatePointLight("GreenGoalLight", new Vector3(0f, 3f, 10.5f), new Color(0.05f, 1f, 0.38f), 3.8f, parent);
            CreatePointLight("AmberBridgeLight", new Vector3(2.5f, 2.4f, 2.5f), new Color(1f, 0.55f, 0.12f), 2.4f, parent);
        }

        private static void CreatePointLight(string name, Vector3 position, Color color, float intensity, Transform parent)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = 7f;
            light.intensity = intensity;
        }

        private static Transform CreateMarker(string name, Vector3 position, Transform parent)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(parent);
            marker.transform.position = position;
            return marker.transform;
        }

        private static void BuildDeck(string name, Vector3 position, Vector3 scale, Material deck, Material trim, Material glow, Transform parent)
        {
            CreateCube(name, position, scale, deck, parent);
            CreateCube($"{name}_FrontTrim", position + new Vector3(0f, 0.19f, -scale.z * 0.5f + 0.08f), new Vector3(scale.x * 0.94f, 0.08f, 0.08f), trim, parent, false);
            CreateCube($"{name}_BackTrim", position + new Vector3(0f, 0.19f, scale.z * 0.5f - 0.08f), new Vector3(scale.x * 0.94f, 0.08f, 0.08f), trim, parent, false);
            CreateCube($"{name}_LeftGlow", position + new Vector3(-scale.x * 0.5f + 0.12f, 0.23f, 0f), new Vector3(0.07f, 0.06f, scale.z * 0.75f), glow, parent, false);
            CreateCube($"{name}_RightGlow", position + new Vector3(scale.x * 0.5f - 0.12f, 0.23f, 0f), new Vector3(0.07f, 0.06f, scale.z * 0.75f), glow, parent, false);
        }

        private static void BuildBridge(string name, Vector3 position, Vector3 scale, Material deck, Material glow, Transform parent)
        {
            CreateCube(name, position, scale, deck, parent);
            CreateCube($"{name}_LeftGuard", position + new Vector3(-scale.x * 0.5f - 0.08f, 0.28f, 0f), new Vector3(0.1f, 0.32f, scale.z), glow, parent);
            CreateCube($"{name}_RightGuard", position + new Vector3(scale.x * 0.5f + 0.08f, 0.28f, 0f), new Vector3(0.1f, 0.32f, scale.z), glow, parent);
            CreateCube($"{name}_CenterLine", position + Vector3.up * 0.17f, new Vector3(scale.x * 0.55f, 0.05f, 0.08f), glow, parent, false);
        }

        private static void BuildWallPanels(Transform parent, Material trim, Material cyanGlow, Material amberGlow)
        {
            for (int i = 0; i < 6; i++)
            {
                float z = -9f + i * 3.6f;
                Material glow = i % 2 == 0 ? cyanGlow : amberGlow;
                CreateCube($"LeftWall_NeonPanel_{i}", new Vector3(-7.08f, 1.35f, z), new Vector3(0.08f, 0.95f, 1.05f), trim, parent, false);
                CreateCube($"LeftWall_NeonLine_{i}", new Vector3(-7f, 1.35f, z), new Vector3(0.05f, 0.08f, 0.72f), glow, parent, false);
                CreateCube($"RightWall_NeonPanel_{i}", new Vector3(7.08f, 1.35f, z), new Vector3(0.08f, 0.95f, 1.05f), trim, parent, false);
                CreateCube($"RightWall_NeonLine_{i}", new Vector3(7f, 1.35f, z), new Vector3(0.05f, 0.08f, 0.72f), glow, parent, false);
            }

            CreateCube("SecurityGate_TopWarning", new Vector3(0f, 2.15f, 7.55f), new Vector3(6f, 0.16f, 0.18f), amberGlow, parent, false);
            CreateCube("SecurityGate_LeftPost", new Vector3(-3.15f, 1.1f, 7.55f), new Vector3(0.18f, 2f, 0.18f), amberGlow, parent, false);
            CreateCube("SecurityGate_RightPost", new Vector3(3.15f, 1.1f, 7.55f), new Vector3(0.18f, 2f, 0.18f), amberGlow, parent, false);
        }

        private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool hasCollider = true)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            if (!hasCollider)
            {
                Object.DestroyImmediate(cube.GetComponent<Collider>());
            }

            return cube;
        }
    }
}
