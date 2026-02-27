GNU Go README (한국어 번역본)

이 프로그램은 바둑 프로그램인 GNU Go입니다. [cite: 4]
프로젝트에 도움을 주시고 싶다면 TODO 파일을 참고해 주세요. [cite: 5]

[설치]
요약하자면, './configure; make' 명령어를 통해 GNU Go를 빌드할 수 있습니다. [cite: 6] 
선택 사항으로 'make install'을 실행하면 /usr/local/bin에 설치되며 매뉴얼 페이지도 함께 설치됩니다. [cite: 6]
또한 CGoban을 설치하는 것을 권장합니다. [cite: 7]

[문서]
사용자 문서는 터미널에서 'gnugo --help' 또는 'man gnugo'를 실행하여 확인할 수 있습니다. [cite: 8]
Texinfo 문서에는 사용자를 위한 지침뿐만 아니라 프로그래머와 개발자를 위한 알고리즘 설명이 포함되어 있습니다. [cite: 9]

[CGoban을 통한 실행]
CGoban은 X 윈도우 시스템에서 아름다운 그래픽 사용자 인터페이스(GUI)를 제공합니다. [cite: 14]
Go Modem Protocol 설정을 통해 플레이어를 "Program"으로 선택하고 GNU Go 경로를 입력하여 연동합니다. [cite: 15, 16]
규칙(Rules Set)을 Japanese로 설정해야 치석 기능이 정상 작동합니다. [cite: 17]

[ASCII 인터페이스]
GUI 없이 터미널에서 'gnugo'를 입력하여 플레이할 수 있습니다. [cite: 28, 29]
게임 종료 후 두 번 패스하면 계가 과정을 안내합니다. [cite: 30]

[주요 옵션]
* --level LEVEL: GNU Go의 실력(기본값 10)을 조절합니다. [cite: 55, 56]
* --mode MODE: 실행 모드('ascii', 'gmp', 'gtp')를 설정합니다. [cite: 61]
* --japanese-rules / --chinese-rules: 일본식 또는 중국식 규칙을 선택합니다. [cite: 66, 67]

[라이선스]
대부분의 파일은 GPL 라이선스를 따르지만, gtp.c와 gtp.h는 GTP 프로토콜의 확산을 위해 무제한 사용이 가능하도록 라이선스가 완화되어 있습니다. [cite: 70, 71, 72]
