# Kkami Streaming Work Log

작성일: 2026-07-09

## 1. 스테이지 데이터테이블 갱신

- 바탕화면 원본 파일 `20260708_kkami_까미스트리밍데이터테이블_백승오_ver00 (1).xlsx`의 `stage` 시트를 기준으로 프로젝트 스테이지 데이터를 갱신했다.
- 스테이지 데이터는 `30001`부터 `30050`까지 50개 행으로 구성했다.
- 각 스테이지에 `time_limit_sec` 컬럼을 추가하고 전 행에 `30`초를 입력했다.
- 기물 생성 확률은 프로젝트 기물 ID 기준 컬럼으로 변환했다.
  - `piece_100001_weight`: 키보드
  - `piece_100002_weight`: 카메라
  - `piece_100003_weight`: 에너지드링크
  - `piece_100004_weight`: 상자
  - `piece_100005_weight`: 빨간상자
- 관련 파일:
  - `Assets/GameKamiStreaming/Resources/GameKamiStreaming/DataTables/stage.csv`
  - `Assets/GameKamiStreaming/DataTables/Source/까미스트리밍_테이블.xlsx`

## 2. 스테이지 제한시간 데이터 로딩

- `StageRow`에 `timeLimitSeconds` 필드를 추가했다.
- `KkamiTableDatabase`가 `stage.csv`의 `time_limit_sec` 값을 읽도록 수정했다.
- 값이 없거나 0 이하일 경우 기본값 30초를 사용하도록 했다.
- 관련 파일:
  - `Assets/GameKamiStreaming/Scripts/Runtime/KkamiDataModels.cs`
  - `Assets/GameKamiStreaming/Scripts/Runtime/KkamiTableDatabase.cs`

## 3. 이미지 숫자 라벨 크기 통일

- 자원 표시 숫자 이미지가 서로 다른 크기로 보일 수 있던 문제를 정리했다.
- 기존 씬에 배치된 `Digit` 자식 이미지도 `PixelNumberLabel`이 다시 캐싱해서 동일한 규칙을 적용하도록 했다.
- 모든 숫자 이미지는 고정 크기와 `LayoutElement` 값을 사용한다.
- `preserveAspect`에 의한 표시 크기 흔들림을 막기 위해 숫자 이미지는 고정 크기로 표시한다.
- 관련 파일:
  - `Assets/GameKamiStreaming/Scripts/Runtime/PixelNumberLabel.cs`

## 4. 라운드 타이머 UI 추가

- 화면 왼쪽 아래에 이미지 숫자 기반 라운드 타이머를 추가했다.
- `dotdot` 스프라이트를 `:` 문자에도 매핑해서 `0:30` 형식으로 표시한다.
- 타이머는 현재 스테이지의 `timeLimitSeconds` 값을 기준으로 감소한다.
- 관련 파일:
  - `Assets/GameKamiStreaming/Scripts/Runtime/KkamiPrototypeGame.cs`
  - `Assets/GameKamiStreaming/Scripts/Runtime/PixelNumberLabel.cs`

## 5. 자원 숫자 확대 효과 제거

- 자원을 획득할 때 점수 숫자 라벨이 커졌다 작아지는 효과를 제거했다.
- 자원 파티클이 점수 위치로 이동하는 연출은 유지했다.
- 숫자 라벨은 값이 변경되어도 `Vector3.one` 스케일을 유지하도록 했다.
- 관련 파일:
  - `Assets/GameKamiStreaming/Scripts/Runtime/KkamiPrototypeGame.cs`
  - `Assets/GameKamiStreaming/Scripts/Runtime/PixelNumberLabel.cs`

## 6. 스테이지 종료 후 스킬트리 화면 추가

- 라운드 제한시간이 0초가 되면 즉시 다음 스테이지로 넘어가지 않고 스킬트리 화면을 표시하도록 변경했다.
- 스킬트리는 별도 `Skill Tree Canvas`를 런타임에 생성하는 방식으로 구현했다.
- 스킬트리 필드는 현재 빈 패널로 두었다.
- 임시 `NEXT STAGE` 버튼을 추가했다.
- 버튼을 누르면 스킬트리 캔버스가 숨겨지고 다음 스테이지가 시작된다.
- 스킬트리 화면이 열려 있는 동안 타이머, 채굴, 기물 리스폰은 멈추도록 처리했다.
- 관련 파일:
  - `Assets/GameKamiStreaming/Scripts/Runtime/KkamiPrototypeGame.cs`

## 7. 검증 기록

- `stage.csv` 검증:
  - 데이터 행 50개
  - `time_limit_sec` 누락 0개
  - 기물 가중치 합 오류 0개
- C# 빌드 검증:
  - `dotnet build kkami_streaming.slnx`
  - 경고 0개
  - 오류 0개

## 8. 편집 가능한 스킬트리 캔버스 씬 생성

- 런타임 생성 방식만 있던 `Skill Tree Canvas`를 `KkamiPrototype.unity` 씬 안에 실제 오브젝트로 생성했다.
- Unity 에디터 메뉴 `GameKamiStreaming/Ensure Editable Skill Tree Canvas`를 추가했다.
- 해당 메뉴는 씬을 열고 `Skill Tree Canvas`, 전체 화면 스킬트리 필드, 우하단 `NEXT STAGE` 버튼을 생성한 뒤 저장한다.
- `KkamiPrototypeGame`의 `skillTreeCanvasRoot`, `startNextStageButton` 참조가 씬에 연결되도록 했다.
- `Skill Tree Canvas`는 기본 비활성 상태로 저장해 기존 스테이지 플레이 캔버스를 덮거나 조작하지 않게 했다.
- 스테이지 종료 시에도 스테이지 캔버스는 끄지 않고, 스킬트리 캔버스만 활성/비활성으로 전환하도록 수정했다.
- 관련 파일:
  - `Assets/Scenes/KkamiPrototype.unity`
  - `Assets/GameKamiStreaming/Scripts/Editor/KkamiPrototypeSceneBuilder.cs`
  - `Assets/GameKamiStreaming/Scripts/Runtime/KkamiPrototypeGame.cs`

## 9. 스폰 범위 고정

- 화면 크기에 따라 `E` 패널 기준 스폰 범위가 크게 달라질 수 있던 문제를 수정했다.
- 기존 `E` 전체 RectTransform 대신 `spawnpoint1`~`spawnpoint4` 네 판넬의 위치를 꼭짓점으로 사용한다.
- 네 점을 `Spawned Pieces` 레이어 로컬 좌표로 변환한 뒤, 그 네 점을 꼭짓점으로 가진 마름모 범위 안에서만 기물이 스폰되도록 했다.
- 네 점의 축 정렬 바운딩 박스가 아니라 실제 네 꼭짓점 마름모/폴리곤 내부에서 좌표를 샘플링하도록 보강했다.
- 기물의 네 모서리가 스폰 폴리곤 안에 들어오는 후보를 우선 사용하고, 랜덤 후보가 실패하면 격자 검색으로 내부 후보를 찾는다.
- 기존 이름 `spawnpoint`였던 판넬은 `spawnpoint4`로 정리했다.
- `KkamiPrototypeGame`에 네 스폰 포인트 참조를 씬에서 연결했다.
- 관련 파일:
  - `Assets/Scenes/KkamiPrototype.unity`
  - `Assets/GameKamiStreaming/Scripts/Runtime/KkamiPrototypeGame.cs`
  - `Assets/GameKamiStreaming/Scripts/Editor/KkamiPrototypeSceneBuilder.cs`

## 10. 채굴 범위 표시 기준 수정

- 채굴 범위 커서가 여전히 `E` 패널의 `RectangleContainsScreenPoint` 기준으로 표시되어, 화면 크기와 `E`의 회전/스케일에 따라 사라질 수 있던 문제를 확인했다.
- 커서 표시 여부도 `spawnpoint1`~`spawnpoint4`가 만드는 스폰 마름모 범위 기준으로 변경했다.
- 스폰 포인트 범위를 얻지 못하는 경우에만 기존 `E` 패널 기준으로 fallback한다.
- 관련 파일:
  - `Assets/GameKamiStreaming/Scripts/Runtime/KkamiPrototypeGame.cs`

## 11. 채굴 GIF 모션 판정 연동

- 바탕 화면의 `blit_edbb9423_motion_master.gif`를 12개 PNG 프레임으로 추출해 `Resources/GameKamiStreaming/Sprites/mining_attack`에 추가했다.
- 채굴 판정을 매 프레임 즉시 적용하던 방식에서, GIF 모션을 빠르게 재생한 뒤 모션이 끝나는 순간 판정이 들어가도록 변경했다.
- GIF 재생 중에는 이미지가 마우스 포인터를 계속 따라가도록 했다.
- GIF 이미지의 피벗을 전체 폭의 왼쪽 1/3 지점으로 설정해, 해당 지점이 채굴 판정 범위 중앙과 맞도록 했다.
- GIF 이미지의 세로 피벗을 하단으로 변경해 기존 기준 위치보다 이미지가 위로 출력되도록 했다.
- GIF 표시 크기를 70%로 줄였고, 가로/세로는 같은 비율로 줄여 종횡비가 바뀌지 않게 했다.
- 스킬트리 화면으로 전환될 때 재생 중인 채굴 모션은 즉시 숨기고 판정이 들어가지 않도록 했다.
- 검증:
  - `dotnet build kkami_streaming.slnx`
  - 경고 0개
  - 오류 0개
- 관련 파일:
  - `Assets/GameKamiStreaming/Scripts/Runtime/KkamiPrototypeGame.cs`
  - `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/mining_attack/frame_000.png` ~ `frame_011.png`

## 12. kkami appear 랜덤 표정 출력

- 전달받은 표정 이미지 6개를 `Resources/GameKamiStreaming/Sprites/kkami_appear` 폴더에 추가했다.
- `kkami appear` 패널의 `Image` 컴포넌트를 런타임에서 찾아 표정 표시 대상으로 사용하도록 했다.
- 게임 중 5초마다 표정 이미지가 랜덤으로 교체되도록 했다.
- 같은 이미지가 연속으로 뽑히지 않도록 보정했다.
- 표정이 바뀔 때 기존 기물 피격 효과처럼 패널이 살짝 커진 뒤 이미지가 교체되고 원래 크기로 돌아오도록 했다.
- 패널 이미지는 `preserveAspect`를 켜서 가로/세로 비율이 바뀌지 않게 했다.
- 검증:
  - `dotnet build kkami_streaming.slnx`
  - 경고 0개
  - 오류 0개
- 관련 파일:
  - `Assets/GameKamiStreaming/Scripts/Runtime/KkamiPrototypeGame.cs`
  - `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/kkami_appear/love.png`
  - `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/kkami_appear/confused.png`
  - `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/kkami_appear/angry.png`
  - `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/kkami_appear/super_angry.png`
  - `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/kkami_appear/shocked.png`
  - `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/kkami_appear/sad.png`

## 13. 스킬트리 줌/팬 조작 추가

- 스킬트리 빈 필드인 `Skill Tree Empty Fields`를 큰 작업 영역으로 보정하도록 했다.
- 스킬트리 화면이 열려 있을 때 마우스 휠로 줌 인/줌 아웃할 수 있게 했다.
- 줌 인 상태에서 마우스 좌클릭을 유지하고 움직이면 스킬트리 작업 영역을 드래그 이동할 수 있게 했다.
- 드래그/줌 이동 시 콘텐츠가 화면 밖으로 과도하게 빠지지 않도록 위치를 제한했다.
- 기존 `NEXT STAGE` 버튼은 스킬트리 콘텐츠 확대/이동의 영향을 받지 않도록 캔버스 루트에 고정하고, 항상 화면 우하단 위치와 기존 크기를 유지하도록 했다.
- 검증:
  - `dotnet build kkami_streaming.slnx`
  - 경고 0개
  - 오류 0개
- 관련 파일:
  - `Assets/GameKamiStreaming/Scripts/Runtime/KkamiPrototypeGame.cs`

## 14. 스폰 기물 크기 및 스폰 포인트 비율 보정

- 소환되는 기물의 크기를 기존 랜덤 크기 범위의 80%로 줄였다.
- `spawnpoint1`~`spawnpoint4`가 화면 크기에 따라 크게 흔들리지 않도록 stretch 앵커 대신 중앙 고정 앵커를 사용하도록 씬 값을 수정했다.
- 런타임 초기화와 에디터 보정 메뉴에서도 스폰 포인트 앵커, 피벗, 크기, 스케일을 안정화하도록 보강했다.
- 기존 네 스폰 포인트의 배치 좌표는 유지하면서 화면 크기 변화에 따른 비율 왜곡을 줄이도록 했다.
- 검증:
  - `dotnet build kkami_streaming.slnx`
  - 경고 0개
  - 오류 0개
- 관련 파일:
  - `Assets/GameKamiStreaming/Scripts/Runtime/KkamiPrototypeGame.cs`
  - `Assets/GameKamiStreaming/Scripts/Editor/KkamiPrototypeSceneBuilder.cs`
  - `Assets/Scenes/KkamiPrototype.unity`

## 15. 자원 획득 연출 표시 복구

- 자원 획득 시 `Resource Score Fly` 연출 코루틴은 호출되고 있었지만, 부모 오브젝트인 `Effect Layer`가 씬에서 비활성화되어 있어 화면에 보이지 않는 문제를 확인했다.
- `Effect Layer`를 씬에서 활성화했다.
- 런타임 초기화 시에도 `Effect Layer`를 찾아 활성화하고 전체 화면 레이어로 보정하도록 했다.
- 자원 획득 연출을 생성할 때 `Effect Layer`를 다시 최상단 sibling으로 올려 다른 UI 뒤에 가려지지 않도록 했다.
- 검증:
  - `dotnet build kkami_streaming.slnx`
  - 경고 0개
  - 오류 0개
- 관련 파일:
  - `Assets/GameKamiStreaming/Scripts/Runtime/KkamiPrototypeGame.cs`
  - `Assets/Scenes/KkamiPrototype.unity`

## 16. 스킬트리 배경 이미지 적용

- 전달받은 `skilltree bg.png`를 `Resources/GameKamiStreaming/Sprites/skilltree_bg.png`로 추가했다.
- `Skill Tree Backdrop` 이미지가 해당 배경 스프라이트를 사용하도록 씬과 런타임 코드를 수정했다.
- 스킬트리 배경이 캔버스를 덮도록 `AspectRatioFitter.EnvelopeParent` 보정을 추가했다.
- 검증:
  - `dotnet build kkami_streaming.slnx`
  - 경고 0개
  - 오류 0개
- 관련 파일:
  - `Assets/GameKamiStreaming/Scripts/Runtime/KkamiPrototypeGame.cs`
  - `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/skilltree_bg.png`
  - `Assets/Scenes/KkamiPrototype.unity`

## 17. 10스테이지 보스 1차 구조 추가

- `stage.csv`에서 10/20/30/40/50 스테이지에 보스 ID가 이미 들어 있는 것을 확인했다.
- 우선 10스테이지 보스인 `50001`을 `piece.csv`에 기물 데이터로 추가했다.
- `50001`은 `resource_id=20006`, `resource_int=1`로 설정해 처치 시 정기구독자 자원 1개를 획득하도록 했다.
- `boss_1.png`, 이동 GIF 프레임, 사망 GIF 프레임을 `Resources/GameKamiStreaming/Sprites/boss/boss_1` 아래에 추가했다.
- 보스도 `DestructiblePieceView`를 사용하는 기물로 생성하고, 이동/사망 연출만 `BossPieceView` 컴포넌트에서 담당하도록 구조를 분리했다.
- 보스는 현재 스테이지의 `boss_id`를 기준으로 1회 스폰된다.
- 보스는 2초마다 4방향 중 이동 가능한 방향을 선택해 이동하며, 이동할 때 `vfx_boss1_01` 프레임 애니메이션을 재생한다.
- 보스 이동 목표는 기존 `spawnpoint1`~`spawnpoint4` 스폰 마름모 범위 안에 들어오는 경우에만 허용한다.
- 보스 체력이 0 이하가 되면 즉시 제거하지 않고 `vfx_boss1_02` 프레임 애니메이션을 재생한 뒤 보상 지급 및 제거를 처리한다.
- 이후 보스는 `BossDefinitions`에 ID, 크기, 이동/사망 프레임 경로만 추가하는 방식으로 확장할 수 있게 했다.
- 검증:
  - `dotnet build kkami_streaming.slnx`
  - 경고 0개
  - 오류 0개
- 관련 파일:
  - `Assets/GameKamiStreaming/Scripts/Runtime/KkamiPrototypeGame.cs`
  - `Assets/GameKamiStreaming/Scripts/Runtime/DestructiblePieceView.cs`
  - `Assets/GameKamiStreaming/Scripts/Runtime/BossPieceView.cs`
  - `Assets/GameKamiStreaming/Resources/GameKamiStreaming/DataTables/piece.csv`
  - `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/boss/boss_1/boss_1.png`
  - `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/boss/boss_1/move/frame_000.png` ~ `frame_007.png`
  - `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/boss/boss_1/death/frame_000.png` ~ `frame_011.png`

## 18. 보스 표시 순서 보강

- 보스가 일반 기물에 가려지지 않도록 보스 RectTransform을 같은 기물 레이어의 마지막 sibling으로 올리도록 했다.
- 일반 기물이 새로 스폰된 직후에도 활성 보스를 다시 앞으로 올리도록 했다.
- 보스 이동 및 사망 애니메이션 재생 중에도 보스가 최상단 표시 순서를 유지하도록 했다.
- 검증:
  - `dotnet build kkami_streaming.slnx`
  - 경고 0개
  - 오류 0개
- 관련 파일:
  - `Assets/GameKamiStreaming/Scripts/Runtime/KkamiPrototypeGame.cs`
  - `Assets/GameKamiStreaming/Scripts/Runtime/BossPieceView.cs`

## 19. 채굴 UI 표시 순서 보강

- 채굴 범위 커서와 채굴 애니메이션은 캔버스 루트 직속이라 기본적으로 보스보다 앞에 표시되는 구조임을 확인했다.
- 보스 이동/사망 중 sibling 순서가 계속 바뀌는 상황에서도 가려지지 않도록 채굴 커서와 채굴 애니메이션을 최상단 sibling으로 다시 올리는 보정을 추가했다.
- 커서 갱신, 채굴 애니메이션 시작, 채굴 애니메이션 위치 갱신 시 `BringMiningVisualsToFront`를 호출하도록 했다.
- 검증:
  - `dotnet build kkami_streaming.slnx`
  - 경고 0개
  - 오류 0개
- 관련 파일:
  - `Assets/GameKamiStreaming/Scripts/Runtime/KkamiPrototypeGame.cs`

## 20. boss_1 이동 값 조정

- 보스 이동 주기를 기존 2초에서 1초로 줄였다.
- 보스 이동 거리를 기존 190 기준의 1.8배인 342로 늘렸다.
- 이동 목표는 기존과 동일하게 `spawnpoint1`~`spawnpoint4`가 만드는 스폰 마름모 안에 보스의 네 모서리가 모두 들어오는 경우에만 허용함을 재확인했다.
- 스폰 범위 밖 후보는 선택하지 않으며, 전체 거리 이동 가능한 4방향 후보가 없으면 범위 안에서 가능한 가장 긴 짧은 이동으로 대체한다.
- 짧은 이동도 불가능한 경우에는 스폰 마름모 중앙으로 이동하도록 했다.
- 검증:
  - `dotnet build kkami_streaming.slnx`
  - 경고 0개
  - 오류 0개
- 관련 파일:
  - `Assets/GameKamiStreaming/Scripts/Runtime/BossPieceView.cs`

## 참고

- `Assets/Scenes/KkamiPrototype.unity`는 작업 시작 시점부터 수정된 상태였으며, 위 기능들은 주로 런타임 코드에서 캔버스와 UI를 생성하는 방식으로 구현했다.
