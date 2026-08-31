using UnityEngine;

namespace OnlyUp
{
    /// <summary>
    /// 좁은 외길 발판을 타고 끊김 없이 정상까지 오르는 단일 코스(Only Up! 스타일).
    /// 씬 실행 시점에 플레이어/발판 코스/Goal/UI를 전부 코드로 생성한다.
    /// 씬에는 이 스크립트가 붙은 빈 오브젝트와 Camera, Light만 있으면 된다.
    /// 실제 생성 로직(플레이어/카메라/UI/발판/장애물)은 CourseKit에 공통으로 모아뒀고,
    /// 이 스크립트는 "코스를 어떻게 배치할지"만 담당한다.
    ///
    /// Only Up!처럼 구간(Zone)마다 난이도가 달라지고(쓰레기장→저택→하늘로 갈수록 발판이
    /// 좁아지고 간격이 빡빡해짐), 계속 위로만 가지 않고 가끔 옆으로 도는 구간이 섞여 있으며,
    /// 높이가 오를수록 하늘 색이 지상의 파스텔톤에서 정상의 짙은 색으로 서서히 바뀐다.
    ///
    /// 중간에 씬을 나누지 않는 하나의 긴 등반이므로 체크포인트도 없다 — 떨어지면 항상 맨 처음으로
    /// 돌아간다(Only Up!의 가혹한 낙사 페널티와 동일한 컨셉). Goal에 도달하면 GAME CLEAR UI를 표시한다.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        /// <summary>
        /// 코스를 이루는 한 구간의 난이도 파라미터. 구간이 뒤로 갈수록(쓰레기장→저택→하늘)
        /// 발판은 작아지고, 간격은 빡빡해지고, 장애물은 잦아지고 세진다.
        /// </summary>
        /// <summary>
        /// 구간마다 발판이 놓이는 "경로 모양" 자체를 다르게 한다 (숫자만 다른 게 아니라 구조 자체가 다름).
        /// Straight: 거의 수직으로 살짝만 흔들리며 안정적으로 직진 상승.
        /// Zigzag: 저택 계단처럼 매 발판마다 좌우로 확실하게 갈아타는 스위치백 구조.
        /// Tight: 사방으로 흔들리는 좁고 불규칙한 경로로 정밀한 점프를 요구.
        /// </summary>
        public enum MovementPattern
        {
            Straight,
            Zigzag,
            Tight,
        }

        /// <summary>
        /// 장애물 종류. 구간마다 쓸 수 있는 종류를 배열로 지정하면 장애물이 나올 때마다
        /// 이 배열을 순서대로 돌려쓴다 (같은 종류만 계속 나오지 않게).
        ///
        /// Static: 발판 가장자리에 고정된 장애물 (반대편으로 돌아가면 됨)
        /// Moving: 발판 위를 가로로 왕복 (타이밍을 맞춰 지나감)
        /// VerticalMoving: 발판 바로 위를 위아래로 오르내림 — 내려와 있을 땐 착지 자체가 막히므로
        ///                 올라간 순간에 맞춰 착지해야 한다 (가장 압박이 큰 종류)
        /// Rotating: 발판 중심 주위를 계속 공전 — 올라선 뒤에도 계속 피해야 한다.
        ///           원 안쪽에 안전지대가 남아야 해서 발판이 넓은 구간에서만 쓴다.
        /// </summary>
        public enum ObstacleKind
        {
            Static,
            Moving,
            VerticalMoving,
            Rotating,
        }

        [System.Serializable]
        public class CourseZone
        {
            public string name = "Zone";
            public int platformCount = 15;
            public Vector3 platformSize = new Vector3(2.2f, 1f, 2.2f);
            public float verticalStep = 3.4f;
            public float horizontalVariance = 4.2f;
            public MovementPattern pattern = MovementPattern.Straight;
            public int obstacleEveryNPlatforms = 3;
            public float movingObstacleSpeed = 0.6f;
            public float staticKnockbackForce = 14f;
            public float staticKnockbackUpward = 6f;
            public float movingKnockbackForce = 16f;
            public float movingKnockbackUpward = 7f;

            [Tooltip("이 구간에서 쓸 장애물 종류들. 장애물이 나올 때마다 순서대로 돌려쓴다")]
            public ObstacleKind[] obstacleKinds = new[] { ObstacleKind.Static, ObstacleKind.Moving };

            [Tooltip("공전 장애물의 초당 회전 각도(도)")]
            public float rotatingObstacleSpeed = 90f;

            [Tooltip("몇 개 발판마다 '밟으면 무너지는 발판'을 넣을지 (0이면 사용 안 함)")]
            public int crumblingEveryNPlatforms = 0;
        }

        [Header("구간(Zone) 설정 - Only Up!처럼 갈수록 좁고 빡빡해짐")]
        public CourseZone[] zones = new CourseZone[]
        {
            new CourseZone
            {
                name = "쓰레기장 (쉬움)",
                platformCount = 15,
                platformSize = new Vector3(2.6f, 1f, 2.6f),
                verticalStep = 3.6f,
                horizontalVariance = 4.2f,
                pattern = MovementPattern.Straight,
                obstacleEveryNPlatforms = 4,
                movingObstacleSpeed = 0.55f,
                staticKnockbackForce = 12f,
                staticKnockbackUpward = 5f,
                movingKnockbackForce = 14f,
                movingKnockbackUpward = 6f,
                obstacleKinds = new[] { ObstacleKind.Static, ObstacleKind.Moving, ObstacleKind.Rotating },
                rotatingObstacleSpeed = 70f,
                crumblingEveryNPlatforms = 0,
            },
            new CourseZone
            {
                name = "저택 (보통)",
                platformCount = 15,
                platformSize = new Vector3(2.2f, 1f, 2.2f),
                verticalStep = 3.7f,
                horizontalVariance = 4.2f,
                pattern = MovementPattern.Zigzag,
                obstacleEveryNPlatforms = 3,
                movingObstacleSpeed = 0.7f,
                staticKnockbackForce = 14f,
                staticKnockbackUpward = 6f,
                movingKnockbackForce = 16f,
                movingKnockbackUpward = 7f,
                // 공전 장애물을 앞에 두어 이 구간에서 확실히 여러 번 등장하게 한다
                // (공전은 원 안쪽 안전지대가 필요해서 발판이 좁아지는 뒤 구간에서는 쓰지 않는다)
                obstacleKinds = new[] { ObstacleKind.Rotating, ObstacleKind.Moving, ObstacleKind.Static },
                rotatingObstacleSpeed = 85f,
                crumblingEveryNPlatforms = 0,
            },
            new CourseZone
            {
                name = "하늘 (어려움)",
                platformCount = 15,
                platformSize = new Vector3(1.6f, 1f, 1.6f),
                verticalStep = 3.6f,
                horizontalVariance = 4.5f,
                pattern = MovementPattern.Tight,
                obstacleEveryNPlatforms = 2,
                movingObstacleSpeed = 0.85f,
                staticKnockbackForce = 17f,
                staticKnockbackUpward = 7f,
                movingKnockbackForce = 20f,
                movingKnockbackUpward = 8f,
                // 위아래로 오르내리는 장애물 등장 — 내려와 있을 땐 착지 자체가 막혀서 타이밍 압박이 크다
                obstacleKinds = new[] { ObstacleKind.Static, ObstacleKind.VerticalMoving, ObstacleKind.Moving },
                crumblingEveryNPlatforms = 5,
            },
            new CourseZone
            {
                name = "우주 (매우 어려움)",
                platformCount = 15,
                platformSize = new Vector3(1.4f, 1f, 1.4f),
                verticalStep = 3.5f,
                horizontalVariance = 4.4f,
                pattern = MovementPattern.Tight,
                obstacleEveryNPlatforms = 2,
                movingObstacleSpeed = 1.05f,
                staticKnockbackForce = 19f,
                staticKnockbackUpward = 7.5f,
                movingKnockbackForce = 22f,
                movingKnockbackUpward = 8.5f,
                obstacleKinds = new[] { ObstacleKind.VerticalMoving, ObstacleKind.Moving, ObstacleKind.Static },
                crumblingEveryNPlatforms = 4,
            },
        };

        /// <summary>
        /// 정적 장애물을 발판 가장자리로 밀어낼 때 시도해볼 후보 방향들 (8방향).
        /// 실제로 어느 방향이 안전한지는 각 발판의 실제 콜라이더에 레이캐스트를 쏴서 런타임에 결정한다.
        /// </summary>
        private static readonly Vector3[] candidatePushDirections = new Vector3[]
        {
            Vector3.right, Vector3.left, Vector3.forward, Vector3.back,
            new Vector3(1f, 0f, 1f).normalized, new Vector3(1f, 0f, -1f).normalized,
            new Vector3(-1f, 0f, 1f).normalized, new Vector3(-1f, 0f, -1f).normalized,
        };

        [Header("옆으로 도는 구간 - 계속 위로만 가지 않고 가끔 옆으로 돌아서 지나감")]
        [Tooltip("전체 발판 번호 기준으로 이 값마다 한 번씩 옆으로 도는 구간을 삽입한다")]
        public int traverseEveryNPlatforms = 9;
        [Tooltip("한 번 삽입될 때 몇 개의 발판이 연달아 옆으로 도는 구간인지")]
        public int traverseRunLength = 2;

        [Header("무너지는 발판 설정")]
        [Tooltip("밟은 뒤 무너지기까지의 시간(초). 이 동안 발판이 흔들려 경고를 준다")]
        public float crumbleDelay = 0.9f;
        [Tooltip("무너진 뒤 다시 생기기까지의 시간(초). 떨어져서 재시도할 때 코스가 끊기지 않도록 반드시 복구된다")]
        public float crumbleRespawnDelay = 3.5f;

        [Header("시작/도착 발판 설정")]
        public Vector3 startPlatformSize = new Vector3(4.5f, 1f, 4.5f);
        public Vector3 goalPlatformSize = new Vector3(5f, 1f, 5f);
        public float fallDistance = 18f;

        [Header("스테이지 전환")]
        [Tooltip("비어있으면 이 코스가 최종 스테이지(클리어 UI 표시). 값이 있으면 그 씬으로 전환한다 (Build Settings에 등록 필요)")]
        public string nextStageSceneName = "";

        private Vector3 startPosition = Vector3.zero;
        private Transform courseParent;

        private void Awake()
        {
            courseParent = CourseKit.FindOrCreateCourseParent();
            Vector3 goalPosition = startPosition;
            if (courseParent.childCount == 0)
            {
                goalPosition = BuildCourse();
            }
            else
            {
                Goal existingGoal = courseParent.GetComponentInChildren<Goal>();
                if (existingGoal != null)
                {
                    goalPosition = existingGoal.transform.position;
                }
            }

            GameObject player = CourseKit.CreatePlayer(
                startPosition + new Vector3(0f, startPlatformSize.y * 0.5f + 1.05f, 0f),
                startPosition.y - fallDistance);

            CourseKit.SetupCameraFollow(player.transform);
            CourseKit.SetupHeightSky(Camera.main, player.transform, startPosition.y, goalPosition.y);
            AudioSource bgmSource = CourseKit.SetupBackgroundMusic();

            GameClearUI clearUI = CourseKit.CreateUI(player.transform, startPosition.y, bgmSource, player.GetComponent<PlayerController>());

            CourseKit.ConfigureGoal(courseParent, goalPosition, goalPlatformSize, player.GetComponent<PlayerController>(), clearUI, nextStageSceneName);
        }

        [ContextMenu("코스 생성/재생성 (에디터)")]
        public void GenerateCourseInEditor()
        {
            GameObject existing = GameObject.Find("Course");
            if (existing != null)
            {
                DestroyImmediate(existing);
            }

            courseParent = new GameObject("Course").transform;
            Vector3 goalPosition = BuildCourse();
            CourseKit.CreatePlatform(courseParent, "Goal", goalPosition, goalPlatformSize, isGoal: true);

            CourseKit.SetupEnvironment(Camera.main);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        }

        /// <summary>
        /// 두 발판이 수평으로 얼마나 겹쳐도 되는지의 안전 비율. 캐릭터는 점프 정점이 발판 간격보다
        /// 훨씬 높아서(점프 궤적 자체의 특성), 이전 발판에서 뛰어오를 때 아직 다음 발판의 가로 범위
        /// 안에 있는 상태로 그 발판 밑면 높이까지 먼저 올라가버리면 머리가 낀다. 실측(캡슐 콜라이더로
        /// 실제 점프 궤적을 시뮬레이션)해보니 0.75~0.85배로는 부족한 경우가 남아있어서, 두 발판이
        /// 아예 수평으로 겹치지 않는 지점(반지름 합 * 1.0)보다 살짝 더 여유(1.05배)를 뒀다.
        /// </summary>
        private const float MinHorizontalOffsetRatio = 1.05f;

        /// <summary>
        /// 패턴 공식(사인/코사인)이 우연히 두 성분을 동시에 0 근처로 만들면(위상이 겹치는 지점)
        /// 발판이 거의 바로 위/아래로 겹치게 배치되어 머리가 끼는 원인이 된다. 계산된 (dx,dz)의
        /// 길이가 두 발판 크기 기준 안전 최소값보다 작으면, 방향은 그대로 두고 길이만 늘려서
        /// 최소 수평 거리를 항상 보장한다 (완전히 0이면 인덱스 기반 대체 방향을 쓴다).
        /// </summary>
        private static Vector2 EnsureMinHorizontalOffset(float dx, float dz, float minMagnitude, int fallbackIndex)
        {
            float mag = Mathf.Sqrt(dx * dx + dz * dz);
            if (mag < 0.01f)
            {
                float fallbackAngle = fallbackIndex * 2.4f; // 골든 앵글 근사값 — 연속 인덱스가 항상 다른 방향을 향하게 함
                dx = Mathf.Cos(fallbackAngle);
                dz = Mathf.Sin(fallbackAngle);
                mag = 1f;
            }
            if (mag < minMagnitude)
            {
                float scale = minMagnitude / mag;
                dx *= scale;
                dz *= scale;
            }
            return new Vector2(dx, dz);
        }

        private Vector3 BuildCourse()
        {
            CourseKit.CreatePlatform(courseParent, "StartPlatform", startPosition, startPlatformSize, isGoal: false);

            Vector3 prev = startPosition;
            float prevPlatformSizeX = startPlatformSize.x; // 직전 발판의 가로 크기 (구간이 바뀌는 첫 발판에서만 실제로 다름)
            int globalIndex = 0;   // 구간이 바뀌어도 이어지는 전체 발판 번호 (Platform_01, 02...)
            int obstacleIndex = 0; // 장애물 배치 간격 판단도 구간을 넘어 계속 이어짐
            int traverseRemaining = 0;
            int traverseDir = 1;

            for (int zoneIdx = 0; zoneIdx < zones.Length; zoneIdx++)
            {
                CourseZone zone = zones[zoneIdx];
                // 장애물 종류는 구간마다 0번부터 다시 돌린다. 전체 통산 번호(obstacleIndex)로 고르면
                // 앞 구간에서 몇 개가 나왔는지에 따라 위상이 밀려서, 구간이 새로 도입한 종류(예: 공전)가
                // 한두 번밖에 안 나오는 일이 생긴다.
                int zoneObstacleIndex = 0;
                for (int i = 1; i <= zone.platformCount; i++)
                {
                    globalIndex++;
                    bool isVeryLast = zoneIdx == zones.Length - 1 && i == zone.platformCount;

                    // 옆으로 도는 구간을 일정 간격마다 새로 시작한다 (마지막 발판은 Goal 자리라 제외)
                    if (traverseRemaining == 0 && traverseEveryNPlatforms > 0
                        && globalIndex % traverseEveryNPlatforms == 0 && !isVeryLast)
                    {
                        traverseRemaining = traverseRunLength;
                        traverseDir = -traverseDir; // 좌우로 번갈아 돌아서 매번 같은 방향으로 쏠리지 않게 함
                    }

                    bool isTraverse = traverseRemaining > 0;

                    // 마지막 칸은 구간 발판이 아니라 Goal 발판(goalPlatformSize)이 놓이는 자리다.
                    // Goal은 보통 구간 발판보다 훨씬 넓어서(예: 1.4 vs 5.0), 여기서 zone.platformSize로
                    // 최소 수평 간격을 계산하면 Goal 밑면이 직전 발판 바로 위를 덮어버려 마지막 점프에서
                    // 머리가 끼고 클리어 자체가 불가능해진다(실제로 궤적 시뮬레이션으로 충돌 확인).
                    // 그래서 마지막 칸만 Goal의 실제 크기로 간격을 계산한다.
                    float curPlatformSizeX = isVeryLast ? goalPlatformSize.x : zone.platformSize.x;
                    float minOffset = MinHorizontalOffsetRatio * (prevPlatformSizeX + curPlatformSizeX) * 0.5f;
                    Vector3 pos;
                    if (isTraverse)
                    {
                        // 수직 상승은 적게, 한쪽 방향으로 크게 이동해서 "옆으로 돌아가는" 느낌을 낸다.
                        // 상승폭이 작을수록 도달에 필요한 점프 거리 여유가 커지므로(1.3배를 곱해도 안전) 일반 구간보다
                        // 오히려 안전하다. 배율을 넉넉히 키운 이유는 발판을 확실히 옆으로 떼어놓아서, 수직 상승폭이
                        // 작은 구간에서도 다음 발판이 바로 위(발판 폭 이내)에 겹쳐 캐릭터 머리가 끼는 일이 없게 하기 위함.
                        float dx = traverseDir * zone.horizontalVariance * 1.3f;
                        float dz = Mathf.Sin(globalIndex * 0.8f) * zone.horizontalVariance * 0.25f;
                        Vector2 offset = EnsureMinHorizontalOffset(dx, dz, minOffset, globalIndex);
                        // 옆으로 도는 구간은 수직 상승도 기존(0.4배)보다 넉넉히(0.6배) 줘서, 자칫 발판들이
                        // 가로로는 충분히 떨어져 있어도 진행 경로 중간에 다른 발판 밑을 스치는 경우의 여유를 늘린다.
                        pos = prev + new Vector3(offset.x, zone.verticalStep * 0.6f, offset.y);
                        traverseRemaining--;
                    }
                    else
                    {
                        float dx, dz;
                        switch (zone.pattern)
                        {
                            case MovementPattern.Zigzag:
                                // 저택 계단처럼 매 발판마다 좌우로 확실하게 갈아타는 스위치백 구조
                                float side = (i % 2 == 0) ? 1f : -1f;
                                dx = side * zone.horizontalVariance;
                                dz = Mathf.Sin(globalIndex * 0.9f) * zone.horizontalVariance * 0.3f;
                                break;
                            case MovementPattern.Tight:
                                // 사방으로 흔들리는 좁고 불규칙한 경로 (하늘 구간, 정밀한 점프 요구)
                                dx = Mathf.Sin(globalIndex * 2.3f) * zone.horizontalVariance;
                                dz = Mathf.Cos(globalIndex * 1.9f) * zone.horizontalVariance;
                                break;
                            case MovementPattern.Straight:
                            default:
                                // 거의 수직으로 살짝만 흔들리며 안정적으로 직진 상승 (쓰레기장 구간)
                                dx = Mathf.Sin(globalIndex * 0.6f) * zone.horizontalVariance * 0.5f;
                                dz = Mathf.Cos(globalIndex * 0.5f) * zone.horizontalVariance * 0.5f;
                                break;
                        }
                        Vector2 offset = EnsureMinHorizontalOffset(dx, dz, minOffset, globalIndex);
                        pos = prev + new Vector3(offset.x, zone.verticalStep, offset.y);
                    }
                    prevPlatformSizeX = curPlatformSizeX;

                    if (!isVeryLast)
                    {
                        string label = isTraverse ? $"Platform_{globalIndex:00}_Traverse" : $"Platform_{globalIndex:00}";
                        GameObject platformGO = CourseKit.CreatePlatform(courseParent, label, pos, zone.platformSize, isGoal: false);

                        // 밟으면 무너지는 발판: 오래 서서 다음 점프를 고민할 수 없게 만들어 압박을 준다.
                        // 옆으로 도는 구간은 이동 자체가 이미 도전 요소라 제외한다.
                        if (!isTraverse && zone.crumblingEveryNPlatforms > 0 && i % zone.crumblingEveryNPlatforms == 0)
                        {
                            CourseKit.MakePlatformCrumbling(platformGO, zone.platformSize, crumbleDelay, crumbleRespawnDelay);
                        }

                        // 옆으로 도는 구간에는 장애물을 두지 않는다 (이미 이동 자체가 도전 요소이므로)
                        if (!isTraverse && zone.obstacleEveryNPlatforms > 0 && i % zone.obstacleEveryNPlatforms == 0)
                        {
                            obstacleIndex++;
                            ObstacleKind kind = (zone.obstacleKinds != null && zone.obstacleKinds.Length > 0)
                                ? zone.obstacleKinds[zoneObstacleIndex % zone.obstacleKinds.Length]
                                : ObstacleKind.Static;
                            zoneObstacleIndex++;

                            // 장애물 크기를 발판 크기에 비례하게 만든다 — 예전엔 1유닛 고정이라 발판이
                            // 작은 구간(하늘)에서는 발판 폭 대부분을 장애물이 차지해 착지할 공간이 아예
                            // 없어지는 경우가 있었다 (80m 이상 구간에서 발판 1.5유닛 vs 장애물 1유닛 →
                            // 8방향 전부 착지 불가능한 것을 실측으로 확인).
                            // 장애물 Visual이 큐브에서 둥근 3D 모델(구)로 바뀌면서 같은 박스 콜라이더라도
                            // 시각적으로 더 작아 보여 체감 난이도가 낮아졌다는 피드백이 있어, 비율을
                            // 0.3 -> 0.35로, 최대 크기를 1 -> 1.15로 살짝 올려 눈에 보이는 장애물을 키웠다.
                            float obstacleWidth = Mathf.Clamp(zone.platformSize.x * 0.35f, 0.35f, 1.15f);
                            Vector3 obstacleSize = new Vector3(obstacleWidth, 1f, obstacleWidth);

                            switch (kind)
                            {
                                case ObstacleKind.Moving:
                                {
                                    float travel = zone.platformSize.x * 0.8f;
                                    Vector3 basePos = pos + new Vector3(0f, zone.platformSize.y * 0.5f + 0.5f, 0f);
                                    CourseKit.CreateMovingObstacle(
                                        courseParent,
                                        basePos + new Vector3(-travel * 0.5f, 0f, 0f),
                                        basePos + new Vector3(travel * 0.5f, 0f, 0f),
                                        obstacleSize,
                                        zone.movingObstacleSpeed,
                                        zone.movingKnockbackForce,
                                        zone.movingKnockbackUpward);
                                    break;
                                }

                                case ObstacleKind.VerticalMoving:
                                {
                                    // 발판 바로 위에서 위아래로 오르내린다. 내려와 있을 땐 착지 자체가 막히고
                                    // 올라간 순간에는 발판 전체가 비므로 "타이밍을 맞추면 반드시 통과 가능"하다.
                                    // 위쪽 끝을 발판 위 2.6으로 잡으면 장애물(높이 1) 아랫면이 발판 위 2.1이 되어
                                    // 캐릭터 키(CharacterController.height = 2.0)보다 높아 확실히 지나갈 수 있다.
                                    Vector3 lowPos = pos + new Vector3(0f, zone.platformSize.y * 0.5f + 0.5f, 0f);
                                    Vector3 highPos = pos + new Vector3(0f, zone.platformSize.y * 0.5f + 2.6f, 0f);
                                    CourseKit.CreateMovingObstacle(
                                        courseParent, lowPos, highPos, obstacleSize,
                                        zone.movingObstacleSpeed,
                                        zone.movingKnockbackForce,
                                        zone.movingKnockbackUpward);
                                    break;
                                }

                                case ObstacleKind.Rotating:
                                {
                                    // 공전 반지름을 발판 반폭보다 작게 잡아 원 안쪽에 안전지대를 남긴다.
                                    // (안전지대 반지름 = radius - 장애물 반폭 이 캐릭터 반지름 0.4보다 커야 한다)
                                    float platformHalf = zone.platformSize.x * 0.5f;
                                    float rotatingWidth = obstacleWidth * 0.8f;
                                    Vector3 rotatingSize = new Vector3(rotatingWidth, 1f, rotatingWidth);
                                    float radius = platformHalf * 0.85f;
                                    Vector3 center = pos + new Vector3(0f, zone.platformSize.y * 0.5f + 0.5f, 0f);
                                    CourseKit.CreateRotatingObstacle(
                                        courseParent, center, radius, rotatingSize,
                                        zone.rotatingObstacleSpeed,
                                        obstacleIndex * 57f, // 장애물마다 시작 각도를 다르게 해서 전부 같은 위상으로 돌지 않게 한다
                                        zone.movingKnockbackForce,
                                        zone.movingKnockbackUpward);
                                    break;
                                }

                                default:
                                {
                                    // 발판 정중앙이 아니라 한쪽 가장자리 쪽으로 치우쳐 배치해서, 반대편에
                                    // 캐릭터 한 명이 설 수 있는 착지 공간을 항상 남겨둔다.
                                    float platformHalf = zone.platformSize.x * 0.5f;
                                    float edgeOffset = Mathf.Max(0f, platformHalf - obstacleWidth * 0.5f - 0.15f);
                                    Vector3 pushDir = FindSafePushDirection(platformGO, pos, platformHalf, obstacleIndex);
                                    CourseKit.CreateStaticObstacle(
                                        courseParent, pos + pushDir * edgeOffset, zone.platformSize, obstacleSize,
                                        zone.staticKnockbackForce, zone.staticKnockbackUpward);
                                    break;
                                }
                            }
                        }
                    }

                    prev = pos;
                }
            }

            return prev;
        }

        /// <summary>
        /// 고정 장애물을 발판의 어느 쪽 가장자리로 밀지 정한다.
        /// 모델마다(별/구름/받침대) 실제 충돌 모양이 달라서 "가장 넓은 방향"이 X/Z축과 항상
        /// 일치하지는 않으므로, 미리 정한 방향으로 무조건 미는 대신 실제로 생성된 콜라이더에
        /// 레이캐스트를 쏴서 반대편에 정말 착지 가능한 방향을 찾아 그쪽으로 민다
        /// (발판 폭 대부분을 장애물이 차지해 8방향 전부 착지 불가능했던 문제의 근본 수정).
        /// </summary>
        private static Vector3 FindSafePushDirection(GameObject platformGO, Vector3 platformPos, float platformHalf, int obstacleIndex)
        {
            float standDistance = Mathf.Max(0f, platformHalf - 0.4f - 0.1f);
            int startIndex = obstacleIndex % candidatePushDirections.Length;
            Vector3 fallbackDir = candidatePushDirections[startIndex];

            // 발판 본체 콜라이더를 찾는다. 무너지는 발판에는 밟힘 감지용 트리거 BoxCollider가 추가로
            // 붙어있는데, 그건 발판 위쪽 공간에 떠 있어서 착지 가능 여부 판정에 쓰면 안 되므로 제외한다.
            Collider platformCollider = platformGO.GetComponentInChildren<MeshCollider>();
            if (platformCollider == null)
            {
                foreach (BoxCollider box in platformGO.GetComponents<BoxCollider>())
                {
                    if (!box.isTrigger) { platformCollider = box; break; }
                }
            }
            if (platformCollider == null) return fallbackDir;

            for (int c = 0; c < candidatePushDirections.Length; c++)
            {
                Vector3 candidate = candidatePushDirections[(startIndex + c) % candidatePushDirections.Length];
                Vector3 standPoint = platformPos + (-candidate) * standDistance;
                Vector3 rayOrigin = standPoint + Vector3.up * 5f;
                RaycastHit hit;
                if (platformCollider.Raycast(new Ray(rayOrigin, Vector3.down), out hit, 20f))
                {
                    return candidate;
                }
            }
            return fallbackDir;
        }
    }
}
