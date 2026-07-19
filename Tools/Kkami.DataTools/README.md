# Kkami.DataTools

프로젝트의 XLSX 데이터 변환과 이미지 이름 동기화를 담당하는 .NET 7 C# 도구입니다.

```powershell
dotnet run --project Tools/Kkami.DataTools -- export SourceData/kkami_datatable_ver03.xlsx --project-root .
dotnet run --project Tools/Kkami.DataTools -- export SourceData/kkami_datatable_ver03.xlsx --output Temp/KkamiDataValidation/Raw --runtime-output Temp/KkamiDataValidation/Runtime
dotnet run --project Tools/Kkami.DataTools -- sync-images --project-root . --external-source "C:\path\to\images"
```

인자 없이 실행하면 정식 원본인 `SourceData/kkami_datatable_ver03.xlsx`를 기준으로 다음 위치를 갱신합니다.

- Unity 런타임 CSV: `Assets/GameKamiStreaming/Resources/GameKamiStreaming/DataTables`
- 원본 시트 보관 CSV: `Assets/GameKamiStreaming/DataTables/XlsxExport`

원본 시트 보관 CSV는 플레이어 빌드에 포함되지 않도록 `Resources` 밖에 둡니다.
런타임 CSV를 기록하기 전에 모든 행의 열 수를 헤더와 대조하며, 불일치가 있으면 내보내기를 중단합니다.

프로젝트의 공식 데이터 검증은 아래 명령으로 실행합니다. 실제 런타임 파일을 덮어쓰지 않고 임시 폴더에 내보낸 뒤 테이블 행 수, 기물 이미지 ID, 스테이지 가중치 합, 보스 배치를 검사합니다.

```powershell
powershell -ExecutionPolicy Bypass -File Tools/Validate-KkamiData.ps1
```
