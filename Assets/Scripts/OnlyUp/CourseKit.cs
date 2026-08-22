using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
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

            // 에디터에서 Visual을 늘리거나 옮기면 이 콜라이더도 자동으로 따라오게 동기화한다.
            // 발판은 capThickness를 켜서 콜라이더 두께를 얇게 고정한다 — 모델에 달린 장식(토퍼 등)이
            // 바운즈 높이를 부풀려도 다음 발판까지의 점프 공간이 항상 확보되어 캐릭터가 위쪽 발판에 끼지 않는다.
            PlatformColliderSync sync = platform.AddComponent<PlatformColliderSync>();
            sync.targetCollider = collider;
            sync.visual = visual.transform;
            sync.capThickness = true;

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
            else
            {
                // Resources/Platforms에 VARCO 3D로 만든 발판 모델이 있으면 그 중 하나를 무작위로 골라 교체하고,
                // 없으면(모델이 아직 없을 때) 기존처럼 연한 보라색 큐브로 대체한다.
                GameObject[] rockPrefabs = Resources.LoadAll<GameObject>("Platforms");
                if (rockPrefabs != null && rockPrefabs.Length > 0)
                {
                    GameObject chosen = rockPrefabs[Random.Range(0, rockPrefabs.Length)];
                    // SwapVisual이 기존 Visual(큐브)의 로컬 위치/회전/스케일을 그대로 새 모델에 적용해주므로
                    // 발판 크기(size)에 맞춰 자동으로 늘어난다.
                    Transform newVisual = visualSwap.SwapVisual(chosen);

                    // 별/쿠션 모양처럼 각지고 오목한 실제 모델은 사각형 BoxCollider와 눈에 보이는 모양이
                    // 많이 어긋난다(구석에서 공중에 뜨거나, 뾰족한 부분을 밟았는데 그대로 통과하는 등).
                    // 그래서 이 경우엔 BoxCollider 대신 모델 메시로 직접 만든 콜라이더로 교체해 눈에 보이는
                    // 모양과 최대한 맞춘다.
                    DestroySafe(sync);
                    DestroySafe(collider);
                    CreateModelCollisionMesh(platform, newVisual);
                }
                else
                {
                    SetUniqueColor(visual.GetComponent<Renderer>(), new Color(0.78f, 0.65f, 0.95f));
                }
            }

            return platform;
        }

        /// <summary>
        /// 발판 콜라이더 두께의 최댓값. BoxCollider capThickness와 같은 값을 써서, 어떤 방식으로
        /// 콜라이더를 만들든 다음 발판까지의 점프 공간(헤드룸)이 항상 똑같이 확보되게 한다.
        /// </summary>
        private const float PlatformColliderMaxThickness = 0.4f;

        /// <summary>
        /// 실제 3D 모델(별/쿠션처럼 각지고 오목한 형태)의 위에서 내려다본 윤곽선(2D 볼록 껍질)을 뽑아서,
        /// 그 윤곽선을 얇게(PlatformColliderMaxThickness) 압출한 프리즘 모양으로 콜라이더를 만든다.
        /// 이렇게 하면 사각형 BoxCollider보다 눈에 보이는 모양과 충돌 범위가 훨씬 비슷해지고
        /// (별 모서리 공중 뜸/구멍으로 통과 같은 문제가 줄어듦), 메시 전체(수천 개 정점)를 그대로
        /// 컨벡스 콜라이더에 넘길 때 생기는 PhysX 정점 제한(256개) 경고도 피할 수 있다.
        /// 두께를 얇게 하는 이유는 모델에 달린 장식(작은 토퍼 등)까지 그대로 쓰면 콜라이더가 높아져서
        /// 다음 발판까지의 점프 공간(헤드룸)이 줄어들 수 있기 때문이다. 윗면(착지면) 위치는 그대로 둔다.
        /// </summary>
        private static void CreateModelCollisionMesh(GameObject platform, Transform visual)
        {
            MeshFilter meshFilter = visual.GetComponentInChildren<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null) return;

            Mesh sourceMesh = meshFilter.sharedMesh;
            Transform meshTransform = meshFilter.transform;
            Vector3[] vertices = sourceMesh.vertices;

            float maxLocalY = float.MinValue;
            var footprint = new List<Vector2>(vertices.Length);
            for (int i = 0; i < vertices.Length; i++)
            {
                if (vertices[i].y > maxLocalY) maxLocalY = vertices[i].y;
                footprint.Add(new Vector2(vertices[i].x, vertices[i].z));
            }

            List<Vector2> hull = ComputeConvexHull2D(footprint);
            if (hull.Count < 3) return; // 퇴화된 모양(흔치 않음) — 콜라이더 없이 두는 것보다는 기존 로직이 낫지만 매우 드문 케이스

            float scaleY = meshTransform.lossyScale.y;
            float maxThicknessLocal = scaleY > 0.0001f ? PlatformColliderMaxThickness / scaleY : PlatformColliderMaxThickness;
            float floorLocalY = maxLocalY - maxThicknessLocal;

            int hullCount = hull.Count;
            Vector3[] prismVerts = new Vector3[hullCount * 2];
            for (int i = 0; i < hullCount; i++)
            {
                prismVerts[i] = new Vector3(hull[i].x, floorLocalY, hull[i].y);
                prismVerts[hullCount + i] = new Vector3(hull[i].x, maxLocalY, hull[i].y);
            }

            var tris = new List<int>();
            for (int i = 1; i < hullCount - 1; i++) // 아랫면
            {
                tris.Add(0); tris.Add(i + 1); tris.Add(i);
            }
            for (int i = 1; i < hullCount - 1; i++) // 윗면
            {
                tris.Add(hullCount); tris.Add(hullCount + i); tris.Add(hullCount + i + 1);
            }
            for (int i = 0; i < hullCount; i++) // 옆면
            {
                int next = (i + 1) % hullCount;
                tris.Add(i); tris.Add(hullCount + i); tris.Add(hullCount + next);
                tris.Add(i); tris.Add(hullCount + next); tris.Add(next);
            }

            Mesh collisionMesh = new Mesh();
            collisionMesh.vertices = prismVerts;
            collisionMesh.triangles = tris.ToArray();
            collisionMesh.RecalculateBounds();
            collisionMesh.RecalculateNormals();

            GameObject collisionGO = new GameObject("CollisionMesh");
            collisionGO.transform.SetParent(platform.transform, false);
            collisionGO.transform.position = meshTransform.position;
            collisionGO.transform.rotation = meshTransform.rotation;
            collisionGO.transform.localScale = meshTransform.lossyScale;

            MeshCollider meshCollider = collisionGO.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = collisionMesh;
            meshCollider.convex = true;
        }

        /// <summary>
        /// Andrew's monotone chain 알고리즘으로 2D 점 집합의 볼록 껍질(convex hull)을 구한다.
        /// </summary>
        private static List<Vector2> ComputeConvexHull2D(List<Vector2> points)
        {
            points.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            int n = points.Count;
            if (n < 3) return points;

            Vector2[] hull = new Vector2[2 * n];
            int k = 0;

            for (int i = 0; i < n; i++)
            {
                while (k >= 2 && Cross2D(hull[k - 2], hull[k - 1], points[i]) <= 0) k--;
                hull[k++] = points[i];
            }

            int lower = k + 1;
            for (int i = n - 2; i >= 0; i--)
            {
                while (k >= lower && Cross2D(hull[k - 2], hull[k - 1], points[i]) <= 0) k--;
                hull[k++] = points[i];
            }

            var result = new List<Vector2>(k - 1);
            for (int i = 0; i < k - 1; i++) result.Add(hull[i]);
            return result;
        }

        private static float Cross2D(Vector2 o, Vector2 a, Vector2 b)
        {
            return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
        }

        /// <summary>
        /// Resources/Obstacles에 실제 3D 장애물 모델이 있으면 그 중 하나를 무작위로 골라 Visual을
        /// 교체하고, 없으면(모델이 아직 없을 때) 기존처럼 단색 큐브로 남겨둔다.
        /// 발판(CreatePlatform)과 달리 콜라이더는 손대지 않는다 — 장애물의 충돌 판정은 항상
        /// 코드로 계산한 obstacleSize 박스 그대로 유지해야, 모델이 어떤 모양이든(둥근 구 등)
        /// 장애물 크기에 비례한 착지 공간 계산(GameBootstrap의 가장자리 배치/레이캐스트)이 그대로 맞는다.
        /// </summary>
        private static void ApplyObstacleVisual(VisualSwapTarget visualSwap, PlatformColliderSync sync, GameObject fallbackCubeVisual, Color fallbackColor)
        {
            GameObject[] obstacleModels = Resources.LoadAll<GameObject>("Obstacles");
            if (obstacleModels != null && obstacleModels.Length > 0)
            {
                GameObject chosen = obstacleModels[Random.Range(0, obstacleModels.Length)];
                sync.visual = visualSwap.SwapVisual(chosen);
            }
            else
            {
                SetUniqueColor(fallbackCubeVisual.GetComponent<Renderer>(), fallbackColor);
            }
        }

        /// <summary>
        /// 발판 위에 고정된 장애물(기본값: 빨간 큐브, 모델이 있으면 무작위 3D 모델)을 놓는다.
        /// 점프로 넘거나 옆으로 피해야 하며, 부딪히면 Obstacle 값에 따라 플레이어가 튕겨나간다.
        /// obstaclePosition은 발판 중심이 아니라 (필요시) 한쪽으로 치우친 실제 배치 위치를 받는다 —
        /// 발판이 작을 때 장애물을 정중앙에 두면 발판 전체를 거의 다 차지해서 착지할 곳이 없어지므로,
        /// 호출부(GameBootstrap)에서 가장자리 쪽으로 옮긴 위치를 넘겨 반대편에 착지 공간을 확보한다.
        /// </summary>
        public static void CreateStaticObstacle(Transform parent, Vector3 obstaclePosition, Vector3 platformSize, Vector3 obstacleSize, float knockbackForce = 14f, float knockbackUpward = 6f)
        {
            GameObject obstacle = new GameObject("Obstacle_Static");
            obstacle.transform.SetParent(parent, false);
            obstacle.transform.position = obstaclePosition + new Vector3(0f, platformSize.y * 0.5f + obstacleSize.y * 0.5f, 0f);

            BoxCollider collider = obstacle.AddComponent<BoxCollider>();
            collider.size = obstacleSize;

            VisualSwapTarget visualSwap = obstacle.AddComponent<VisualSwapTarget>();
            GameObject visual = CreatePrimitiveVisual(obstacle.transform, PrimitiveType.Cube, obstacleSize);
            visualSwap.visual = visual.transform;

            PlatformColliderSync sync = obstacle.AddComponent<PlatformColliderSync>();
            sync.targetCollider = collider;
            sync.visual = visual.transform;

            ApplyObstacleVisual(visualSwap, sync, visual, Color.red);

            Obstacle obstacleScript = obstacle.AddComponent<Obstacle>();
            obstacleScript.knockbackForce = knockbackForce;
            obstacleScript.knockbackUpward = knockbackUpward;
        }

        /// <summary>
        /// 지정한 두 지점 사이를 왕복하는 장애물(기본값: 주황색 큐브, 모델이 있으면 무작위 3D 모델)을 놓는다.
        /// 타이밍을 맞춰 지나가거나 뛰어넘어야 하며, 부딪히면 정지 장애물보다 더 세게 튕겨나간다.
        /// </summary>
        public static void CreateMovingObstacle(Transform parent, Vector3 pointA, Vector3 pointB, Vector3 obstacleSize, float speed = 0.6f, float knockbackForce = 16f, float knockbackUpward = 7f)
        {
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

            PlatformColliderSync sync = obstacle.AddComponent<PlatformColliderSync>();
            sync.targetCollider = collider;
            sync.visual = visual.transform;

            ApplyObstacleVisual(visualSwap, sync, visual, new Color(1f, 0.45f, 0f));

            Obstacle obstacleScript = obstacle.AddComponent<Obstacle>();
            obstacleScript.knockbackForce = knockbackForce;
            obstacleScript.knockbackUpward = knockbackUpward;

            MovingObstacle mover = obstacle.AddComponent<MovingObstacle>();
            mover.pointA = pointA;
            mover.pointB = pointB;
            mover.speed = speed;
        }

        /// <summary>
        /// 배경 음악을 재생한다. 씬에 이미 "BGM" 오브젝트가 있으면 그 AudioSource를 그대로 반환하고
        /// (중복 생성 방지), Resources/Audio에 클립이 없으면 아무것도 만들지 않고 null을 반환한다.
        /// 반환하는 AudioSource는 PauseMenuUI가 배경음악 켜기/끄기 토글에 사용한다.
        /// </summary>
        public static AudioSource SetupBackgroundMusic()
        {
            GameObject existing = GameObject.Find("BGM");
            if (existing != null) return existing.GetComponent<AudioSource>();

            AudioClip clip = Resources.Load<AudioClip>("Audio/BGM_StarHopParade");
            if (clip == null) return null;

            GameObject bgmGO = new GameObject("BGM");
            AudioSource source = bgmGO.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.volume = 0.4f;
            source.playOnAwake = false;
            // BackgroundMusicPlayer.Start()에서 재생한다 — 이 오브젝트를 만든 바로 그 프레임
            // (GameBootstrap.Awake)에서 곧바로 Play()를 호출하면 아직 컴포넌트 초기화가 끝나지
            // 않아 조용히 무시되는 경우가 있어서, 씬의 모든 Awake가 끝난 뒤로 재생을 미룬다.
            bgmGO.AddComponent<BackgroundMusicPlayer>();
            return source;
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

            // 점프 효과음: Resources/Audio에 있으면 재생하고, 없으면 조용히 건너뛴다
            AudioSource jumpAudioSource = player.AddComponent<AudioSource>();
            jumpAudioSource.playOnAwake = false;
            playerController.jumpAudioSource = jumpAudioSource;
            playerController.jumpClip = Resources.Load<AudioClip>("Audio/SFX_JumpChirp");

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

            // 카메라를 만든 김에 발판(연보라)·캐릭터(파란색)와 어울리는 배경도 함께 설정한다
            SetupEnvironment(mainCamera);
        }

        /// <summary>
        /// 배경을 발판(연보라)·캐릭터(파란색) 톤에 어울리는 파스텔 하늘색으로 맞추고,
        /// 같은 색의 안개를 살짝 깔아 멀리 있는 발판이 배경으로 자연스럽게 사라지도록 한다.
        /// 텍스처/스카이박스 에셋 없이 Camera의 단색 배경 + Fog만 사용해서(기본 Unity 기능만),
        /// WebGL 빌드에서도 셰이더 호환성 문제 없이 항상 동일하게 보인다.
        /// </summary>
        public static void SetupEnvironment(Camera camera)
        {
            if (camera == null) return;

            Color skyColor = new Color(0.74f, 0.83f, 0.97f); // 파스텔 하늘색

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = skyColor;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = skyColor;
            RenderSettings.fogStartDistance = 40f;
            RenderSettings.fogEndDistance = 140f;
        }

        /// <summary>
        /// 플레이어 높이에 따라 하늘/안개 색이 지상 색 → 정상(우주) 색으로 서서히 바뀌도록
        /// 카메라에 <see cref="HeightSkyController"/>를 붙이고 시작/끝 높이를 설정한다.
        /// SetupEnvironment가 정한 초기 색을 그대로 groundColor로 이어받아 시작 순간에 색이 튀지 않게 한다.
        /// </summary>
        public static void SetupHeightSky(Camera camera, Transform player, float startHeight, float endHeight)
        {
            if (camera == null) return;

            HeightSkyController sky = camera.GetComponent<HeightSkyController>();
            if (sky == null)
            {
                sky = camera.gameObject.AddComponent<HeightSkyController>();
            }
            sky.player = player;
            sky.startHeight = startHeight;
            sky.endHeight = endHeight;
        }

        /// <summary>
        /// 높이 표시 텍스트 + (숨겨진) 게임 클리어 패널 + ESC 일시정지 메뉴를 담은 UI Canvas를 생성한다.
        /// TextMeshPro Essentials가 프로젝트에 없으므로 기본 UGUI Text를 사용한다.
        /// </summary>
        public static GameClearUI CreateUI(Transform playerTransform, float startHeight, AudioSource bgmSource, PlayerController playerController)
        {
            // Button의 클릭을 받으려면 EventSystem이 있어야 한다. 프로젝트가 새 Input System 패키지를
            // 쓰므로(레거시 Input은 예외를 던짐) StandaloneInputModule 대신 InputSystemUIInputModule을 쓴다.
            if (GameObject.Find("EventSystem") == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<InputSystemUIInputModule>();
            }

            GameObject canvasGO = new GameObject("UICanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasGO.AddComponent<GraphicRaycaster>();

            // 기본 내장 폰트(LegacyRuntime.ttf)는 한글 글리프가 없어서 "소리"/"게임 끝내기" 같은
            // 한글 UI 텍스트가 WebGL 빌드에서 빈 칸으로 보이는 문제가 있었다(에디터에서는 OS 폰트로
            // 대체 렌더링되어 문제가 드러나지 않음). 한글이 포함된 나눔고딕(SIL OFL 라이선스, 무료
            // 재배포 가능)을 Resources/Fonts에 넣어 모든 UI 텍스트에 공통으로 사용한다.
            Font builtinFont = Resources.Load<Font>("Fonts/NanumGothic");
            if (builtinFont == null)
            {
                builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

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

            GameObject timeGO = new GameObject("TimeText");
            timeGO.transform.SetParent(canvasGO.transform, false);
            Text timeText = timeGO.AddComponent<Text>();
            timeText.font = builtinFont;
            timeText.fontSize = 36;
            timeText.color = Color.white;
            timeText.alignment = TextAnchor.UpperLeft;
            RectTransform timeRT = timeText.rectTransform;
            timeRT.anchorMin = new Vector2(0f, 1f);
            timeRT.anchorMax = new Vector2(0f, 1f);
            timeRT.pivot = new Vector2(0f, 1f);
            timeRT.anchoredPosition = new Vector2(20f, -80f);
            timeRT.sizeDelta = new Vector2(400f, 60f);

            PlayTimeUI playTimeUI = canvasGO.AddComponent<PlayTimeUI>();
            playTimeUI.timeText = timeText;

            GameObject fallCountGO = new GameObject("FallCountText");
            fallCountGO.transform.SetParent(canvasGO.transform, false);
            Text fallCountText = fallCountGO.AddComponent<Text>();
            fallCountText.font = builtinFont;
            fallCountText.fontSize = 36;
            fallCountText.color = Color.white;
            fallCountText.alignment = TextAnchor.UpperRight;
            RectTransform fallCountRT = fallCountText.rectTransform;
            fallCountRT.anchorMin = new Vector2(1f, 1f);
            fallCountRT.anchorMax = new Vector2(1f, 1f);
            fallCountRT.pivot = new Vector2(1f, 1f);
            fallCountRT.anchoredPosition = new Vector2(-20f, -20f);
            fallCountRT.sizeDelta = new Vector2(300f, 60f);

            FallCountUI fallCountUI = canvasGO.AddComponent<FallCountUI>();
            fallCountUI.respawn = playerTransform.GetComponent<RespawnController>();
            fallCountUI.fallCountText = fallCountText;

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
            playTimeUI.clearUI = clearUI;

            CreatePauseMenu(canvasGO.transform, builtinFont, bgmSource, playerController);

            return clearUI;
        }

        /// <summary>
        /// ESC로 여닫는 일시정지 메뉴(소리/게임 끝내기 메인 바 + 배경음악·점프 사운드 서브 메뉴)를 만든다.
        /// </summary>
        private static void CreatePauseMenu(Transform canvasParent, Font font, AudioSource bgmSource, PlayerController playerController)
        {
            GameObject pausePanelGO = new GameObject("PausePanel");
            pausePanelGO.transform.SetParent(canvasParent, false);
            Image pauseBg = pausePanelGO.AddComponent<Image>();
            pauseBg.color = new Color(0f, 0f, 0f, 0.7f);
            RectTransform pauseBgRT = pauseBg.rectTransform;
            pauseBgRT.anchorMin = Vector2.zero;
            pauseBgRT.anchorMax = Vector2.one;
            pauseBgRT.offsetMin = Vector2.zero;
            pauseBgRT.offsetMax = Vector2.zero;

            Button soundButton;
            CreateMenuButton(pausePanelGO.transform, font, "소리", new Vector2(0f, 40f), out soundButton);

            Button quitButton;
            CreateMenuButton(pausePanelGO.transform, font, "게임 끝내기", new Vector2(0f, -30f), out quitButton);

            GameObject soundPanelGO = new GameObject("SoundPanel");
            soundPanelGO.transform.SetParent(pausePanelGO.transform, false);
            RectTransform soundPanelRT = soundPanelGO.AddComponent<RectTransform>();
            soundPanelRT.anchorMin = new Vector2(0.5f, 0.5f);
            soundPanelRT.anchorMax = new Vector2(0.5f, 0.5f);
            soundPanelRT.anchoredPosition = new Vector2(0f, -150f);
            soundPanelRT.sizeDelta = new Vector2(320f, 120f);

            Button bgmButton;
            Text bgmToggleText = CreateMenuButton(soundPanelGO.transform, font, "배경음악: 켜짐", new Vector2(0f, 30f), out bgmButton);

            Button jumpButton;
            Text jumpToggleText = CreateMenuButton(soundPanelGO.transform, font, "점프 사운드: 켜짐", new Vector2(0f, -30f), out jumpButton);

            soundPanelGO.SetActive(false);
            pausePanelGO.SetActive(false);

            PauseMenuUI pauseMenu = canvasParent.gameObject.AddComponent<PauseMenuUI>();
            pauseMenu.pausePanel = pausePanelGO;
            pauseMenu.soundPanel = soundPanelGO;
            pauseMenu.bgmSource = bgmSource;
            pauseMenu.playerController = playerController;
            pauseMenu.bgmToggleText = bgmToggleText;
            pauseMenu.jumpToggleText = jumpToggleText;

            soundButton.onClick.AddListener(pauseMenu.ToggleSoundPanel);
            quitButton.onClick.AddListener(pauseMenu.QuitGame);
            bgmButton.onClick.AddListener(pauseMenu.ToggleBgm);
            jumpButton.onClick.AddListener(pauseMenu.ToggleJumpSfx);
        }

        /// <summary>
        /// 배경 이미지 + 클릭 가능한 Button + 가운데 정렬된 라벨 Text로 구성된 메뉴 버튼 하나를 만든다.
        /// </summary>
        private static Text CreateMenuButton(Transform parent, Font font, string label, Vector2 anchoredPosition, out Button button)
        {
            GameObject buttonGO = new GameObject(label + "Button");
            buttonGO.transform.SetParent(parent, false);
            Image bg = buttonGO.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.9f);
            RectTransform rt = bg.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = new Vector2(300f, 50f);

            button = buttonGO.AddComponent<Button>();

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            Text text = textGO.AddComponent<Text>();
            text.font = font;
            text.text = label;
            text.fontSize = 24;
            text.color = Color.black;
            text.alignment = TextAnchor.MiddleCenter;
            RectTransform textRT = text.rectTransform;
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            return text;
        }
    }
}
