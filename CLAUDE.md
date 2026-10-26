# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

Unity 6 (`6000.3.22f1`, URP) 기반 3D FPS 게임. 게임 코드는 전부 `Assets/02.Scripts/` 아래에 있으며 namespace 없이 전역에 둔다. 메인 씬은 `Assets/01.Scenes/GameScene.unity`.

- 답변은 한국어로 작성한다.
- 요청받지 않았다면 Commit하지 않고, 요청과 관련 없는 파일은 수정하지 않는다.
- Unity 에디터에서 실행 결과를 확인하지 않았다면 동작을 확인했다고 표현하지 않는다. (CLI로 게임을 실행/테스트할 수단이 없음. 테스트 코드도 아직 없음.)

## 명령어

```bash
# 포맷/네이밍 검사 (pre-commit 훅과 동일). 실패 시 커밋이 막힌다.
dotnet format 3DFPSGame.sln --verify-no-changes --severity warn --include Assets/02.Scripts/

# 자동 수정
dotnet format 3DFPSGame.sln --severity warn --include Assets/02.Scripts/

# Husky.Net 훅 설치 (최초 1회)
dotnet tool restore && dotnet husky install
```

`.csproj`/`.sln`은 Unity가 생성하므로 직접 수정하지 않는다.

## 커밋 규칙

`.husky/commit-msg`가 `type: 설명` 형식을 강제한다. 허용 type: `asset|build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test` (`type(scope): ...`, `!` 허용). 설명은 한국어로 쓴다 (예: `feat: 미니맵 확대/축소`).

`main`에 `Assets/02.Scripts/**/*.cs` 변경이 push되면 `.github/workflows/gemini-commit-review.yml`이 `GEMINI.md` + `Docs/CodeReviewGuide.md` 기준으로 자동 코드 리뷰를 수행한다. 코드 리뷰 요청 시에도 `Docs/CodeReviewGuide.md`의 기준(Critical/High/Medium/Low 중요도, 매직 넘버, 과도한 추상화 지양 등)을 따른다.

## 코드 스타일 (`.editorconfig`, `Assets/02.Scripts/**.cs`에만 적용)

- private 필드: `_camelCase` / private `const`, `static readonly`: `PascalCase` / 타입명: `PascalCase` (`_` 금지) / 지역변수: `camelCase`
- 중괄호는 항상 새 줄(Allman). 단일 행 조기 리턴은 중괄호 생략 가능 (`if (...) return;`)
- 4칸 스페이스 들여쓰기
- 조정값은 `[SerializeField] private` 필드 + `[Header]`로 노출하고, 외부 공개는 읽기 전용 프로퍼티(`public float Health => _health;`)로 한다.
- UI 스크립트는 `UI_` 접두사 클래스명을 쓴다 (`UI_Minimap`, `UI_PlayerStats` 등 — 네이밍 규칙의 예외로 기존 관례).
- 주석은 한국어.

## 아키텍처

**게임 상태**: `GameManager`(싱글톤, `GameManager.Instance`)가 `GameState`(Ready → Playing → GameOver)를 관리한다. 입력을 처리하는 컴포넌트들은 `Update()` 첫 줄에서 `GameManager.Instance.State != GameState.Playing`이면 return하는 패턴을 따른다.

**데미지 시스템**: `Core/IDamageable` + `Core/DamageInfo`(데미지량, 맞은 위치/노멀). `Player`, `Zombie`, `Drum`, `Box` 등이 `IDamageable`을 구현하고, 공격 측(`PlayerGunFire`의 Raycast, `Bomb`/`Drum` 폭발, `Zombie` 공격)은 `GetComponent<IDamageable>()`로 대상에 `TakeDamage`를 호출한다.

**커서와 사격**: `CursorManager`가 커서를 잠그며, Left Alt를 누르는 동안 커서를 보이게 한다. `PlayerGunFire`는 `Cursor.visible`일 때 발사하지 않는다 — 커서 상태가 사격 가능 여부를 겸한다.

**카메라**: 메인 카메라에 `CameraController` + `CameraViewSwitcher` + `CameraRotate`(+ 비활성화되는 `CameraFollow`)가 함께 붙는다. `CameraViewSwitcher`가 V 키로 `CameraViewSetting` 프리셋(1인칭/3인칭/숄더/쿼터/탑다운/사이드)을 순환하며 `CameraController.ApplyViewSetting()`에 전달하고, `CameraController`가 `LateUpdate`에서 위치/회전을 적용한다. 마우스 회전 시점에서는 `CameraRotate`를 활성화하며, 그 private 누적 각도(`_accumulationX/Y`)를 **리플렉션으로** 동기화한다 — `CameraRotate`/`CameraFollow`의 private 필드명을 바꾸면 조용히 깨진다. `PlayerRotate`는 플레이어 몸체의 Y 회전을 담당한다.

**미니맵**: `MinimapCamera`(플레이어 상공 추적 카메라 → RenderTexture)와 `UI_Minimap`(Mask로 둥글게 표시, 확대/축소). 리소스는 `Assets/Minimap_Resource`.

**몬스터**: `Zombie`는 enum 기반 FSM(Idle/Patrol/Trace/Comeback/Attack/Hit/Death)으로, `CharacterController`로 이동하고 플레이어는 `"Player"` 태그로 찾는다. `ZombieHealthBar`는 빌보드 체력바(좀비 본체가 아닌 체력바만 회전시켜야 함).

**오브젝트 풀링**: 수류탄(`Bomb`)은 `LeanPool.Spawn/Despawn`(Assets/Plugins의 Lean Pool)을 사용한다. 그 외 이펙트는 아직 `Instantiate`를 쓴다.

**입력**: 새 Input System 패키지가 설치되어 있지만, 게임 코드는 레거시 `Input.GetKey/GetButton`을 사용한다.

`Assets/02.Scripts/Example/`은 C# 학습용 예제(코루틴/제네릭/컬렉션)로 게임 로직과 무관하다.
