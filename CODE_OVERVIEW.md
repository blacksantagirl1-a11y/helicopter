# Chu thich tong quan code du an

Tai lieu nay tom tat cong viec cua cac nhom script chinh trong Unity project. Cac plugin/asset ben ngoai nhu DOTween, RainMaker, SlimUI, Kino Glitch, Bitgem Water, TreePack va Unity DefaultPlayables khong duoc chu thich chi tiet vi do la code thu vien.

## Menu va save/load

- `Assets/MainMenu.cs`: dieu khien man hinh menu chinh, nut New Game/Load/Quit, nhac nen main menu, fullscreen/window, slider am thanh va mouse sensitivity.
- `Assets/MenuManager.cs`: dieu khien pause menu trong scene gameplay bang phim `Z`, tat/bat UI, khoa chuot va tam tat cac script dieu khien player khi mo menu.
- `Assets/SaveManager.cs`: trung tam luu va tai game. Script gom du lieu player, inventory, story, quest, dialogue thanh `AllGameData`, ghi/doc JSON hoac binary, roi restore lai scene khi load.
- `Assets/SaveSlot.cs`: nut save theo tung slot, hien mo ta slot va popup canh bao khi ghi de save cu.
- `Assets/LoadSlot.cs`: nut load theo tung slot, chi cho load khi slot co file save.
- `Assets/AllGameData.cs`: cac lop du lieu serialize cho save file: player, inventory, story, quest va dialogue.
- `Assets/PlayerData.cs`: du lieu save rieng cua player nhu chi so sinh ton, vi tri, goc xoay va stamina.
- `Assets/SettingsManager.cs`: man settings cu, doc/ghi volume qua `SaveManager`.

## Player va dieu khien

- `Assets/scripts/Player/PlayerState.cs`: luu trang thai song con cua player nhu mau, calories, hydration va lien ket den body player.
- `Assets/scripts/Player/PlayerMovement.cs`: xu ly di chuyen nhan vat theo input.
- `Assets/scripts/Player/PlayerLook.cs` va `MouseMovement.cs`: xoay camera/huong nhin bang chuot.
- `Assets/scripts/Player/Crouch.cs`: xu ly cui nguoi.
- `Assets/scripts/Player/ActionScript.cs`: xu ly hanh dong tuong tac/chinh cua player.
- `Assets/scripts/UI/Stamina.cs`: quan ly thanh stamina, tieu hao va hoi phuc suc ben.

## Tuong tac trong scene

- `Assets/scripts/Interact/Interactable.cs`: lop/giao dien nen cho cac vat the co the tuong tac.
- `Assets/scripts/Interact/PickUpScript.cs`: xu ly viec nhan input pickup/interact tu player.
- `Assets/scripts/Interact/SimplePickup.cs`, `TreeLogPickup.cs`, `MeatPickup.cs`: cac vat pham co the nhat.
- `Assets/scripts/Interact/Door.cs`: logic mo/cua hoac chuyen trang thai cua cua.
- `Assets/scripts/Interact/CuttingTreeSystem.cs`: he thong chat cay/lay go.
- `Assets/scripts/Interact/HintDay3Interactable.cs`, `PCVideoInteractable.cs`, `InteractableOnce.cs`: cac tuong tac dac biet gan voi tien trinh story.

## Inventory va nau an

- `Assets/scripts/Inventory/PlayerInventory.cs`: luu cac slot item cua nguoi choi va thao tac them/xoa item.
- `Assets/scripts/Inventory/InventoryItemDefinition.cs`: du lieu ScriptableObject mo ta item, id, icon va thong tin hien thi.
- `Assets/scripts/Inventory/InventoryUIController.cs`: hien/ an UI inventory va dong bo slot len man hinh.
- `Assets/scripts/Inventory/InventorySlotClickHandler.cs`: xu ly click vao tung slot UI.
- `Assets/scripts/Inventory/InventoryPickup.cs`: dua item pickup vao inventory.
- `Assets/scripts/Inventory/CampingCookingInteractable.cs`, `CampingCookingModeController.cs`, `MiniGameCookingController.cs`: chuoi logic nau an/camping va mini game cooking.

## Quest va story

- `Assets/scripts/Quest/DailyQuestManager.cs`: quan ly quest theo ngay, tien trinh muc tieu, hoan thanh quest va cac trang thai dac biet ngay 3/5/6.
- `Assets/scripts/Quest/DailyQuestDefinition.cs` va `DailyQuestDatabase.cs`: du lieu cau hinh quest.
- `Assets/scripts/Quest/DailyQuestId.cs`, `QuestObjectiveType.cs`: enum dinh danh quest va loai muc tieu.
- `Assets/scripts/Quest/GatherWoodTurnInInteractable.cs`, `Day3BundOfWoodInteractable.cs`, `Day3BedAdvanceInteractable.cs`, `Day5DataCubeInteractable.cs`: cac diem tuong tac gan voi quest cu the.

## Dialogue va ngay choi

- `Assets/scripts/Dialogue/DialogueController.cs`: chay hoi thoai, hien tung line va xu ly ket thuc dialogue.
- `Assets/scripts/Dialogue/DialogueDatabase.cs`, `DialogueEntry.cs`, `DialogueLineData.cs`: du lieu noi dung hoi thoai.
- `Assets/scripts/Dialogue/DialogueDay.cs`, `DialogueEventId.cs`: enum ngay va su kien hoi thoai.
- `Assets/scripts/Dialogue/DialogueTrigger.cs`: kich hoat dialogue khi player vao/tuong tac voi trigger.
- `Assets/scripts/Dialogue/DialogueSaveService.cs`: luu ngay va tien trinh dialogue vao PlayerPrefs.
- `Assets/scripts/Dialogue/DialogueNewGameResetService.cs`, `DayReset.cs`: reset du lieu khi bat dau game moi hoac sang ngay.
- `Assets/scripts/Dialogue/Day3HintSequenceController.cs`, `Day6LostSignalController.cs`: logic story dac biet cho ngay 3 va ngay 6.

## Audio, loading va hieu ung

- `Assets/scripts/AudioManager/MusicManager.cs`: phat/dung nhac nen theo track.
- `Assets/scripts/AudioManager/MusicLibrary.cs`: danh sach track nhac nen.
- `Assets/scripts/AudioManager/ReSoundManager.cs` va `SoundLibrary.cs`: phat sound effect theo thu vien am thanh.
- `Assets/scripts/LoadingManager.cs`: load scene qua man loading neu co, fallback ve `SceneManager.LoadScene`.
- `Assets/scripts/RainDayController.cs`: dieu khien mua theo ngay/story.
- `Assets/scripts/Rendering/HintDay3KinoGlitchRendererFeature.cs` va `HintDay3KinoGlitchState.cs`: bat/tat glitch render effect cho hint ngay 3.

## Cutscene va clue vision

- `Assets/scripts/Cutscene/IntroSequenceController.cs`: dieu khien cutscene/gioi thieu dau game.
- `Assets/scripts/Cutscene/CutsceneTrigger.cs`: kich hoat cutscene khi den trigger.
- `Assets/scripts/Clue.cs`: du lieu/hanh vi cua clue trong scene.
- `Assets/scripts/ClueVision.cs`: che do nhin clue.
- `Assets/scripts/ClueVisionCamera.cs`: camera/render ho tro clue vision.
