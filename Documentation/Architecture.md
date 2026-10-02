# 현재 구조 — 3D Stage 1

현재 월드 구성과 제한은 `3D_Stage1_Report.md`를 기준으로 합니다. 월드는 3D, 게임 규칙은 XZ 평면이며 완전한 마우스 입력 연결은 Stage 2에 남아 있습니다.

- PlanarSpace: XZ 투영/변환/거리 계산.
- TabletopSurface: 수평 표면 범위, 카메라 참조와 Raycast LayerMasks. 게임 상태를 소유하지 않음.
- WaypointPath/EnemyPathFollower: 설정된 XZ 경로와 path root Y에서 이동.
- TowerPlacementSlot: 점유/하이라이트 상태와 Changed 이벤트만 소유.
- PlacementSlotView, PathVisualization, EnemyHealthBar: 교체하거나 끌 수 있는 표시 컴포넌트.
- TryPlace/스킬 조준 API는 유지. 기존 Physics2D/ScreenToWorldPoint 입력은 중단했으며 아직 3D 입력으로 연결하지 않았음.
- Turret/Enemy의 시각적 자식 Mesh와 런타임 책임 분리. 사거리/스킬 radius에 Y는 사용하지 않음.

아래는 유지되는 카드/상태 구조 설명입니다. 마우스 down/release 흐름은 API의 의미를 설명하며 현재 연결된 사용자 입력을 뜻하지 않습니다.


## 재사용한 기반

Scene은 계속 MainMenu와 TestStage 두 개입니다. Enemy/BasicEnemy, EnemyHealth, EnemyPathFollower, WaypointPath, EnemySpawner, FixedIntervalSpawner, EnemyRegistry, Turret/BasicTurret, targeting/attack composition, ResourceWallet, EnemyKillRewards, BaseHealth, 슬롯 색상, HP 바를 유지했습니다. 무한 스폰이나 기존 전투 구현을 다시 작성하지 않았습니다.

TurretDefinition에는 prefab, Damage, AttackRange, AttackCooldown, Cost, Targeting이 있습니다. 15개 TestTurret 에셋이 동일한 BasicTurret prefab과 ClosestTargeting/DirectDamageAttack을 공유합니다. StageDefinition은 맵 씬 이름과 초기 전투 수치를 제공합니다.

## 세션 모델과 UI 분리

| 타입 | 책임 |
|---|---|
| GameCatalog | ScriptableObject. stage 순서, 사용 가능한 tower 및 card 정의, 초기 6개 덱 슬롯 |
| PlayerSession | 정적 세션 소유자. 씬 오브젝트를 보관하지 않으며 도메인 재로드 비활성 상태에서도 새 Play 시작 시 리셋 |
| StageSelection | selectedStageIndex와 경계가 있는 앞/뒤 이동 |
| StageProgressionState | 완료/잠금 상태. 첫 항목만 기본 해금, 완료한 다음 항목 해금 |
| MainMenuUI | Start/Lobby/Deck/Settings 패널 간 이동 |
| LobbyUI | Lobby 조작·진입 요청. 데이터와 애니메이션은 별도 객체 사용 |
| KeyRepeat | 입력 방향에 대한 즉시 이동·최초 지연·반복 간격 |
| StageCarouselView | 임의의 stage 수에 대응하는 카드 위치/크기 보간, 잠금 표시 |
| LobbyDeckPreview | PlayerDeck.Changed 구독, T1–T6의 읽기 전용 표시 |
| PlayerDeck | 서로 다른 CardDefinition 6개(최소 Tower Card 1개)의 세션 할당. 생성·교체 중복 및 최소 포탑 검증, 읽기 전용 노출, Changed 이벤트 |
| DeckEditorController | 전체 카탈로그 minus PlayerDeck으로 Available을 계산하고, 해당 card만 정확한 슬롯에 교체 |
| DeckEditorUI | 책 형태 배치, Available Cards 스크롤 목록, T1–T6 고정 표시 |
| DeckCardDrag / DeckSlotDrop | uGUI 드래그 ghost/드롭 이벤트. 유효 드롭 때만 controller 호출 |

전투 진입 때 StageInitializer가 PlayerSession을 준비합니다. Lobby에서 선택한 StageDefinition이 현재 씬과 일치하면 그 초기 수치를 사용합니다. 서로 다른 정의가 같은 TestStage 맵을 사용하는 것도 가능합니다.

## 영구 덱과 전투 순서

여기서 영구는 **현재 앱 세션 동안의 유지**를 뜻합니다. 디스크 저장이 아닙니다.

```text
GameCatalog.InitialDeck
    → PlayerSession.Deck : PlayerDeck[T1..T6]
        ├─ LobbyDeckPreview
        ├─ DeckEditorController → DeckEditorUI
        └─ stage 진입 때 RuntimeCardCycle 생성
             ├─ 새 RuntimeCard 6개 (SourceSlot, Definition)
             ├─ Fisher–Yates shuffle 1회
             ├─ Hand[3]
             └─ UpcomingQueue[3]
```

같은 카드 정의를 T1과 T4에 중복 할당할 수 없습니다. SourceSlot은 원래 덱 위치를 추적하는 정보이지 추첨 우선순위가 아닙니다. RuntimeCardCycle은 시작과 순환 전후에 3장 Hand + 3장 Queue와 6개 정의의 고유성을 검증합니다. Shuffle은 생성자에서만 실행하며, 카드 사용 시 난수를 호출하지 않습니다. 생성자에 System.Random을 전달할 수 있어 테스트 재현이 가능합니다. 일반 플레이에는 seed 조절 UI가 없습니다. 새로운 실행은 새 난수로 섞지만 우연히 같은 순서가 나올 가능성은 정상입니다.

## 공통 Card와 사용 계약

```text
CardDefinition (ID / 표시 이름 / ElixirCost / 색상)
├─ TowerCardDefinition → TurretDefinition → Turret / BasicTurret
└─ SkillCardDefinition → SkillDefinition → Skill / CircularDamageSkill
```

- `GameplayHand`는 Cycle과 선택 카드를 소유하며, `CanUse`와 `CompleteUse`가 두 카드 타입의 공통 Elixir 검증·차감·순환 경로입니다.
- `BuildMenuUI`는 선택과 표시만 담당합니다. Tower 선택은 기존 placement에 TurretDefinition을 넘기며 Skill 선택은 placement 선택을 비웁니다.
- `TurretPlacementController`는 위치·점유·기존 자원·Elixir·프리팹을 검증하고 포탑 생성/점유 성공 후 자원 차감과 `CompleteUse(Success)`를 호출합니다. 기존 Placed 이벤트는 알림 용도로 유지합니다.
- `SkillCastingController`는 유효 영역 LMB down → 조준/반경 표시 → release를 처리합니다. UI 위·영역 밖 release는 Cancelled, 잔액 부족은 Failure입니다.
- 성공 시에만 `CompleteUse`가 Elixir를 한 번 차감하고 선택 해제 및 `Cycle.Use`를 실행합니다. UI는 자원이나 피해를 직접 변경하지 않습니다.
- Skill 효과는 성공 결제/순환 이후 CastingTime에 따라 실행합니다. 지연된 효과는 이미 결제되었으므로 재결제/재검증하지 않습니다. Pause는 대기 시간을 멈추고 Game Over는 대기 효과를 폐기합니다.
- `CircularDamageSkill`은 EnemyRegistry의 snapshot을 순회하여 살아 있는 반경 내 모든 Enemy에 ReceiveDamage를 호출합니다. 사망 중 registry 수정과 기존 처치 보상을 안전하게 재사용합니다.
- `GameplayPointer`는 현재 화면 좌표를 uGUI에 직접 raycast하여 이전 프레임 hover cache에 의존하지 않습니다.

## Elixir와 표시

`ElixirSystem`의 float Current가 실제 상태입니다. 시작 3, 최대 10, Playing에서 초당 .36 회복하며 `CanAfford` / `TrySpend`로 비용을 처리합니다. 기존 ResourceWallet과는 독립적이며 Tower의 기존 자원 비용은 그대로입니다.

`ElixirBarUI`는 별도 DisplayedFill을 실제 비율로 보간합니다. `FadingMessageUI`는 unscaled 시간으로 안내를 유지한 후 .3초 동안 fade합니다. 마지막 Tower 제거 안내는 3초, Elixir 부족 안내는 1.5초 유지합니다.

## 진행도와 화면 상태

캐러셀은 선택 변경 시 현재 보간 위치에서 새 중앙 위치로 부드럽게 이동합니다. 카드 중심 간격과 scale을 동일한 visualIndex에서 계산하며 stage별 애니메이션은 없습니다. 이동 중 Enter는 잠시 기다리라는 안내를 표시하여 아직 중앙에 도달하지 않은 카드를 로드하지 않습니다. 끝에서는 이동이 멈추며 wrap하지 않습니다.

Settings/Deck으로 갈 때 Lobby를 숨기므로 selectedStageIndex와 보간 위치를 보존합니다. 게임에서 돌아올 때 같은 세션 모델을 읽어 Lobby를 다시 만듭니다.

완료 조건은 기존 prototype에 없으므로 새 승리/웨이브 시스템을 만들지 않았습니다. 미래 목표 시스템이 `PlayerSession.CompleteActiveStage()`를 호출하면 다음 stage가 해금됩니다. 자동 테스트에서 이 API를 실행해 검증했습니다.

## 범위 밖

디스크 저장, 덱 프리셋, 인벤토리 수량, 카드 희귀도, reroll/discard, 중간 재셔플, 터치/swipe, 추가 적, projectile, 완성형 아트는 추가하지 않았습니다. 기존 자원 보상과 세 테스트 포탑을 유지하고, 추가 12개 포탑 정의를 같은 전투 코드로 구성했습니다.

## 중복 방지 / Available / Retry 패치

PlayerDeck 생성자는 null·개수·중복을 검증합니다. Replace는 현재 슬롯의 동일 값은 무변경으로 처리하며 다른 슬롯과 겹치는 정의는 변경 전에 거부합니다. DeckEditorController.Available은 매 접근 시 전체 정의에서 현재 덱을 제외하므로 별도 인벤토리 상태가 없습니다. DeckEditorUI는 기존 카드 view를 재사용하고 Changed에 맞춰 활성화·배열 위치·스크롤 높이만 갱신합니다.

StageSelection.MovePrevious/MoveNext는 키보드와 마우스 공통 진입점입니다. StageCarouselView는 좌우 카드의 Button 클릭을 이 메서드에 연결합니다. 위치가 이동 중인 카드의 클릭은 잠시 비활성화되며, 키보드 held-key 로직과 보간은 그대로 유지합니다.

GameFlow.Retry는 GameOver일 때만 현재 씬을 다시 로드합니다. PlayerSession.ActiveStage/Selection/Deck/Progression은 그대로 유지하고, 새 씬의 StageInitializer가 HP·자원·Elixir·스폰·카드 순환을 초기화합니다. 새 EnemyRegistry·슬롯·UI가 생성되어 이전 실행의 적/포탑/점유/선택/이벤트 연결은 씬과 함께 제거됩니다. Retry는 timeScale을 1로 복원하고 한 번의 중복 로드 요청을 막습니다.
