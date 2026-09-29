# Unity 2D Defense — Minimum Playable Prototype

Unity **6000.6.0f1** 프로젝트입니다. `Documentation/Main.md`, `Tower.md`, `enemy.md`와 사용자의 현재 구현 범위를 기준으로 작성했습니다. 작업 시작 시 프로젝트 폴더는 비어 있었습니다.

## 실행

1. Unity Hub에서 이 `GameDesign` 폴더를 프로젝트로 추가하고 Unity 6000.6.0f1로 엽니다.
2. `Assets/Defense/Scenes/MainMenu.unity`를 열고 **Play**를 누릅니다.
3. **Start Game → Test Stage**를 선택합니다.
4. 오른쪽 아래 **Turrets**를 눌러 선택 패널을 열고 **Test Turret 1/2/3** 중 하나를 선택한 뒤 초록색 설치 슬롯을 클릭합니다. 선택은 설치 후에도 유지되며 화면 하단에 이름과 비용이 표시됩니다. Turrets를 다시 누르면 패널이 닫힙니다.
5. 우측 상단 **Pause** 또는 **Esc**로 정지/재개합니다.

두 Scene, 프리팹, ScriptableObject, Build Settings와 Inspector 연결은 이미 생성되어 있습니다. 별도 설정이 필요하지 않습니다. 설정 화면은 placeholder이며 실제 설정을 변경하지 않습니다.

- 빨간 사각형: 적 (머리 위에 현재 HP 비율 표시)
- 파란 작은 사각형: 설치한 포탑
- 초록색 사각형: 사용 가능한 설치 슬롯 (8개). 호버 시 밝아지고, 설치 후 갈색으로 변경
- 오른쪽 파란 큰 사각형: Base
- 회색 선: 이동 경로

## 기본 테스트 값

| 설정 | 값 | 수정할 에셋 |
|---|---:|---|
| Base HP | 10 | `Assets/Defense/Data/TestStage.asset` |
| 시작 자원 | 150 | 동일 |
| 생성 간격 | 2초, 첫 생성도 2초 후 | 동일 |
| 적 HP / 속도 / Base 피해 | 40 / 1.5 / 1 | `Assets/Defense/Data/BasicEnemy.asset` |
| Test Turret 1 비용 / 피해 / 범위 / 쿨다운 | 50 / 10 / 3 / 1초 | `Assets/Defense/Data/TestTurret1.asset` |
| Test Turret 2 비용 / 피해 / 범위 / 쿨다운 | 75 / 30 / 5 / 2.5초 | `Assets/Defense/Data/TestTurret2.asset` |
| Test Turret 3 비용 / 피해 / 범위 / 쿨다운 | 100 / 5 / 2.5 / 약 0.333초 | `Assets/Defense/Data/TestTurret3.asset` |

적을 처치하면 해당 EnemyDefinition의 Resource Reward만큼 자원을 얻습니다. BasicEnemy의 기본 보상은 10이며, Base 도달·일반 제거에는 보상이 없습니다. 시간에 따른 자동 수급과 승리 조건은 없습니다. 적은 계속 생성됩니다. 처음 주어진 자원으로 Test Turret 1은 3개를 설치할 수 있으며, 선택한 종류에 따라 설치 가능 수가 달라집니다. 아무것도 설치하지 않으면 적이 Base에 도달하여 Game Over를 테스트할 수 있습니다.

## 적별 처치 보상 설정

`Assets/Defense/Data/BasicEnemy.asset`을 선택하고 Inspector의 **Resource Reward**를 변경하세요. 0은 보상 없음입니다. 다른 적은 별도의 EnemyDefinition 에셋을 만들어 각각 다른 값을 설정할 수 있습니다. 각 적은 스폰 시 보상 값을 저장하며 처치 시 한 번만 지급합니다. Resource HUD는 자동 갱신됩니다.

## 구조 및 설정 문서

- `Documentation/Architecture.md`: 책임, 상속·조합 구조, 통신과 확장 지점
- `Documentation/SceneSetup.md`: Scene/GameObject/Prefab/Inspector 연결 및 새 스테이지 구성
- `Documentation/Validation.md`: 자동 검증 실행 방법과 수동 확인 항목
- `Documentation/ValidationResult.txt`: 마지막 자동 검증 결과

`Defense → Create Missing Prototype Assets`는 초기 에셋 생성용 Editor 도구입니다. Scene이 이미 있으면 수정을 보호하기 위해 실행을 건너뜁니다. 평소에는 다시 실행하지 않습니다.

## 의존성

Unity에 포함된 **uGUI 2.6.0**과 기본 모듈만 사용합니다. 외부 패키지, 새로운 Input System, TMP, 렌더 파이프라인 패키지는 추가하지 않았습니다. 입력은 Unity의 기본 Input Manager를 사용하며 프로젝트 설정이 이에 맞춰져 있습니다.

프로젝트 데이터와 `.meta` 파일을 함께 보관하세요. `Library`, `Temp`, `Logs`는 캐시이며 버전 관리 대상이 아닙니다.

테스트 포탑·빌드 메뉴·HP 바 변경 내역: `Documentation/TurretUIUpdate.md`.
