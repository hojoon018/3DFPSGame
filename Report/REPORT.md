# AI와 함께하는 그래픽 품질 개선 및 최적화 — 3D FPS (Unity 6 / URP)

> 프로젝트: 3DFPSGame (Unity 6000.3.22f1, URP, Forward+) · 씬: `GameScene` (산업 단지 맵, 약 150m × 150m)
> 작업 방식: Claude Code + Unity MCP로 씬/에셋 설정, 스크립트 수정, Lightmap Bake, 화면·Stats·Profiler 캡처, 수치 측정을 수행.
> 모든 수치와 스크린샷은 `Report/` 폴더에 있음 (상세 수치: `metrics.md`)

---

## 0. 목표와 측정 방법

**목표 분위기**: 밝고 허름한 "폐허" → 어둡고 피 튀기는 **전장**. 전체는 어둡지만, 어두운 골목의 가로등처럼 **띄엄띄엄 공간을 비추는 조명**이 있어 너무 깜깜하지 않게.

**측정 조건 (모든 단계 동일)**
- 에디터 Play 모드, Quality = PC, Game 뷰 1920×1080, VSync Off
- 시나리오: Play 진입 → 2초 대기 → Profiler 기록 시작 → 스폰 지점에서 입력 없이 약 6~8초(좀비 3마리 활동) → 일시정지 → 캡처
- 주 지표: Profiler에 기록된 마지막 600프레임의 평균값(프레임 시간, PlayerLoop, Batches, SetPass, Tris, Shadow Casters)
- 화면 비교: 4개 고정 지점(A 스폰 / B 공터 / C 골목 / D 드럼통 구역)을 같은 카메라(FOV 60, 1920×1080)로 렌더
- Baseline은 git에 커밋된 원래 씬을 임시로 열어 같은 방법으로 측정

**측정하면서 겪은 문제와 대응 (분석 포인트)**
- Unity가 백그라운드일 때 에디터가 프레임을 제한해 순간 FPS가 크게 흔들림 → 순간 FPS 대신 Profiler 평균값을 주 지표로 사용
- 같은 조건에서도 Play 직후 워밍업 프레임이 섞이면 8.41ms ↔ 5.03ms처럼 차이가 남 → 대기 시간을 늘리고, 실행마다 거의 같은 **렌더 카운터(Batches/Tris/Shadow Casters)를 우선 비교**
- 에디터 메모리(Total/Textures)는 세션 상태에 따라 크게 흔들림 → Profiler Rendering 모듈의 Used Textures / Render Textures로 비교

---

## 1. 조명 (Lighting)

### 적용 기술
| 항목 | Before | After |
|---|---|---|
| Directional Light | 흰색, 강도 2.0, Realtime | 붉은 해질녘 (1.0, 0.52, 0.33), 강도 0.8, 낮은 각도 14°, **Mixed** |
| Skybox | 기본 파란 하늘 | Procedural `Sky_Battlefield` (붉은 틴트, 대기 두께 1.35, 노출 0.42) |
| 환경광 | Skybox, 강도 1.0 | **Trilight** (하늘 보라-회색 / 지평선 갈색 / 바닥 어두운 갈색) |
| Fog | Off | **Exponential Squared**, 밀도 0.013, 갈색 연기색 |
| 국지 조명 | 없음 | **19개**: 나트륨 가로등 Spot 11(주황) · 드럼통 불빛 Point 5(주황-빨강) · 비상등 Point 3(빨강) + 연기 파티클 5 |
| Lightmap | 없음 | **Baked GI** (Progressive GPU, Shadowmask, 4 texel/unit, Bounce 2, AO) → Lightmap 3장 + Shadowmask 3장 (4.7MB) |
| Light Probe | 없음 | **584개** — 지면 레이캐스트로 6m 격자 × 높이 0.5/2.5m + 국지광 주변 링. 좀비/플레이어가 구워진 국지광 색을 받음 |

### 과정
1. **직접 배치 → 확인**: 국지광을 Realtime으로 먼저 배치. URP는 거리 제곱 감쇠라 처음 값(강도 6)은 거의 안 보임 → 70~90으로 조정
2. **Bake**: 국지광을 Baked로 전환하고 Light Probe 배치 (스폰 근처 가로등 3개만 Mixed로 남겨 실시간 그림자 유지)
3. **피드백 반영**: "너무 어두워서 안 보인다" → 메인 라이트 강도와 환경광을 올리고 재Bake

### 결과 화면 (왼쪽 Before / 오른쪽 최종)
- `compare_A_spawn.png`, `compare_B_yard.png`, `compare_C_alley.png`, `compare_D_fire.png`
- 단계별: `00_before_*` → `01_light_realtime_*` → `01_light_baked_*` → `02_pp_off_*`(조명만)

### 조명이 성능에 준 영향 (분석)
| 단계 | 프레임(ms) | Batches | Tris | Shadow Casters |
|---|---|---|---|---|
| Baseline | 4.84 | 175 | 3.65M | 703 |
| 국지광 Realtime (그림자 3개) | 5.44 | 403 | 5.81M | 1682 |
| Bake + Mixed 3개 | 5.03 | 380 | 5.77M | 1631 |

- 국지광을 Bake해도 Tris와 Shadow Casters가 거의 줄지 않음 → 원인은 "라이트 개수"가 아니라 **실시간 그림자를 그리는 Mixed 가로등 3개**. 그림자를 쓰는 Spot마다 그림자 맵을 위해 장면을 다시 그림 → 3단계 최적화의 핵심 근거가 됨

---

## 2. 포스트 프로세싱 (Post Processing)

### 적용 기술 (Global Volume)
| 효과 | 설정 | 의도 |
|---|---|---|
| Tonemapping | Neutral → **ACES** | 영화 같은 대비, 밝은 불빛을 부드럽게 압축 |
| Color Adjustments | Post Exposure +0.8, Contrast +8, Saturation −20, Color Filter 약한 붉은빛 | 어둡지만 보이게, 탁하고 건조한 전장 톤 |
| Shadows/Midtones/Highlights | 그림자 붉은기, 하이라이트 주황 | 그늘은 핏빛, 불빛은 화염색 |
| Lift Gamma Gain | Gamma +0.15 | 중간 톤을 올려 형태가 보이게 |
| Bloom | Threshold 1.1, Intensity 0.9, 주황 틴트 | 가로등/불빛/비상등만 번짐 |
| Vignette | 어두운 빨강, 0.3 | 가장자리를 어둡게 → 긴장감, 시선 집중 |
| Film Grain | Medium3, 0.2 | 거친 필름 질감 |
| Chromatic Aberration | 0.12 | 가장자리 약한 색수차 |
| Motion Blur | Off 유지 | FPS 시점 회전 시 조준 방해 |
| SSAO (Renderer Feature) | 0.4 → 0.6 | 구석/틈 접촉 그림자로 깊이감 |

### 튜닝 과정 (실패 사례 포함)
1. ACES + 노출 +0.55 + Lift → **더 어둡고 뿌옇게** 됨. ACES가 어두운 영역을 더 누르고, Lift가 검은색을 회갈색으로 띄워 탁해짐
2. 노출 +1.15, Lift 제거 → 밝아졌지만 그늘이 검게 뭉개지고 붉은빛 과다
3. **원인은 PP가 아니라 그늘의 간접광 부족**이라 판단 → 환경광을 올려 재Bake, 노출은 +0.8로 낮추고 붉은 틴트 완화 → FPS 시점에서 형태가 보이면서 붉은 전장 톤 유지

### 결과
- 같은 조명에서 Volume만 Off/On: `02_pp_off_*` ↔ `02_pp_on_*`
- 비용: 평균 프레임 5.03 → 5.75ms (약 +0.7ms). 풀스크린 효과는 픽셀 단위 처리라 GPU 비용 증가가 정상

---

## 3. 최적화 (Optimization)

### AI가 제안한 최적화 후보 (CPU / GPU / RAM)
| 분류 | 후보 | 판단 |
|---|---|---|
| GPU | 실시간 그림자 라이트 줄이기(Bake), Shadow Distance/Cascade 축소 | **적용** — 측정상 가장 큰 병목(Shadow Casters 2.4배) |
| GPU | SSAO 절반 해상도 | **적용** — 어둡고 Grain 있는 화면이라 품질 차이 미미 |
| GPU | 오클루전 컬링, LOD | 이전 커밋에서 이미 적용됨 |
| GPU | GPU Instancing / SRP Batcher | SRP Batcher 이미 On, 정적 맵은 Static Batching 중 → 추가 효과 작음 |
| CPU | 이펙트 Instantiate/Destroy → 오브젝트 풀링 | **적용** — 피 이펙트 누수 발견 |
| CPU | 피격마다 Debug.Log → 조건부 로거 | **적용** — 프로젝트의 `GameLogger` 재사용 |
| CPU | Update 안의 GetComponent/Find 캐싱 | 확인 결과 대부분 Awake에서 캐싱 중 → 효과 작음 |
| RAM | 텍스처 Max Size/압축 | **일부 적용** — 대부분 이미 DXT1·2048 이하, 캐릭터/검 텍스처만 축소 |
| RAM | 불필요한 렌더 텍스처 제거 | **적용** — 추가광 그림자 아틀라스 Off |
| RAM | 오디오 Load Type | 오디오 메모리 1.2MB 수준 → 효과 작음 |

### 3-1. GPU (렌더링)
| 항목 | Before | After |
|---|---|---|
| 실시간 그림자 가로등 | 3개 (Mixed) | 0개 (전부 Baked) |
| Shadow Distance | 50 | 30 |
| Shadow Cascade | 4 | 2 |
| SSAO | Full 해상도 | Half (Downsample) |

| 지표 | 02 (최적화 전) | 03 (최적화 후) | 변화 |
|---|---|---|---|
| 평균 프레임 | 5.75ms (174 FPS) | **4.33ms (231 FPS)** | −25% |
| PlayerLoop | 3.34ms | 2.52ms | −25% |
| Shadow Casters | 1743 | **416** | −76% |
| Triangles | 5.81M | **3.12M** | −46% |
| SetPass Calls | 90 | 69 | −23% |
| Batches | 506 | 197 | −61% |

→ 조명과 PP를 모두 넣은 최종 화면이 **원래 Baseline(4.84ms, Shadow Casters 703, Tris 3.65M)보다도 가벼워짐**
→ 화질 영향: 가로등 그림자는 라이트맵에 구워져 유지. 움직이는 캐릭터만 가로등 그림자를 잃음(Light Probe로 빛 색은 받음). 30m 밖 그림자는 Fog/어둠과 Shadowmask가 대신함

증거: `02_pp_on_stats.png` ↔ `03_opt_stats.png`, `02_pp_on_profiler_rendering.png` ↔ `03_opt_profiler_rendering.png`

### 3-2. CPU (스크립트)
**발견한 문제**
1. `Zombie.TakeDamage` — 피격마다 `Instantiate(BloodSprayFX)`. 이 프리팹은 **Loop 파티클 + Stop Action None**이라 **영원히 사라지지 않음(누수)**. 좀비는 풀링되어 재사용되므로 피 이펙트가 계속 누적
2. `PlayerGunFire.Shoot` — 발사마다 히트 이펙트 `Instantiate` 후 0.1초 뒤 Destroy (생성/파괴 반복)
3. `Zombie.ChangeState` — 피격마다 `Debug.Log` (스택 트레이스 수집 비용)

**수정** (`Zombie.cs`, `PlayerGunFire.cs`)
```csharp
// Zombie.TakeDamage: 루프 파티클이라 직접 회수하지 않으면 쌓인다 → 풀에서 꺼내고 일정 시간 뒤 반환
GameObject bloodSpray = LeanPool.Spawn(_bloodSprayPrefab, damageInfo.HitPoint, Quaternion.LookRotation(damageInfo.HitNormal), transform);
LeanPool.Despawn(bloodSpray, _bloodSprayLifetime);

// PlayerGunFire.Shoot: 발사마다 Instantiate/Destroy 하던 이펙트를 풀에서 재사용
ParticleSystem effect = LeanPool.Spawn(_hitEffectPrefab, hitInfo.point, Quaternion.LookRotation(hitInfo.normal));
ParticleSystem.MainModule main = effect.main;
main.stopAction = ParticleSystemStopAction.None; // 프리팹의 Destroy 설정이 풀 오브젝트를 파괴하지 않도록
effect.Play();
LeanPool.Despawn(effect, _hitEffectLifetime);

// Zombie.ChangeState: 빌드에서 호출 자체가 제거되는 GameLogger([Conditional("ENABLE_LOG")]) 사용
GameLogger.Log($"{_state} -> {next}");
```

**전투 부하 테스트** (좀비 3마리에게 피격 60회 + 사격 60회를 한 번에 실행, Profiler Off, 같은 세션에서 2회 연속)
| 지표 | Before 1회 | Before 2회 | After 1회 (풀 생성) | After 2회 (풀 재사용) |
|---|---|---|---|---|
| 피격 60회 처리 | 25.17ms | 32.61ms | 25.31ms | **19.15ms** |
| 사격 60회 처리 | 15.67ms | 13.06ms | 14.83ms | **1.47ms** |
| 3초 후 남은 ParticleSystem | 190 | **370 (계속 증가)** | 10 | **10** |
| 3초 후 살아있는 파티클 | 2620 | **5140** | 220 | **220** |
| SetPass (테스트 후) | — | 129 | — | 71 |

- After 1회차는 풀을 처음 채우느라 Before와 비슷하지만, 2회차부터 사격 비용 **약 89% 감소**
- 핵심은 **누수 제거**: Before는 싸울수록 파티클이 끝없이 쌓여 SetPass/GPU 부하가 계속 늘어나는 구조였고, After는 항상 원래 수준으로 돌아옴
- 한계: Mono 메모리 증가량(+약 1MB)은 Before/After가 같아(테스트 스크립트 자체 할당이 대부분) 이 방법으로는 GC 개선을 확인하지 못함

### 3-3. RAM (에셋/메모리)
- 분석: 씬 텍스처 33장 65.7MB, 이미 전부 DXT1 압축·최대 2048 / 라이트맵 4.7MB → 큰 낭비는 없음
- 적용
  1. 1인칭에서 작게 보이는 캐릭터(Ichigo)·검(Sword) 텍스처 4장: Max Size 2048 → 1024 (21.6MB → 5.4MB)
  2. 실시간 그림자 추가광이 0개가 되었으므로 URP Additional Lights Shadows Off (2048 그림자 아틀라스 불필요)

| 지표 (Profiler Rendering) | 02 (최적화 전) | 04 (최종) | 변화 |
|---|---|---|---|
| Used Textures | 41.3MB | 33.3MB | −8.0MB (−19%) |
| Render Textures | 233.4MB | 207.1MB | −26.3MB (−11%) |
| Used Buffers | 124.9MB | 112.3MB | −12.6MB |

증거: `02_pp_on_profiler_rendering.png` ↔ `04_final_profiler_rendering.png`, `04_final_profiler_memory.png`

---

## 4. 전체 요약

| 단계 | 프레임(ms) | FPS | PlayerLoop | Batches | SetPass | Tris | Shadow Casters |
|---|---|---|---|---|---|---|---|
| 00 Baseline | 4.84 | 207 | 2.75 | 175 | 77 | 3.65M | 703 |
| 01a 국지광 Realtime | 5.44 | 184 | 3.13 | 403 | 91 | 5.81M | 1682 |
| 01b Bake + Light Probe | 5.03 | 199 | 3.20 | 380 | 91 | 5.77M | 1631 |
| 02 포스트 프로세싱 | 5.75 | 174 | 3.34 | 506 | 90 | 5.81M | 1743 |
| 03 GPU·CPU 최적화 | **4.33** | **231** | **2.52** | 197 | **69** | **3.12M** | **416** |
| 04 최종 (+RAM) | 4.64 | 216 | 2.65 | 293 | 71 | 3.16M | 454 |

- **그래픽**: 밝은 낮 → 붉은 해질녘의 어두운 전장 (국지 조명 19개, Lightmap, Light Probe, Fog, ACES 컬러 그레이딩)
- **성능**: 조명·PP 추가로 늘어난 비용(+0.9ms)을 최적화로 되돌리고, 원래 Baseline보다 가벼워짐 (최종 04 기준 Baseline 대비 프레임 4.84 → 4.64ms, Shadow Casters −35%, Tris −13%, SetPass −8%)
- **배운 점**
  - 병목은 "라이트 개수"가 아니라 "실시간 그림자를 그리는 라이트" — Bake만으로는 부족했고, 측정으로 원인을 좁혔음
  - 화면이 어두울 때 PP 노출만 올리면 탁해짐 — 원인(간접광 부족)을 조명 단계에서 해결해야 함
  - 에디터 측정은 포커스/워밍업에 크게 흔들림 — 렌더 카운터와 Profiler 평균을 함께 봐야 신뢰할 수 있음
  - 코드 분석으로 측정에 드러나지 않던 누수(피 이펙트)를 찾았고, 부하 테스트로 증명함

## 5. 변경 파일
- `Assets/01.Scenes/GameScene.unity` (조명, Light Probe, 연기, 환경 설정) + `Assets/01.Scenes/GameScene/` (LightingSettings, Lightmap, LightingData)
- `Assets/09.Materials/Sky_Battlefield.mat` (신규 Skybox)
- `Assets/Settings/SampleSceneProfile.asset` (Volume), `PC_Renderer.asset` (SSAO), `PC_RPAsset.asset` (그림자)
- `Assets/02.Scripts/Monster/Zombie.cs`, `Assets/02.Scripts/Player/PlayerGunFire.cs` (풀링, 로그)
- `Assets/05.Models/Ichigo|Sword/*.png.meta` (텍스처 Max Size)
