# Card / Elixir / Skill 구현 보고서

## 기존 구조와 재사용

기존 MainMenu / TestStage Scene, 15개 TurretDefinition, Turret/BasicTurret의 targeting 및 attack composition, Enemy/BasicEnemy/EnemyHealth/EnemyRegistry, 자원 보상, TowerPlacementSlot, GameFlow, StageCarousel, Lobby, 진행도 및 Retry를 유지했습니다.

PlayerDeck, DeckEditorController, RuntimeCard, RuntimeCardCycle, GameplayHand와 기존 Hand/Deck UI는 카드 타입 일반화와 연결 부분만 확장했습니다. 기존 골드 성격의 ResourceWallet은 유지되며 Elixir와 별도로 작동합니다. 신규 외부 패키지는 없습니다.

## 최종 데이터 구조

```text
CardDefinition: CardId, DisplayName, ElixirCost, Color, Type
├── TowerCardDefinition → TurretDefinition → 기존 Turret 런타임
└── SkillCardDefinition → SkillDefinition → Skill 효과 abstraction
                                            └── CircularDamageSkill
```

SkillDefinition은 Damage, Radius, CastingTime, Effect 참조를 갖습니다. Skill은 상태 없는 ScriptableObject 효과 전략이며, 조준/대기 중인 실행 상태는 SkillCastingController가 소유합니다. 두 스킬은 같은 원형 피해 구현을 공유하므로 Fireball/Arrow Rain마다 시전 로직을 복제하지 않습니다. Turret과 Skill의 상속 구조는 분리되어 있습니다.

## 덱 검증

PlayerDeck 생성과 교체에서 정확히 6장, 모든 CardDefinition 참조 고유, 최소 TowerCardDefinition 1장을 보장합니다. 마지막 Tower를 Skill로 교체하면 변경 전에 거부하고 Rejected 이벤트를 보냅니다. UI는 `덱에 최소 1장의 Tower 카드가 들어가야 합니다`를 3초 유지하고 .3초 동안 fade합니다. UI를 우회하는 도메인 호출도 같은 검증을 통과해야 합니다.

Available은 GameCatalog.AvailableCards에서 현재 덱을 제외하여 매번 계산합니다. 카탈로그는 Tower 15장 + Skill 2장 = 17장이고 Available은 11장입니다. 실제 콘텐츠의 스킬은 2종이지만 도메인은 1 Tower + 5 unique Skills도 허용하며 합성 테스트 데이터로 마지막 Tower 보호를 검증했습니다.

기본 덱은 기존 TowerCard1~6으로 유지했습니다. Deck Editor에서 Fireball/Arrow Rain을 넣으면 전투 Hand에 함께 등장합니다. 영속성은 기존과 동일한 앱 세션 범위이며 저장 파일은 추가하지 않았습니다.

## 비용과 카드 순환

ElixirSystem은 시작 3, 최소 0, 최대 10의 float 상태와 Playing에서 초당 .36 회복을 소유합니다. Pause/Game Over에서 회복하지 않습니다. ElixirBarUI의 DisplayedFill은 별도 보간 값이며 논리 잔액을 변경하지 않습니다.

GameplayHand.CanUse / CompleteUse는 Tower와 Skill의 공통 비용 경로입니다. 성공한 사용만 Elixir를 한 번 차감하고 RuntimeCardCycle.Use를 실행합니다. Failure/Cancelled는 차감·순환하지 않습니다. 부족 안내 `엘릭서가 부족합니다`는 FadingMessageUI를 통해 1.5초 유지 후 .3초 fade합니다.

TowerPlacementController가 기존 위치·점유·ResourceWallet·프리팹 조건과 Elixir를 검증합니다. 생성과 점유 성공 후 기존 자원 비용과 공통 Elixir 비용을 처리합니다. 기존 Tower Cards의 ElixirCost는 모두 0이지만 유료 Tower Card도 테스트했습니다.

RuntimeCardCycle은 같은 6개 정의를 새 RuntimeCard로 복사하여 Fisher–Yates를 생성자에서 한 번만 실행합니다. 성공 시 사용 카드는 Queue 뒤로, Queue 첫 카드는 Hand 오른쪽으로 이동합니다. 중간 재셔플이나 PlayerDeck 변경은 없습니다. Retry는 기존 같은 씬 재로드를 사용하여 Elixir 3, 새 Cycle, 빈 조준/대기 상태로 시작합니다.

## 스킬과 입력

| 카드 | 피해 | 반경 | CastingTime | Elixir |
|---|---:|---:|---:|---:|
| Fireball | 60 | 1.5 | 0 | 3 |
| Arrow Rain | 25 | 3 | 0 | 2 |

선택만으로 사용하지 않습니다. 유효 월드 영역에서 LMB down → 실제 Radius의 LineRenderer 원 표시 → held cursor 따라 이동 → LMB release가 시전 진입점입니다. 기본 월드 영역은 x=-9~9, y=-4.5~4.5(Rect 상단 경계 제외)입니다. 기존 슬롯과 무관하며 비어 있는 영역도 성공입니다.

현재 포인터 좌표로 uGUI raycast를 수행하여 원래 카드나 다른 UI 위에서 놓으면 취소합니다. 영역 밖 release, Pause, 선택 변경도 조준을 취소합니다. 실패/취소 시 카드가 Hand에 남으며 비용·피해·순환은 발생하지 않습니다.

성공한 시전은 먼저 결제하고 카드를 순환한 다음 CastingTime을 기다려 효과를 실행합니다. 대기 중 Pause는 타이머를 멈추며 Game Over는 대기 효과를 폐기합니다. 결제된 효과는 Elixir를 다시 확인하지 않습니다. 현재 두 스킬은 CastingTime=0입니다.

원형 피해는 EnemyRegistry snapshot을 순회하여 범위 내 살아 있는 Enemy.ReceiveDamage를 호출합니다. 여러 적이 동시에 죽어 registry가 변경되어도 모두 처리하며 기존 HP 바, 사망, 처치 보상을 재사용합니다.

## 파일 목록

아래 경로는 프로젝트 루트 기준입니다. 새 Unity 파일의 `.meta`도 함께 포함합니다.

### 신규 C#

- Assets/Defense/Runtime/Data/CardDefinition.cs
- Assets/Defense/Runtime/Data/TowerCardDefinition.cs
- Assets/Defense/Runtime/Data/SkillCardDefinition.cs
- Assets/Defense/Runtime/Data/SkillDefinition.cs
- Assets/Defense/Runtime/Core/ElixirSystem.cs
- Assets/Defense/Runtime/Skills/Skill.cs
- Assets/Defense/Runtime/Skills/CircularDamageSkill.cs
- Assets/Defense/Runtime/Skills/SkillCastingController.cs
- Assets/Defense/Runtime/UI/GameplayPointer.cs
- Assets/Defense/Runtime/UI/FadingMessageUI.cs
- Assets/Defense/Runtime/UI/ElixirBarUI.cs
- Assets/Defense/Editor/CardAssetSetup.cs
- Assets/Defense/Editor/CardSkillValidation.cs

### 수정 C#

- Assets/Defense/Runtime/Data/GameCatalog.cs
- Assets/Defense/Runtime/Deck/PlayerDeck.cs
- Assets/Defense/Runtime/Deck/DeckEditorController.cs
- Assets/Defense/Runtime/Hand/RuntimeCard.cs
- Assets/Defense/Runtime/Hand/RuntimeCardCycle.cs
- Assets/Defense/Runtime/Hand/GameplayHand.cs
- Assets/Defense/Runtime/Placement/TurretPlacementController.cs
- Assets/Defense/Runtime/Core/StageInitializer.cs
- Assets/Defense/Runtime/UI/DeckCardDrag.cs
- Assets/Defense/Runtime/UI/DeckEditorUI.cs
- Assets/Defense/Runtime/UI/BuildMenuUI.cs
- Assets/Defense/Runtime/UI/GameplayUI.cs
- Assets/Defense/Runtime/UI/UiFactory.cs
- Assets/Defense/Editor/LobbyAssetSetup.cs
- Assets/Defense/Editor/PrototypeValidation.cs
- Assets/Defense/Editor/PatchInvariantValidation.cs

### 데이터와 문서

- 신규: Assets/Defense/Data/TowerCard1.asset ~ TowerCard15.asset
- 신규: Assets/Defense/Data/FireballCard.asset, ArrowRainCard.asset
- 신규: Assets/Defense/Data/FireballSkill.asset, ArrowRainSkill.asset, CircularDamageSkill.asset
- 수정: Assets/Defense/Resources/GameCatalog.asset
- 신규 specification 사본: Documentation/MainImplementationPrompt.md, CardSystem.md, Elixir.md, Skill.md
- 신규 보고서: Documentation/CardElixirSkillUpdate.md
- 수정: README.md, Documentation/Architecture.md, SceneSetup.md, Validation.md, ValidationResult.txt
- Scene, 기존 Turret/Enemy 에셋과 프리팹, 패키지 설정은 변경하지 않았습니다.

## 실제 자동 검증

Unity 6000.6.0f1, 격리 복사본 `/tmp/defense-lobby-validation`, Play Mode에서 **378 assertions 통과**, 종료 코드 0.

실행 진입점: `Defense.Editor.PrototypeValidation.Run`

로그: `/tmp/defense-card-validation-final.log`

- 기존 15종 포탑 피해/범위/공격 간격/비용, 적 스폰/이동/보상/HP 바, 배치 실패, Pause, Lobby/Deck/Carousel, 같은 stage Retry.
- 혼합 덱, ID 고유성, 중복 거부, 마지막 Tower 거부와 원본 불변, Available 동기화, 혼합 순환, 한 번의 셔플 및 기존 20 seed/1,200회 순환.
- Elixir 시작/회복/상하한/잘못된 비용/부족/일시정지/재개/Game Over.
- 두 스킬 선택 무동작, UI/영역 밖에서 시작 차단, 조준 위치와 실제 원 꼭짓점의 반경, 원래 Hand 버튼 및 Pause 버튼의 실제 uGUI raycast와 취소.
- 영역 밖 release, Pause 취소, 잔액 부족 시 선택/Hand/Queue/잔액 불변, 정확히 한 번 결제, 정확한 순환, 두 번째 release 재시전 방지.
- 반경 내 여러 적 피해와 범위 밖 제외, 빈 영역 성공, 여러 적 동시 사망과 정상 처치 보상.
- CastingTime 1초 합성 데이터: 즉시 결제/순환, Pause 중 대기, 지연 후 재결제 없이 피해.
- ElixirCost=2인 합성 Tower Card: 부족/무효 위치 무변경, 성공 시 자원과 Elixir 정확히 한 번 차감.
- 두 안내의 유지 시간/fade/clear, Elixir 표시 보간과 실제 잔액 분리.
- 1-1/1-2 Retry 및 두 스킬을 포함한 실제 세션 덱의 Retry: 같은 stage/덱 보존, 신규 runtime 카드, 시작 Elixir 복원.

## 수동 확인 및 미검증

수동 UI 조작은 수행하지 않았습니다. 버튼은 onClick, 드래그는 ExecuteEvents, 스킬은 실제 컴포넌트의 입력 처리 메서드와 uGUI raycast를 호출한 자동 검사입니다. OS 마우스 down/hold/up 또는 키보드 이벤트를 직접 합성한 end-to-end 테스트가 아닙니다.

화면 렌더링, 한국어 글리프의 시각적 표시, 다양한 화면 비율에서 배치/조준 조작감, standalone build와 다른 OS는 미검증입니다. 한국어 폰트는 OS fallback을 사용하므로 대상 OS에 한국어 폰트가 없는 경우 배포용 폰트 에셋이 필요합니다.

Unity Editor SearchDatabase의 기존 인덱싱 예외는 게임 코드와 분리하여 경고로 기록합니다. Defense 게임 코드의 Error/Exception/Assert는 테스트 실패로 처리합니다. 새 셔플은 직전과 다른 순서를 강제하지 않으며 우연히 같은 순서가 나올 수 있습니다.
