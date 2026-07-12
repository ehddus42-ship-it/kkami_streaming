# 32. boss5 공중 이동 패턴 및 애니메이션 추가

- `vfx_boss5_01.png`, `vfx_boss5_02.png`를 기존 보스 리소스 네이밍 규칙에 맞춰 추가했다.
- boss5 이동 시트는 12프레임, 사망 시트는 16프레임으로 연결했다.
- `BossPieceView`에 `Airborne` 패턴을 추가했다.
- boss5은 제자리에서 완전히 투명해진 다음 그림자 없이 이동하고, 도착한 위치에서 다시 나타난다.
- boss5 표시 크기는 최초 설정의 2배이며, 이동이 시작되기 전에 본체가 완전히 투명해진다.
- 2배 표시 크기로 인해 이동 경로가 막히지 않도록 boss5의 이동 경계 계산 크기는 원래 크기로 분리했다.
- 공중 이동 중에도 타격 판정은 유지되며, 이동 영역과 경계 반사는 기존 이동형 보스의 `TryGetBossMovePath`를 공유한다.
- boss5 기본 이미지 및 VFX 데이터 테이블 항목을 보강했다.

## 변경 파일

- `Assets/GameKamiStreaming/Scripts/Runtime/BossPieceView.cs`
- `Assets/GameKamiStreaming/Scripts/Runtime/KkamiPrototypeGame.cs`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/DataTables/piece.csv`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/DataTables/xlsx_export/res_vfx.csv`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/vfx_boss5_01.png`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/vfx_boss5_01.png.meta`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/vfx_boss5_02.png`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/vfx_boss5_02.png.meta`
