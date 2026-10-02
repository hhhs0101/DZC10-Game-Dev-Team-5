# Scene / Inspector — 3D Stage 1

현재 설정은 `3D_Stage1_Report.md`의 Scene 구성 및 Collider/Layer 표를 기준으로 합니다. 아래는 이전 카드/로비 구성 기록이며, 2D 외형에 관한 내용은 이번 전환으로 대체되었습니다.

- TestStage의 Battlefield/Tabletop: 크기 20×12, 표면 Y=0, BoxCollider, GameplaySurface layer.
- Main Camera: Perspective FOV 45, 위치 (10,19,-22), 원점 방향. Inspector에서 조절.
- WaypointPath: 자식 17개 Transform과 points 배열. 높이는 path root Y 사용.
- PlacementSlots: 8개 독립 Scene 위치, root BoxCollider와 별도 Slot Visual.
- BasicEnemy/BasicTurret prefab: 로직 root 아래 Visual 자식의 Sphere/Cuboid를 교체.
- EnemyHealthBar는 SpriteRenderer 참조 없이 월드 Canvas를 만들고 카메라를 바라봄.
- PathVisualization/PlacementSlotView는 표시 전용이며 비활성화/제거해도 상태 로직은 유지.
- TabletopSurface의 카메라와 Surface/Placement/Enemy masks는 다음 단계 입력용 연결 지점.
- 별도 수동 연결이나 변환 도구 재실행 없이 저장된 Scene을 열면 됨.
- 마우스 월드 배치/시전은 Stage 2 대기 상태. UI와 programmatic API만 현재 동작.

# Scene / GameObject / Inspector 설정

## 기존 씬 유지

Build Settings는 MainMenu → TestStage입니다. 새로운 씬이나 프리팹은 추가하지 않았습니다.

**MainMenu.unity**

- Main Camera, Canvas(1280×720 기준), EventSystem + StandaloneInputModule 유지.
- MainMenuUI의 Canvas 참조 유지. 이전 Stages 배열은 GameCatalog로 이동했습니다.
- Play 시 Start Screen / Lobby / Deck Editor 패널을 생성합니다.
- Lobby 패널 아래 StageCarouselView, LobbyDeckPreview를 생성합니다.
- Deck Editor 패널 아래 Available 카드의 DeckCardDrag와 고정 슬롯의 DeckSlotDrop을 생성합니다.
- SettingsUI는 기존 callback 기반 네비게이션을 재사용합니다.

**TestStage.unity**

- 카메라, 경로, Base, 적 root, 8개 설치 슬롯과 기존 게임 시스템은 유지합니다.
- StageSystems에 GameplayHand를 추가했습니다. StageInitializer가 세션 덱·배치 컨트롤러·GameFlow를 연결합니다.
- TurretPlacementController는 GameplayHand를 RequireComponent로 요구합니다.
- GameplayUI + BuildMenuUI의 기존 placement 참조는 유지합니다. BuildMenuUI의 이전 stage 참조는 제거했습니다.
- StageInitializer의 기본 definition 참조(TestStage)는 직접 씬 테스트 fallback입니다. Lobby에서 진입하면 선택한 정의의 초기 값을 사용합니다.
- Pause Exit과 Game Over의 Lobby 버튼은 MainMenu 씬을 로드한 뒤 Lobby를 표시합니다.

## 설정 에셋

**Assets/Defense/Resources/GameCatalog.asset**

- Stages: Stage1_1부터 Stage1_10까지. 순서가 진행도 순서입니다.
- Available Towers: TestTurret1부터 TestTurret15까지.
- Available Cards: TowerCard1~15, FireballCard, ArrowRainCard.
- Initial Deck: 정확히 6개 CardDefinition 참조. T1부터 배열 순서대로 Test1, Test2, Test3, Test4, Test5, Test6. 여섯 정의는 모두 달라야 합니다.

GameCatalog는 Resources.Load로 한 번 읽는 작은 composition root입니다. Runtime은 카탈로그 ScriptableObject를 변경하지 않습니다. Scene 참조나 덱 현재값을 디스크에 저장하지 않습니다.

**Stage1_1.asset부터 Stage1_10.asset**

- 표시 이름은 1-1부터 1-10까지.
- 현재는 Scene Name = TestStage로 같은 테스트 맵을 재사용합니다.
- 실제 신규 맵을 추가할 때 별도 Scene Name 지정 후 Build Settings에 씬을 등록하세요.
- 목록에 추가하려면 GameCatalog.Stages에 배치합니다. 첫 항목은 기본 해금됩니다.

**테스트 포탑과 적**

- 기존 TestTurret1/2/3.asset 수치와 BasicTurret prefab 유지. TestTurret4~15는 새로운 수치의 정의 에셋으로 추가했습니다.
- 기존 BasicEnemy prefab, EnemyHealthBar 및 EnemyDefinition.ResourceReward 유지.
- StageDefinition.AvailableTurrets는 기존 테스트 데이터 호환을 위해 남아 있지만 Hand는 이를 읽지 않습니다. 현재 덱 선택의 기준은 GameCatalog.AvailableCards와 PlayerSession.Deck입니다.

## 조정 가능한 UI 값

- LobbyUI: Initial Hold Delay=0.4, Repeat Interval=0.15 (런타임 생성 컴포넌트 기본값).
- StageCarouselView: Duration=0.2초. 위치와 scale을 함께 보간합니다. 좌/우 카드에는 이전/다음 단계 Button이 연결되며 중앙·이동 중 카드는 클릭 이동하지 않습니다.
- DeckEditorUI: 왼쪽 3열 스크롤 목록(현재 덱 6종을 제외한 11종), 오른쪽 T1/T2, T3/T4, T5/T6.
- 슬롯 색상·적 HP 바 설정은 이전과 동일합니다.

UI 오브젝트는 기존 방식처럼 코드로 생성합니다. 영구적인 레이아웃 변경은 해당 UI 스크립트의 생성 위치/크기를 바꾸면 됩니다. Play 중 Inspector 변경은 종료 시 저장되지 않습니다.

## 생성 및 검증 도구

- `Defense → Create Missing Prototype Assets`: 기존 Scene이 있으면 덮어쓰지 않습니다. 빈 상태 생성 때 새 catalog도 생성합니다.
- `Defense → Create Missing Lobby Catalog`: GameCatalog가 없을 때만 기존 정의를 사용해 초기 catalog와 부족한 placeholder stage 데이터를 만듭니다.
- `Defense → Run Prototype Validation (Play Mode)`: 모델·UI 이벤트·전투 통합 자동 검증.

이미 생성된 프로젝트에서 수동 연결이나 생성 도구 재실행은 필요하지 않습니다.

## Retry

Game Over 패널은 Retry / Lobby를 표시합니다. Retry는 GameFlow.Retry에 연결됩니다. 새로운 Scene/Prefab/Inspector 연결은 필요하지 않습니다. 같은 TestStage 씬을 공유하는 1-2 등의 정의도 ActiveStage를 유지하므로 해당 정의의 자원·스폰 설정으로 다시 시작합니다. 패치 적용 시 기존 Play Mode는 종료하고 다시 실행하여 수정된 초기 덱을 로드하세요.

## Card / Elixir / Skill 설정

추가 Scene/Prefab 연결 없이 기존 씬을 실행할 수 있습니다. `StageInitializer`가 StageSystems에 ElixirSystem과 SkillCastingController가 없으면 추가하고, 기존 GameFlow/GameplayHand/EnemyRegistry/Main Camera를 연결합니다. GameplayUI가 ElixirBarUI와 FadingMessageUI를 생성합니다.

- `TowerCard1~15.asset`: 공통 Card ID, 표시 이름, 색상, Elixir Cost(현재 0), 기존 TestTurret 참조.
- `FireballCard.asset` / `ArrowRainCard.asset`: Elixir Cost 각각 3 / 2, Skill 정의 참조.
- `FireballSkill.asset` / `ArrowRainSkill.asset`: Damage 60 / 25, Radius 1.5 / 3, Casting Time 0. 두 정의가 CircularDamageSkill.asset 효과를 공유합니다.
- 지연 시전은 Skill Definition의 Casting Time을 양수로 바꾸어 테스트할 수 있습니다.
- 유효 월드 영역 기본값은 x=-9~9, y=-4.5~4.5입니다(Rect 상단 경계 제외). UI 위에서는 항상 취소됩니다. 영구 변경하려면 StageSystems에 SkillCastingController를 미리 추가하고 Gameplay Area를 Inspector에서 설정하세요. 런타임 기본 생성도 동일하게 지원됩니다.
- Elixir 값은 ElixirSystem의 프로토타입 상수(시작 3, 최대 10, 회복 .36)입니다. 카드별 비용은 Inspector에서 변경합니다.
- 기본 덱은 기존 포탑 6장입니다. Deck Editor에서 두 스킬을 드래그하여 혼합 덱을 만드세요.
- 한국어 안내는 OS 동적 폰트(Apple SD Gothic Neo, Malgun Gothic, Noto Sans CJK KR 순서, Arial fallback)를 사용합니다. 타깃 플랫폼에 한국어 폰트가 없는 경우 배포용 폰트 에셋을 별도 준비해야 합니다.
- `Defense → Create Missing Card Assets`: 누락된 기본 카드/스킬 에셋을 생성하며 기존 설정값과 유효한 덱 선택을 유지합니다.
