using UnityEngine;
using UnityEngine.UI;

namespace OnlyUp
{
    /// <summary>
    /// 여러 스테이지(씬)의 Bootstrap 스크립트가 공통으로 쓰는 생성 로직 모음.
    /// 플레이어/카메라/UI/발판/장애물을 코드로 만드는 방식은 모든 스테이지에서 동일하고,
    /// 코스를 "어떻게 배치할지"만 스테이지마다 다르기 때문에 그 공통 부분만 여기로 뽑아냈다.
    /// 새 스테이지를 추가할 때는 이 클래스를 그대로 재사용하고, Bootstrap 쪽에서는
    /// 코스 배치 알고리즘만 새로 작성하면 된다.
    /// </summary>
    public static class CourseKit
    {
        /// <summary>
        /// Object.Destroy는 Play 모드에서만 동작하므로, 에디터에서 미리 코스를 생성할 때(Play 중이 아닐 때)는
        /// DestroyImmediate를 써야 한다. 이 헬퍼로 어느 쪽에서 호출되든 안전하게 제거한다.
        /// </summary>
        private static void DestroySafe(Object obj)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(obj);
            }
            else
            {
                Object.DestroyImmediate(obj);
            }
        }

        /// <summary>
        /// renderer.material은 접근할 때마다 인스턴스를 새로 만들어서(에디터에서 특히) 경고와 함께
        /// 머티리얼을 누수시킬 수 있으므로, 직접 인스턴스를 만들어 sharedMaterial에 대입한다.
        /// (다른 오브젝트와 색을 공유하지 않는 이 오브젝트만의 고유 머티리얼이 된다)
        /// </summary>
        private static void SetUniqueColor(Renderer renderer, Color color)
        {
            if (renderer == null) return;
            Material instance = new Material(renderer.sharedMaterial);
            instance.color = color;
            renderer.sharedMaterial = instance;
        }

        /// <summary>
        /// 지정한 프리미티브로 Visual 자식 오브젝트를 만들고, 프리미티브가 기본으로 들고 있는
        /// Collider는 제거한다 (충돌은 항상 부모 로직 오브젝트가 담당하기 때문).
        /// </summary>
        public static GameObject CreatePrimitiveVisual(Transform logicParent, PrimitiveType type, Vector3 localScale)
        {
            GameObject visual = GameObject.CreatePrimitive(type);
            visual.name = "Visual";
            visual.transform.SetParent(logicParent, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = localScale;

            Collider primitiveCollider = visual.GetComponent<Collider>();
            if (primitiveCollider != null)
            {
                DestroySafe(primitiveCollider);
            }

            return visual;
        }

        /// <summary>
        /// 캡슐(Visual)의 자식으로 작은 눈 하나를 만든다. 순전히 방향 구분용 표식이라
        /// 콜라이더는 제거하고 충돌에 전혀 관여하지 않게 한다.
        /// </summary>
        public static void CreateEye(Transform visualParent, Vector3 localPosition)
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "Eye";
            eye.transform.SetParent(visualParent, false);
            eye.transform.localPosition = localPosition;
            eye.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);

            Collider eyeCollider = eye.GetComponent<Collider>();
            if (eyeCollider != null)
            {
                DestroySafe(eyeCollider);
            }

            SetUniqueColor(eye.GetComponent<Renderer>(), Color.black);
        }

        /// <summary>
        /// 씬에 이미 "Course"라는 이름의 오브젝트가 있으면(에디터에서 미리 생성/편집해둔 코스) 그걸 그대로 쓰고,
        /// 없으면 새로 만든다. 이 함수 하나로 "런타임에 매번 새로 생성" vs "에디터에서 만든 걸 재사용"이 갈린다.
        /// </summary>
        public static Transform FindOrCreateCourseParent()
        {
            GameObject existing = GameObject.Find("Course");
            if (existing != null)
            {
                return existing.transform;
            }
            return new GameObject("Course").transform;
        }

        /// <summary>
        /// Course 안에 이미 Goal이 있으면(에디터에서 미리 만들어둔 코스) 그 Goal을 그대로 쓰고,
        /// 없으면 fallbackPosition에 새로 만든다. 어느 쪽이든 player/clearUI/nextSceneName 참조는
        /// 매 Play마다 다시 연결해준다 (플레이어/UI는 항상 런타임에 새로 생성되기 때문).
        /// </summary>
        public static Goal ConfigureGoal(Transform courseParent, Vector3 fallbackPosition, Vector3 goalPlatformSize, PlayerController player, GameClearUI clearUI, string nextSceneName)
        {
            Goal goalScript = courseParent.GetComponentInChildren<Goal>();
            if (goalScript == null)
            {
                GameObject goal = CreatePlatform(courseParent, "Goal", fallbackPosition, goalPlatformSize, isGoal: true);
                goalScript = goal.GetComponent<Goal>();
            }

            goalScript.player = player;
            goalScript.clearUI = clearUI;
            goalScript.nextSceneName = nextSceneName;
            return goalScript;
        }

        /// <summary>
        /// Cube 프리미티브로 발판(또는 바위 덩어리) 하나를 생성한다.
        /// isGoal이면 Goal 스크립트와 트리거를 추가한다 (player/clearUI/nextSceneName은 호출부에서 설정).
        /// rotationY를 주면 발판을 y축으로 회전시켜 좀 더 자연스러운 바위처럼 배치할 수 있다.
        /// </summary>
        public static GameObject CreatePlatform(Transform parent, string name, Vector3 position, Vector3 size, bool isGoal, float rotationY = 0f)
        {
            GameObject platform = new GameObject(name);
            platform.transform.SetParent(parent, false);
            platform.transform.position = position;
            platform.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);

            // 로직: 충돌을 담당하는 BoxCollider는 부모(로직 오브젝트)에 둔다
            BoxCollider collider = platform.AddComponent<BoxCollider>();
            collider.size = size;

            // 비주얼: Visual 자식으로 Cube 프리미티브 생성 (나중에 모델로 교체될 부분)
            VisualSwapTarget visualSwap = platform.AddComponent<VisualSwapTarget>();
            GameObject visual = CreatePrimitiveVisual(platform.transform, PrimitiveType.Cube, size);
            visualSwap.visual = visual.transform;

            // 에디터에서 Visual을 늘리거나 옮기면 이 콜라이더도 자동으로 따라오게 동기화한다
            PlatformColliderSync sync = platform.AddComponent<PlatformColliderSync>();
            sync.targetCollider = collider;
            sync.visual = visual.transform;

            if (isGoal)
            {
                // Goal 발판은 색을 다르게 표시해 눈에 띄게 한다 (임시 더미 비주얼 구분용)
                SetUniqueColor(visual.GetComponent<Renderer>(), Color.yellow);

                // 발판 위 공간에 트리거 콜라이더를 추가로 붙여 Goal 도달을 감지한다
                BoxCollider trigger = platform.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.center = new Vector3(0f, size.y, 0f);
                trigger.size = new Vector3(size.x * 0.8f, size.y * 2f, size.z * 0.8f);

                platform.AddComponent<Goal>();
            }

            return platform;
        }

        /// <summary>
        /// 발판 위에 고정된 장애물(빨간 큐브)을 놓는다. 점프로 넘거나 옆으로 피해야 하며,
        /// 부딪히면 Obstacle 값에 따라 플레이어가 튕겨나간다.
        /// </summary>
        public static void CreateStaticObstacle(Transform parent, Vector3 platformPosition, Vector3 platformSize, float knockbackForce = 14f, float knockbackUpward = 6f)
        {
            Vector3 obstacleSize = new Vector3(1f, 1f, 1f);
            GameObject obstacle = new GameObject("Obstacle_Static");
            obstacle.transform.SetParent(parent, false);
            obstacle.transform.position = platformPosition + new Vector3(0f, platformSize.y * 0.5f + obstacleSize.y * 0.5f, 0f);

            BoxCollider collider = obstacle.AddComponent<BoxCollider>();
            collider.size = obstacleSize;

            VisualSwapTarget visualSwap = obstacle.AddComponent<VisualSwapTarget>();
            GameObject visual = CreatePrimitiveVisual(obstacle.transform, PrimitiveType.Cube, obstacleSize);
            visualSwap.visual = visual.transform;
            SetUniqueColor(visual.GetComponent<Renderer>(), Color.red);

            PlatformColliderSync sync = obstacle.AddComponent<PlatformColliderSync>();
            sync.targetCollider = collider;
            sync.visual = visual.transform;

            Obstacle obstacleScript = obstacle.AddComponent<Obstacle>();
            obstacleScript.knockbackForce = knockbackForce;
            obstacleScript.knockbackUpward = knockbackUpward;
        }

        /// <summary>
        /// 지정한 두 지점 사이를 왕복하는 장애물(주황색 큐브)을 놓는다. 타이밍을 맞춰 지나가거나
        /// 뛰어넘어야 하며, 부딪히면 정지 장애물보다 더 세게 튕겨나간다.
        /// </summary>
        public static void CreateMovingObstacle(Transform parent, Vector3 pointA, Vector3 pointB, float speed = 0.6f, float knockbackForce = 16f, float knockbackUpward = 7f)
        {
            Vector3 obstacleSize = new Vector3(1f, 1f, 1f);

            GameObject obstacle = new GameObject("Obstacle_Moving");
            obstacle.transform.SetParent(parent, false);
            obstacle.transform.position = pointA;

            // 스크립트로 매 프레임 위치를 옮기므로, 정적 콜라이더가 아니라 Kinematic Rigidbody로
            // 표시해야 물리 엔진이 이동을 올바르게 반영하고 충돌 판정도 정확해진다.
            Rigidbody rb = obstacle.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            BoxCollider collider = obstacle.AddComponent<BoxCollider>();
            collider.size = obstacleSize;

            VisualSwapTarget visualSwap = obstacle.AddComponent<VisualSwapTarget>();
            GameObject visual = CreatePrimitiveVisual(obstacle.transform, PrimitiveType.Cube, obstacleSize);
            visualSwap.visual = visual.transform;
            SetUniqueColor(visual.GetComponent<Renderer>(), new Color(1f, 0.45f, 0f));

            PlatformColliderSync sync = obstacle.AddComponent<PlatformColliderSync>();
            sync.targetCollider = collider;
            sync.visual = visual.transform;

            Obstacle obstacleScript = obstacle.AddComponent<Obstacle>();
            obstacleScript.knockbackForce = knockbackForce;
            obstacleScript.knockbackUpward = knockbackUpward;

            MovingObstacle mover = obstacle.AddComponent<MovingObstacle>();
            mover.pointA = pointA;
            mover.pointB = pointB;
            mover.speed = speed;
        }

        /// <summary>
        /// 플레이어 로직(CharacterController + 조작/리스폰 스크립트) + Visual(Capsule+눈)을 생성한다.
        /// </summary>
        public static GameObject CreatePlayer(Vector3 spawnPosition, float fallLimitY)
        {
            GameObject player = new GameObject("Player");
            player.tag = "Player"; // Goal 트리거 판정에 사용되는 Unity 기본 태그
            player.transform.position = spawnPosition;

            CharacterController controller = player.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 1f, 0f);
            controller.height = 2f;
            controller.radius = 0.4f;

            VisualSwapTarget visualSwap = player.AddComponent<VisualSwapTarget>();

            // Resources/PlayerModel 프리팹이 있으면(VARCO 3D나 외부 에셋으로 교체된 실제 캐릭터) 그것을 쓰고,
            // 없으면 기존 캡슐+눈 더미 비주얼로 대체한다. Resources.Load는 빌드에도 포함되어 WebGL에서도 동작한다.
            GameObject playerModelPrefab = Resources.Load<GameObject>("PlayerModel");
            GameObject visual;
            if (playerModelPrefab != null)
            {
                // 프리팹 자체에 이미 CharacterController 크기에 맞는 스케일/위치가 저장되어 있으므로 그대로 사용한다
                visual = Object.Instantiate(playerModelPrefab, player.transform, false);
                visual.name = "Visual";
            }
            else
            {
                visual = CreatePrimitiveVisual(player.transform, PrimitiveType.Capsule, Vector3.one);
                visual.transform.localPosition = new Vector3(0f, 1f, 0f); // CharacterController center와 맞춤

                // 앞/뒤/좌/우를 눈으로 구분할 수 있도록 캡슐 앞면(+Z, 이동 방향과 동일)에 눈 2개를 붙인다.
                CreateEye(visual.transform, new Vector3(-0.17f, 0.45f, 0.56f));
                CreateEye(visual.transform, new Vector3(0.17f, 0.45f, 0.56f));
            }
            visualSwap.visual = visual.transform;

            PlayerController playerController = player.AddComponent<PlayerController>();
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                playerController.cameraTransform = mainCamera.transform;
            }

            RespawnController respawn = player.AddComponent<RespawnController>();
            respawn.spawnPosition = spawnPosition;
            respawn.fallLimitY = fallLimitY;

            return player;
        }

        /// <summary>
        /// 씬에 이미 배치된 메인 카메라에 CameraFollow(마우스 궤도 3인칭)를 붙인다.
        /// 메인 카메라가 없으면(사용자가 씬 준비를 잘못한 경우를 대비) 새로 하나 만든다.
        /// </summary>
        public static void SetupCameraFollow(Transform playerTransform)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[CourseKit] 씬에 MainCamera가 없어 새로 생성합니다. 씬에 카메라를 미리 배치해두는 것을 권장합니다.");
                GameObject camGO = new GameObject("Main Camera");
                mainCamera = camGO.AddComponent<Camera>();
                camGO.tag = "MainCamera";
            }

            CameraFollow follow = mainCamera.GetComponent<CameraFollow>();
            if (follow == null)
            {
                follow = mainCamera.gameObject.AddComponent<CameraFollow>();
            }
            follow.target = playerTransform;
        }

        /// <summary>
        /// 높이 표시 텍스트 + (숨겨진) 게임 클리어 패널을 담은 UI Canvas를 생성한다.
        /// TextMeshPro Essentials가 프로젝트에 없으므로 기본 UGUI Text를 사용한다.
        /// </summary>
        public static GameClearUI CreateUI(Transform playerTransform, float startHeight)
        {
            GameObject canvasGO = new GameObject("UICanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasGO.AddComponent<GraphicRaycaster>();

            Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject heightGO = new GameObject("HeightText");
            heightGO.transform.SetParent(canvasGO.transform, false);
            Text heightText = heightGO.AddComponent<Text>();
            heightText.font = builtinFont;
            heightText.fontSize = 36;
            heightText.color = Color.white;
            heightText.alignment = TextAnchor.UpperLeft;
            RectTransform heightRT = heightText.rectTransform;
            heightRT.anchorMin = new Vector2(0f, 1f);
            heightRT.anchorMax = new Vector2(0f, 1f);
            heightRT.pivot = new Vector2(0f, 1f);
            heightRT.anchoredPosition = new Vector2(20f, -20f);
            heightRT.sizeDelta = new Vector2(400f, 60f);

            HeightUI heightUI = canvasGO.AddComponent<HeightUI>();
            heightUI.player = playerTransform;
            heightUI.heightText = heightText;
            heightUI.startHeight = startHeight;

            GameObject panelGO = new GameObject("GameClearPanel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            Image panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.6f);
            RectTransform panelRT = panelImage.rectTransform;
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            GameObject clearTextGO = new GameObject("GameClearText");
            clearTextGO.transform.SetParent(panelGO.transform, false);
            Text clearText = clearTextGO.AddComponent<Text>();
            clearText.text = "GAME CLEAR";
            clearText.font = builtinFont;
            clearText.fontSize = 80;
            clearText.alignment = TextAnchor.MiddleCenter;
            clearText.color = Color.yellow;
            RectTransform clearRT = clearText.rectTransform;
            clearRT.anchorMin = Vector2.zero;
            clearRT.anchorMax = Vector2.one;
            clearRT.offsetMin = Vector2.zero;
            clearRT.offsetMax = Vector2.zero;

            panelGO.SetActive(false);

            GameClearUI clearUI = canvasGO.AddComponent<GameClearUI>();
            clearUI.panel = panelGO;

            return clearUI;
        }
    }
}
