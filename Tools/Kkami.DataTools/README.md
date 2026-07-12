# Kkami.DataTools

프로젝트의 XLSX 데이터 변환과 이미지 이름 동기화를 담당하는 .NET 8 C# 도구입니다.

```powershell
dotnet run --project Tools/Kkami.DataTools -- export SourceData/kkami_datatable_ver02.xlsx --project-root .
dotnet run --project Tools/Kkami.DataTools -- sync-images --project-root . --external-source "C:\path\to\images"
```

인자 없이 실행하면 `SourceData/kkami_datatable_ver02.xlsx`를 기준으로 Unity 런타임 CSV와 `xlsx_export`를 갱신합니다.
