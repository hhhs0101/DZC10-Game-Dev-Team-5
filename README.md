# Unity 2D Defense — Lobby / Deck / Hand Prototype

Unity **6000.6.0f1** 프로젝트입니다. 기존 전투에 세션 Lobby·6칸 덱 편집·3장 Hand를 연결했습니다. final graphics나 디스크 저장은 없습니다.

## 실행과 조작

1. Unity Hub에서 이 `GameDesign` 폴더를 열고 `Assets/Defense/Scenes/MainMenu.unity`를 실행합니다.
2. **Start Game → Lobby**. **A / D**로 스테이지를 이동합니다. 길게 누르면 0.4초 후 0.15초마다 반복합니다.
3. 중앙 스테이지가 정렬되면 **Enter** 또는 **Enter Stage**로 입장합니다. 목록은 1-1부터 1-10까지이며, 기존 해금 규칙대로 처음에는 1-1만 열려 있습니다.
4. **Deck**에서 왼쪽 포탑 카드를 오른쪽 T1–T6 슬롯에 드래그합니다. 변경은 즉시 적용됩니다. **Back to Lobby**로 돌아갑니다.
5. 전투 하단의 Hand 카드 3장 중 하나를 선택하고 초록 설치 슬롯을 클릭합니다. 성공하면 사용 카드가 Queue 뒤로 가고 다음 카드가 Hand 오른쪽에 들어옵니다.
6. **Pause / Esc**로 정지·재개합니다. Pause → Exit → Yes 또는 Game Over → Lobby로 돌아오면 덱은 유지됩니다.

Settings는 placeholder입니다. Main Menu, Lobby, Deck Editor, Pause에서 진입한 화면으로 돌아갑니다.

## 데이터와 현재 가정

- `Assets/Defense/Resources/GameCatalog.asset`: 스테이지 순서, Available Towers, 기본 T1–T6.
- 포탑은 총 15종이며, 초기 덱은 기존 구성을 유지해 `[Test1, Test2, Test3, Test1, Test2, Test3]`입니다. 중복 정의는 허용하지만 카드 엔트리는 슬롯별로 구분합니다.
- Available Towers는 **수량 제한 없는 타입 카탈로그**입니다. 넣은 타입도 계속 사용 가능하며, 교체된 타입도 목록에서 다시 사용할 수 있습니다. 별도 인벤토리 수량은 없습니다.
- T1–T6는 덱 편집 위치입니다. 전투 진입 시 별도 복사본만 한 번 섞습니다. Hand/Queue 변화는 영구 덱을 수정하지 않습니다.
- 세션 동안 유지하며 앱을 다시 실행하면 초기 덱·진행도로 돌아옵니다. 파일 저장/PlayerPrefs는 사용하지 않습니다.
- `1-1`부터 `1-10`까지는 모두 TestStage 맵을 재사용합니다. 시작 자원은 150부터 375까지 증가하고, 스폰 간격은 2초부터 1.1초까지 감소합니다.
- 진행도는 `PlayerSession.CompleteActiveStage()` 통지로 다음 항목을 해금합니다. 무한 스폰 테스트에 임의의 승리 조건은 추가하지 않았으므로 정상 플레이에서 자동 완료되지는 않습니다. Game Over는 해금하지 않습니다.

## 기본 수치

| 포탑 | 피해 | 범위 | 초당 공격 | 비용 |
|---|---:|---:|---:|---:|
| Test Turret 1 | 10 | 3 | 1 | 50 |
| Test Turret 2 | 30 | 5 | 0.4 | 75 |
| Test Turret 3 | 5 | 2.5 | 3 | 100 |

1-1 기준 Base HP 10, 시작 자원 150, 적 스폰 간격 2초입니다. 적 HP 40, 속도 1.5, Base 피해 1, 처치 보상 10입니다. `BasicEnemy.asset`의 **Resource Reward**를 적 정의별로 변경할 수 있습니다.

초록 슬롯은 호버 시 밝아지고 점유 시 갈색입니다. 빨간 적 위에 HP 바가 표시됩니다. 15종 모두 기존 BasicTurret 프리팹과 전투 코드를 공유합니다. 덱 편집기 왼쪽 목록을 스크롤하면 추가 포탑을 볼 수 있습니다. 전체 수치는 `Documentation/TestCatalog.md`에 있습니다.

## 문서

- `Documentation/LobbyDeckHandUpdate.md`: 변경 파일 전체 목록, 데이터 구조, 카드 순환, 구현 범위
- `Documentation/Architecture.md`: 현재 구조
- `Documentation/SceneSetup.md`: Scene/Inspector 연결
- `Documentation/Validation.md`: 233개 자동 검증과 미검증 항목
- `Documentation/Main.md`, `Tower.md`, `enemy.md`, `Lobby.md`, `Deck.md`, `HandCycle.md`: 원본 specification 사본

Unity 기본 uGUI와 모듈만 사용합니다. 새 외부 패키지는 추가하지 않았습니다. `.meta` 파일은 해당 에셋과 함께 보관하세요.
