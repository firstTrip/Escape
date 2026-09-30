# 《결번》 프롤로그~제2장 연도별 클로즈업 리소스 요청서

- 문서 상태: 제작 요청 v1.0
- 작성일: 2026-09-26
- 대상 연도: 1961 / 1981 / 2001 / 2021
- 대상 공간: 현관 / 거실 / 진우 작업실
- 기준 해상도: 1920×1080, 16:9
- 목적: 방 배경의 연도와 클로즈업 화면의 물체·노후도·배치가 끊기지 않도록 연도별 아트 변형을 제작한다.
- 범위 제한: 이 문서는 **신규 리소스 제작 요청서**다. 기존 승인 PNG, `.meta`, `ChannelZeroCloseupCatalog.asset`은 덮어쓰거나 교체하지 않는다.

## 1. 현재 구현에서 확인된 문제

- 방 배경은 거실과 작업실 모두 1961/1981/2001/2021 완성 화면이 이미 분리되어 있다.
- 클로즈업 카탈로그의 키는 현재 `closeupId + stateId`뿐이며 연도 키가 없다.
- `LIV-Z10` 가족사진과 `WKS-Z05` 수리 기록장은 네 시대를 각각 확인하는 퍼즐이지만, 현재 카탈로그에는 각각 `default` 이미지 한 장만 연결되어 있다.
- 대부분의 클로즈업이 2001 분위기의 단일 이미지라서 연도 전환 뒤 열면 방 배경과 재질·노후도·소품 위치가 어긋날 수 있다.
- 기존 PNG는 모두 1920×1080이다. 새 리소스도 같은 캔버스, 같은 안전 영역과 카메라 크롭을 유지한다.

새 이미지는 먼저 스테이징에서 검수하고, 런타임이 연도별 상태를 지원한 뒤 별도 반입한다. 기존 파일명에 같은 이름으로 저장하지 않는다.

## 2. 제작 원칙

1. 각 연도 `PlayRooms` 완성 배경을 필수 참조 이미지로 사용한다.
2. 동일 ID의 카메라 위치, 대상 크기, 소실점, 조작부 위치는 네 시대에서 고정한다. 연도 변경 시 물체가 순간 이동한 것처럼 보이면 안 된다.
3. 달라지는 요소는 시대에 맞는 재질 노후도, 주변 소품, 종이 변색, 수리 흔적, 부품 보급 상태로 한정한다.
4. 숫자, 한글, 날짜, 버튼 글자, 선택 테두리, 계기 바늘, 입력 상태는 가능한 한 TMP/Unity UI로 처리한다.
5. `default/open/acquired`처럼 구조가 실제로 바뀌는 경우만 상태 이미지를 나눈다.
6. 1961 전용 물건을 1981/2001/2021용으로 억지 제작하지 않는다. 해당 연도에는 핫스팟을 비활성화한다.
7. 시간 장치 UI인 `LIV-Z08`, `LIV-Z09`는 시대에 종속된 방 물건이 아니라 CRT 인터페이스의 고정 화면으로 취급한다. 연도별 4벌을 만들지 않는다.
8. 고어 표현은 금지한다. 손 부상은 작은 베임, 금속 가루, 소독 흔적, 붕대만 보여준다.

## 3. 파일명과 납품 구조

### 3.1 신규 파일명

```text
{id_lower}_{subject}_{year}_{state}_v01.png
```

예:

```text
liv_z10_family_photos_1961_default_v01.png
liv_z06_injured_hand_2001_disinfected_v01.png
wks_z02_parts_drawer_2021_open_v01.png
```

- `year`: `1961`, `1981`, `2001`, `2021`, 또는 시대 공통 UI만 `shared`
- `state`: 런타임 상태 ID와 같은 영문 소문자 사용
- 수정본은 기존 파일을 덮지 않고 `v02`, `v03`으로 증가
- 이미지 내부에 ID, 연도, 상태명을 굽지 않는다.

### 3.2 스테이징 권장 경로

```text
Staging/ChannelZero/Closeups_EraVariants_20260926/
├─ LivingRoom/{ID}/{Year}/
├─ Workshop/{ID}/{Year}/
├─ Shared/{ID}/
└─ QA/
```

승인 전에는 `Assets/Resources/ChannelZero/Closeups/`로 복사하지 않는다.

### 3.3 향후 Unity 상태 키

런타임 연결 시 다음처럼 연도와 상태를 결합한다.

```text
y1961_default
y1981_open
y2001_disinfected
y2021_acquired
shared_default
```

현재 카탈로그는 이 키를 자동 생성하지 않으므로, 본 요청 리소스를 반입할 때 코드/카탈로그 변경을 별도 작업으로 진행한다.

## 4. 연도 소유권 요약

| 구분 | 연도 | Closeup ID | 처리 |
|---|---|---|---|
| 프롤로그 문서 | 2001 | `PRO-Z01` | 기존 2001 전용 유지. 추가 제작 없음 |
| 거실 CRT 수리 사건 | 2001 | `LIV-Z01`, `LIV-Z02`, `LIV-Z03`, `LIV-Z06`, `LIV-Z07` | 2001 사건용 상태 보강 |
| 거실 의료함 | 1961 | `LIV-Z05` | 1961 전용 상태 3종 |
| 거실 시대 비교 | 네 시대 | `LIV-Z04`, `LIV-Z10`, `LIV-Z12` | 동일 구도 네 시대 변형 |
| CRT 시간 UI | 공통 | `LIV-Z08`, `LIV-Z09`, `LIV-Z11` | 연도 공통 화면. 상태만 보강 |
| 작업실 도면·크랭크 | 1961 | `WKS-Z03`, `WKS-I01` | 1961 전용 |
| 작업실 시대 비교 | 네 시대 | `WKS-Z01`, `WKS-Z02`, `WKS-Z05`, `WKS-Z06` | 동일 조작 좌표를 유지한 네 시대 변형 |
| 작업실 배선도 | 공통 문서 | `WKS-Z04` | 동일 문서 1벌. 정확한 글자는 TMP |

## 5. P0 제작 요청 — 게임 진행과 시대 비교 필수

### 5.1 `LIV-Z10` 시대별 가족사진

| 연도 | 파일 | 상태 | 필수 차이 |
|---|---|---|---|
| 1961 | `liv_z10_family_photos_1961_default_v01.png` | `default` | 가장 오래된 인화지, 젊은 부모, 초기 거실 가구. 미나는 같은 나이·얼굴 |
| 1981 | `liv_z10_family_photos_1981_default_v01.png` | `default` | 컬러 퇴색, 가족의 20년 노화, 1981 거실 소품 |
| 2001 | `liv_z10_family_photos_2001_default_v01.png` | `default` | 현재 거실과 직접 이어지는 액자·배경·의상 |
| 2021 | `liv_z10_family_photos_2021_default_v01.png` | `default` | 가장 최근 인화/재출력 흔적, 가족의 추가 노화, 미나는 동일 |

요구사항:

- 네 장 모두 사진 프레임과 인물 배치 기준점을 동일하게 유지한다.
- 미나의 얼굴·키·좌석은 네 시대에서 정확히 같아야 한다.
- 연도 표기와 뒷면 메모는 이미지에 넣지 않고 TMP로 합성한다.
- 기존 `liv_z10_family_photos_default_v01.png`는 참고 원본으로 보존한다.

### 5.2 `WKS-Z05` 진우의 시대별 수리 기록장

| 연도 | 파일 | 상태 | 페이지 시각 정보 |
|---|---|---|---|
| 1961 | `wks_z05_repair_journal_1961_default_v01.png` | `default` | 새 기록장, CRT 잔상과 잠금축 점검 흔적 |
| 1981 | `wks_z05_repair_journal_1981_default_v01.png` | `default` | 사용감 증가, 옷본의 네 점을 참고한 메모 공간 |
| 2001 | `wks_z05_repair_journal_2001_default_v01.png` | `default` | 찢긴 도면 조각이 끼워진 페이지, 기름때와 접힌 모서리 |
| 2021 | `wks_z05_repair_journal_2021_default_v01.png` | `default` | 습기·곰팡이 얼룩, 번진 잉크, 바닥 이음선 스케치 공간 |

요구사항:

- 책 표지·제본·페이지 크기는 같은 기록장이 시간에 따라 노후한 것으로 보여야 한다.
- 정확한 한국어 본문은 굽지 않는다. 문장 영역을 비워 두고 TMP로 합성한다.
- 종이 위 중요한 도형은 텍스트가 올라갈 안전 영역을 침범하지 않는다.

### 5.3 `LIV-Z04` 숫자 카펫

| 연도 | 파일 | 상태 |
|---|---|---|
| 1961 | `liv_z04_number_rug_1961_default_v01.png` | `default` |
| 1981 | `liv_z04_number_rug_1981_default_v01.png` | `default` |
| 2001 | `liv_z04_number_rug_2001_default_v01.png` | `default` |
| 2021 | `liv_z04_number_rug_2021_default_v01.png` | `default` |

- 3×3 칸의 중심점과 클릭 좌표는 모든 연도에서 동일해야 한다.
- 숫자 `0~9`, 입력 순서, 선택/오답/성공 표시는 이미지에 굽지 않는다.
- 시대가 흐르며 직물 퇴색, 닳은 자리, 수선 흔적만 누적한다.

### 5.4 `LIV-Z12` 8시 8분 괘종시계

| 연도 | 파일 | 상태 |
|---|---|---|
| 1961 | `liv_z12_grandfather_clock_1961_default_v01.png` | `default` |
| 1981 | `liv_z12_grandfather_clock_1981_default_v01.png` | `default` |
| 2001 | `liv_z12_grandfather_clock_2001_default_v01.png` | `default` |
| 2021 | `liv_z12_grandfather_clock_2021_default_v01.png` | `default` |

- 같은 시계가 낡아 가는 흐름을 유지하며 교체품처럼 새 디자인하지 않는다.
- 문자판과 바늘 회전축 위치는 고정한다. 바늘 강조는 UI로 처리한다.
- 네 시대 모두 8:08 정지 상태가 읽혀야 한다.

### 5.5 `WKS-Z01` 진공관 시험기

| 연도 | 파일 | 상태 | 시대 차이 |
|---|---|---|---|
| 1961 | `wks_z01_tube_tester_1961_default_v01.png` | `default` | 비교적 새 장비, 깨끗한 눈금판 |
| 1981 | `wks_z01_tube_tester_1981_default_v01.png` | `default` | 손때와 정비 라벨 흔적 |
| 2001 | `wks_z01_tube_tester_2001_default_v01.png` | `default` | 산화·스크래치·교체 노브 |
| 2021 | `wks_z01_tube_tester_2021_default_v01.png` | `default` | 먼지·부식·오래된 케이블 피복 |

- 소켓, 스위치, 계기판, 관 장착 위치는 네 장에서 픽셀 기준으로 최대한 동일하게 유지한다.
- `measuring/normal/faulty`는 별도 완성 그림을 만들지 않고 계기 바늘·램프 UI 오버레이로 처리한다.

### 5.6 `WKS-Z02` 부품 서랍

네 시대 각각 아래 3개 상태를 제작한다. 총 12장이다.

```text
wks_z02_parts_drawer_{year}_default_v01.png
wks_z02_parts_drawer_{year}_open_v01.png
wks_z02_parts_drawer_{year}_acquired_v01.png
```

- `default`: 닫힌 서랍
- `open`: 정상 후보/그을린 후보 진공관이 보이는 상태
- `acquired`: 두 후보를 챙긴 뒤 빈 완충 홈이 남은 상태
- 서랍 손잡이와 클릭 영역은 네 시대에서 고정한다.
- 종이 라벨의 글자는 읽을 수 없는 흔적으로만 표현한다.

### 5.7 `WKS-Z06` 열쇠 절삭기

| 연도 | 파일 | 상태 |
|---|---|---|
| 1961 | `wks_z06_key_cutter_1961_default_v01.png` | `default` |
| 1981 | `wks_z06_key_cutter_1981_default_v01.png` | `default` |
| 2001 | `wks_z06_key_cutter_2001_default_v01.png` | `default` |
| 2021 | `wks_z06_key_cutter_2021_default_v01.png` | `default` |

- 같은 수동 절삭기가 노후한 흐름으로 보이게 한다.
- 고정구, 손잡이, 절삭 휠 위치는 동일하게 유지한다.
- 제2장에서는 실제 절삭 상태를 만들지 않는다.

## 6. P0 제작 요청 — 특정 연도 사건 상태

### 6.1 2001 CRT 전면 `LIV-Z01`

| 파일 | 상태 | 화면 |
|---|---|---|
| `liv_z01_crt_front_2001_off_v01.png` | `off` | 전원 플러그가 빠진 기본 CRT |
| `liv_z01_crt_front_2001_noise_v01.png` | `noise` | 백색 잡음이 흐르는 상태 |
| `liv_z01_crt_front_2001_mina_v01.png` | `mina` | 화면 속 미나가 고정된 상태 |

- 현재 `liv_z01_crt_front_off_v01.png`의 목재 프레임, 다이얼, 스피커 비율을 그대로 기준으로 삼는다.
- CRT 유리 외곽과 화면 안전 영역은 세 상태에서 완전히 일치해야 한다.
- 채널 번호와 정확한 텍스트는 Unity UI로 처리한다.

### 6.2 2001 CRT 후면 `LIV-Z02`

| 파일 | 상태 |
|---|---|
| `liv_z02_crt_rear_2001_closed_v01.png` | `default` |
| `liv_z02_crt_rear_2001_open_v01.png` | `open` |
| `liv_z02_crt_rear_2001_tube_removed_v01.png` | `tube_removed` |
| `liv_z02_crt_rear_2001_replaced_v01.png` | `replaced` |
| `liv_z02_crt_rear_2001_bracket_removed_v01.png` | `bracket_removed` |

- 기존 5개 상태 이미지를 보호 원본으로 사용하고, 연도 명시 사본은 승인 뒤 신규 이름으로 납품한다.
- 나사, 진공관 소켓, 황동 브래킷 위치가 상태 전환 중 움직이지 않아야 한다.
- 최종 브래킷 제거 시 부상 원인은 전달하되 피·상처를 후면 화면에 과도하게 그리지 않는다.

### 6.3 2001 진공관 수리함 `LIV-Z03`

```text
liv_z03_tube_case_2001_default_v01.png
liv_z03_tube_case_2001_open_v01.png
liv_z03_tube_case_2001_acquired_v01.png
```

- 기존 닫힘/열림/획득 이미지를 동일 구도로 유지한다.
- 드라이버와 교체용 관의 위치는 아이템 획득 핫스팟과 분리 가능하게 충분히 떨어뜨린다.

### 6.4 1961 철제 의료함 `LIV-Z05`

```text
liv_z05_medical_cabinet_1961_default_v01.png
liv_z05_medical_cabinet_1961_open_v01.png
liv_z05_medical_cabinet_1961_acquired_v01.png
```

- `default`: 닫힘
- `open`: 소독 도구와 붕대가 각각 보임
- `acquired`: 사용 후 재획득 대상으로 오인하지 않도록 내부가 빈 상태
- 내용물은 UI 버튼 위치와 겹치지 않으며 서로 독립 클릭 가능해야 한다.

### 6.5 2001 진우의 손 치료 `LIV-Z06`

```text
liv_z06_injured_hand_2001_injured_v01.png
liv_z06_injured_hand_2001_disinfected_v01.png
liv_z06_injured_hand_2001_treated_v01.png
```

- `injured`: 금속 브래킷에 긁힌 작은 상처
- `disinfected`: 금속 가루와 오염이 닦인 상태
- `treated`: 붕대가 안정적으로 감긴 완료 상태
- 손의 자세, 화면 크롭, 조명은 세 장에서 동일해야 한다.

### 6.6 2001 네 자리 자물쇠 상자 `LIV-Z07`

```text
liv_z07_lockbox_2001_default_v01.png
liv_z07_lockbox_2001_open_v01.png
```

- 네 다이얼의 숫자와 선택 표시는 TMP/UI로 처리한다.
- 열림 상태에서도 상자의 위치와 카메라 구도는 유지한다.

### 6.7 1961 주택 보수 도면과 접이식 크랭크

| ID | 파일 | 상태 | 비고 |
|---|---|---|---|
| `WKS-Z03` | `wks_z03_floor_lock_plan_1961_default_v01.png` | `default` | 중앙 복도 네 잠금축과 소켓 도형. 정확한 문구는 TMP |
| `WKS-I01` | `wks_i01_folding_crank_1961_default_v01.png` | `default` | 작업실에서 발견한 상태 |
| `WKS-I01` | `wks_i01_folding_crank_1961_acquired_v01.png` | `acquired` | 빈 받침/보관 위치. 재획득 방지 시각 상태 |

1981/2001/2021에는 크랭크 클로즈업을 제작하지 않는다. 해당 핫스팟은 비활성화한다.

## 7. P1 제작 요청 — 공통 인터페이스와 문서

| ID | 신규 파일 | 상태 | 요청 |
|---|---|---|---|
| `LIV-Z08` | `liv_z08_rec_panel_shared_default_v01.png` | `default` | 버튼 글자 없는 패널 베이스. REC/PLAY/LOAD/REW/HOLD는 TMP/UI |
| `LIV-Z08` | `liv_z08_rec_panel_shared_active_v01.png` | `active` | 전원/기록 램프만 구조적으로 점등 |
| `LIV-Z09` | `liv_z09_tape_slots_shared_default_v01.png` | `default` | 빈 슬롯 베이스. 테이프 A/B/C/MASTER는 UI 아이콘 |
| `LIV-Z09` | `liv_z09_tape_slots_shared_complete_v01.png` | `complete` | MASTER 합성 완료 구조 상태가 필요할 때만 제작 |
| `LIV-Z11` | `liv_z11_wiring_diagram_shared_default_v01.png` | `default` | CRT 신호 경로용 낡은 도면. 정확한 기호 설명은 TMP |
| `WKS-Z04` | `wks_z04_power_wiring_shared_default_v01.png` | `default` | CRT와 바닥 장치의 연결 도면. 정확한 문구는 TMP |

이 그룹은 시간 이동 중에도 동일한 UI/기록을 호출하므로 종이 노후도나 프레임을 연도마다 바꾸지 않는다.

## 8. 제작하지 않을 연도 변형

| ID | 제외 연도 | 이유 |
|---|---|---|
| `PRO-Z01` | 1961/1981/2021 | 서비스 요청서는 2001-10-10 프롤로그 전용 |
| `LIV-Z02`, `LIV-Z03`, `LIV-Z06`, `LIV-Z07` | 1961/1981/2021 | CRT 수리와 손 부상은 2001 사건으로 고정 |
| `LIV-Z05` | 1981/2001/2021 | 의료함 획득은 1961 사건으로 고정 |
| `WKS-Z03`, `WKS-I01` | 1981/2001/2021 | 도면 확인과 크랭크 획득은 1961 전용 |
| `LIV-Z08`, `LIV-Z09`, `LIV-Z11`, `WKS-Z04` | 연도별 4벌 전체 | 시대 공통 시간 UI 또는 동일 문서로 처리 |

연도 전용 대상이 아닌 시점에 클릭됐을 때 다른 연도 이미지를 재사용하지 않는다. 핫스팟을 숨기거나 “지금은 여기서 할 게 없다” 피드백만 표시한다.

## 9. 화면 연속성 체크리스트

각 파일은 같은 연도의 방 배경과 나란히 놓고 다음을 확인한다.

- 대상 물체의 목재 색, 금속 산화, 천 무늬, 손잡이·노브 형태가 방 배경과 같은가?
- 배경에서 왼쪽에 있던 손상/장식이 클로즈업에서 반대로 이동하지 않았는가?
- 광원 방향과 그림자 방향이 해당 연도 방 배경과 같은가?
- 카메라가 가까워졌을 뿐, 물체 비율이나 설계가 새로 바뀌지 않았는가?
- 네 시대 변형에서 상호작용 좌표가 유지되는가?
- 상태 전환 이미지끼리 고정 부품이 흔들리거나 크기가 달라지지 않는가?
- 정확한 숫자·한글이 이미지에 잘못 생성되지 않았는가?
- 하단 TMP 대화 영역과 오른쪽 CRT 인벤토리 영역에 중요한 단서가 가려지지 않는가?

## 10. 우선순위와 납품 배치

### Batch A — 시대 비교 핵심 P0

1. `LIV-Z10` 4장
2. `WKS-Z05` 4장
3. `LIV-Z04` 4장
4. `LIV-Z12` 4장

목적: 네 시대를 전환했을 때 실제로 다른 정보를 읽을 수 있게 한다.

### Batch B — 현재 진행 막힘 방지 P0

1. `LIV-Z05` 1961 상태 3장
2. `LIV-Z06` 2001 상태 3장
3. `LIV-Z01` 2001 상태 3장
4. `LIV-Z02` 2001 상태 5장
5. `LIV-Z03` 2001 상태 3장
6. `LIV-Z07` 2001 상태 2장

목적: CRT 수리 → 부상 → 소독 → 붕대와 아이템 획득 상태를 시각적으로 일치시킨다.

### Batch C — 작업실 연도 배치 P0

1. `WKS-Z01` 4장
2. `WKS-Z02` 12장
3. `WKS-Z06` 4장
4. `WKS-Z03` 1961 1장
5. `WKS-I01` 1961 2장

### Batch D — 공통 UI P1

1. `LIV-Z08`, `LIV-Z09`, `LIV-Z11`
2. `WKS-Z04`

공통 UI는 기존 이미지로 기능 검증이 가능하므로 시대별 비교 리소스보다 후순위다.

## 11. 검수와 승인 기준

1. 스테이징 파일명과 연도/상태가 본 문서의 매트릭스와 일치한다.
2. 기존 승인 리소스의 파일 내용, GUID, `.meta`가 변경되지 않았다.
3. 각 이미지가 1920×1080 PNG이며 불필요한 글자, 워터마크, 생성 아티팩트가 없다.
4. 같은 ID의 네 시대 이미지를 빠르게 넘겼을 때 조작부가 흔들리지 않는다.
5. 각 연도 `PlayRooms` 배경과 클로즈업을 나란히 비교한 QA 시트를 제공한다.
6. 사용자 승인 전 Unity 카탈로그에 연결하지 않는다.
7. 승인 후에도 이미지 반입, 카탈로그 연결, 핫스팟 연도 조건, Play Mode 검증을 별도 완료 항목으로 기록한다.

## 12. 예상 신규 수량

| 배치 | 예상 PNG |
|---|---:|
| Batch A | 16 |
| Batch B | 19 |
| Batch C | 23 |
| Batch D | 6 내외 |
| 합계 | 약 64 |

수량이 부담되면 `Batch A → Batch B → Batch C → Batch D` 순서로 제작한다. 계기 바늘, 버튼 점등, 숫자, 정확한 문구를 Unity UI로 분리하면 상태 이미지 증가를 더 줄일 수 있다.
