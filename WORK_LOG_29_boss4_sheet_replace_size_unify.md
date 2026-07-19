# 29. boss4 시트 교체 및 애니메이션 출력 크기 통일

- 새 `vfx_boss4_1.png`를 프로젝트 Resources에 추가했다.
- boss4의 사라짐 애니메이션 경로를 `vfx_boss4_01`에서 `vfx_boss4_1`로 변경했다.
- boss4의 사라짐, 재등장, 사망 애니메이션 시트는 모두 16프레임, 512x512 프레임 구조를 유지했다.
- 세 시트 모두 최대 캐릭터 출력 크기 기준이 같아지도록 프레임 내부 크기를 보정했다.

## 검증

- `dotnet build kkami_streaming.slnx`
- 경고 0개 / 오류 0개

## 관련 파일

- `Assets/GameKamiStreaming/Scripts/Runtime/KkamiMaster.cs`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/vfx_boss4_1.png`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/vfx_boss4_01_1.png`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/vfx_boss4_02.png`
