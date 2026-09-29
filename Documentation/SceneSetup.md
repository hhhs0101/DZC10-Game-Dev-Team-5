# Scene / Prefab / Inspector 설정

아래 연결은 생성된 프로젝트에 이미 저장되어 있습니다. Unity 기본 Input Manager를 사용합니다. 8번 레이어는 `PlacementSlot`이며 UI용 기본 레이어와 분리되어 있습니다.

## MainMenu.unity

- **Main Camera**: Orthographic, background color, z = -10.
- **Canvas**: Screen Space Overlay, CanvasScaler(1280×720, Match 0.5), GraphicRaycaster.
- **EventSystem**: EventSystem + StandaloneInputModule.
- **MainMenuUI**: MainMenuUI(Canvas, Stages = TestStage asset) + SettingsUI.

UI 자식 오브젝트들은 Play 시 생성합니다. Stage Selection은 Stages 배열을 순회하며 스크롤 가능한 목록을 생성합니다. 메뉴 배치와 외형을 바꾸려면 `Runtime/UI`를 수정합니다. 고정된 최종 UI 프리팹은 현재 만들지 않았습니다.

## TestStage.unity

| GameObject | 컴포넌트 / 연결 |
|---|---|
| Main Camera | Orthographic Size 6, z = -10, MainCamera tag |
| GameFlow | GameFlow(Main Menu Scene = MainMenu) |
| StageSystems | ResourceWallet, EnemyKillRewards, EnemyRegistry, EnemySpawner, FixedIntervalSpawner, TurretPlacementController, StageInitializer |
| WaypointPath | WaypointPath의 Points 배열, 순서 있는 6개의 자식 Transform, 회색 경로 표시 |
| Base | SpriteRenderer + BaseHealth |
| Enemies | EnemySpawner의 생성 부모 Transform |
| PlacementSlots | Slot 1–8 자식. SpriteRenderer + BoxCollider2D + TowerPlacementSlot(hovered/occupied 색상 설정), PlacementSlot 레이어 |
| Canvas | MainMenu와 동일한 uGUI 설정 |
| EventSystem | EventSystem + StandaloneInputModule |
| GameplayUI | GameplayUI + BuildMenuUI + SettingsUI |

### StageSystems 필수 참조

- `StageInitializer`: Definition=TestStage, Flow=GameFlow, Wallet=동일 오브젝트, Base Health=Base, Schedule=FixedIntervalSpawner.
- `EnemySpawner`: Path=WaypointPath, Base Health=Base, Registry=동일 오브젝트, Flow=GameFlow, Enemy Root=Enemies.
- `FixedIntervalSpawner`: Spawner=EnemySpawner.
- `EnemyKillRewards`: Spawner=EnemySpawner, Wallet=ResourceWallet. 생성 도구와 TestStage 씬에 연결되어 있습니다.
- `TurretPlacementController`: World Camera=Main Camera, Flow=GameFlow, Wallet=ResourceWallet, Registry=EnemyRegistry, Placement Layers=PlacementSlot만 포함.

### GameplayUI 필수 참조

- `GameplayUI`: Canvas, Flow, Base Health, Wallet, Placement.
- `BuildMenuUI`: Stage=TestStage, Placement=TurretPlacementController.
- `SettingsUI`: 별도 Inspector 연결 없음. UI 구성 시 Canvas를 전달받음.

### 프리팹과 에셋

- **BasicEnemy.prefab**: SpriteRenderer, BasicEnemy, EnemyHealth, EnemyPathFollower, EnemyHealthBar. 경로와 Base는 프리팹에 저장하지 않고 스폰 시 주입합니다. 적 충돌 판정에 의존하지 않으므로 Collider/Rigidbody는 불필요합니다.
- **BasicTurret.prefab**: SpriteRenderer, BasicTurret, DirectDamageAttack. Turret의 Attack 필드는 같은 오브젝트의 DirectDamageAttack을 참조합니다.
- **BasicEnemy.asset**: BasicEnemy prefab과 기본 수치, Resource Reward=10. 적별 에셋에서 각각 변경 가능하며 0이면 지급하지 않습니다.
- **BasicTurret.asset**: BasicTurret prefab, 기본 수치, ClosestTargeting asset.
- **TestStage.asset**: BasicEnemy asset, Available Turrets=[TestTurret1, TestTurret2, TestTurret3], 초기 수치.

씬 내 설치 슬롯은 단위 스케일을 사용합니다. 포탑은 슬롯 자식으로 생성하므로 슬롯 부모를 확대하면 포탑도 함께 확대됩니다. 슬롯 클릭은 2D Collider와 레이어 마스크로 판정합니다. 맵 위 클릭만 설치에 사용하며 uGUI 위 클릭은 무시합니다.

## 새 맵 / stage 구성

1. TestStage 씬을 복제하고 다른 이름으로 저장합니다.
2. `Create → Defense → Stage Definition`으로 새 에셋을 생성하고 Scene Name을 정확히 지정합니다.
3. 복제한 씬의 StageInitializer와 BuildMenuUI에 같은 새 정의를 지정합니다.
4. WaypointPath.Points에 최소 두 개 이상의 유효한 Transform을 순서대로 연결합니다. 마지막 점에 Base를 놓습니다. 회색 표시 선은 placeholder이므로 waypoint를 변경할 때 표시도 함께 수정합니다.
5. 지정된 위치에 슬롯을 복제합니다. Collider 크기, PlacementSlot 레이어를 유지합니다.
6. MainMenuUI.Stages에 새 정의를 추가합니다.
7. Build Profiles의 Scene List에 새 씬을 등록합니다.

현재 Build Scene 순서는 `MainMenu`, `TestStage`입니다. 게임 내 Main Menu Exit은 빌드에서는 Application.Quit, 에디터에서는 Play 종료를 실행합니다.

## 초기 생성 도구

`PrototypeBuilder.Build`는 빈 프로젝트에서 데이터·프리팹·씬·Build Settings를 생성하는 Editor 전용 코드입니다. 기존 Scene이 있으면 모두 건너뛰므로 수정한 씬을 덮어쓰지 않습니다. 에셋을 삭제한 경우 개별적으로 복구하거나 프로젝트의 저장된 파일을 복원하세요. 이 도구를 런타임에서 호출하지 않습니다.

## 테스트 포탑 UI 및 HP 바

세 TestTurret 에셋은 동일한 BasicTurret 프리팹과 ClosestTargeting 에셋을 참조합니다. BasicTurret.asset은 보존하지만 현재 메뉴 목록에서는 제외합니다. BuildMenuUI는 오른쪽 아래 Turrets 토글, 선택 패널, 지속 표시되는 Selected 라벨을 생성합니다. 선택 시 패널은 닫히고, Pause/Game Over에서는 닫힌 상태로 비활성화됩니다.

EnemyHealthBar는 BasicEnemy 프리팹에 연결되어 있으며, 플레이 시 적 자식으로 Background/Fill SpriteRenderer를 만듭니다. Inspector의 Offset / Size / Fill Color / Background Color로 조정할 수 있습니다. 현재 사각형 placeholder sprite를 재사용합니다. 다른 Enemy 프리팹에도 EnemyHealthBar를 붙이면 EnemyHealth.Changed를 통해 동일하게 갱신합니다. 원본 스프라이트를 최종 그래픽으로 교체할 때 HP 바의 사각형 스프라이트도 별도로 구성해야 합니다.
