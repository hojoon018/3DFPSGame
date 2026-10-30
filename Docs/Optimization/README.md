# 렌더링 최적화 결과 (오클루전 컬링 / LOD Group / Mesh LODs)

- 측정일: 2026-10-30, Unity 6000.3.22f1 (URP, PC_RPAsset), GameScene
- 측정 방법: 에디터 Play 모드, Game View `UnityStats` 값 (Draw Calls / SetPass / Tris / Verts / Shadow Casters)
- 측정 조건(Before/After 동일):
  - ZombieSpawner 비활성 + 좀비 제거 (랜덤 요소 제거)
  - MinimapCamera 비활성 (메인 카메라만 비교)
  - V2~V5는 카메라 스크립트를 끄고 같은 좌표·각도로 고정
- 화면 좌상단 오버레이는 Play 모드 중에만 런타임으로 만든 측정용 UI이며 씬에 저장되지 않았다.
- 에디터는 백그라운드 자동 틱(약 60Hz 제한) 상태여서 프레임 시간은 신뢰할 수 없다. 그래서 렌더링 카운터만 비교한다.

## 1. 시점별 Before → After

| 시점 | Draw Calls | SetPass | Tris | Verts | Shadow Casters |
|---|---|---|---|---|---|
| V1 1인칭 시작 | 189 → 180 | 52 → 52 | 3548.8k → 3544.4k (≈0%) | 2486.1k → 2478.1k | 112 → 100 |
| V2 골목 | 544 → 549 | 51 → 51 | 2603.2k → **139.3k (-94.6%)** | 1714.3k → 304.6k | 336 → 332 |
| V3 맵 외곽 | 704 → **367 (-47.9%)** | 53 → 50 | 3143.0k → **82.4k (-97.4%)** | 2140.3k → 171.6k | 178 → 153 |
| V4 상공 조감 | 663 → 624 | 44 → 44 | 2123.5k → **121.6k (-94.3%)** | 1479.5k → 231.9k | 0 → 0 |

스크린샷: `Before/V1~V4_*.png`, `After/V1~V4_*.png` (화면이 육안상 동일한지 함께 비교)

## 2. 기법별 효과를 분리한 A/B (After 상태, 같은 프레임 조건)

| 실험 | OFF / 강제 | ON / 자동 | 증거 |
|---|---|---|---|
| 오클루전 컬링, V5 격납고 벽 정면 | Draw 707, Tris 187.5k, Shadow Casters 213 | Draw **44 (-93.8%)**, Tris **5.5k**, Shadow Casters **4** | `After/V5_Wall_OcclusionOFF.png` / `ON.png` |
| Mesh LOD, 플레이어 캐릭터+검 23m | `forceMeshLod=0`: Tris **4082.4k** | 자동: Tris **277.0k (-93.2%)** | `After/MeshLOD_Player_ForceLOD0.png` / `Auto.png` |
| LOD Group, 컨테이너 | 근거리 11~13m: LOD0(LD2close, 132 tris) | 원거리 57m: LOD1(LD1close, 12 tris), 빨강으로 표시 | `After/LOD_Container_Near.png` / `Far_LOD1Red.png` |

## 3. 적용 내용

### 오클루전 컬링
- Occluder Static은 높이 2.5m 이상, 가로 4m 이상, 불투명인 오브젝트 **116개**에만 남겼다 (격납고, 콘크리트 벽, 컨테이너, 탱크 등).
- 나머지 **286개**(작은 소품, 바닥, 반투명 철망, 파이프 세트)는 Occludee Static만 유지했다.
- 베이크 설정: Smallest Occluder 3, Smallest Hole 0.25, Backface Threshold 100. 결과 데이터는 `Assets/01.Scenes/GameScene/OcclusionCullingData.asset` (약 250KB).

### LOD Group (에셋 팩의 수제 LOD 활용, 씬 인스턴스에 추가하고 프리팹 원본은 수정하지 않음)
- `Cargo_container_v1_LD2close` 14개: LOD0 LD2close(132 tris) → 화면 높이 0.3 → LOD1 LD1close(12 tris) → 0.02 → Culled
- `Barrel_v1_LD1`, `Wooden_box_v1_LD1`, `Wooden_box_v1_LD1square` 51개: LOD0(현재 메시) → 0.015 → Culled. 가까이서 보이는 모습은 그대로다.

### Mesh LODs (ModelImporter → Generate Mesh LODs)
- `Anime Shinigami`(플레이어, 500k tris, 15단계), `Black Broadsword`(검, 500k tris, 16단계)
- `Hangar_v2/v3/v4`, `Oil_tank_v2`, `Palet_v1`, `Pipes_set_v1`
- `Oil_tank_v1`, `Industrial_pipe_v1`, `Concrete_fence_v1`은 폴리곤이 너무 적어 LOD가 생성되지 않아서 원래대로 되돌렸다.

## 4. 해석 및 남은 과제
- 개선분의 대부분은 **50만 tris짜리 플레이어 캐릭터와 검의 Mesh LOD**에서 나온다. 맵 소품은 원래 로우폴리라서 LOD의 절대 효과가 작다.
- **V1(1인칭)은 거의 변화가 없다.** 캐릭터와 검이 카메라 바로 옆에 있어서 메인 패스와 그림자 캐스케이드(4단계) 모두 LOD0으로 그려지기 때문이다.
  - 1인칭에서 최대 비용이 그대로 남아 있다. 다음 후보: 캐릭터 메시 경량화(임포트 단계 단순화), 1인칭에서 캐릭터 그림자 끄기 또는 `meshLodSelectionBias` 조정.
- 오클루전 컬링은 시야가 막힌 곳(V5)에서 효과가 크다. 시야가 트인 곳(V2 골목, V4 조감)에서는 Draw Call 감소가 거의 없다.
- 컨테이너의 LOD0→1 전환값은 After V1~V4 측정 뒤에 0.15에서 0.3으로 조정했다. 영향은 컨테이너 14개 × 약 120 tris 규모라서 표의 수치와 사실상 같다.
