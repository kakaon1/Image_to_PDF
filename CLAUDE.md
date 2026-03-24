# CLAUDE.md — 에이전트 작업 지침서

> 적용 대상: Claude / ChatGPT / Gemini 등 모든 AI 에이전트 공통 사용

---

## 1. 프로젝트 개요

| 항목 | 내용 |
|------|------|
| 프로젝트명 | Image_to_PDF |
| 목적 | JPG/PNG 이미지를 파일명 뒷번호 순으로 정렬 후 1장 1페이지로 PDF 변환 |
| 상태 | 개발 완료 (v1.0) |

---

## 2. 환경 정보

| 항목 | 내용 |
|------|------|
| 운영체제 | Windows 11 Pro (10.0.26200) |
| 셸 | bash (Unix 문법 사용) |
| 언어 | C# / .NET 8.0 |
| 라이브러리 | itext7 7.2.6, SixLabors.ImageSharp 3.1.12, System.Drawing.Common 8.0.0 |

---

## 3. 프로젝트 구조

```
Image_to_PDF/
├── Image_to_PDF.csproj ← 프로젝트 파일 (Release 빌드 시 자동 Publish 포함)
├── Program.cs          ← 진입점 및 전체 로직
├── CLAUDE.md           ← 에이전트 작업 지침서
├── README.md           ← 사용자 문서
└── bin/
    └── Release/
        └── Publish/
            ├── Image_to_PDF.exe ← 단일 실행 파일 (win-x64, self-contained)
            └── merge/           ← 변환할 이미지 넣는 폴더 (실행 시 자동 생성)
```

---

## 4. 실행 방법

```bash
Image_to_PDF.exe
```

1. 실행하면 exe 옆에 `merge` 폴더 자동 생성 (최초 1회) → 이미지를 넣고 다시 실행
2. `merge` 폴더에 파일이 없으면 안내 메시지 출력 후 2초 뒤 자동 종료
3. 파일이 있으면 즉시 변환 → exe 위치에 `output.pdf` 생성 → 2초 뒤 자동 종료

- **파일명 끝 숫자 오름차순** 정렬 (예: `scan_1.jpg` → `scan_2.jpg` → `scan_10.jpg`)
- 사진 1장 = PDF 1페이지, A4 기준 비율 유지 중앙 정렬

---

## 5. 빌드 방법

| 방법 | 명령 |
|------|------|
| CLI | `dotnet build -c Release` |
| Visual Studio | Release 모드로 빌드 → 자동 Publish 실행 |

- 결과물: `bin\Release\Publish\Image_to_PDF.exe` (단일 파일, pdb 없음)
- 빌드 시 자동 게시 설정: `.csproj` 내 `AutoPublish` 타겟

---

## 6. 핵심 로직 요약

| 단계 | 내용 |
|------|------|
| 1 | `Environment.ProcessPath`로 exe 위치 확인 |
| 2 | `merge` 폴더 없으면 생성 후 안내 메시지 출력 → 종료 |
| 3 | `merge` 폴더에 파일 없으면 안내 메시지 출력 → 종료 |
| 4 | 파일명 끝 숫자 추출(`Regex`) → 오름차순 정렬 |
| 5 | JPEG: raw 바이트 그대로 itext7에 전달 (화질 손실 없음) |
| 6 | PNG: ImageSharp으로 로드 → 흰 배경 합성(`CompositeOnWhite`) → RGB PNG |
| 7 | 로딩 실패 시 System.Drawing 폴백 |
| 8 | A4 페이지(가로/세로 자동) + 비율 유지 스케일 + 중앙 정렬 |
| 9 | `PdfCanvas.AddImageWithTransformationMatrix()` 로 배치 |
| 10 | merge 루트 이미지 → `output.pdf`, 직하위 폴더 이미지 → `{폴더명}.pdf` 저장 |
| 11 | 완료 후 2초 후 자동 종료 |

---

## 7. 워크플로우

```
작업지시
→ 소스 수정 / 삭제 / 추가
→ 전체 소스 점검
→ 빌드
→ CLAUDE.md 및 README.md 업데이트
→ 작업 결과 기록
```

---

## 8. 반복 오류 기록

| 날짜 | 오류 | 원인 | 조치 |
|------|------|------|------|
| 2026-03-24 | CS0104 `Path` 모호한 참조 | `iText.Kernel.Geom.Path` vs `System.IO.Path` 충돌 | `using SysPath = System.IO.Path;` 별칭 적용 |
| 2026-03-24 | `Image format cannot be recognized` (PNG) | itext7이 비표준 PNG(인덱스 컬러 등) 직접 처리 불가 | `SixLabors.ImageSharp`으로 Rgba32 정규화 후 전달 |
| 2026-03-24 | `Parameter is not valid` (PNG) | `System.Drawing`도 16비트·특수 컬러 PNG 처리 불가 | `System.Drawing.Common` 제거, `SixLabors.ImageSharp 3.1.12`로 교체 |
| 2026-03-24 | NU1902/NU1903 취약점 경고 | `SixLabors.ImageSharp` 3.1.5~3.1.6에 알려진 취약점 | 3.1.12로 업그레이드하여 해결 |
| 2026-03-24 | `Image cannot be loaded` (JPG) | ImageSharp이 CMYK JPEG 등 특수 JPG 처리 불가 | `System.Drawing.Common` 폴백 추가 |
| 2026-03-24 | PDF 페이지 크기 초과 | 이미지 픽셀 크기 그대로 페이지 크기로 사용 | A4 고정 + 비율 유지 스케일로 변경 |
| 2026-03-24 | 이미지 픽셀 깨짐 (PNG 알파) | RGBA PNG를 PDF에 직접 삽입 시 투명 영역이 검게 렌더링 | 흰 배경 합성(`CompositeOnWhite`) 후 RGB PNG로 저장 |
| 2026-03-24 | JPEG 화질 손실 | JPEG → PNG 재인코딩 과정에서 품질 저하 | JPEG는 raw 바이트 그대로 itext7에 전달 |
| 2026-03-24 | 변환 완료 후 창 미종료 | FileSystemWatcher 방식으로 계속 대기 | 원샷 방식으로 변경, 완료 후 2초 자동 종료 |
| 2026-03-24 | `OverflowException` (파일명 숫자) | `ExtractTrailingNumber` 반환형 `int` → 21억 초과 파일명에서 오버플로 | 반환형을 `long`으로 변경, `long.TryParse`로 안전 처리 |
| 2026-03-24 | 특수문자 깨짐 (`—` → `?`) | `Console.OutputEncoding` 미설정으로 em dash 등 유니코드 문자가 `?`로 출력 | `Main()` 진입부에 `Console.OutputEncoding = UTF8` 추가 |

---

## 9. 코드맵

> 현재 오류 없음. 빌드 오류 발생 시 별도 코드맵 문서 생성.

---

## 변경 이력

| 날짜 | 변경 내용 | 구분 |
|------|-----------|------|
| 2026-03-24 | 최초 문서 작성 (신규 프로젝트, 언어 미결정) | 추가 |
| 2026-03-24 | C# / .NET 8.0 언어 결정 및 v1.0 구현 완료 | 추가 |
| 2026-03-24 | merge 폴더 자동 생성 + FileSystemWatcher 자동 감지 변환 기능 추가 | 수정 |
| 2026-03-24 | PNG 파일 지원 추가 | 수정 |
| 2026-03-24 | PNG 인식 오류 수정 — SixLabors.ImageSharp 3.1.12로 교체, 모든 PNG 변형 처리 | 수정 |
| 2026-03-24 | FileSystemWatcher → 원샷 방식으로 변경, 2초 자동 종료 | 수정 |
| 2026-03-24 | 프로젝트명 및 exe 이름 `jpg_to_pdf` → `Image_to_PDF` 로 변경 | 수정 |
| 2026-03-24 | 전체 소스 점검 — `long.TryParse` 적용, `Console.OutputEncoding = UTF8` 추가 | 수정 |
| 2026-03-24 | merge 하위 폴더 이미지 미탐색 — `TopDirectoryOnly` → `AllDirectories` 로 변경 | 수정 |
| 2026-03-24 | Program.cs 타이틀 문자열 `jpg_to_pdf` → `Image_to_PDF` 수정, CLAUDE.md 실행 방법·결과물 경로 구 이름 잔존 수정 | 수정 |
| 2026-03-24 | Image_to_PDF.sln 프로젝트 참조 `jpg_to_pdf.csproj` → `Image_to_PDF.csproj` 수정, README.md 전체 이름 교체, dotnet restore로 obj NuGet 캐시 갱신 | 수정 |
