> 이전 변경 기록입니다. 현재 Turrets 토글 메뉴는 3장 Hand로 대체되었습니다. 최신 구조는 LobbyDeckHandUpdate.md를 참고하세요.

# Test Turrets / Build UI 변경 보고

## 새 파일

- `Assets/Defense/Data/TestTurret1.asset`
- `Assets/Defense/Data/TestTurret2.asset`
- `Assets/Defense/Data/TestTurret3.asset`
- `Assets/Defense/Runtime/UI/EnemyHealthBar.cs`
- 위 네 에셋/스크립트의 `.meta`
- `Documentation/TurretUIUpdate.md` (이 문서)

## 수정 파일

- `Assets/Defense/Runtime/UI/BuildMenuUI.cs`: 오른쪽 아래 Turrets 토글, 처음 닫힌 선택 패널, 지속 선택 라벨, 선택 항목 색상.
- `Assets/Defense/Runtime/UI/GameplayUI.cs`: 안내 문구 변경 및 Pause/Game Over에서 빌드 메뉴 닫기/비활성화.
- `Assets/Defense/Runtime/Placement/TurretPlacementController.cs`: SelectionChanged 이벤트, 슬롯 hover 판정과 UI 위/정지 중 강조 해제.
- `Assets/Defense/Runtime/Placement/TowerPlacementSlot.cs`: available 색상 유지, hovered/occupied 색상, 점유 우선 표시.
- `Assets/Defense/Runtime/Enemies/EnemyHealth.cs`: 초기화·피해 시 Changed(current, maximum) 이벤트.
- `Assets/Defense/Data/TestStage.asset`: 사용 가능한 목록을 TestTurret1/2/3으로 변경.
- `Assets/Defense/Prefabs/BasicEnemy.prefab`: EnemyHealthBar 컴포넌트 연결.
- `Assets/Defense/Editor/PrototypeBuilder.cs`: 초기 생성 시 동일한 세 정의 및 HP 바 연결 생성.
- `Assets/Defense/Editor/PrototypeValidation.cs`: 새 UI·표시·세 포탑 전투 검증 추가.
- `README.md`, `Documentation/Architecture.md`, `Documentation/SceneSetup.md`, `Documentation/Validation.md`, `Documentation/ValidationResult.txt`: 현재 구현·설정·검증 결과 반영.

기존 Main.md/Tower.md/enemy.md 원문, BasicTurret 프리팹과 원래 BasicTurret.asset을 보존했습니다. 원래 BasicTurret.asset은 현재 스테이지 메뉴에서만 제외했습니다.

## 공통 구현 재사용

| 정의 | 피해 | 범위 | 초당 공격 | AttackCooldown | 비용 |
|---|---:|---:|---:|---:|---:|
| Test Turret 1 | 10 | 3 | 1 | 1 | 50 |
| Test Turret 2 | 30 | 5 | 0.4 | 2.5 | 75 |
| Test Turret 3 | 5 | 2.5 | 3 | 0.3333333 | 100 |

세 정의 모두 기존 BasicTurret 프리팹을 참조합니다. `BasicTurret : Turret`, `ClosestTargeting`, `DirectDamageAttack`의 코드는 복제하거나 변경하지 않았습니다. 공통 Turret.Initialize가 선택한 정의를 전달받아 자신의 피해·범위·쿨다운을 사용합니다. 플레이 가능한 종류는 데이터 에셋 세 개로 표현되며 새로운 subclass 세 개를 만들지 않았습니다.

## UI → 배치 통신

`Turrets`는 패널의 표시 상태만 토글합니다. 옵션 버튼은 `placement.Select(definition)`을 호출하고 패널을 닫습니다. SelectionChanged 이벤트로 Selected 라벨과 항목 강조를 갱신합니다. 선택은 설치 이후에도 유지됩니다.

월드 클릭, 슬롯 검증, 자원 확인, 포탑 생성, 비용 차감은 TurretPlacementController가 담당합니다. UI 버튼에서는 생성/피해/공격을 수행하지 않습니다. 자원 부족 시 기존 `Not enough resources.`와 실패 시 미차감 동작을 유지합니다.

## 실제 검증 및 한계

Unity 6000.6.0f1 Play Mode에서 **117개 assertion 통과**. 원본에서 열려 있는 Editor를 방해하지 않도록 임시 프로젝트 복사본을 사용했습니다.

- 활성 uGUI Button의 onClick으로 메뉴 토글·세 정의 선택·선택 라벨 유지 검증.
- 각 선택 포탑 실제 설치 후 해당 비용 차감 확인.
- 각 포탑의 범위 바로 바깥에 정지한 적을 두어 공격하지 않는지 확인하고, 범위 안으로 옮겨 실제 피해 이벤트 3회와 공격 간격 확인.
- 슬롯 SetHovered 호출 후 기본/강조/점유 색상과 점유 우선 확인.
- 적 HP 바의 초기 비율·피해 반영·죽을 때 즉시 숨김 확인.
- 기존 설치 실패, 자원 보상, Pause, Game Over, 씬 재진입 흐름 회귀 검증.

macOS Computer Use 권한이 없어 실제 마우스/키보드 입력과 GPU 렌더링 외형은 직접 검증하지 못했습니다. 자동 테스트는 onClick/TryPlace/SetHovered와 실제 Play Mode 프레임을 사용합니다. 다양한 화면 비율과 standalone player build는 미검증입니다. Unity Editor SearchDatabase의 초기 인덱싱 예외는 게임 코드 검증에서 분리해 기록했습니다.
