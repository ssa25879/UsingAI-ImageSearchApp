# 이미지 검색 앱

Unity 6 기반 세로형 이미지 검색 UI 앱입니다. 검색 화면은 UI Toolkit으로 구현했으며, 검색과 썸네일 로딩은 Domain Repository 인터페이스를 통해 연결합니다.

## 개발 환경

- Unity Editor: `6000.3.19f1`
- 렌더 파이프라인: URP `17.3.0`
- 비동기 API: UniTask `2.5.11`
- 테스트: Unity Test Framework `1.6.0`

Unity Hub에서 이 폴더를 Unity `6000.3.19f1` 프로젝트로 엽니다. 실제 작업 화면은 `Assets/UI/ImageSearch/Scenes/ImageSearchPreview.unity`입니다. 에디터에서 이 씬을 열고 Play를 누르면 검색 UI를 실행할 수 있습니다.

> 현재 `ProjectSettings/EditorBuildSettings.asset`에는 `Assets/Scenes/SampleScene.unity`만 등록되어 있습니다. 따라서 일반 빌드의 시작 장면은 아직 검색 화면이 아닙니다. 모바일 앱 빌드 전에 Unity 에디터의 Build Profiles에서 검색 화면 씬을 빌드 장면에 등록해야 합니다.

## 검색 데이터 선택

검색 화면의 `Image Search Composition Root`가 실제 Pixabay 구현 또는 Mock 구현을 선택합니다.

- `Force Mock`을 켜면 API 키와 관계없이 Mock을 사용합니다.
- Mock의 `Mock Search Scenario`에서 `Success`, `EmptyResults`, `ConnectionFailure`, `ServerError`를 선택할 수 있습니다. Play 중 값을 바꾸면 다음 검색부터 적용됩니다.
- 에디터에서 `Force Mock`이 꺼져 있고 키 파일이 읽히면 실제 Pixabay 검색과 썸네일 HTTP 요청을 사용합니다.
- 키가 없거나 비어 있으면 경고를 기록하고 Mock으로 실행합니다.
- 모바일 빌드는 로컬 키를 읽지 않으며 Mock을 사용합니다. 운영 서비스에서 앱에 Pixabay 키를 넣지 말고, 서버가 키를 보관하며 앱이 자체 API를 호출하도록 구성해야 합니다.

## Pixabay API 키 설정

Pixabay API 키는 [Pixabay API 문서](https://pixabay.com/api/docs/)에서 발급받습니다. 현재 Editor 개발 코드는 프로젝트의 `UserSettings/ImageSearch/pixabay.key`에서 키를 읽습니다. 폴더를 만들고 파일에 키만 저장하면 되며, 앞뒤 공백과 줄바꿈은 제거됩니다. `UserSettings/`는 `.gitignore`에서 제외되어 있습니다.

이 파일 방식은 현재 구현의 Editor 전용 개발 편의 설정입니다. 프로젝트 [AGENTS.md](AGENTS.md)의 보안 규칙은 비밀 값을 프로젝트 외부의 안전한 저장소 또는 환경 변수로 주입하도록 요구하므로, 팀/CI 사용 전에 키 로더를 외부 경로 또는 환경 변수 방식으로 전환해야 합니다. 키를 소스, 씬, 커밋 파일, 로그, 문서에 넣지 마세요.

Pixabay 검색 응답은 `UserSettings/ImageSearch/Cache/`에 24시간 보관합니다. 이미지 URL은 결과 화면에서만 일시적으로 사용합니다. 실제 결과에는 Pixabay 출처를 표시합니다. Pixabay 콘텐츠의 사용 조건과 표기 기준은 [공식 API 문서](https://pixabay.com/api/docs/)를 확인하세요.

## 테스트 실행

### Unity Test Runner

1. Unity 에디터에서 **Window > General > Test Runner**를 엽니다.
2. **EditMode** 탭에서 오프라인 단위 테스트를 실행합니다.
3. **PlayMode** 탭에서 UI Toolkit 검색 화면 흐름 테스트를 실행합니다.

### Unity CLI

Unity CLI가 설치되어 있고 `unity` 명령을 사용할 수 있는 환경에서는 프로젝트 루트에서 다음 명령으로 각각 실행할 수 있습니다.

```powershell
unity test . --editor-version 6000.3.19f1 --mode EditMode --timeout 600
unity test . --editor-version 6000.3.19f1 --mode PlayMode --timeout 600
```

테스트는 외부 네트워크를 호출하지 않습니다. HTTP DataSource는 URL 생성과 응답 JSON 파싱 같은 네트워크 없는 동작을 테스트하고, Repository/화면 동작은 Fake 또는 Mock으로 검증합니다. 실제 Pixabay 연결은 키가 설정된 로컬 Editor에서 별도로 확인합니다.

마지막 기록 검증은 EditMode `53/53`, PlayMode `8/8` 통과입니다. 이 수치는 기록된 이전 실행 결과이며 현재 체크아웃에서 이번 문서 변경을 검증하기 위해 테스트를 다시 실행한 것은 아닙니다.

## 구조

레이어 의존 방향은 `Presentation → Domain ← Data`, 공통 타입은 `Core`, 구체 구현 연결은 `Composition Root`입니다. 각 주요 레이어는 별도 asmdef로 분리되어 있습니다.

```text
Assets/Scripts/Core/                 Result<T>, NetworkError
Assets/Scripts/Domain/               ImageItem, Repository 인터페이스
Assets/Scripts/Data/Pixabay/         Pixabay DTO, HTTP DataSource, Mapper, Repository
Assets/UI/ImageSearch/Runtime/       UI Toolkit 화면과 SearchScreenPresenter
Assets/Scripts/Composition/          실제/Mock 구현 선택 및 의존성 조립
Assets/Tests/EditMode/               Core, Domain, Data, Presentation 단위 테스트
Assets/Tests/PlayMode/               검색 UI 흐름 테스트
```

상세 클래스와 검색/취소 시퀀스는 [diagram.puml](diagram.puml)에서 확인할 수 있습니다. PlantUML 뷰어에서 이 파일을 열어 다이어그램으로 렌더링하세요.

## 알려진 점

- 빌드 장면 목록이 아직 검색 화면을 포함하지 않습니다. 위 실행 방법은 Editor에서 미리보기 씬을 직접 여는 절차입니다.
- `SearchScreenPresenter`가 취소 예외를 내부에서 잡고 종료합니다. 현재 동작은 `AGENTS.md`의 “취소는 `OperationCanceledException`으로 전파” 규칙과 다르므로, UI 취소 경계의 예외 적용 범위를 정리해야 합니다.
- 화면은 현재 첫 페이지 검색만 요청합니다. 다음 페이지 로드/무한 스크롤은 구현 범위에 포함되어 있지 않습니다.
