# Card game
1. 데이터 (Core, Poker_Card)
파일	역할
Core/Suit.cs	문양 enum입니다. Spade, Heart, Diamond, Clover.
Core/Rank.cs	숫자 enum입니다. Two(2)~Ten(10)과 Ace(14). J·Q·K는 덱에 없어서 정의하지 않았습니다.
Player/PokerCardData.cs	카드 1장의 데이터(문양, 숫자, 스프라이트)입니다. 읽기 전용 프로퍼티 Suit, Rank, Sprite만 있습니다.
Player/PokerCardDatabase.cs	40장을 담는 ScriptableObject입니다. 프로퍼티 Cards로 목록을 읽습니다.

2. 게임 로직 (Player, 화면과 무관한 순수 C#)
클래스	메서드와 속성	역할
PokerDeck	Reset()	40장을 전부 되돌려 놓고 섞습니다(Fisher-Yates).
	TryDraw(out card)	1장을 뽑아 덱에서 제거합니다. 비면 false를 돌려줍니다. 리롤에 사용합니다.
	DrawHand(count)	count장을 뽑아 목록으로 돌려줍니다.
	RemainingCount	남은 카드 수입니다.
PokerHandSelection	TryToggle(index)	선택과 해제를 바꿉니다. 이미 5장이면 새 선택은 무시하고 false를 돌려줍니다.
	Remove(index)	해당 자리의 선택만 해제합니다. 리롤에서 씁니다.
	Clear()	선택을 모두 풉니다.
	IsSelected, Count, SelectedIndices	선택 상태를 읽습니다.
	SelectionChanged 이벤트	선택이 바뀔 때 화면 갱신용으로 발생합니다.
PokerRerollCounter	TryConsume()	횟수를 1 쓰고 성공 여부를 돌려줍니다.
	Reset()	횟수를 처음 값으로 되돌립니다.
	Remaining, MaxCount	남은 횟수와 최대 횟수입니다.

3. 화면 (UI)
클래스	역할
PokerCardSlot	카드 1칸을 담당합니다. 클릭을 받아 Clicked(index) 이벤트로 알립니다. Initialize, SetSprite, SetSelected(선택 시 위로 올림), SetDimmed(어둡게)가 있습니다.
PokerSubmitButton	제출 버튼입니다. SetState(canSubmit, isSubmitted)로 활성화와 문구("제출 확인"과 "제출 취소")를 바꾸고, Clicked 이벤트를 보냅니다.
PokerRerollButton	리롤 버튼입니다. SetState(remaining, max, isRerollMode, canReroll)로 문구("리롤 1/1"과 "리롤 취소")와 활성화를 바꾸고, Clicked 이벤트를 보냅니다.
PokerHandView	위 부품들을 연결하는 중심 클래스입니다. 아래 표를 보세요.

PokerHandView의 메서드

Awake: 덱, 선택 모델, 리롤 카운터를 만들고 슬롯에 번호를 부여합니다.
OnEnable과 OnDisable: 이벤트를 구독하고 해제합니다.
Start와 Deal: 덱을 섞어 7장을 나눠 주고 상태를 초기화합니다. Deal은 인스펙터 우클릭 메뉴로도 실행할 수 있습니다(Play 중에만 동작).
OnSlotClicked: 카드를 누르면 선택을 바꾸거나, 리롤 모드면 Reroll로 보냅니다. 제출 중에는 무시합니다.
OnRerollButtonClicked: 리롤 모드를 켜고 끕니다.
Reroll: 카드를 새로 뽑아 교체합니다. 그 자리의 선택은 해제하고, 뽑기에 실패하면 횟수를 쓰지 않습니다.
OnSubmitButtonClicked: 제출과 취소를 처리합니다. 5장일 때만 제출됩니다.
RefreshView: 슬롯과 두 버튼의 모양을 현재 상태에 맞춥니다.
바깥으로 내보내는 이벤트: HandSubmitted(선택한 5장 전달), SubmitCancelled

4. 에셋과 씬
이미지: Assets/Sprites/PokerCards/에 40장(A, 2~10 × 4문양)을 복사하고 스프라이트(Single)로 설정했습니다.
데이터: Assets/Data/PokerCardDatabase.asset에 40칸을 채웠습니다.
씬: Assets/Scenes/Card.unity입니다.
Canvas(Overlay)에 HandRow(카드 7칸: Card1~Card7과 자식 Face), SubmitButton, RerollButton
PokerHandViewer(PokerHandView 부착)
EventSystem(새 Input System용)

5. 전체 동작 흐름
Play 시작 → 덱을 섞어 7장을 보여 줍니다.
카드를 눌러 5장까지 선택합니다(위로 올라감).
필요하면 "리롤"을 눌러 카드 1장을 교체합니다.
5장이 되면 "제출 확인"을 누릅니다. 빠진 카드는 어두워지고 클릭이 잠깁니다.
"제출 취소"를 누르면 다시 고를 수 있습니다.
