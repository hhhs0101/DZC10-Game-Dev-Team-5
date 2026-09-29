# 검증 결과 — Card / Elixir / Skill

Unity **6000.6.0f1**에서 Play Mode 자동 검증 **378개 assertion 통과**.
최종 결과는 `ValidationResult.txt`입니다. 원본에서 열려 있을 수 있는 Editor를 방해하지 않도록 Assets/Packages/ProjectSettings/Documentation을 `/tmp/defense-lobby-validation`에 복사해 실행했습니다.

기존 확장의 1-1~1-10 순서와 캐러셀 양 끝 경계, 15종 정의·참조 및 덱 편집기 목록, 추가 포탑을 T3에 드롭하는 동작도 확인했습니다. 최종 로그: `/tmp/defense-card-validation-final.log`.

## 최신 Card / Elixir / Skill 검증

기존 회귀 검사에 혼합 카드 덱, 마지막 Tower 보호, Available 11장 동기화, 두 스킬 시전/취소/범위 피해, 지연 효과, Elixir 공통 결제와 회복, 안내 fade, 표시 보간, 혼합 덱 Retry를 추가했습니다. 상세 assertion 범위와 파일 목록은 `CardElixirSkillUpdate.md`를 확인하세요.

자동 스킬 검사는 입력 처리 메서드와 현재 UI 위치의 실제 GraphicRaycaster 결과를 사용합니다. 원래 Hand 카드와 다른 UI의 취소는 자동으로 확인했지만, 실제 OS 마우스 down/hold/up 조작이나 렌더링을 수동으로 확인한 것은 아닙니다. 한국어 글리프 렌더링 및 다른 OS 폰트 fallback도 미검증입니다.

## 최신 패치 집중 검증

- 중복 초기 덱 생성 거부, controller와 PlayerDeck 양쪽에서 중복 교체 거부, 실패 시 데이터/이벤트 불변.
- Available = 전체 카탈로그 - 현재 덱. 24회 교체와 실제 드래그 이벤트 4회 반복에서 들어간 정의 제외·빠진 정의 복귀 및 6종 고유성 유지.
- 이미 덱에 들어가 숨겨진 drag source로 다시 드롭해도 중복 생성 불가.
- 20개 seed / 1,200회 순환에서 Hand 3종·전체 6종 고유성 유지. 의도적으로 손상시킨 사이클은 회전 전에 예외로 검출.
- 좌/우 stage Button 클릭이 A/D와 같은 단계 및 보간을 사용. 마우스 이동 중 키보드 retarget, 중앙 클릭 무동작, 애니메이션 중 반복 클릭 차단.
- Game Over의 Retry 실제 onClick 실행. 1-1과 1-2에서 HP/자원/timeScale/상태/스폰 복원, 이전 적·포탑 파괴, 슬롯·선택 초기화.
- Retry의 새로운 Cycle/카드 생성, 초기 Hand/Queue 분리, 원본 덱·진행도·선택 스테이지 보존. 1-2는 자체 초기 자원 175 사용.
- 성공 배치 순환, 실패/취소 미순환과 15종 전투 등 기존 회귀 테스트도 유지.

## 실제 검증한 것

### 모델

- 첫 stage만 해금, 잠긴 stage 완료 거부, 순서에 따른 다음 stage 해금.
- 키 반복 즉시 이동, 0.4초 초기 지연, 0.15초 반복, 방향 변경·해제.
- 6칸 덱의 유효하지 않은 교체 거부와 데이터 보존.
- 최초 Fisher–Yates의 정확히 5회 난수 호출.
- 6개 슬롯의 포탑 정의가 서로 고유하고 각각 한 번만 포함됨.
- 셔플 순서 앞 3장/뒤 3장의 정확한 Hand/Queue 분리.
- 서로 다른 선택 위치로 30회 순환한 결과 비교, 추가 난수 호출 없음.
- 덱 원본 불변, Hand가 아닌 카드 사용 거부, seed별 순서 변화.

### 실제 씬·컴포넌트 이벤트

- Start Game → Lobby, Settings/Back, Deck/Back to Lobby.
- Carousel visualIndex가 중간값을 거쳐 중앙에 정렬됨, locked entry 차단.
- Available 카드에 BeginDrag/EndDrag 실행 시 취소 불변.
- 실제 uGUI ExecuteEvents BeginDrag/Drop/EndDrag로 T3만 교체, 교체된 정의 사용 가능, 프리뷰 동기화.
- Deck에서 Settings 왕복, Lobby 선택/캐러셀 위치 유지.
- 전투 진입 시 동일 세션 덱 참조를 기반으로 새 카드 생성, 실제 Hand 버튼 3개.
- 선택 후 유효 슬롯에 실제 포탑 생성과 비용 차감, 사용한 identity의 정확한 순환.
- invalid/occupied/insufficient/cancelled 결과에서 Hand와 Queue 불변.
- 씬 종료·재입장·Deck 재개방 시 T1–T6 유지, 새 run의 새 Cycle/카드 객체.
- 명시적 완료 API 호출 시 다음 stage 진입 가능, Game Over는 해금하지 않음.

### 기존 게임 회귀

- 주기적 적 생성, Base HP와 초기 자원, 슬롯 호버/점유 색상.
- 피해/사망 HP 바, 적 정의별 처치 보상과 중복 지급 방지.
- 최근접 표적, 범위 이탈 시 재선택.
- 15종 포탑 실제 피해 이벤트 및 공격 간격, 정의별 비용/범위.
- Pause 중 위치·HP·적 수 정지, Settings에서 복귀 시 Paused 유지.
- Base 도착 시 피해 1회·보상 없음, Exit No/Yes, Game Over와 재진입 초기화.

## 검증 방식과 미검증

- 버튼은 실제 활성 uGUI Button.onClick을 호출했습니다.
- 드래그앤드롭은 실제 UI 컴포넌트에 ExecuteEvents와 PointerEventData를 전달했습니다.
- A/D와 Enter는 입력 처리 메서드 및 KeyRepeat에 값을 전달했습니다. 실제 OS 키보드 입력을 합성하거나 사람이 수동 조작한 것은 아닙니다.
- 전투·Pause 검증은 실제 Play Mode 프레임을 진행했습니다.
- macOS 화면 조작 권한이 없어 실제 마우스/키보드 end-to-end 및 화면 렌더링·다양한 해상도 레이아웃은 미검증입니다.
- 별도 player build, 앱 재실행에 의한 종료 동작은 미검증입니다. 영구 저장 자체는 구현하지 않았습니다.
- 완료 API는 테스트했지만 승리 조건은 없으므로 자연스러운 플레이 완료/해금 연출은 미구현입니다.
- 새 셔플이 항상 직전과 다르도록 강제하지 않습니다. 균등 무작위 특성상 같은 순서가 다시 나올 수 있습니다.
- Unity Editor SearchDatabase 초기 인덱싱에서 발생한 Editor 전용 예외는 기존 검증 도구에서 분리해 기록했습니다. 게임 코드의 Error/Exception/Assert는 실패로 처리합니다.

## 재실행

Play Mode를 종료한 뒤 `Defense → Run Prototype Validation (Play Mode)`를 실행합니다. 결과가 갱신되고 종료 시 Play Mode에서 나옵니다. 검증은 현재 Editor 세션의 테스트 덱/진행도를 변경하므로 실제 테스트 플레이와 분리해 실행하세요.

Unity 인스턴스가 프로젝트를 열고 있지 않다면:

```sh
/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath /Users/hyunseungjeon/Documents/GameDesign \
  -executeMethod Defense.Editor.PrototypeValidation.Run \
  -logFile /tmp/defense-lobby-validation.log
```

도구가 직접 종료하므로 `-quit`을 붙이지 않습니다. 성공 코드 0 / 실패 코드 1. 별도 테스트 패키지는 추가하지 않았습니다.
