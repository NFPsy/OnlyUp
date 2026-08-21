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
            public int movingObstacleEveryNth = 2;
            public float movingObstacleSpeed = 0.6f;
            public float staticKnockbackForce = 14f;
            public float staticKnockbackUpward = 6f;
            public float movingKnockbackForce = 16f;
            public float movingKnockbackUpward = 7f;
        }

        [Header("구간(Zone) 설정 - Only Up!처럼 갈수록 좁고 빡빡해짐")]
        public CourseZone[] zones = new CourseZone[]
        {
            new CourseZone
            {
                name = "쓰레기장 (쉬움)",
                platformCount = 15,
                platformSize = new Vector3(3.2f, 1f, 3.2f),
                verticalStep = 3.0f,
                horizontalVariance = 3.2f,
                pattern = MovementPattern.Straight,
                obstacleEveryNPlatforms = 5,
                movingObstacleEveryNth = 2,
                movingObstacleSpeed = 0.5f,
                staticKnockbackForce = 12f,
                staticKnockbackUpward = 5f,
                movingKnockbackForce = 14f,
                movingKnockbackUpward = 6f,
            },
            new CourseZone
            {
                name = "저택 (보통)",
                platformCount = 15,
                platformSize = new Vector3(2.2f, 1f, 2.2f),
                verticalStep = 3.4f,
                horizontalVariance = 4.2f,
                pattern = MovementPattern.Zigzag,
                obstacleEveryNPlatforms = 3,
                movingObstacleEveryNth = 2,
                movingObstacleSpeed = 0.6f,
                staticKnockbackForce = 14f,
                staticKnockbackUpward = 6f,
                movingKnockbackForce = 16f,
                movingKnockbackUpward = 7f,
            },
            new CourseZone
            {
                name = "하늘 (어려움)",
                platformCount = 15,
                platformSize = new Vector3(1.5f, 1f, 1.5f),
                verticalStep = 3.2f,
                horizontalVariance = 4.6f,
                pattern = MovementPattern.Tight,
                obstacleEveryNPlatforms = 2,
                movingObstacleEveryNth = 2,
                movingObstacleSpeed = 0.8f,
                staticKnockbackForce = 17f,
                staticKnockbackUpward = 7f,
                movingKnockbackForce = 20f,
                movingKnockbackUpward = 8f,
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

        [Header("시작/도착 발판 설정")]
        public Vector3 startPlatformSize = new Vector3(6f, 1f, 6f);
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
            CourseKit.SetupBackgroundMusic();

            GameClearUI clearUI = CourseKit.CreateUI(player.transform, startPosition.y);

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

        private Vector3 BuildCourse()
        {
            CourseKit.CreatePlatform(courseParent, "StartPlatform", startPosition, startPlatformSize, isGoal: false);

            Vector3 prev = startPosition;
            int globalIndex = 0;   // 구간이 바뀌어도 이어지는 전체 발판 번호 (Platform_01, 02...)
            int obstacleIndex = 0; // 장애물 배치 간격 판단도 구간을 넘어 계속 이어짐
            int traverseRemaining = 0;
            int traverseDir = 1;

            for (int zoneIdx = 0; zoneIdx < zones.Length; zoneIdx++)
            {
                CourseZone zone = zones[zoneIdx];
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
                    Vector3 pos;
                    if (isTraverse)
                    {
                        // 수직 상승은 적게, 한쪽 방향으로 크게 이동해서 "옆으로 돌아가는" 느낌을 낸다.
                        // 상승폭이 작을수록 도달에 필요한 점프 거리 여유가 커지므로(1.3배를 곱해도 안전) 일반 구간보다
                        // 오히려 안전하다. 배율을 넉넉히 키운 이유는 발판을 확실히 옆으로 떼어놓아서, 수직 상승폭이
                        // 작은 구간에서도 다음 발판이 바로 위(발판 폭 이내)에 겹쳐 캐릭터 머리가 끼는 일이 없게 하기 위함.
                        float dx = traverseDir * zone.horizontalVariance * 1.3f;
                        float dz = Mathf.Sin(globalIndex * 0.8f) * zone.horizontalVariance * 0.25f;
                        pos = prev + new Vector3(dx, zone.verticalStep * 0.4f, dz);
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
                        pos = prev + new Vector3(dx, zone.verticalStep, dz);
                    }

                    if (!isVeryLast)
                    {
                        string label = isTraverse ? $"Platform_{globalIndex:00}_Traverse" : $"Platform_{globalIndex:00}";
                        GameObject platformGO = CourseKit.CreatePlatform(courseParent, label, pos, zone.platformSize, isGoal: false);

                        // 옆으로 도는 구간에는 장애물을 두지 않는다 (이미 이동 자체가 도전 요소이므로)
                        if (!isTraverse && zone.obstacleEveryNPlatforms > 0 && i % zone.obstacleEveryNPlatforms == 0)
                        {
                            obstacleIndex++;

                            // 장애물 크기를 발판 크기에 비례하게 만든다 — 예전엔 1유닛 고정이라 발판이
                            // 작은 구간(하늘)에서는 발판 폭 대부분을 장애물이 차지해 착지할 공간이 아예
                            // 없어지는 경우가 있었다 (80m 이상 구간에서 발판 1.5유닛 vs 장애물 1유닛 →
                            // 8방향 전부 착지 불가능한 것을 실측으로 확인).
                            float obstacleWidth = Mathf.Clamp(zone.platformSize.x * 0.3f, 0.35f, 1f);
                            Vector3 obstacleSize = new Vector3(obstacleWidth, 1f, obstacleWidth);

                            if (obstacleIndex % zone.movingObstacleEveryNth == 0)
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
                            }
                            else
                            {
                                // 발판 정중앙이 아니라 한쪽 가장자리 쪽으로 치우쳐 배치해서, 반대편에
                                // 캐릭터 한 명이 설 수 있는 착지 공간을 항상 남겨둔다.
                                // 모델마다(별/구름/받침대) 실제 충돌 모양이 달라서 "가장 넓은 방향"이
                                // X/Z축과 항상 일치하지는 않으므로, 미리 정한 방향으로 무조건 미는 대신
                                // 실제로 생성된 콜라이더에 레이캐스트를 쏴서 반대편에 정말 착지 가능한
                                // 방향을 찾아 그쪽으로 장애물을 민다 (80m 이상 구간에서 발판 폭 대부분을
                                // 장애물이 차지해 8방향 전부 착지 불가능했던 문제의 근본 수정).
                                float platformHalf = zone.platformSize.x * 0.5f;
                                float obstacleHalf = obstacleWidth * 0.5f;
                                float edgeOffset = Mathf.Max(0f, platformHalf - obstacleHalf - 0.15f);
                                float standDistance = Mathf.Max(0f, platformHalf - 0.4f - 0.1f);

                                int startIndex = obstacleIndex % candidatePushDirections.Length;
                                Vector3 pushDir = candidatePushDirections[startIndex];
                                Collider platformCollider = platformGO.GetComponentInChildren<MeshCollider>();
                                if (platformCollider == null) platformCollider = platformGO.GetComponent<BoxCollider>();

                                if (platformCollider != null)
                                {
                                    for (int c = 0; c < candidatePushDirections.Length; c++)
                                    {
                                        Vector3 candidate = candidatePushDirections[(startIndex + c) % candidatePushDirections.Length];
                                        Vector3 standPoint = pos + (-candidate) * standDistance;
                                        Vector3 rayOrigin = standPoint + Vector3.up * 5f;
                                        RaycastHit hit;
                                        if (platformCollider.Raycast(new Ray(rayOrigin, Vector3.down), out hit, 20f))
                                        {
                                            pushDir = candidate;
                                            break;
                                        }
                                    }
                                }

                                Vector3 obstacleOffset = pushDir * edgeOffset;

                                CourseKit.CreateStaticObstacle(
                                    courseParent, pos + obstacleOffset, zone.platformSize, obstacleSize,
                                    zone.staticKnockbackForce, zone.staticKnockbackUpward);
                            }
                        }
                    }

                    prev = pos;
                }
            }

            return prev;
        }
    }
}
