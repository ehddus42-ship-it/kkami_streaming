# 27. boss1/boss2 idle 애니메이션 출력 복구

- boss1, boss2는 원본 PNG를 idle 표시용으로 쓰지 않고 이동 애니메이션 시트 첫 프레임을 idle 기본 스프라이트로 사용하게 했다.
- boss1, boss2에 `animateIdleWithMoveAnimation` 옵션을 켰다.
- 이동형 보스가 다음 이동을 기다리는 동안 `vfx_boss1_01`, `vfx_boss2_01` 프레임을 루프 재생하게 했다.
- 사망 애니메이션은 기존 `vfx_bossn_02` 흐름을 유지했다.

## 검증

- `dotnet build kkami_streaming.slnx`
- 경고 0개 / 오류 0개

## 관련 파일

- `Assets/GameKamiStreaming/Scripts/Runtime/KkamiMaster.cs`
- `Assets/GameKamiStreaming/Scripts/Runtime/BossPieceView.cs`
