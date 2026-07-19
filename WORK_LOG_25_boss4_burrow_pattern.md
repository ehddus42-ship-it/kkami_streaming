# 25. stage 40 boss4 잠복 패턴 추가

- `stage.csv`의 40스테이지 `boss_id=50004`에 대응하는 `piece.csv` 행을 추가했다.
- `boss4.png` 원본과 `vfx_boss4_01`, `vfx_boss4_01_1`, `vfx_boss4_02` 시트를 Resources에 추가했다.
- `BossPieceView`에 `Burrow` 패턴을 추가했다.
- boss4는 0.7초 대기 후 `vfx_boss4_01`을 재생하며 사라지고, 0.5초 뒤 랜덤 다른 위치에서 `vfx_boss4_01_1`을 재생하며 다시 등장한다.
- 같은 객체를 유지하므로 HP는 그대로 이어진다.
- 사라진 동안에는 채굴 판정을 받지 않도록 `DestructiblePieceView.SetHittable`을 추가했다.
- 애니메이션 진행 중 HP가 0 이하가 되면 반복 패턴을 즉시 멈추고 `vfx_boss4_02` 사망 애니메이션을 재생한다.

## 검증

- `dotnet build kkami_streaming.slnx`
- 경고 0개 / 오류 0개

## 관련 파일

- `Assets/GameKamiStreaming/Scripts/Runtime/KkamiMaster.cs`
- `Assets/GameKamiStreaming/Scripts/Runtime/BossPieceView.cs`
- `Assets/GameKamiStreaming/Scripts/Runtime/DestructiblePieceView.cs`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/DataTables/piece.csv`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/boss/boss_4/boss4.png`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/vfx_boss4_01.png`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/vfx_boss4_01_1.png`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/vfx_boss4_02.png`
