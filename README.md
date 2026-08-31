# OnlyUp

"Only Up!" / *Getting Over It* / *Chained Together* 스타일의 3D 수직 등반 플랫포머입니다.
플레이어는 발판을 밟고 점프하며 계속 위로 올라가고, 떨어지면 마지막으로 밟은 체크포인트(없으면 시작 지점)에서 다시 시작합니다.
씬에는 로직 오브젝트(Camera, Light, `GameBootstrap`)만 두고, 발판 코스·플레이어·UI는 전부 코드로 생성합니다.

## 개발 환경

| 항목 | 값 |
|---|---|
| Unity 버전 | 6000.3.21f1 |
| 렌더 파이프라인 | Universal Render Pipeline (URP) |
| 플랫폼 | PC(에디터) 기준 개발, **WebGL 빌드로 itch.io에 배포 완료** |
| Active Input Handling | **Input System Package (New)** — 레거시 `UnityEngine.Input`은 예외를 던지므로 사용 금지 |
| 추가 패키지 | `com.unity.cloud.gltfast` (VARCO 3D에서 만든 GLB 모델을 텍스처 손실 없이 가져오기 위해 추가) |
| UI | 기본 UGUI (`Text`, `Canvas`) — TextMeshPro Essentials 미설치 상태라 TMP 대신 사용 |
| UI 폰트 | `Assets/Resources/Fonts/NanumGothic.ttf` (나눔고딕, SIL OFL 라이선스) — 기본 내장 폰트(`LegacyRuntime.ttf`)는 한글 글리프가 없어서 에디터/PC 빌드에서는 OS 폰트로 대체되어 안 보이지만 WebGL 빌드는 OS 폰트 대체가 없어 한글 UI(일시정지 메뉴 등)가 빈칸으로 보였다. `CourseKit.CreateUI()`가 이 폰트를 모든 UI 텍스트에 공통으로 사용해서 해결 |

## 조작법

- **WASD**: 이동 (카메라가 보는 방향 기준)
- **Space**: 점프
- **마우스 좌클릭 후 드래그**: 카메라 각도 회전 (포인터 락, `Esc`로 해제)
- **P**: 일시정지 메뉴 열기/닫기 (다시 누르면 재개)
- 방향키/WASD는 카메라 각도에 전혀 영향을 주지 않음 — 카메라는 마우스로만 회전하는 궤도(orbit) 카메라

## 일시정지 메뉴 (P)

`P`를 누르면 `Time.timeScale = 0`으로 게임이 멈추고 "소리" / "게임 끝내기" 버튼이 있는 메뉴가 뜬다.
다시 `P`를 누르면 메뉴가 닫히고 게임이 재개된다. (원래 `Esc`였는데, 브라우저의 Pointer Lock API는
`Esc`를 누르면 게임 스크립트와 무관하게 항상 강제로 포인터 락을 해제하는 내장 동작이 있어서, WebGL에서
일시정지 키로 쓰기에 안정적이지 않아 `P`로 바꿨다.)

- **소리**: 누르면 "배경음악 켜짐/꺼짐", "점프 사운드 켜짐/꺼짐" 서브 메뉴가 열린다. 각 버튼은 해당
  `AudioSource.mute`를 토글하고 라벨 텍스트를 즉시 갱신한다.
- **게임 끝내기**: 에디터에서는 Play 모드를 종료하고, 빌드에서는 `Application.Quit()`으로 게임을 종료한다.

UGUI Button을 처음 쓰기 시작하면서 클릭을 받을 `EventSystem`이 필요해졌는데, 프로젝트가 새
Input System 패키지를 쓰므로(레거시 Input은 예외를 던짐) 기본 `StandaloneInputModule` 대신
`InputSystemUIInputModule`을 붙였다 (`CourseKit.CreateUI`가 "EventSystem"이 없을 때만 생성).

**주의**: `Update()`는 `Time.timeScale = 0`이어도 계속 호출된다. 처음 구현했을 땐 일시정지 중에도
`CameraFollow`가 좌클릭을 감지해 커서를 다시 잠그고 숨겨버려서, 메뉴 버튼을 눌러도 같은 프레임에
클릭 좌표가 화면 중앙으로 튀어 반응이 없는 것처럼 보이는 문제가 있었다. `PauseMenuUI.IsPaused`
정적 플래그를 두고 `CameraFollow.Update()` 맨 앞에서 확인해, 메뉴가 열려있는 동안은 커서 잠금/카메라
회전을 아예 건드리지 않도록 고쳤다.

## 씬 구성

| 씬 | 설명 |
|---|---|
| `Assets/Scenes/SampleScene.unity` | Unity 기본 템플릿 씬 (건드리지 않음) |
| `Assets/Scenes/OnlyUp.unity` | **유일한 스테이지.** 시작부터 정상까지 끊기지 않는 단일 등반 코스 (`GameBootstrap`, 발판 60개) |

원래 씬을 둘로 나눠 중간에 전환하는 구조였지만, 실제 *Only Up!*처럼 "떨어지면 처음부터"라는 긴장감을 살리기 위해
씬 전환 없는 **하나의 긴 코스**로 통합했습니다.
(`Goal.nextSceneName`이 비어있으면 최종 스테이지로 동작 — 확장하려면 이 필드에 다음 씬 이름을 넣으면 됨)

### 체크포인트(세이브 포인트)

코스가 60개 발판(약 200m)까지 길어지면서, 체크포인트 없이 낙사할 때마다 항상 맨 처음으로 돌아가는 페널티가
지나치게 가혹해졌다. 그래서 `GameBootstrap.checkpointHeights`(기본 `{50, 100, 150}`)에 지정한 높이를 처음
넘는 발판을 초록색 `Checkpoint` 발판으로 바꿨다 — Goal(노란 큐브)과 똑같은 방식으로 색과 트리거만 다르게
만든 "단순한 특수 발판"이다. 밟으면([`Checkpoint.cs`](Assets/Scripts/OnlyUp/Checkpoint.cs)) 그 순간부터
낙사해도 시작 지점이 아니라 그 발판 위로 리스폰된다.

`Checkpoint`는 어떤 플레이어 인스턴스도 미리 연결해둘 필요가 없다는 점이 `Goal`과 다르다 — 발판은 플레이어보다
먼저 생성되므로(`BuildCourse()`가 `CreatePlayer()`보다 먼저 실행됨) 생성 시점엔 참조할 플레이어가 아직 없는데,
트리거에 실제로 들어온 Collider에서 `GetComponentInParent<RespawnController>()`로 바로 찾아 쓰면 미리
와이어링하지 않아도 항상 정확히 동작한다. 체크포인트 발판에는 장애물이나 무너짐을 섞지 않아 항상 안전하게
숨 돌릴 수 있는 지점으로 유지한다.

### 맵 구조 — 실제 Only Up!과 비슷하게 반영한 요소들

- **구간(Zone)별 난이도 변화**: `GameBootstrap.zones` 배열로 코스를 **4구간(쓰레기장→저택→하늘→우주, 각 15개 발판 = 총 60개)**
  으로 나눴다. 뒤 구간으로 갈수록 발판(`platformSize`)이 작아지고(2.6→2.2→1.6→1.4), 장애물 등장 간격
  (`obstacleEveryNPlatforms`)이 짧아지며, 넉백 세기가 세진다. 구간마다 값이 달라도 `moveSpeed=7 / jumpHeight=5 /
  gravity=-25` 기준의 점프 궤적으로 모든 발판이 도달 가능한지를 실제 탄도 공식(높이 H·수평거리 D에 대해 도달
  가능한 최대 수평거리)으로 검증한다 — 마지막 검증에서 60개 발판 전체가 도달 불가 0건(최소 여유 6.1%)으로 통과했다.

- **구간별 경로 구조(`CourseZone.pattern`)**: 숫자(크기·간격)만 다른 게 아니라 발판이 놓이는 모양 자체가 구간마다 다르다.
  쓰레기장은 `Straight`(진폭을 줄여 거의 수직으로 살짝만 흔들리는 안정적인 직진 상승), 저택은 `Zigzag`(매 발판마다
  좌우로 확실하게 갈아타는 계단식 스위치백), 하늘·우주는 `Tight`(진폭이 크고 방향이 자주 바뀌는 불규칙한 경로로
  정밀한 점프를 요구)를 사용한다.

- **구간별 장애물 종류(`CourseZone.obstacleKinds`)**: 구간마다 쓸 장애물 종류를 배열로 지정하고, 장애물이 나올 때마다
  그 배열을 순서대로 돌려쓴다(같은 종류만 반복되지 않게). 종류는 4가지다.

  | 종류 | 동작 | 공정성 보장 |
  |---|---|---|
  | `Static` | 발판 가장자리에 고정 | 레이캐스트로 반대편 착지 공간이 실제로 있는 방향을 찾아 배치 |
  | `Moving` | 발판 위를 가로로 왕복 | 왕복하므로 타이밍을 맞추면 통과 |
  | `VerticalMoving` | 발판 바로 위를 위아래로 오르내림 | 최고점에서 장애물 아랫면이 발판 위 2.1(캐릭터 키 2.0)이라 반드시 지나갈 틈이 생김 |
  | `Rotating` | 발판 중심 주위를 계속 공전 | 공전 반지름을 발판 반폭의 0.85배로 잡아 원 **안쪽**에 안전지대를 남김(실측 안전반경 0.63~0.74 > 캐릭터 반지름 0.4) |

  `Rotating`은 원 안쪽 안전지대가 필요해서 발판이 넓은 쓰레기장·저택 구간에서만 쓰고, 발판이 좁아지는
  하늘·우주 구간에서는 대신 `VerticalMoving`이 등장한다.

- **밟으면 무너지는 발판(`CrumblingPlatform`)**: 하늘 구간부터 `crumblingEveryNPlatforms`마다 한 개씩,
  밟으면 잠깐 흔들리다가(`crumbleDelay` 0.9초) 사라지는 발판이 섞인다. 오래 서서 다음 점프를 고민할 수 없게 만들어
  압박을 준다. **반드시 다시 생긴다**(`crumbleRespawnDelay` 3.5초) — 체크포인트에서 리스폰돼도 그 사이에
  있는 발판들은 그대로 밟고 지나가야 하므로, 한 번 무너진 발판이 영영 사라지면 재시도할 때 코스가 끊겨
  클리어가 불가능해지기 때문이다.
  무너지는 동안에도 밟힘 감지용 트리거 콜라이더는 켜둔 채로 두어(본체 콜라이더/렌더러만 끔) 재시도 시 다시 작동한다.
- **옆으로 도는 구간(Traverse)**: 전체 발판 번호 기준 `traverseEveryNPlatforms`(기본 9)마다
  `traverseRunLength`(기본 2)개 발판이 한쪽 방향으로 크게 이동하며 수직 상승은 거의 없는 구간이 삽입된다
  (이름에 `_Traverse`가 붙음). 계속 위로만 올라가는 단조로움을 깨고, 상승폭이 작아서 오히려 일반 구간보다
  점프 성공 여유가 크다.
- **높이에 따라 변하는 하늘**: `HeightSkyController`가 카메라에 붙어 매 프레임 플레이어의 현재 높이를
  시작(0)~Goal 높이 사이에서 보간해 `Camera.backgroundColor`와 `RenderSettings.fogColor`를 지상의
  파스텔 하늘색에서 정상 근처의 짙은 남색으로 서서히 바꾼다. 에디터의 "코스 생성/재생성" 미리보기는
  플레이어가 없어 정적인 지상 색만 보여주고, 실제 Play 중에만 높이에 따라 동적으로 바뀐다.

### 발판 사이 수평 간격 최소 보장 (머리 부딪힘 방지)

플레이 테스트 피드백으로 "발판 사이가 좁다", "점프할 때 머리가 위 발판에 부딪힌다"는 문제가 있었다.
캐릭터 캡슐(높이 2, 반지름 0.4)로 실제 점프 궤적을 시뮬레이션(`CharacterController.Move`를 프레임 단위로
직접 호출)해서 재현해보니, 원인은 점프 속도나 발판 두께가 아니라 **연속된 두 발판의 가로 범위(footprint)가
너무 많이 겹치는 것**이었다 — 점프 정점이 발판 수직 간격보다 훨씬 높아서, 다음 발판을 향해 뛰면 아직 그
발판의 가로 범위 안에 있는 상태로 그 발판 밑면 높이까지 먼저 올라가버려 머리가 낀다. 발판 간격을 넓히거나
(`verticalStep`), 점프 정점을 낮추는(`jumpHeight`) 시도는 거의 효과가 없었다(정점을 낮추면 오히려 점프
사거리가 줄어 다른 구간이 아슬아슬하게 닿지 않게 됨) — 실측으로 확인하고 나서야 원인이 수평 겹침이라는
것을 알았다.

`GameBootstrap.EnsureMinHorizontalOffset()`이 각 발판 배치 시 계산된 (dx, dz) 오프셋의 길이가
`MinHorizontalOffsetRatio(1.05) * (이전 발판 크기 + 이번 발판 크기) / 2`보다 작으면, 방향은 유지한 채
길이만 이 최솟값까지 늘린다(두 발판이 가로로 거의 겹치지 않을 만큼). `쓰레기장(Straight)` 구간은 원래
"거의 수직으로 살짝만 흔들리는" 진폭이 작은 패턴이라 이 최솟값에 자주 걸려서, 결과적으로 이전보다 좌우로
더 확실하게 갈아타는 모양이 됐다 — 안전성이 우선이라 받아들인 트레이드오프다. 시작 발판 크기도
6→4.5로 줄여 극단적으로 큰 첫 발판이 다음 발판과 과도하게 겹치는 것을 막았다.

수정 후 45개 발판 전체를 캡슐 시뮬레이션으로 재검증해 머리 부딪힘이 14~16건에서 3건으로 줄었고(모두
옆으로 도는 구간이나 하늘 구간처럼 원래도 정밀한 타이밍을 요구하는 구간), 도달 가능성 검증은 100%
통과(실패 0건)를 확인했다.

## 핵심 아키텍처

### 로직 / 비주얼 분리

모든 게임 오브젝트는 두 부분으로 나뉩니다.

- **부모(로직)**: Collider, Rigidbody, 기능 스크립트 — 항상 유지됨
- **자식 `Visual`**: 실제 보이는 메시. 발판/장애물은 VARCO 3D로 만든 실제 모델을 쓰고(아래 "VARCO 3D 모델
  파이프라인" 참고), 플레이어는 `Resources/PlayerModel` 캐릭터 모델을 쓴다. 해당 리소스가 없으면 각각
  Cube/Capsule 프리미티브로 자동 대체된다.

`VisualSwapTarget.SwapVisual()` 하나로 Visual만 교체하면 외형이 바뀌고, Collider/스크립트는 전혀 손댈 필요가 없습니다.
발판/장애물의 Visual 크기·위치가 바뀌면 `PlatformColliderSync`가 부모의 BoxCollider를 자동으로 맞춰줍니다
(에디터에서 씬 뷰의 메시를 직접 클릭해 늘리거나 옮겨도 충돌 판정이 항상 따라옵니다).

발판(`PlatformColliderSync.capThickness = true`)은 콜라이더 두께를 `maxThickness`(기본 0.3)로 강제로 얇게 고정하고
항상 Visual의 맨 윗면에 붙인다. 모델에 달린 장식(작은 토퍼 등)이 바운즈 높이를 부풀려도 착지면 위치는 그대로 유지하면서
다음 발판까지의 점프 공간(headroom)은 항상 확보한다 — 두께를 그대로(1유닛) 뒀을 때 구간에 따라 다음 발판까지의
빈 공간이 캐릭터 캡슐 높이(2유닛)와 같거나 그보다 작아져서 점프 중 위쪽 발판에 머리가 끼는 문제가 있었다.
장애물(`Obstacle_Static`/`Obstacle_Moving`/`Obstacle_Rotating`)은 이 옵션을 끈 채로 써서 원래 크기(1유닛 큐브) 그대로 충돌한다.

**마지막 Goal 발판 간격 예외**: 코스 생성 루프는 발판 사이 최소 수평 간격을 `MinHorizontalOffsetRatio`로 보장하는데,
마지막 칸은 구간 발판이 아니라 `goalPlatformSize`(기본 5.0)로 만들어지는 Goal 발판 자리다. 여기서 구간 발판 크기
(우주 구간 1.4)로 간격을 계산하면 훨씬 넓은 Goal의 밑면이 직전 발판 바로 위를 덮어버려서, 마지막 점프 때 머리가
Goal 밑면에 끼여 **클리어 자체가 불가능**해진다(궤적 시뮬레이션으로 t=0.06초에 충돌하는 것을 실측 확인).
그래서 마지막 칸만 Goal의 실제 크기로 간격을 계산한다 — 이 수정으로 직전 발판→Goal 수평거리가 2.90(Goal 반폭
2.50보다 작아 밑면에 갇힘) → 3.36으로 벌어졌다.

실제 3D 모델(별/쿠션 등 각지고 오목한 형태)로 교체된 발판은 위 BoxCollider 방식 대신
`CourseKit.CreateModelCollisionMesh()`가 만드는 **컨벡스 프리즘 콜라이더**를 쓴다. 사각형 BoxCollider는
발판 폭에 맞춘 정사각형이라 별 모양 모서리 사이(오목한 부분)에서 눈에는 안 보이는데 서 있어지거나,
반대로 뾰족한 끝부분을 밟았는데 그대로 통과하는 등 "보이는 모양과 실제 충돌 범위가 다른" 문제가 있었다.
이를 고치기 위해 모델을 위에서 내려다본 윤곽선(2D 볼록 껍질, Andrew's monotone chain으로 계산)을 뽑아서
그 윤곽선 모양 그대로 얇게 압출한 프리즘을 만든다 — 메시 전체(수천 개 정점)를 그대로 컨벡스 콜라이더에
넘기면 PhysX 정점 제한(256개)에 걸려 자동 단순화 경고가 뜨므로, 직접 정확한 윤곽선(수십 개 정점)으로
만들어 경고 없이 더 정확한 모양을 낸다. 두께 캡/윗면 고정 로직은 BoxCollider 버전과 동일하다.

### 발판 크기에 비례하는 장애물 크기 + 가장자리 배치

장애물(특히 고정 장애물)이 예전엔 발판 크기와 무관하게 항상 1유닛 고정이었다. 하늘 구간처럼 발판이
작을 때(1.5유닛) 이 크기가 발판 폭 대부분을 차지해서, 실제로 레이캐스트로 검증해보니 발판 위 8방향
전부 캐릭터가 착지할 수 없는(장애물과 발판 가장자리 사이 공간이 캐릭터 지름보다 좁은) 지점이 있었다.
`GameBootstrap.BuildCourse()`에서 두 가지로 고쳤다.

- **비례 크기**: 장애물 폭 = `platformSize.x * 0.35`(0.35~1.15 사이로 clamp). 발판이 작을수록 장애물도 작아진다.
  (원래 0.3배·최대 1이었는데, 장애물 Visual을 큐브에서 둥근 3D 모델로 바꾼 뒤 같은 박스 콜라이더라도
  시각적으로 더 작아 보여 체감 난이도가 낮아졌다는 피드백을 받아 0.35배·최대 1.15로 살짝 키웠다.)
- **가장자리 배치 + 실제 콜라이더 검증**: 고정 장애물을 발판 정중앙이 아니라 한쪽 가장자리로 밀어서
  반대편에 착지 공간을 남긴다. 어느 방향이 안전한지는 미리 정하지 않고, 그 발판에 실제로 생성된
  콜라이더(모델마다 모양이 다른 컨벡스 프리즘 또는 BoxCollider)에 8방향으로 레이캐스트를 쏴서
  반대편 착지 지점이 실제로 존재하는 방향을 찾아 그쪽으로 장애물을 민다. 모델 실루엣이 축과 안 맞는
  경우(예: 별 모양이 대각선으로 뾰족함)에도 항상 안전한 방향을 찾을 수 있다.

수정 후 전체 코스의 고정 장애물이 있는 모든 발판을 레이캐스트로 재검증해 착지 가능 지점 0개인
발판이 없는 것을 확인했다.

### 코스 생성 로직 공유 — `CourseKit`

플레이어/카메라/UI/발판/장애물을 만드는 코드는 [`CourseKit.cs`](Assets/Scripts/OnlyUp/CourseKit.cs)에 static 메서드로 모아뒀습니다.
`GameBootstrap`은 "코스를 어떤 모양으로 배치할지"만 담당하고, 실제 생성은 전부 `CourseKit`을 호출합니다.
나중에 스테이지를 다시 나누고 싶어지면 새 Bootstrap 스크립트를 추가해 이 로직을 그대로 재사용하면 됩니다.

### 에디터에서 코스 편집하기

`GameBootstrap` 컴포넌트의 인스펙터 우측 상단 **⋮ 메뉴 → "코스 생성/재생성 (에디터)"** 를 누르면
Play 없이 코스가 실제 씬 오브젝트로 생성되어 Hierarchy/Scene 뷰에서 직접 선택·이동·삭제할 수 있습니다.

- 씬에 `Course`가 이미 있으면 Play해도 **다시 생성되지 않고** 그대로 재사용됩니다 (수동으로 편집한 내용이 유지됨).
- 다시 생성하고 싶으면 같은 메뉴를 또 누르면 기존 것을 지우고 새로 만듭니다.
- 발판을 편집할 땐 Hierarchy에서 **부모 오브젝트**(예: `Platform_05`)를 선택하는 것이 안전합니다. 자식 `Visual`을
  직접 늘리거나 옮겨도 `PlatformColliderSync`가 자동으로 충돌 범위를 맞춰주지만, 헷갈리지 않으려면 부모를 다루는 편이 낫습니다.

## 스크립트 목록 (`Assets/Scripts/OnlyUp/`)

| 스크립트 | 역할 |
|---|---|
| `GameBootstrap.cs` | 단일 등반 코스(구간별 난이도 + 옆으로 도는 구간, 4구간·발판 60개) 배치 |
| `CourseKit.cs` | 플레이어/카메라/UI/발판/장애물 생성 공용 로직 |
| `PlayerController.cs` | WASD 이동, 점프, 장애물 넉백, 애니메이터 파라미터 갱신 |
| `CameraFollow.cs` | 마우스 궤도 3인칭 카메라 (WASD와 완전히 분리) |
| `HeightSkyController.cs` | 플레이어 높이에 따라 하늘/안개 색을 지상→정상 색으로 보간 |
| `RespawnController.cs` | 낙사 감지, 리스폰(체크포인트 갱신 시 그 지점으로), 낙사 횟수(`fallCount`) 집계 |
| `Checkpoint.cs` | 밟으면 리스폰 지점을 그 발판으로 갱신하는 세이브 포인트(초록 발판) |
| `Goal.cs` | 골 도달 감지 → 클리어 UI 표시 또는 다음 씬 전환 |
| `GameClearUI.cs` | GAME CLEAR 패널 표시/숨김 |
| `HeightUI.cs` | 현재 높이 실시간 표시 (좌측 상단) |
| `PlayTimeUI.cs` | 플레이 시작부터 흐른 시간 실시간 표시 (좌측 상단, 높이 바로 아래) |
| `FallCountUI.cs` | 낙사 후 리스폰된 횟수 실시간 표시 (우측 상단) |
| `BackgroundMusicPlayer.cs` | 배경 음악 재생 (생성 즉시 Play() 호출 시 씹히는 문제 방지용) |
| `PauseMenuUI.cs` | P 일시정지 메뉴 (소리 켜기/끄기, 게임 끝내기) |
| `Obstacle.cs` | 장애물 마커 (넉백 힘 값 보유) |
| `MovingObstacle.cs` | 두 지점을 왕복하는 장애물 이동 (가로 왕복 / 위아래 왕복 둘 다 이걸로 처리) |
| `RotatingObstacle.cs` | 발판 중심 주위를 계속 공전하는 장애물 |
| `CrumblingPlatform.cs` | 밟으면 잠시 뒤 무너졌다가 다시 생기는 발판 |
| `VisualSwapTarget.cs` | Visual 자식 교체(모델 스왑)를 위한 공용 컴포넌트 |
| `PlatformColliderSync.cs` | 에디터에서 Visual 변경 시 부모 Collider 자동 동기화 |

## 캐릭터 & 애니메이션

- 모델: `Assets/Blue Cartoon Figure/Blue Cartoon Figure(리깅).fbx` — Mixamo 스타일 표준 뼈대를 가진 Humanoid 리그로 임포트
  (임베디드 diffuse/normal 텍스처를 `ModelImporter.ExtractTextures`로 강제 추출해 사용)
- 런타임에는 `Resources/PlayerModel.prefab`을 로드해서 플레이어의 Visual로 사용하고, 없으면 캡슐+눈 더미로 자동 대체
- `Assets/Animators/PlayerAnimator.controller`: Idle / 4방향 이동 블렌드 트리(Locomotion) / 점프 4종(제자리·이동중 3종 순환)
  / 제자리 좌우 90도 방향전환 / 장애물 피격 리액션까지 총 9개 상태로 애니메이션 11종을 전부 사용
- **애니메이션 클립은 `Assets/Animations/*.anim` 네이티브 파일**입니다. 원래 각 동작마다 캐릭터 메시+텍스처까지 통째로
  중복 포함된 FBX(11개, 총 약 240MB)로 제공됐는데, 실제로 필요한 애니메이션 데이터만 뽑아 가벼운 `.anim`(총 약 13MB)으로
  변환하고 원본 FBX는 삭제했습니다. 리깅용 FBX(`Blue Cartoon Figure(리깅).fbx`, 메시+아바타 포함)만 남아있습니다.

## VARCO 3D 모델 파이프라인

- 발판: `Assets/Resources/Platforms/PlatformRock_*.prefab` (Pastel Cloud Podium / Pastel Display Podium /
  Pastel Gradient Cloud / Pastel Heart Star / Pastel Puffy Stars) — VARCO 3D에서 만든 모델을 **glTFast로 직접
  임포트**해서 사용. `CourseKit`이 일반 발판을 생성할 때 이 중 하나를 무작위로 골라 쓰고, 폴더가 비어있으면 기존
  연보라 큐브로 자동 대체된다. (이전에 쓰던 Celestial Plush Cloud / Pastel Cloud Cushion / Star Moon Cloud 3종은
  마음에 들지 않아 이 5종으로 교체했다.)
- 장애물: `Assets/Resources/Obstacles/Obstacle_*.prefab` (Frosted Blue Sphere / Peach Sphere / Textured Iridescent Orb) —
  발판과 동일한 파이프라인으로 임포트한 구슬 모양 모델 3종. `CourseKit.ApplyObstacleVisual()`이 정지(`Static`)/
  왕복(`Moving`, 가로·세로 공용)/공전(`Rotating`) 장애물을 만들 때마다 이 중 하나를 무작위로 골라 Visual만
  교체하고, 폴더가 비어있으면 기존 빨강(정지)/주황(왕복)/분홍(공전) 큐브로 자동 대체된다. 발판(`CreatePlatform`)과
  달리 콜라이더는 절대 모델 모양을 따라가지 않고 항상 코드로 계산한 `obstacleSize` 박스 그대로 유지한다 —
  장애물 난이도(발판 크기에 비례한 폭, 가장자리 배치, 착지 공간 레이캐스트 검증, 공전 반지름)가 전부 이 박스
  크기를 기준으로 계산되므로, 모델이 둥근 구 형태라도 실제 충돌 범위가 눈에 보이는 모양보다 넓어 보일 수 있다
  (의도된 동작).
- 발판/장애물 모델 모두 원본이 GLB가 아니라 **FBX**로 제공됐고, 5십만 트라이앵글짜리 고폴리였다. Blender를 헤드리스로 돌려
  FBX를 임포트한 뒤 디시메이트하고 **GLB로 export**해서 기존 glTFast 임포트 파이프라인을 그대로 재사용했다(6000트라이앵글까지 축소).
  원본 FBX 자체가 베이스컬러+노멀 텍스처만 갖고 있고 ORM(금속성/거칠기) 채널이 없었기 때문에, FBX→GLB 변환 과정에서
  잃어버릴 채널이 애초에 없어 이전에 겪었던 "FBX 경유 시 텍스처 유실" 문제가 재발하지 않았다.
- `VisualSwapTarget.SwapVisual()`은 새 모델의 스케일 값을 그대로 복사하지 않고, 기존 Visual과 새 Visual의 **실제 렌더링된
  바운즈 크기**를 비교해 배율을 계산한다 (임포트된 모델마다 원본 메시 단위가 제각각이라 스케일 숫자 자체는 믿을 수 없음).
  `PlatformColliderSync`도 동일하게 바운즈 기준으로 콜라이더를 맞춘다. 이 덕분에 이번처럼 완전히 다른 형태(구름, 별,
  받침대 모양)의 모델로 통째로 교체해도 코드 변경 없이 발판 크기에 맞춰 자동으로 스케일된다.
- **텍스처 해상도**: glTFast로 임포트된 GLB의 텍스처는 일반 `TextureImporter`가 아니라 `GLTFast.Editor.GltfImporter`의
  서브에셋으로 생성되기 때문에, Unity 인스펙터의 Max Size/Compression 설정이 아예 적용되지 않는다(직접 확인 결과
  `textureImporter=null`). 그래서 원본이 베이스컬러 4096×4096·노멀 2048×2048 무압축(ARGB32)으로 그대로 들어와
  WebGL 빌드가 180MB까지 부풀었었다. 압축 설정을 못 바꾸는 대신, Blender로 GLB를 열어 이미지 자체를
  1024×1024/512×512로 축소한 뒤 다시 GLB로 export해서 소스 단계에서 줄였다 — WebGL 빌드가 180MB → 35MB로 줄었다.
  (이후 장애물 종류 추가·발판 증가 등으로 기능이 늘면서 최신 빌드 용량은 약 47MB다 — 텍스처 최적화 자체의
  효과는 여전히 유효하고, 늘어난 만큼은 새로 추가된 기능 때문이다.)

## 확장 예정 (설계상 이미 고려됨)

- **스테이지 재도입**: `Goal.nextSceneName`에 다음 씬 이름만 지정하면 언제든 다시 씬을 이어붙일 수 있음 (지금은 비워둬서 단일 스테이지)
- **새 장애물/발판 종류**: `CourseKit`에 생성 함수를 추가하고 Bootstrap의 배치 로직에서 호출하면 됨 (발판/장애물 모두
  `VisualSwapTarget`을 통해 Visual만 교체하는 구조라, 새 VARCO 3D 모델을 추가하는 것도 같은 방식으로 확장하면 됨).
  `RotatingObstacle`(공전)·`CrumblingPlatform`(무너지는 발판)이 실제로 이 구조를 따라 추가된 예시다.

## 사운드

`Assets/Resources/Audio/`에 있는 클립을 런타임에 `Resources.Load`로 불러와 코드로 재생한다
(다른 발판/캐릭터 리소스와 동일한 패턴).

- `BGM_StarHopParade.mp3`: 배경 음악. `CourseKit.SetupBackgroundMusic()`이 씬에 "BGM" 오브젝트를
  만들어 루프 재생한다(볼륨 0.4). `BackgroundMusicPlayer.Start()`에서 재생하는 이유는, 오브젝트를
  만든 바로 그 프레임(`GameBootstrap.Awake`)에서 곧바로 `AudioSource.Play()`를 호출하면 아직
  컴포넌트 초기화가 끝나지 않아 조용히 무시되는 경우가 있어서, 씬의 모든 Awake가 끝난 뒤(Start
  단계)로 재생을 미루기 위함이다.
- `SFX_JumpChirp.mp3`: 점프 효과음("boing" 튕기는 소리, 원본 `sfx-boing9.mp3`). `PlayerController`가
  Space로 점프할 때마다 재생한다.

## itch.io(WebGL) 배포 시 주의사항

실제 itch.io에 올려서 플레이 테스트까지 완료했다. 업로드 zip은 반드시 `index.html`/`Build`/`TemplateData`가
zip 최상위에 바로 오도록, 그리고 하위 폴더에 대한 디렉터리 항목이 실제로 포함되도록 압축해야 한다 — Windows
PowerShell `Compress-Archive`로 만든 zip은 디렉터리 항목 없이 파일 경로만 슬래시로 표기해서, itch.io 서버가
`Build`/`TemplateData` 폴더를 아예 못 만들고 그 안의 파일이 전부 404가 나는 문제가 있었다.

**해결**: `Compress-Archive` 대신 Windows에 내장된 `tar.exe`(bsdtar)를 zip 포맷으로 써서 압축한다 —
`tar.exe -a -c -f OnlyUp_WebGL.zip index.html Build TemplateData`. 이렇게 만든 zip은 `Build/`,
`TemplateData/` 디렉터리 항목이 명시적으로 포함되어(`ZipFile.OpenRead`로 항목 목록을 확인해 검증)
itch.io 업로드 시 폴더가 정상적으로 만들어진다.

## 알려진 제한사항

- Idle 애니메이션 전용 클립이 없어 모델의 기본 포즈(한쪽 다리를 살짝 든 액션 포즈)를 그대로 사용 중
- 좌우 90도 방향전환 트리거는 "거의 멈춰있다가 100도 이상 급격히 방향을 바꿀 때"라는 휴리스틱으로 감지 (완벽하지 않을 수 있음)
