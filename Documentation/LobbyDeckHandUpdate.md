> 이전 변경 기록입니다. 현재 덱은 6종 고유 정의이며 Available은 덱을 제외합니다. 최신 동작은 DeckLobbyHandRetryPatch.md를 참고하세요.

# Lobby / Deck / Hand 구현 보고

## 구현 전 확인한 구조

- 기존 Scene: MainMenu, TestStage.
- 관련 코드: MainMenuUI, BuildMenuUI, GameplayUI, SettingsUI, GameFlow, StageInitializer, TurretPlacementController, TurretDefinition, StageDefinition.
- 포탑 데이터: TestTurret1/2/3 ScriptableObject가 같은 BasicTurret prefab, ClosestTargeting, DirectDamageAttack을 공유.
- 기존 흐름: Main Menu → Stage Selection → TestStage → Pause/Game Over → Main Menu.
- 재사용: 적 스폰/이동/HP/사망, 포탑 상속·표적·공격, 자원 및 보상, Base, 슬롯 검증/색상, HP 바, Settings, uGUI 생성 helper.

Main/Tower/enemy/Lobby/Deck/HandCycle 순서로 요구사항을 검토했습니다. game ui.pdf의 page 1 상단 캐러셀·하단 덱, page 2 좌우 책 형태만 참고했습니다. 최종 디자인은 만들지 않았습니다.

## 생성 파일

### 세션·Lobby

- `Assets/Defense/Runtime/Data/GameCatalog.cs`
- `Assets/Defense/Runtime/Core/PlayerSession.cs`
- `Assets/Defense/Runtime/Lobby/StageSelection.cs`
- `Assets/Defense/Runtime/Lobby/StageProgressionState.cs`
- `Assets/Defense/Runtime/Lobby/KeyRepeat.cs`
- `Assets/Defense/Runtime/UI/LobbyUI.cs`
- `Assets/Defense/Runtime/UI/StageCarouselView.cs`
- `Assets/Defense/Runtime/UI/LobbyDeckPreview.cs`

### 덱

- `Assets/Defense/Runtime/Deck/PlayerDeck.cs`
- `Assets/Defense/Runtime/Deck/DeckEditorController.cs`
- `Assets/Defense/Runtime/UI/DeckEditorUI.cs`
- `Assets/Defense/Runtime/UI/DeckCardDrag.cs`
- `Assets/Defense/Runtime/UI/DeckSlotDrop.cs`

### Hand

- `Assets/Defense/Runtime/Hand/RuntimeCard.cs`
- `Assets/Defense/Runtime/Hand/RuntimeCardCycle.cs`
- `Assets/Defense/Runtime/Hand/GameplayHand.cs`

### 에셋·도구·문서

- `Assets/Defense/Resources/GameCatalog.asset`
- `Assets/Defense/Data/Stage1_2.asset`
- `Assets/Defense/Data/Stage1_3.asset`
- `Assets/Defense/Editor/LobbyAssetSetup.cs`
- 해당 새 에셋/스크립트/폴더의 `.meta`
- `Documentation/Lobby.md`, `Deck.md`, `HandCycle.md`: 제공된 specification 사본
- `Documentation/LobbyDeckHandUpdate.md`: 이 보고서

## 수정 파일

- `Assets/Defense/Runtime/UI/MainMenuUI.cs`: Start/Lobby/Deck/Settings 네비게이션.
- `Assets/Defense/Runtime/UI/BuildMenuUI.cs`: 기존 all-turrets toggle 대신 Hand 3장 표현. 클래스 이름은 씬/호출 호환을 위해 유지.
- `Assets/Defense/Runtime/UI/GameplayUI.cs`: Hand 안내, 메시지 위치, Game Over의 Lobby 이동.
- `Assets/Defense/Runtime/Placement/TurretPlacementController.cs`: 선택 Hand 검증과 설치 성공 이벤트. 기존 검증/생성/비용 처리 유지.
- `Assets/Defense/Runtime/Core/StageInitializer.cs`: 선택 stage 데이터와 세션 덱으로 전투 Hand 초기화.
- `Assets/Defense/Runtime/Core/GameFlow.cs`: 게임 종료 뒤 Lobby로 돌아가도록 세션 네비게이션 상태 설정.
- `Assets/Defense/Scenes/MainMenu.unity`: 기존 stage 목록 필드 제거; catalog 사용.
- `Assets/Defense/Scenes/TestStage.unity`: GameplayHand 연결, obsolete BuildMenuUI.stage 참조 제거. 맵과 기존 컴포넌트 연결 보존.
- `Assets/Defense/Editor/PrototypeBuilder.cs`: 초기 생성에 catalog 포함, UI 연결 변경 반영.
- `Assets/Defense/Editor/PrototypeValidation.cs`: 새 흐름에 맞춘 테스트와 기존 전투 회귀 검증 통합.
- `README.md`, `Documentation/Architecture.md`, `SceneSetup.md`, `Validation.md`, `ValidationResult.txt`: 현재 구조/사용법/결과 반영.
- `Documentation/TurretUIUpdate.md`: 이전 UI 변경 기록임을 표시.

새 Scene/Prefab/외부 패키지는 추가하지 않았습니다. 기존 Turret/Enemy 전투 코드, test turret 수치, 처치 보상, HP 바는 수정하지 않았습니다.

## 주요 동작

### Lobby

Start Game은 같은 MainMenu 씬의 Lobby 패널을 표시합니다. StageSelection과 StageProgressionState가 상태를 소유하고 StageCarouselView가 visualIndex로 위치와 scale을 보간합니다. A/D는 KeyRepeat를 통해 최초 즉시 이동, 0.4초 대기, 0.15초 반복입니다. Enter는 중앙 정렬 후 unlocked stage만 엽니다.

Settings/Deck에서 돌아오면 같은 Lobby 객체와 세션 상태를 유지합니다. 게임에서 돌아오면 세션 상태를 읽어 Lobby를 복원합니다. Game Over는 진행도를 완료하지 않습니다.

### 덱

PlayerDeck은 정확히 여섯 개의 TurretDefinition을 캡슐화합니다. 기본 덱은 세 기존 타입을 두 번씩 배정합니다. Available Towers는 수량 없는 정의 카탈로그이므로 할당한 타입과 교체된 타입 모두 재사용 가능합니다. 교체는 지정한 슬롯 하나만 변경합니다. UI는 PlayerDeck.Changed를 구독합니다.

드래그는 원본 카드를 이동시키지 않고 raycast를 막지 않는 ghost를 띄웁니다. 유효한 DeckSlotDrop에서만 DeckEditorController.Replace를 호출합니다. 취소/잘못된 드롭은 모델에 명령을 보내지 않습니다. Settings 전환 등으로 source가 비활성화되면 ghost도 정리합니다.

### Hand / Queue

매 run마다 PlayerDeck으로부터 RuntimeCard 6개를 만듭니다. SourceSlot과 Definition을 저장하고 복사본을 Fisher–Yates로 1회 섞습니다. 중복된 Definition도 SourceSlot이 달라 별도 엔트리입니다. 처음 3장은 Hand, 뒤 3장은 UpcomingQueue입니다.

플레이어 선택 → GameplayHand → 기존 placement → 성공 이벤트 → Cycle.Use 순서입니다. Hand 내 사용 카드 제거, Queue 첫 장을 Hand 오른쪽에 추가, 사용 카드를 Queue 뒤로 이동합니다. 실패·취소는 상태를 바꾸지 않습니다. 성공 후 선택은 해제합니다. UpcomingQueue는 화면에 노출하지 않습니다.

## 검증

Unity Play Mode **155개 assertion 통과**. 모델의 반복/경계/무작위 호출 수/30회 순환, 실제 UI 이벤트에 의한 navigation/drag/drop, 실제 씬 배치에 의한 카드 순환, 세 포탑 전투·자원·HP·Pause·Game Over 회귀를 검증했습니다. 자세한 범위는 Validation.md에 있습니다.

물리적 마우스/키보드를 사용한 수동 UI 테스트는 하지 않았습니다. 자동 테스트는 Button.onClick, ExecuteEvents, 입력 처리 메서드 및 실제 Play Mode 프레임을 사용했습니다. 최종 GPU 렌더링 외형·다양한 화면 비율·standalone build는 미검증입니다.

## 의도적인 제한

- 1-2 / 1-3은 임시 정의이며 같은 TestStage 맵을 재사용합니다.
- 완료 통지는 `PlayerSession.CompleteActiveStage()`로 연결할 수 있습니다. 기존 prototype에 승리 조건이 없으므로 완료 조건은 새로 만들지 않았습니다. 자동 테스트에서 완료 통지/해금을 확인했습니다.
- 덱/진행도는 앱 세션에만 유지됩니다. 디스크 저장 없음.
- 새 셔플은 매번 실행하지만, 우연히 직전과 같은 순서가 나오는 것은 허용합니다.
