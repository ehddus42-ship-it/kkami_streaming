# 31. boss3 빠른 이동 패턴 및 애니메이션 추가

- `vfx_boss3_01.png`, `vfx_boss3_02.png`를 기존 보스 리소스 네이밍 규칙에 맞춰 추가했다.
- boss3 이동 시트는 8프레임, 사망 시트는 12프레임으로 연결했다.
- boss3은 0.5초마다 이동하며, boss2의 이동 거리와 가장 빠른 이동 시간인 0.14초를 고정 사용한다.
- 이동 가능 영역과 경계 반사 판정은 기존 이동형 보스와 동일하게 `TryGetBossMovePath`를 사용한다.
- boss3 기본 이미지 및 VFX 데이터 테이블 항목을 보강했다.

## 변경 파일

- `Assets/GameKamiStreaming/Scripts/Runtime/KkamiPrototypeGame.cs`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/DataTables/piece.csv`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/DataTables/xlsx_export/res_vfx.csv`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/vfx_boss3_01.png`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/vfx_boss3_01.png.meta`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/vfx_boss3_02.png`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/vfx_boss3_02.png.meta`
