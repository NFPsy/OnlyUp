# OnlyUp

"Only Up!" / *Getting Over It* / *Chained Together* 스타일의 3D 수직 등반 플랫포머입니다.
플레이어는 발판을 밟고 점프하며 계속 위로 올라가고, 떨어지면 시작 지점에서 다시 시작합니다.
씬에는 로직 오브젝트(Camera, Light, `GameBootstrap`)만 두고, 발판 코스·플레이어·UI는 전부 코드로 생성합니다.

## 개발 환경

| 항목 | 값 |
|---|---|
| Unity 버전 | 6000.3.21f1 |
| 렌더 파이프라인 | Universal Render Pipeline (URP) |
| 플랫폼 | PC(에디터) 기준 개발, **WebGL 빌드 → itch.io 배포**가 최종 목표 |
| Active Input Handling | **Input System Package (New)** — 레거시 `UnityEngine.Input`은 예외를 던지므로 사용 금지 |
| 추가 패키지 | 없음 (기본 Unity 기능 + 이미 설치되어 있던 Input System만 사용) |
| UI | 기본 UGUI (`Text`, `Canvas`) — TextMeshPro Essentials 미설치 상태라 TMP 대신 사용 |

## 조작법

- **WASD**: 이동 (카메라가 보는 방향 기준)
- **Space**: 점프
- **마우스 좌클릭 후 드래그**: 카메라 각도 회전 (포인터 락, `Esc`로 해제)
- 방향키/WASD는 카메라 각도에 전혀 영향을 주지 않음 — 카메라는 마우스로만 회전하는 궤도(orbit) 카메라

## 씬 구성

| 씬 | 설명 |
|---|---|
| `Assets/Scenes/SampleScene.unity` | Unity 기본 템플릿 씬 (건드리지 않음) |
| `Assets/Scenes/OnlyUp.unity` | **1스테이지.** 좁은 외길 지그재그 발판 코스 (`GameBootstrap`) |
| `Assets/Scenes/OnlyUpMountain.unity` | **2스테이지(최종).** 넓게 흩어진 바위를 골라 오르는 산 코스 (`MountainBootstrap`) |

1스테이지 Goal을 밟으면 `OnlyUpMountain`으로 씬 전환되고, 2스테이지 Goal을 밟으면 GAME CLEAR UI가 뜹니다.
(Build Settings에 두 씬 모두 등록되어 있어야 `SceneManager.LoadScene`이 동작합니다.)

## 핵심 아키텍처

### 로직 / 비주얼 분리

모든 게임 오브젝트는 두 부분으로 나뉩니다.

- **부모(로직)**: Collider, Rigidbody, 기능 스크립트 — 항상 유지됨
- **자식 `Visual`**: 실제 보이는 메시 (지금은 Cube/Capsule 프리미티브, 나중에 VARCO 3D 등으로 만든 실제 모델로 교체 예정)

`VisualSwapTarget.SwapVisual()` 하나로 Visual만 교체하면 외형이 바뀌고, Collider/스크립트는 전혀 손댈 필요가 없습니다.
발판/장애물의 Visual 크기·위치가 바뀌면 `PlatformColliderSync`가 부모의 BoxCollider를 자동으로 맞춰줍니다
(에디터에서 씬 뷰의 메시를 직접 클릭해 늘리거나 옮겨도 충돌 판정이 항상 따라옵니다).

### 스테이지 생성 로직 공유 — `CourseKit`

플레이어/카메라/UI/발판/장애물을 만드는 코드는 [`CourseKit.cs`](Assets/Scripts/OnlyUp/CourseKit.cs)에 static 메서드로 모아뒀습니다.
각 스테이지의 Bootstrap 스크립트(`GameBootstrap`, `MountainBootstrap`)는 "코스를 어떤 모양으로 배치할지"만 담당하고,
실제 생성은 전부 `CourseKit`을 호출합니다. 새 스테이지를 추가할 때는 배치 알고리즘만 새로 작성하면 됩니다.

### 에디터에서 코스 편집하기

`GameBootstrap`/`MountainBootstrap` 컴포넌트의 인스펙터 우측 상단 **⋮ 메뉴 → "코스 생성/재생성 (에디터)"** 를 누르면
Play 없이 코스가 실제 씬 오브젝트로 생성되어 Hierarchy/Scene 뷰에서 직접 선택·이동·삭제할 수 있습니다.

- 씬에 `Course`가 이미 있으면 Play해도 **다시 생성되지 않고** 그대로 재사용됩니다 (수동으로 편집한 내용이 유지됨).
- 다시 생성하고 싶으면 같은 메뉴를 또 누르면 기존 것을 지우고 새로 만듭니다.
- 발판을 편집할 땐 Hierarchy에서 **부모 오브젝트**(예: `Platform_05`)를 선택하는 것이 안전합니다. 자식 `Visual`을
  직접 늘리거나 옮겨도 `PlatformColliderSync`가 자동으로 충돌 범위를 맞춰주지만, 헷갈리지 않으려면 부모를 다루는 편이 낫습니다.

## 스크립트 목록 (`Assets/Scripts/OnlyUp/`)

| 스크립트 | 역할 |
|---|---|
| `GameBootstrap.cs` | 1스테이지: 좁은 외길 코스 배치 |
| `MountainBootstrap.cs` | 2스테이지: 넓은 산 코스 배치 (메인 경로 + 대안 경로) |
| `CourseKit.cs` | 플레이어/카메라/UI/발판/장애물 생성 공용 로직 |
| `PlayerController.cs` | WASD 이동, 점프, 장애물 넉백, 애니메이터 파라미터 갱신 |
| `CameraFollow.cs` | 마우스 궤도 3인칭 카메라 (WASD와 완전히 분리) |
| `RespawnController.cs` | 낙사 감지 및 시작 지점 리스폰 |
| `Goal.cs` | 골 도달 감지 → 클리어 UI 표시 또는 다음 씬 전환 |
| `GameClearUI.cs` | GAME CLEAR 패널 표시/숨김 |
| `HeightUI.cs` | 현재 높이 실시간 표시 |
| `Obstacle.cs` | 장애물 마커 (넉백 힘 값 보유) |
| `MovingObstacle.cs` | 두 지점을 왕복하는 장애물 이동 |
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

## 확장 예정 (설계상 이미 고려됨)

- **VARCO 3D 연동**: `VisualSwapTarget`을 통해 발판/플레이어/장애물의 Visual을 실제 생성 모델로 교체 가능하도록 준비되어 있음
- **새 스테이지 추가**: `Goal.nextSceneName`에 다음 씬 이름만 지정하면 스테이지 체인을 계속 이어붙일 수 있음
- **새 장애물/발판 종류**: `CourseKit`에 생성 함수를 추가하고 Bootstrap의 배치 로직에서 호출하면 됨

## 알려진 제한사항

- Idle 애니메이션 전용 클립이 없어 모델의 기본 포즈(한쪽 다리를 살짝 든 액션 포즈)를 그대로 사용 중
- 좌우 90도 방향전환 트리거는 "거의 멈춰있다가 100도 이상 급격히 방향을 바꿀 때"라는 휴리스틱으로 감지 (완벽하지 않을 수 있음)
- WebGL 빌드에서의 실제 플레이 테스트는 아직 진행되지 않음 (에디터 Play 모드 기준으로만 검증됨)
