# 현재 구조

## 재사용한 기반

Scene은 계속 MainMenu와 TestStage 두 개입니다. Enemy/BasicEnemy, EnemyHealth, EnemyPathFollower, WaypointPath, EnemySpawner, FixedIntervalSpawner, EnemyRegistry, Turret/BasicTurret, targeting/attack composition, ResourceWallet, EnemyKillRewards, BaseHealth, 슬롯 색상, HP 바를 유지했습니다. 무한 스폰이나 기존 전투 구현을 다시 작성하지 않았습니다.

TurretDefinition에는 prefab, Damage, AttackRange, AttackCooldown, Cost, Targeting이 있습니다. 15개 TestTurret 에셋이 동일한 BasicTurret prefab과 ClosestTargeting/DirectDamageAttack을 공유합니다. StageDefinition은 맵 씬 이름과 초기 전투 수치를 제공합니다.

## 세션 모델과 UI 분리

| 타입 | 책임 |
|---|---|
| GameCatalog | ScriptableObject. stage 순서, 사용 가능한 tower 정의, 초기 6개 덱 슬롯 |
| PlayerSession | 정적 세션 소유자. 씬 오브젝트를 보관하지 않으며 도메인 재로드 비활성 상태에서도 새 Play 시작 시 리셋 |
| StageSelection | selectedStageIndex와 경계가 있는 앞/뒤 이동 |
| StageProgressionState | 완료/잠금 상태. 첫 항목만 기본 해금, 완료한 다음 항목 해금 |
| MainMenuUI | Start/Lobby/Deck/Settings 패널 간 이동 |
| LobbyUI | Lobby 조작·진입 요청. 데이터와 애니메이션은 별도 객체 사용 |
| KeyRepeat | 입력 방향에 대한 즉시 이동·최초 지연·반복 간격 |
| StageCarouselView | 임의의 stage 수에 대응하는 카드 위치/크기 보간, 잠금 표시 |
| LobbyDeckPreview | PlayerDeck.Changed 구독, T1–T6의 읽기 전용 표시 |
| PlayerDeck | 6칸의 세션 할당. 슬롯 읽기 전용 노출, 유효한 교체와 Changed 이벤트 |
| DeckEditorController | 카탈로그에 있는 tower만 정확한 슬롯에 교체 |
| DeckEditorUI | 책 형태 배치, Available Towers 스크롤 목록, T1–T6 고정 표시 |
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

같은 포탑 정의를 T1과 T4에 넣어도 서로 다른 RuntimeCard입니다. SourceSlot은 중복을 구분하는 식별 정보이지 추첨 우선순위가 아닙니다. Shuffle은 생성자에서만 실행하며, 카드 사용 시 난수를 호출하지 않습니다. 생성자에 System.Random을 전달할 수 있어 테스트 재현이 가능합니다. 일반 플레이에는 seed 조절 UI가 없습니다. 새로운 실행은 새 난수로 섞지만 우연히 같은 순서가 나올 가능성은 정상입니다.

## Hand와 배치의 연결

- `GameplayHand`는 스테이지별 Cycle과 선택된 RuntimeCard를 소유합니다.
- 기존 `BuildMenuUI` 컴포넌트는 현재 세 Hand 카드만 표시합니다. 기존 serialized 연결과 GamePlayUI 호출을 유지하기 위해 파일명은 보존했습니다.
- Hand UI → GameplayHand.Select(index) → placement.Select(Definition).
- TurretPlacementController는 선택 카드가 현재 Hand에 있는지 확인한 뒤 기존 위치·점유·자원 검증을 수행합니다.
- 프리팹 생성·슬롯 점유·비용 차감 후에만 `Placed(Definition)` 이벤트를 발생시킵니다.
- GameplayHand는 사용 카드의 identity로 Cycle.Use를 호출하고 선택을 해제합니다.
- Cycle은 Hand의 선택 엔트리만 제거하고, Queue 첫 장을 Hand 오른쪽에 추가하고, 사용 카드를 Queue 뒤에 붙입니다.
- 실패·선택 취소·Pause·Game Over에서는 Placed가 발생하지 않아 Hand와 Queue 모두 유지됩니다.
- UI는 자원 차감/포탑 생성/피해를 직접 실행하지 않습니다.

## 진행도와 화면 상태

캐러셀은 선택 변경 시 현재 보간 위치에서 새 중앙 위치로 부드럽게 이동합니다. 카드 중심 간격과 scale을 동일한 visualIndex에서 계산하며 stage별 애니메이션은 없습니다. 이동 중 Enter는 잠시 기다리라는 안내를 표시하여 아직 중앙에 도달하지 않은 카드를 로드하지 않습니다. 끝에서는 이동이 멈추며 wrap하지 않습니다.

Settings/Deck으로 갈 때 Lobby를 숨기므로 selectedStageIndex와 보간 위치를 보존합니다. 게임에서 돌아올 때 같은 세션 모델을 읽어 Lobby를 다시 만듭니다.

완료 조건은 기존 prototype에 없으므로 새 승리/웨이브 시스템을 만들지 않았습니다. 미래 목표 시스템이 `PlayerSession.CompleteActiveStage()`를 호출하면 다음 stage가 해금됩니다. 자동 테스트에서 이 API를 실행해 검증했습니다.

## 범위 밖

디스크 저장, 덱 프리셋, 인벤토리 수량, 카드 희귀도, reroll/discard, 중간 재셔플, 터치/swipe, 추가 적, projectile, 완성형 아트는 추가하지 않았습니다. 기존 자원 보상과 세 테스트 포탑을 유지하고, 추가 12개 포탑 정의를 같은 전투 코드로 구성했습니다.
