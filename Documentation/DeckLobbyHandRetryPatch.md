# Deck / Lobby / Hand / Retry 패치 보고

## 원인

초기 GameCatalog.InitialDeck이 `[Test1, Test2, Test3, Test1, Test2, Test3]`이었습니다. PlayerDeck은 null과 개수만 검사하고 같은 TurretDefinition의 중복은 허용했습니다. DeckEditorController는 전체 카탈로그를 그대로 Available로 노출했습니다. RuntimeCardCycle은 슬롯별 RuntimeCard identity를 구분했으므로 6개의 카드 객체는 서로 달랐지만 동일 포탑 정의가 반복되어 Hand에도 중복 타입이 나타났습니다.

Fisher–Yates 셔플의 확률/인덱스 문제가 아니라 원본 덱의 중복과 정의 단위 검증 누락이 원인입니다. 당시 포탑 3종에 대한 임시 가정을 이번 패치의 6종 고유성 규칙으로 교체했습니다.

## 수정 방식

### 덱과 Available

- 기본 T1–T6를 TestTurret1부터 TestTurret6까지로 변경했습니다.
- PlayerDeck 생성자는 정확히 6개·null 없음·정의 참조 중복 없음 조건을 검사합니다. 잘못된 설정은 즉시 예외로 드러냅니다.
- Replace는 변경 전에 다른 슬롯에 같은 정의가 있는지 확인합니다. 거부된 변경은 값이나 Changed 이벤트를 수정하지 않습니다.
- DeckEditorController.Available은 매번 전체 정의에서 현재 PlayerDeck을 제외하여 계산합니다. 수동으로 갱신하는 별도 인벤토리 목록은 없습니다.
- DeckEditorUI는 Changed를 구독하고 기존 view를 숨기거나 표시하면서 순서/스크롤 높이를 갱신합니다. 초기 15종 중 덱 6종을 뺀 9종이 표시됩니다.
- 드롭 성공으로 source view가 숨겨지면 OnDisable이 drag ghost를 정리합니다. 비활성 source로 새 드래그를 시작할 수 없고, controller/model의 검증도 중복을 차단합니다.

### Hand / Queue

- 기존 1회 Fisher–Yates 셔플과 카드 순환 순서는 유지합니다.
- RuntimeCardCycle 생성 후 및 Use 전후에 Hand=3 / Queue=3 / 전체 정의=6종 고유성을 검사합니다.
- 내부 상태가 손상되어도 중복을 건너뛰거나 임의 재셔플하지 않고, 변경 전에 오류로 탐지합니다.
- 성공 배치만 Placed 이벤트를 발생시키는 기존 통합을 유지했습니다. 실패·취소는 Hand/Queue를 바꾸지 않습니다.

### 마우스 캐러셀

- StageSelection.MovePrevious/MoveNext를 키보드와 좌우 카드 Button이 함께 호출합니다.
- 기존 Move의 경계 처리, KeyRepeat와 animation 보간은 유지했습니다.
- 중앙 카드는 이동하지 않습니다. 보간 중에는 side card 클릭을 일시적으로 막아 움직이는 카드와 논리 선택이 어긋나는 것을 방지합니다. A/D held-key는 기존처럼 계속 동작합니다.

### Retry

- Game Over에 Retry / Lobby 버튼을 배치했습니다.
- GameFlow.Retry는 GameOver 상태에서만 동작하고 중복 로드 요청을 막습니다.
- 현재 씬을 재로드하면서 timeScale을 1로 복구합니다. PlayerSession.ActiveStage/Selection/Deck/Progression은 수정하지 않습니다.
- 기존 StageInitializer가 해당 stage 정의로 HP·자원·스폰과 새 RuntimeCardCycle을 생성합니다. 이전 씬의 적·포탑·점유·선택·구독은 제거됩니다.
- 같은 TestStage 씬을 공유하는 1-2도 ActiveStage가 유지되어 1-2의 자원/스폰 설정을 사용합니다.
- 새 셔플은 다시 실행되지만 우연히 이전과 같은 순서가 나올 가능성은 허용합니다.

## 변경 파일

### Runtime / 데이터

1. `Assets/Defense/Runtime/Deck/PlayerDeck.cs`
2. `Assets/Defense/Runtime/Deck/DeckEditorController.cs`
3. `Assets/Defense/Runtime/Hand/RuntimeCardCycle.cs`
4. `Assets/Defense/Runtime/Hand/RuntimeCard.cs` — 기존 중복 허용 주석 수정
5. `Assets/Defense/Runtime/Data/GameCatalog.cs` — Inspector 설명 수정
6. `Assets/Defense/Resources/GameCatalog.asset` — 기본 6종 덱
7. `Assets/Defense/Runtime/UI/DeckEditorUI.cs`
8. `Assets/Defense/Runtime/UI/DeckCardDrag.cs`
9. `Assets/Defense/Runtime/Lobby/StageSelection.cs`
10. `Assets/Defense/Runtime/UI/LobbyUI.cs`
11. `Assets/Defense/Runtime/UI/StageCarouselView.cs`
12. `Assets/Defense/Runtime/Core/GameFlow.cs`
13. `Assets/Defense/Runtime/UI/GameplayUI.cs`

### 도구 / 테스트

- 수정: `Assets/Defense/Editor/LobbyAssetSetup.cs` — 새 초기 생성도 6종 고유 덱 사용
- 수정: `Assets/Defense/Editor/PrototypeValidation.cs` — 기존 전투 fixture의 중복 덱 제거, UI·Retry 테스트 통합
- 생성: `Assets/Defense/Editor/PatchInvariantValidation.cs` 및 `.meta` — 집중 모델 회귀 테스트

### 문서

- 생성: `Documentation/Patch_DeckLobbyHandRetry.md` (제공 문서 사본), `Documentation/DeckLobbyHandRetryPatch.md` (이 보고서)
- 수정: `README.md`, `Documentation/Architecture.md`, `SceneSetup.md`, `Validation.md`, `ValidationResult.txt`
- 기존 기록 표시: `Documentation/LobbyDeckHandUpdate.md`, `TestCatalog.md`

Scene/Prefab/외부 패키지는 추가하거나 변경하지 않았습니다. 기존 전투·적·비용·배치 검증·Settings·해금 규칙은 보존했습니다. DeckSlotDrop과 GameplayHand의 기존 이벤트 연결도 재사용했습니다.

## 실제 검증

Unity 6000.6.0f1 Play Mode **301개 assertion 통과**. 집중 검증은 20개 seed에서 1,200회 순환, 24회 모델 교체, 실제 드래그 핸들러의 반복 교체, 마우스 Button/키보드 처리 함수의 혼합 네비게이션을 포함합니다.

1-1과 1-2에서 Retry를 실행해 이전 적·포탑의 파괴, 슬롯/자원/HP/timeScale/선택/스폰 리셋, 새 Cycle/Hand/Queue, 같은 stage 및 원본 덱/진행도 보존을 확인했습니다. 기존 15종 전투·보상·Pause·실패 배치 테스트도 통과했습니다.

실행 로그: `/tmp/defense-patch-validation.log`. 결과 요약: `Documentation/ValidationResult.txt`.

## 미검증 / 사용 시 참고

- 실제 OS 마우스/키보드 수동 조작은 하지 않았습니다. Button.onClick, ExecuteEvents, 입력 처리 함수와 실제 Play Mode 프레임으로 검증했습니다.
- GPU 렌더링 외형, 여러 화면 비율, standalone player build는 미검증입니다.
- 화면 조작 권한 제한은 그대로입니다.
- 프로젝트에서 이미 Play 중이었다면 종료 후 다시 Play하여 새 6종 초기 덱을 로드하세요. 디스크 저장/이전 저장 데이터 마이그레이션은 없습니다.
