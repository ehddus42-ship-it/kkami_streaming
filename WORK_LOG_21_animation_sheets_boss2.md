# 21. 애니메이션 시트 로딩 및 20스테이지 보스 추가

- 기존 프레임 폴더 방식 애니메이션을 시트 이름 우선 로딩 구조로 변경했다.
- `vfx_kkami_01`을 채굴 애니메이션 시트 경로로 연결하고, 시트가 없으면 기존 `mining_attack/frame_000...` 프레임을 사용하도록 유지했다.
- `vfx_boss1_01`, `vfx_boss1_02`를 10스테이지 보스 이동/사망 애니메이션 시트 경로로 연결하고 기존 보스1 프레임 폴더를 폴백으로 유지했다.
- `vfx_boss2_01`, `vfx_boss2_02`를 20스테이지 보스 이동/사망 애니메이션 시트 경로로 연결했다.
- `piece.csv`에 `50002,boss_2,20006,1,40,boss/boss_2/boss_2,`를 추가해 20스테이지 보스도 처치 시 정기구독자 자원 1개를 지급하도록 했다.
- 20스테이지 보스는 기존 보스 규칙을 공유하되 이동 시간은 보통/빠름/매우빠름/엄청빠름 4단계 중 매 이동마다 랜덤으로 선택하도록 했다.
- 보스 idle 이미지가 없을 때는 이동 애니메이션 첫 프레임을 표시용 스프라이트로 대신 사용하도록 했다.

## 검증

- `dotnet build kkami_streaming.slnx`
- 경고 0개 / 오류 0개

## 관련 파일

- `Assets/GameKamiStreaming/Scripts/Runtime/KkamiPrototypeGame.cs`
- `Assets/GameKamiStreaming/Scripts/Runtime/BossPieceView.cs`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/DataTables/piece.csv`
