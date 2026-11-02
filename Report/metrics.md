# 측정 기록 (Before / After)

## 측정 조건 (모든 단계 동일)
- Unity 6000.3.22f1 에디터 Play 모드, GameScene, Quality Level = PC, Game 뷰 Full HD(1920x1080), VSync Off
- 시나리오: Play 진입 → 2초 대기 → Profiler 기록 시작 → 입력 없이 약 6초 대기(스폰 지점, 좀비 3마리 활동) → 일시정지 → 캡처
- **주 지표 = Profiler 기록 기반 평균값** (마지막 600프레임): 평균 프레임 시간, PlayerLoop(게임 루프 CPU 시간), Batches/SetPass/Tris/Shadow Casters 평균, 프레임당 GC 할당
  - 이유: 에디터가 백그라운드일 때 프레임 제한이 걸려 순간 FPS가 크게 흔들림. Profiler 평균값은 이 영향을 덜 받음
- 보조 지표: Stats 창 스크린샷(순간값), 구간 평균 FPS(프레임 수 / 경과 시간)
- Baseline은 git HEAD의 원래 씬을 임시 씬으로 열어 같은 방식으로 측정 (측정 후 임시 씬 삭제)
- 고정 촬영 지점(에디터 모드 동일 카메라 렌더, 1920x1080, FOV 60, Post Processing On)
  - A_spawn: (0, 3.3, -8) 회전 (5, 0, 0) — 스폰 지점
  - B_yard: (66, 3.8, -50) 회전 (5, 150, 0) — 탱크가 있는 넓은 공터
  - C_alley: (-24.5, 1.6, -8) 회전 (3, 180, 0) — 서쪽 도로(골목)
  - D_fire: (0, 3.5, -29) 회전 (8, 141, 0) — 드럼통/전기박스 구역(불빛·비상등)
- 에디터 메모리 수치(Textures/Total)는 세션 상태에 따라 크게 흔들림 → 참고용

## 요약 표
| 단계 | 평균 프레임(ms) | 환산 FPS | PlayerLoop(ms) | Batches | SetPass | Tris | Shadow Casters | GC/frame |
|---|---|---|---|---|---|---|---|---|
| 00 Baseline | 4.84 | 207 | 2.75 | 175 | 77 | 3.65M | 703 | 18.0 KB |
| 01a 조명(국지광 실시간 19개, 그림자 3개) | 5.44 | 184 | 3.13 | 403 | 91 | 5.81M | 1682 | 31.8 KB |
| 01b 조명 Bake(16개 Baked + 3개 Mixed) + Light Probe | 5.03 | 199 | 3.20 | 380 | 91 | 5.77M | 1631 | 10.8 KB |
| 02 포스트 프로세싱 적용 | 5.75 | 174 | 3.34 | 506 | 90 | 5.81M | 1743 | 26.8 KB |
| 03 최적화(GPU: 그림자/SSAO + CPU: 풀링) | 4.33 | 231 | 2.52 | 197 | 69 | 3.12M | 416 | 17.5 KB |
| 04 최종(+ RAM: 텍스처/추가광 그림자 Off) | 4.64 | 216 | 2.65 | 293 | 71 | 3.16M | 454 | 8.4 KB |

- GC/frame은 에디터(EditorLoop) 할당이 섞인 값이라 참고용. 게임 코드(PlayerLoop) 자체의 대기 상태 할당은 프레임당 약 200B 수준
- 03과 04의 시간 차이(4.33 ↔ 4.64ms)는 04에 GPU 작업 변화가 없으므로 측정 변동으로 판단. 렌더 카운터는 거의 동일

## 00. Baseline (변경 전)
증거: `00_before_stats.png`, `00_before_profiler_{cpu,rendering,memory}.png`, `00_before_{A,B,C,D}_*.png`
- 조명: 활성 Directional Light 1개(Realtime, 강도 2.0, 흰색, 그림자 On). 기본 Skybox, Ambient(Skybox, 1.0), Fog Off, Lightmap 없음, 국지 조명 없음
- Post Processing: Bloom, Vignette, Tonemapping, MotionBlur (SampleSceneProfile)
- 구간 평균 FPS 221.9, Stats 순간값은 스크린샷 참고

## 01a. 조명 — 실시간 국지광 (중간 단계)
증거: `01_light_realtime_stats.png`, `01_light_realtime_profiler_*.png`, `01_light_realtime_{A,B,C,D}_*.png`
- Directional: 붉은 해질녘(색 1.0/0.52/0.33, 강도 0.45, 각도 14°), Mixed
- Skybox: Procedural `Sky_Battlefield.mat`(붉은 틴트, 노출 0.32), Ambient Trilight(어둡게), Reflection 0.45
- Fog: Exponential Squared, 밀도 0.018, 갈색 연기색
- 국지광 19개(Realtime): 나트륨 가로등 Spot 11(이 중 3개 그림자), 드럼통 불빛 Point 5, 붉은 비상등 Point 3 + 연기 파티클 5
- 결과: 프레임 +0.6ms(207→184 FPS), **Shadow Casters 703→1682(+139%)**, Tris 3.65M→5.81M, Batches 175→403
- 분석: 그림자를 켠 Spot 3개가 각각 그림자 맵을 따로 렌더링하면서 장면을 다시 그림 → 그림자 패스만큼 Tris/Batches 증가. 다음 단계에서 Bake로 해결

## 01b. 조명 — Lightmap Bake + Light Probe
증거: `01_light_baked_stats.png`, `01_light_baked_profiler_*.png`, `01_light_baked_{A,B,C,D}_*.png`
- LightingSettings `Assets/01.Scenes/GameScene/GameSceneLighting.lighting`: Progressive GPU, Baked GI On / Realtime GI Off, Mixed Mode = Shadowmask(Distance Shadowmask), 해상도 4 texel/unit, 최대 2048, Direct 32 / Indirect 128 / Env 128 샘플, Bounce 2, AO On(1.5m), 압축 Normal → Lightmap 3장 + Shadowmask 3장
- 국지광 19개 중 16개 Baked(런타임 비용 0, 간접광/부드러운 그림자 포함), 스폰·서쪽 도로 가로등 3개만 Mixed(움직이는 플레이어/좀비 그림자용)
- Light Probe Group 584개: 지면 레이캐스트로 6m 격자 × 높이 0.5/2.5m + 국지광 주변 반경 2.5m 링 → 좀비/플레이어가 Baked 국지광 색을 받음
- 결과(01a 대비): 프레임 5.44→5.03ms, Batches 403→380, Shadow Casters 1682→1631, Tris 5.81M→5.77M
- 분석: Baked 전환으로 실시간 조명 계산은 줄었지만, **그림자를 쓰는 Mixed 가로등 3개가 여전히 매 프레임 그림자 맵을 렌더링** → Tris/Shadow Casters는 거의 그대로. 실제 병목은 "라이트 개수"가 아니라 "실시간 그림자를 그리는 라이트" → 3단계 최적화에서 처리
- 측정 노이즈 메모: 같은 조건 1차 측정은 8.41ms(Play 직후 워밍업 프레임이 섞임) → 렌더 카운터(Batches/Tris/Shadow Casters)는 실행마다 거의 동일하므로 이를 주 비교 지표로 사용

## 01c. 조명 밝기 보정 (사용자 피드백: "너무 어두워서 안 보임")
- Directional 강도 0.45 → 0.8, Ambient Trilight 약 2.5배 상향(Sky 0.52/0.44/0.47, Equator 0.44/0.33/0.28, Ground 0.19/0.14/0.12)
- Skybox 노출 0.32 → 0.42, Fog 밀도 0.018 → 0.013, 가로등 강도 70 → 90(범위 20), 불빛 18 → 22
- Lightmap 재Bake. 라이트 개수·그림자 설정은 그대로라 렌더 카운터 변화 없음(성능 재측정 생략)

## 02. 포스트 프로세싱
증거: `02_pp_off_{A,B,C,D}_*.png`(같은 조명, Volume Off) ↔ `02_pp_on_{A,B,C,D}_*.png`(Volume On), `02_pp_on_stats.png`, `02_pp_on_profiler_*.png`
| 효과 | 설정 | 의도 |
|---|---|---|
| Tonemapping | Neutral → **ACES** | 영화 같은 대비, 밝은 불빛 하이라이트를 부드럽게 압축 |
| Color Adjustments | Post Exposure +0.8, Contrast +8, Saturation −20, Color Filter (1, 0.95, 0.92) | 어둡지만 보이게 노출 보정, 탁하고 건조한 전장 톤 |
| Shadows/Midtones/Highlights | 그림자 붉은기(1, 0.93, 0.92), 하이라이트 주황(1, 0.95, 0.86) | 그늘은 핏빛, 불빛은 화염색 |
| Lift Gamma Gain | Gamma +0.15 | 중간 톤을 올려 형태가 보이게 (Lift는 화면이 탁해져 0으로) |
| Bloom | Threshold 1.1, Intensity 0.9, Scatter 0.65, Tint 주황 | 가로등/불빛/비상등만 번짐 |
| Vignette | 색 (0.18, 0, 0), Intensity 0.3, Smoothness 0.45 | 가장자리를 어둡고 붉게 → 긴장감, 시선 집중 |
| Film Grain | Medium3, Intensity 0.2 | 거친 전장 필름 질감 |
| Chromatic Aberration | 0.12 | 가장자리 약한 색수차 (과하면 어지러움) |
| Motion Blur | Off 유지 | FPS 시점 회전 시 조준 방해 |
| SSAO (Renderer Feature) | Intensity 0.4 → 0.6 | 구석/틈 접촉 그림자로 깊이감 |

튜닝 과정(실패 사례 포함):
1. 1차: ACES + 노출 +0.55 + Lift +0.04 + 대비 22 → 오히려 더 어둡고 뿌옇게 됨. ACES가 어두운 영역을 더 누르고, Lift가 검은색을 회갈색으로 띄워 탁해짐
2. 2차: 노출 +1.15, Lift 제거, Gamma +0.12 → 밝아졌지만 그늘이 거의 검게 뭉개지고 붉은 틴트 과다
3. 3차(최종): **원인이 PP가 아니라 그늘의 간접광 부족**임을 확인 → Ambient를 올려 재Bake, PP 노출은 +0.8로 낮추고 붉은 틴트 완화 → FPS 시점 실제 화면에서 형태가 보이면서 붉은 전장 톤 유지

성능(01b 대비): 평균 프레임 5.03 → 5.75ms(약 +0.7ms), PlayerLoop 3.20 → 3.34ms. 풀스크린 효과(ACES/Bloom/Grain/CA/Vignette)는 픽셀 처리라 GPU 비용이 늘어나는 것이 정상. Batches 차이(380 → 506)는 좀비 위치에 따라 화면에 보이는 오브젝트가 달라지는 측정 변동으로 판단(PP는 Batches를 크게 늘리지 않음)

## 03. 최적화 — GPU (렌더링)
증거: `03_opt_stats.png`, `03_opt_profiler_*.png`, `03_opt_{A,B,C,D}_*.png`
| 항목 | Before | After | 근거 |
|---|---|---|---|
| 실시간 그림자 가로등(Mixed) | 3개 | 0개(전부 Baked) | 01a/01b 분석: Shadow Caster 증가의 원인. 그림자는 라이트맵에 구워짐 |
| 메인 라이트 Shadow Distance | 50 | 30 | FPS 시점에서 30m 밖 그림자는 Fog/어둠에 묻혀 거의 안 보임. 먼 곳은 Shadowmask(구운 그림자)가 대신함 |
| Shadow Cascade | 4 | 2 | 캐스케이드마다 그림자 맵을 다시 그림 → 패스 수 절반 |
| SSAO | Full 해상도 | Downsample(Half) | 어둡고 Film Grain이 있는 화면이라 품질 차이가 거의 안 보임 |

결과(02 → 03): 평균 프레임 **5.75 → 4.33ms (174 → 231 FPS, −25%)**, PlayerLoop 3.34 → 2.52ms, **Shadow Casters 1743 → 416 (−76%)**, **Tris 5.81M → 3.12M (−46%)**, SetPass 90 → 69, Batches 506 → 197
→ 조명·PP를 모두 넣은 최종 화면이 **원래 Baseline(4.84ms, Shadow Casters 703, Tris 3.65M)보다도 가벼움**
화질 영향: `02_pp_on_*` ↔ `03_opt_*` 비교 시 차이 미미 (가로등 그림자는 Bake되어 유지, 움직이는 캐릭터만 가로등 그림자를 잃음 — Light Probe로 빛 색은 받음)

## 03. 최적화 — CPU (스크립트)
분석: Profiler GC Alloc 추적 결과 대기 상태의 게임 코드 할당은 작음(프레임당 약 200B, 좀비 스폰 Instantiate와 상태 전이 로그). **부하는 전투 중 이펙트 생성에 집중**됨을 코드 분석으로 확인
1. `Zombie.TakeDamage`: 피격마다 `Instantiate(BloodSprayFX)` — 이 프리팹은 **Loop 파티클 + Stop Action None** 이라 **영원히 파괴되지 않음(누수)**. 좀비가 풀링으로 재사용되므로 계속 누적
2. `PlayerGunFire.Shoot`: 발사마다 `Instantiate(Stone_BulletImpact)` → 0.1초 뒤 Destroy (생성/파괴 반복)
3. `Zombie.ChangeState`: 피격마다 `Debug.Log` (에디터/개발 빌드에서 스택 트레이스 수집 비용)

수정:
- 피 이펙트/히트 이펙트를 기존 LeanPool로 Spawn하고 `LeanPool.Despawn(obj, 1초)`로 반환 (`_bloodSprayLifetime`, `_hitEffectLifetime` SerializeField로 노출)
- 히트 이펙트는 프리팹의 Stop Action(Destroy)이 풀 오브젝트를 파괴하지 않도록 Spawn 시 None으로 설정
- 상태 전이 로그를 프로젝트의 `GameLogger.Log`(`[Conditional("ENABLE_LOG")]`, 빌드에서 호출 자체가 제거됨)로 교체

전투 부하 테스트(Play 모드, Profiler Off, 좀비 3마리에게 피격 60회 + 사격 60회를 한 프레임에 실행, 같은 세션에서 2회 연속):
| 지표 | Before 1회차 | Before 2회차 | After 1회차(풀 생성) | After 2회차(풀 재사용) |
|---|---|---|---|---|
| 피격 60회 처리 시간 | 25.17ms | 32.61ms | 25.31ms | **19.15ms** |
| 사격 60회 처리 시간 | 15.67ms | 13.06ms | 14.83ms | **1.47ms** |
| 3초 후 남은 ParticleSystem | 190 | **370 (계속 증가)** | 10 | **10** |
| 3초 후 살아있는 파티클 수 | 2620 | **5140** | 220 | **220** |
| SetPass (테스트 후) | — | 129 | — | 71 |
- 해석: After 1회차는 풀을 처음 채우느라(Instantiate) Before와 비슷하지만, 2회차부터는 재사용이라 사격 비용이 **약 89% 감소**. 무엇보다 Before는 전투할수록 파티클이 끝없이 쌓여(누수) SetPass/GPU 부하가 계속 증가하는 구조였고, After는 항상 원래 수준으로 돌아옴
- MonoUsed 증가량(+약 1MB)은 Before/After 동일 → 테스트 스크립트 자체 할당이 대부분이라 GC 개선은 이 방법으로는 확인 불가(정직하게 기록)

## 04. 최적화 — RAM (에셋/메모리)
증거: `04_final_stats.png`, `04_final_profiler_*.png`, `04_final_{A,B,C,D}_*.png`
분석: 씬에서 쓰는 텍스처 33장 65.7MB, 이미 전부 DXT1 압축·최대 2048. 라이트맵은 6장 4.7MB로 작음 → 큰 낭비는 없음. 대신
1. 1인칭 FPS에서 화면에 작게 보이는 캐릭터(Ichigo)·검(Sword) 텍스처 4장(Base Color/Normal, 2048px, 각 5.4MB) → Max Size 1024
2. 3단계 이후 실시간 그림자를 쓰는 추가 광원이 0개 → URP Asset의 Additional Lights Shadows 지원 Off (2048 그림자 아틀라스 렌더 텍스처 불필요)

| 지표 (Profiler Rendering 모듈) | 02 (최적화 전) | 04 (최종) | 변화 |
|---|---|---|---|
| Used Textures | 89개 / 41.3MB | 87개 / 33.3MB | −8.0MB (−19%) |
| Render Textures | 55개 / 233.4MB | 52개 / 207.1MB | −26.3MB (−11%) |
| Used Buffers | 7658 / 124.9MB | 5408 / 112.3MB | −12.6MB |
| Ichigo+Sword 텍스처 | 21.6MB | 5.4MB | −16.2MB |
- Render Textures 감소는 SSAO 절반 해상도 + 캐스케이드 축소 + 추가광 그림자 아틀라스 제거의 합산 효과

## 기타 변경 사항
- 측정 중 `Application.runInBackground = true`가 ProjectSettings에 저장됐던 것을 원래 값(false)으로 복구
- `PC_Renderer.asset`의 일부 줄 변경은 Unity가 저장하며 직렬화 버전을 올린 것(AssetVersion 2 → 3)
