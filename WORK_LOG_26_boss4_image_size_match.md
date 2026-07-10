# 26. boss4 원본 이미지 교체 및 애니메이션 출력 크기 보정

- `boss4.png`를 새 원본 이미지로 교체했다.
- 원본 이미지의 가장자리 흰 배경만 투명 처리해 게임 화면에 흰 사각 배경이 남지 않도록 했다.
- `vfx_boss4_01`, `vfx_boss4_01_1`, `vfx_boss4_02`의 프레임 셀 구조는 16프레임, 512x512로 유지했다.
- 각 시트의 실제 캐릭터 영역을 기준으로 프로젝트 복사본의 프레임 내부 출력 크기를 확대 보정했다.
- 원본 캐릭터 점유율과 애니메이션 최대 프레임 점유율이 비슷하게 보이도록 맞췄다.

## 검증

- `dotnet build kkami_streaming.slnx`
- 경고 0개 / 오류 0개

## 관련 파일

- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/boss/boss_4/boss4.png`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/vfx_boss4_01.png`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/vfx_boss4_01_1.png`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/vfx_boss4_02.png`
