using UnityEngine;

namespace OnlyUp
{
    /// <summary>
    /// 2스테이지: 넓은 산을 자유롭게 오르는 코스 (Chained Together / Getting Over It 스타일).
    /// 1스테이지(GameBootstrap)의 좁은 외길과 달리, 각 높이 구간(band)마다 여러 개의 바위 발판이
    /// 넓게 흩어져 있어서 플레이어가 경로를 골라가며 오를 수 있다.
    /// "메인 경로"는 항상 점프로 도달 가능하도록 보장하고, 그 주변의 "보조 발판"은 대안 경로/탐험용으로
    /// 추가 배치한다(필수 경로는 아님). 생성 로직 자체는 CourseKit을 그대로 재사용해서
    /// 플레이어/카메라/UI/장애물이 1스테이지와 완전히 동일하게 동작한다(카메라 구조 유지).
    /// </summary>
    public class MountainBootstrap : MonoBehaviour
    {
        [Header("메인 경로 설정 (항상 점프로 도달 가능하도록 보장됨 - 1스테이지보다 발판은 작고 간격은 넓음)")]
        public int bandCount = 24;                 // 높이 구간 개수
        public float verticalStep = 3.2f;           // 구간마다 올라가는 높이
        public float mainHorizontalVariance = 4.6f; // 메인 발판이 매 구간 벌어질 수 있는 최대 거리
        public Vector2 mainSizeRange = new Vector2(1.8f, 2.4f); // 메인 발판 크기 무작위 범위 (1스테이지 2.2보다 작게)

        [Header("보조 발판 설정 (대안 경로 - 안전망이 되지 않도록 멀고 작게)")]
        public int sidePlatformsPerBand = 1;        // 2 -> 1: 너무 흔하면 사실상 안전망이 되어버림
        public float sideMinDistance = 4.5f;        // 3 -> 4.5: 거저 밟을 수 없도록 실제 점프가 필요한 거리로
        public float sideMaxDistance = 7.5f;
        public Vector2 sideSizeRange = new Vector2(1.2f, 1.6f); // 1.6~2.4 -> 1.2~1.6: 착지가 쉽지 않도록 축소

        [Header("시작/리스폰 설정")]
        public Vector3 startPlatformSize = new Vector3(6f, 1f, 6f);
        public Vector3 goalPlatformSize = new Vector3(5f, 1f, 5f);
        public float fallDistance = 20f;

        [Header("장애물 설정 (1스테이지보다 더 자주, 더 세게)")]
        public int obstacleEveryNBands = 2;         // 3 -> 2: 더 자주 배치
        public int movingObstacleEveryNth = 2;
        public float staticKnockbackForce = 16f;    // 14 -> 16
        public float staticKnockbackUpward = 7f;    // 6 -> 7
        public float movingKnockbackForce = 19f;    // 16 -> 19
        public float movingKnockbackUpward = 8f;    // 7 -> 8
        public float movingObstacleSpeed = 0.6f;    // 왕복 속도 (스테이지가 높아질수록 올려서 타이밍을 빡빡하게 만들 수 있음)

        [Header("코스 재현성")]
        [Tooltip("이 값으로 난수를 고정해서, 실행할 때마다 항상 같은 모양의 산이 만들어지게 한다")]
        public int randomSeed = 20260820;

        [Header("스테이지 전환")]
        [Tooltip("비어있으면 최종 스테이지(클리어 UI 표시). 값이 있으면 이 씬으로 전환한다 (Build Settings에 등록 필요)")]
        public string nextStageSceneName = "";

        private Vector3 startPosition = Vector3.zero;
        private Transform courseParent;

        private void Awake()
        {
            // 씬에 "Course"가 이미 있으면(에디터에서 미리 생성/편집해둔 산) 그대로 쓰고, 없으면 지금 새로 만든다.
            courseParent = CourseKit.FindOrCreateCourseParent();
            Vector3 goalPosition = startPosition;
            if (courseParent.childCount == 0)
            {
                // 고정 시드를 써서 "무작위처럼 보이지만 매번 동일한" 산 모양을 만든다 (재현 가능, 난이도 보장)
                Random.InitState(randomSeed);
                goalPosition = BuildMountain();
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

            // 1스테이지와 동일한 CourseKit.SetupCameraFollow를 그대로 사용 -> 카메라 구조(마우스 궤도 3인칭) 유지
            CourseKit.SetupCameraFollow(player.transform);

            GameClearUI clearUI = CourseKit.CreateUI(player.transform, startPosition.y);

            // nextStageSceneName이 비어있으면 최종 스테이지로 동작(클리어 UI), 값이 있으면 다음 씬으로 전환한다
            CourseKit.ConfigureGoal(courseParent, goalPosition, goalPlatformSize, player.GetComponent<PlayerController>(), clearUI, nextStageSceneName);
        }

        /// <summary>
        /// 인스펙터의 컴포넌트 컨텍스트 메뉴(⋮)에서 실행: Play 없이 에디터에서 바로 산 코스를 생성해
        /// 씬에 실제 오브젝트로 배치한다. 이후 Hierarchy/Scene 뷰에서 바위를 직접 선택·이동·삭제할 수 있다.
        /// 기존에 생성된 Course가 있으면 지우고 새로 만든다.
        /// </summary>
        [ContextMenu("코스 생성/재생성 (에디터)")]
        public void GenerateCourseInEditor()
        {
            GameObject existing = GameObject.Find("Course");
            if (existing != null)
            {
                DestroyImmediate(existing);
            }

            Random.InitState(randomSeed);
            courseParent = new GameObject("Course").transform;
            Vector3 goalPosition = BuildMountain();
            CourseKit.CreatePlatform(courseParent, "Goal", goalPosition, goalPlatformSize, isGoal: true);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        }

        /// <summary>
        /// 메인 경로(항상 도달 가능) + 보조 발판(대안 경로) + 장애물을 배치하며 산을 만든다.
        /// </summary>
        private Vector3 BuildMountain()
        {
            CourseKit.CreatePlatform(courseParent, "StartPlatform", startPosition, startPlatformSize, isGoal: false);

            Vector3 mainPrev = startPosition;
            int obstacleIndex = 0;

            for (int band = 1; band <= bandCount; band++)
            {
                // 메인 경로: 결정론적인 사인/코사인 패턴으로, 매번 이동 거리를 제한해 항상 점프 가능하게 한다
                float dx = Mathf.Sin(band * 1.7f) * mainHorizontalVariance;
                float dz = Mathf.Cos(band * 1.1f) * mainHorizontalVariance;
                Vector3 mainPos = mainPrev + new Vector3(dx, verticalStep, dz);

                bool isLast = band == bandCount;
                if (!isLast)
                {
                    float mainRotationY = Random.Range(0f, 360f);
                    Vector3 mainSize = new Vector3(
                        Random.Range(mainSizeRange.x, mainSizeRange.y), 1f,
                        Random.Range(mainSizeRange.x, mainSizeRange.y));
                    CourseKit.CreatePlatform(courseParent, $"Rock_Main_{band:00}", mainPos, mainSize, isGoal: false, mainRotationY);

                    // 보조 발판: 메인 발판 주변에 흩어놓아 "넓은 산"의 느낌을 준다 (통과 필수는 아님)
                    for (int side = 0; side < sidePlatformsPerBand; side++)
                    {
                        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                        float distance = Random.Range(sideMinDistance, sideMaxDistance);
                        Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                        float heightJitter = Random.Range(-0.6f, 0.6f);
                        Vector3 sidePos = mainPos + dir * distance + new Vector3(0f, heightJitter, 0f);
                        Vector3 sideSize = new Vector3(
                            Random.Range(sideSizeRange.x, sideSizeRange.y), 1f,
                            Random.Range(sideSizeRange.x, sideSizeRange.y));
                        CourseKit.CreatePlatform(courseParent, $"Rock_Side_{band:00}_{side}", sidePos, sideSize, isGoal: false, Random.Range(0f, 360f));
                    }

                    // 일정 구간마다 메인 발판 위에 장애물을 배치한다 (시작 발판 제외)
                    if (band % obstacleEveryNBands == 0)
                    {
                        obstacleIndex++;
                        if (obstacleIndex % movingObstacleEveryNth == 0)
                        {
                            float travel = mainSize.x * 0.8f;
                            Vector3 basePos = mainPos + new Vector3(0f, mainSize.y * 0.5f + 0.5f, 0f);
                            // 왕복 축 방향도 구간마다 다르게 해서(항상 x축이 아니라) 단조롭지 않게 한다
                            float axisAngle = band * 0.9f;
                            Vector3 axis = new Vector3(Mathf.Cos(axisAngle), 0f, Mathf.Sin(axisAngle));
                            CourseKit.CreateMovingObstacle(
                                courseParent,
                                basePos - axis * travel * 0.5f,
                                basePos + axis * travel * 0.5f,
                                movingObstacleSpeed, movingKnockbackForce, movingKnockbackUpward);
                        }
                        else
                        {
                            CourseKit.CreateStaticObstacle(courseParent, mainPos, mainSize, staticKnockbackForce, staticKnockbackUpward);
                        }
                    }
                }

                mainPrev = mainPos;
            }

            return mainPrev; // 마지막 메인 경로 위치 = Goal 위치
        }
    }
}
