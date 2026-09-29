# Channel Zero 구현 차이 보고서 (2026-09-27)

기준 문서: `ChannelZero_CoreMechanicsRules`, `ChannelZero_PuzzleStateSpec_Chapter1_2`,
`ChannelZero_GameplayUXHintItemSpec`, `ChannelZero_AudioPresentationSpec`,
`ChannelZero_30PuzzleMasterList`.

## 현재 확인된 일치 항목

- 퍼즐은 대표 연도 하나에서만 입력 UI가 열리고 다른 연도에서는 관찰 텍스트를 사용한다.
- 획득 플래그와 최종 해결 플래그가 분리되어 있다.
- `Screwdriver`는 CRT 후면 사용 후에도 유지되며 의료품은 중복 획득되지 않는다.
- REC 스냅샷은 방·연도별로 교체 저장되고 REW는 지식 상태를 보존한다.
- 제1·2장 핵심 퍼즐 흐름은 EditMode 테스트로 검증되어 있다.
- 이번 변경으로 실제 씬 ID와 REC/LOAD/REW 라우트가 `Living_REC`로 통일되었다.

## P0 차이와 영향도

### 1. LOAD 의미 충돌 — W01 반영 완료

`ChannelZeroComparisonOverlayService`를 추가해 LOAD를 읽기 전용 비교 세션으로 분리했다.
`ChannelZeroTimelineService.Load()`의 구형 API도 더 이상 물리 상태를 적용하지 않는다.
REC/REW용 `timelineSnapshots`와 저장 버전 3 포맷은 변경하지 않았다.

호환 결정:

- 기존 저장 파일의 `timelineSnapshots`는 REC 기록 및 REW 체크포인트로 계속 사용한다.
- 비교 레이어의 열림 상태는 `TransientState`로 보고 저장하지 않는다.
- LOAD 화면은 퍼즐 액션을 만들지 않으며 닫기/BACK만 허용한다.

남은 작업은 실제 기록 이미지를 반투명 렌더링하고 황동선/적색 노이즈 차이 마커를 연결하는 것이다.

### 2. 퍼즐 상태 머신

`PuzzleLifecycleState`와 `ChannelZeroPuzzleStateCalculator`를 추가해 기존 문자열 상태·플래그를
`Locked/Available/InProgress/Solved`로 계산한다. 기존 퍼즐 JSON은 변경하지 않아 호환된다.
`checkpointPolicy`, `inputSchema`, `wrongFeedbackTextId`의 데이터 일반화는 아직 남아 있다.

### 3. ContextTray

현재 `PuzzleView.actions`를 공통 버튼으로 그린다. `SequenceGrid`, `RotaryDigits`, `ItemSlots`,
`NodeLink`, `PageArrange`, `RecordMixer` 모듈 선택 계층은 아직 없다. 기존 퍼즐 로직을 유지한 채
`IPuzzleInputModule`과 모듈 팩토리를 먼저 추가하고, 화면은 퍼즐별로 순차 교체해야 한다.

### 4. 힌트 단계

현재 힌트 배열과 수동 단계 증가는 저장되지만, 단계 0~4의 공통 의미, 오답 횟수,
정체 시간, 자동 제안 여부는 저장하지 않는다. 정답 공개인 4단계는 명시 요청에서만 열리도록
별도 API가 필요하다.

### 5. 아이템 수명주기와 HOLD

영구 도구의 비소모 처리는 있으나 `PermanentTool/Consumable/Evidence/HoldCore/Synthesis`
분류와 `canHold`, 성공 시 소모 정책이 데이터 필드로 일반화되어 있지 않다. 저장에는 인벤토리와
단일 HOLD 슬롯이 있으므로 메타데이터 확장 후 기존 저장과 호환 가능하다.

### 6. 접근성

자막 크기, 자막 배경, 화자명, CRT 플래시·노이즈·흔들림 강도, 길게 누르기 토글 옵션의
저장 필드와 UI 훅이 없다. 화면 연출 서비스가 이 값을 조회하도록 설정 모델부터 추가해야 한다.

### 7. 오디오 Cue

현재 막 완료음은 런타임 생성 톤이며 공식 Cue ID 라우터가 없다. `IAudioCuePlayer`와
`SFX_UI_*`, `SFX_PUZZLE_*`, `SFX_REC_*`, `SFX_REW`, `SFX_DOOR_LOCK` ID 이벤트를 먼저 연결하면
실제 WAV가 없어도 구현과 리소스 제작을 분리할 수 있다.

## 이번 검증 결과

- EditMode: 51/51 통과
- PlayMode: 3/3 통과
- 자동 검사 항목: REC/LOAD/REW 실제 버튼 라우팅, 콘텐츠 그래프, 클로즈업 마스크와 UI 경계 구조
- 수동 미검증: Unity 에디터 실제 플레이 완주, 16:9/16:10/울트라와이드 육안 가독성,
  실제 오디오·플래시 감각

증거: `Logs/ChannelZeroValidation/2026-09-27-structure-1-7/`
