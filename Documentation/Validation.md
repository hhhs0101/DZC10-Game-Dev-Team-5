# 검증

## 실행 결과

Unity 6000.6.0f1에서 실제 Scene을 로드하는 Play Mode 자동 검증 **117개 assertion 통과**.
결과: `ValidationResult.txt`.

이번 검증은 열려 있는 Editor를 방해하지 않도록 Assets/Packages/ProjectSettings를 임시 프로젝트에 복사하여 실행했습니다.

검증 범위:

- 기본 닫힘, Turrets 토글 열기/닫기, 기존 Basic Turret 버튼 제거
- 세 옵션의 선택 전달, 패널 닫힘 후 선택 라벨 유지, 선택만으로는 자원 미사용
- 세 포탑 각각의 비용·범위 밖 공격 금지·실제 프레임 피해량·공격 간격
- 슬롯의 호버 진입/해제 및 점유 색상 우선
- 적 HP 바의 초기 100%, 피해 후 비율 갱신, 사망 즉시 숨김

- Main Menu의 Settings/Back, Start Game, Test Stage 전환
- fixed interval 적 생성 및 초기 자원/HP
- 유효 슬롯 설치와 정확한 비용 차감
- 잘못된 위치 / 점유 슬롯 / 부족한 자원 메시지 및 실패 시 잔액·슬롯 보존
- 최근접 거리 선택, 범위 이탈 재선택, 죽은 적 즉시 제외
- Pause 버튼 및 Resume 공통 상태 로직
- Pause 중 위치·HP·적 수 정지, 배치·스폰 요청 차단
- Settings 복귀 시 Paused 유지
- 경로 종점 도달 시 Base 피해가 정확히 한 번 적용
- 실제 프레임 진행에 따른 포탑 자동 공격 및 적 사망/제거
- 적 정의별 보상 7 / 23 / 0, 비치명 피해 미지급, 중복 사망 피해의 중복 보상 방지
- Base 도달·일반 제거 미지급, 포탑 처치 보상 지급 및 Resource HUD 갱신
- ResourceWallet의 0/음수 수급 요청으로 잔액이 감소하지 않음
- Base HP 소진 시 Game Over, 생성·배치·Resume 차단
- Game Over → Main Menu → 재입장 시 초기화
- Pause Exit → No / Yes 이동과 timeScale 복원

버튼 검증은 활성화된 실제 uGUI Button의 onClick을 호출합니다. 설치 검증은 TryPlace를 호출합니다. OS 마우스/키보드 입력을 모사한 end-to-end 검증은 아닙니다.

Unity 화면을 통한 입력·레이아웃 확인은 macOS Computer Use 권한이 없어 수행하지 못했습니다. 별도 플랫폼 player build도 실행하지 않았습니다. 자동 검증 통과를 해당 항목들의 검증 완료로 해석하지 않습니다.

## 재실행

Editor에서 Play Mode를 종료한 뒤 **Defense → Run Prototype Validation (Play Mode)**를 실행합니다. 검증 종료 시 Play Mode에서 나옵니다. 마지막 결과 파일을 갱신합니다.

터미널에서는 다른 Unity 인스턴스가 프로젝트를 열고 있지 않은 상태에서:

```sh
/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath /Users/hyunseungjeon/Documents/GameDesign \
  -executeMethod Defense.Editor.PrototypeValidation.Run \
  -logFile /tmp/defense-validation.log
```

이 검증은 스스로 종료하므로 `-quit`을 붙이지 않습니다. 성공 exit code는 0, 실패는 1입니다. 별도 테스트 패키지는 필요하지 않습니다.

초기 검증 과정에서 Unity Editor의 SearchDatabase 초기 인덱싱 자체에서 예외가 발생했습니다. 검증 도구는 이 명시적인 Editor Search 스택만 제외하며 게임 런타임의 Error/Exception/Assert는 검증 실패로 처리합니다. 이번 임시 프로젝트 초기화에서도 해당 Editor 인덱싱 예외가 발생했습니다. 이 Editor 전용 예외를 제외한 게임 코드 검증은 통과했습니다.

## 남은 수동 확인

1. MainMenu 씬 Play → 실제 마우스로 Start Game / Test Stage 선택.
2. 오른쪽 아래 Turrets 클릭 → Test Turret 1 선택 → 초록 슬롯 클릭. 파란 포탑 생성, Resources 150 → 100. Turrets 재클릭으로 열기/닫기 확인.
3. 같은 슬롯 클릭: `A turret is already placed here.`
4. 회색 길/빈 공간 클릭: `You cannot place a turret here.`
5. 총 세 개 설치 후 빈 슬롯 클릭: `Not enough resources.`
6. UI 버튼을 눌렀을 때 월드에 포탑이 추가로 생성되지 않는지 확인.
7. Game View에 포커스를 준 뒤 Esc → 정지, Esc → 재개. Pause 버튼도 같은 동작.
8. Pause → Settings → Back, Exit → No에서는 정지 유지.
9. Pause → Exit → Yes, Game Over → Main Menu의 화면 이동 확인.
10. 메인 메뉴 Exit이 에디터에서 Play Mode를 종료하는지 확인. 별도 빌드를 만든 경우 앱 종료 확인.
11. 16:9 Game View에서 HP·자원·Pause·설치 버튼과 슬롯이 잘 보이는지 확인.

최종 그래픽 및 다양한 화면 비율 최적화는 이 prototype 범위에 포함하지 않습니다.

이번 슬롯 검증은 SetHovered 호출 후 SpriteRenderer 색상 확인입니다. HP 바 검증은 EnemyHealthBar 상태와 비율을 확인합니다. 실제 포인터 hit-test, GPU로 그려진 HP 바/패널 배치와 다양한 해상도의 가독성은 직접 확인하지 못했습니다.
