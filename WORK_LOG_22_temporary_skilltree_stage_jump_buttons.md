# 22. 스킬 트리 임시 스테이지 이동 버튼 추가

- 스킬 트리 콘텐츠 안에 `Temporary Stage Jump Buttons` 그룹을 추가했다.
- 그룹 안에 9, 19, 29, 39, 49 스테이지로 바로 이동하는 테스트용 버튼을 배치했다.
- 버튼 이미지는 기존 테스트 버튼과 같은 `skilltree_button_test` 스프라이트를 사용한다.
- 각 버튼은 숫자 라벨만 표시하며, 클릭 시 해당 스테이지 번호로 이동한 뒤 기존 스테이지 시작 루틴을 재사용한다.
- 테스트 이후 제거하기 쉽도록 임시 버튼 관련 상수/생성/보정/클릭 함수 이름에 `TemporaryStageJump`를 붙였다.

## 검증

- `dotnet build kkami_streaming.slnx`
- 경고 0개 / 오류 0개

## 관련 파일

- `Assets/GameKamiStreaming/Scripts/Runtime/KkamiMaster.cs`
