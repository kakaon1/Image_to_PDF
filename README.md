# Image_to_PDF

JPG / JPEG / PNG 이미지 파일을 파일명 뒷번호 순서대로 정렬하여 PDF 한 파일로 병합하는 도구입니다.
사진 1장이 PDF 1페이지(A4)로 저장되며, 이미지는 비율을 유지하며 A4에 맞게 자동 축소됩니다.

---

## 사용 방법

1. `Image_to_PDF.exe` 실행 → exe 옆에 `merge` 폴더 자동 생성
2. `merge` 폴더 또는 그 하위 폴더에 이미지 파일 복사
3. `Image_to_PDF.exe` 다시 실행 → 변환 시작 → PDF 생성 → 2초 후 자동 종료

| 이미지 위치 | 출력 파일 |
|------------|----------|
| `merge/` 루트 | `output.pdf` |
| `merge/폴더명/` | `폴더명.pdf` |

```
[최초 실행 — merge 폴더 없을 때]
=== Image_to_PDF ===
merge 폴더를 생성했습니다: C:\...\merge
변환할 이미지를 merge 폴더에 넣고 다시 실행하세요.

[이미지 넣은 후 재실행]
=== Image_to_PDF ===
변환 시작 — 3개 파일

  추가: scan_1.jpg  [2480×3508px → 595×842pt]
  추가: scan_2.jpg  [2480×3508px → 595×842pt]
  추가: scan_3.jpg  [2480×3508px → 595×842pt]

완료: C:\...\output.pdf
2초 후 자동 종료...
```

---

## 정렬 기준

파일명 끝에 있는 숫자를 기준으로 오름차순 정렬합니다.

```
photo_1.jpg   → 1페이지
photo_2.jpg   → 2페이지
photo_10.jpg  → 3페이지
scan003.png   → 4페이지
```

---

## 지원 포맷

| 확장자 | 지원 여부 |
|--------|----------|
| .jpg   | O |
| .jpeg  | O |
| .png   | O |

---

## 빌드

```bash
dotnet build -c Release
```

- 결과물: `bin\Release\Publish\Image_to_PDF.exe`
- .NET 8.0 / win-x64 / 단일 실행 파일 (self-contained)

---

## 개발 환경

| 항목 | 내용 |
|------|------|
| 언어 | C# / .NET 8.0 |
| OS | Windows 11 |
| 라이브러리 | itext7 7.2.6, SixLabors.ImageSharp 3.1.12, System.Drawing.Common 8.0.0 |
"# TreeView_Launcher" 
"# TreeView_Launcher" 
