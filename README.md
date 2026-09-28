# 이미지 검색 앱

## 로컬 Pixabay API 키 (Unity Editor 전용)

1. [Pixabay API 문서](https://pixabay.com/api/docs/)에서 개인 API 키를 확인합니다.
2. 프로젝트 루트의 `UserSettings/ImageSearch/` 폴더를 만들고 `pixabay.key` 파일에 키 문자열만 저장합니다. 앞뒤 공백과 줄바꿈은 로딩할 때 제거됩니다.
3. Unity Editor에서 검색 화면을 실행합니다. 키가 있으면 실제 검색 결과와 썸네일을 표시합니다. 키 파일이 없거나 비어 있거나 읽을 수 없으면 경고 로그를 남기고 Mock 검색 데이터로 실행됩니다.

오류 화면을 확인할 때는 씬의 `Image Search Screen` 오브젝트에 있는 `Image Search Composition Root` 컴포넌트에서 `Force Mock`을 켭니다. 키가 있어도 Inspector의 Mock 시나리오를 사용합니다.

`UserSettings/`와 키 파일은 `.gitignore`에서 제외합니다. 실제 키를 `Assets/`, 씬, 설정 파일, 로그, README에 넣지 마세요. 실제 검색 응답은 `UserSettings/ImageSearch/Cache/`에 24시간 저장합니다. Pixabay는 기본적으로 키당 60초에 100요청을 허용하며, 검색 결과에 출처를 표시해야 합니다. 이미지 URL은 일시적 결과 표시용이며 영구 저장·핫링크에는 사용하지 마세요. [Pixabay API 문서](https://pixabay.com/api/docs/)

모바일 빌드는 로컬 키 파일을 읽지 않고 Mock 검색 데이터를 사용합니다. 운영 서비스에서는 Pixabay 키를 서버에 보관하고 앱이 자체 서버의 검색 API를 호출하도록 구성해야 합니다.
