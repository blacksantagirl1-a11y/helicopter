# BÁO CÁO ĐỒ ÁN TỐT NGHIỆP
## Đề tài: Xây dựng game phiêu lưu sinh tồn 3D trên nền tảng Unity

> Ghi chú sử dụng:
> - Tài liệu này được viết lại theo cấu trúc của file mẫu `Assets/dath-do-an-tot-nghiep-game-bao-cao-tot-nghiep-chi-tiet.pdf`.
> - Nội dung chỉ giữ những phần đã thấy rõ trong project hiện tại. Những tính năng còn dở, dữ liệu mẫu hoặc chưa khép kín không được xem là kết quả hoàn thiện.
> - Ở nhiều mục mình đã thêm dòng `Gợi ý ảnh chèn` để bạn biết nên chụp ảnh gì và chèn vào đâu khi hoàn thiện báo cáo Word.

## LỜI MỞ ĐẦU

Trong giai đoạn hiện nay, game không chỉ là một sản phẩm giải trí mà còn là một lĩnh vực kết hợp nhiều mảng kiến thức như lập trình, thiết kế tương tác, đồ họa, âm thanh và tư duy xây dựng hệ thống. Với sự phát triển mạnh của các game engine hiện đại, việc xây dựng một sản phẩm game có thể chơi được đã trở nên khả thi hơn đối với sinh viên công nghệ thông tin, đặc biệt là khi sử dụng Unity và ngôn ngữ C#.

Đề tài này tập trung xây dựng một game 3D góc nhìn thứ nhất trên nền tảng Unity. Trong game, người chơi có thể di chuyển trong môi trường tự nhiên, tương tác với các đối tượng trong scene, thu thập tài nguyên, chặt cây, săn lợn rừng, câu cá và quản lý vật phẩm thông qua giao diện túi đồ. Bên cạnh đó, hệ thống còn có menu chính, phần cài đặt, loading scene và một số thành phần giao diện hỗ trợ trải nghiệm người chơi.

Mục tiêu chính của đề tài là xây dựng một sản phẩm game mẫu có vòng lặp gameplay cơ bản nhưng rõ ràng, thể hiện được quy trình phát triển game từ khâu tìm hiểu công nghệ, phân tích yêu cầu, thiết kế hệ thống đến cài đặt và đánh giá kết quả. Đề tài cũng hướng tới việc rèn luyện khả năng tổ chức project Unity, tách các module gameplay và hiện thực hóa một sản phẩm có tính tương tác thời gian thực.

Phạm vi của báo cáo chỉ tập trung vào những chức năng đã được thể hiện rõ trong mã nguồn, scene và cấu hình build hiện tại, bao gồm: menu chính, loading, điều khiển nhân vật, cơ chế tấn công, chặt cây, săn lợn rừng, câu cá, inventory và một số thành phần giao diện đi kèm. Các phần như dữ liệu hội thoại hoàn chỉnh, hệ thống lưu tiến trình đầy đủ hoặc cốt truyện hoàn chỉnh hiện chưa được xem là hạng mục đã hoàn thành.

**Gợi ý ảnh chèn:**
- Hình MĐ.1: Ảnh tổng quan scene gameplay trong lúc nhân vật đang đứng giữa môi trường rừng.
- Hình MĐ.2: Ảnh menu chính của game.

## LỜI CẢM ƠN

Em xin chân thành cảm ơn quý thầy cô trong khoa đã trang bị cho em những kiến thức nền tảng trong suốt quá trình học tập. Đặc biệt, em xin gửi lời cảm ơn đến giảng viên hướng dẫn đã hỗ trợ, góp ý và định hướng để em có thể hoàn thành đề tài này.

Em cũng xin cảm ơn gia đình, bạn bè và những người đã động viên, hỗ trợ em trong quá trình thực hiện đồ án. Trong quá trình xây dựng sản phẩm và hoàn thiện báo cáo, mặc dù đã cố gắng rất nhiều, đề tài chắc chắn vẫn còn những hạn chế nhất định. Em rất mong nhận được những ý kiến đóng góp từ thầy cô để tiếp tục hoàn thiện sản phẩm trong thời gian tới.

> Ghi chú:
> - Bạn có thể thay đoạn này bằng lời cảm ơn cá nhân hóa hơn, bổ sung tên giảng viên hướng dẫn, tên khoa và trường.

## CHƯƠNG 1: TỔNG QUAN VỀ UNITY

### 1.1. Unity là gì?

Unity là một game engine đa nền tảng, hỗ trợ phát triển game 2D, 3D và các ứng dụng tương tác thời gian thực. Unity cung cấp môi trường làm việc trực quan, cho phép nhà phát triển tổ chức project theo scene, sử dụng hệ thống GameObject - Component để xây dựng logic, đồng thời tích hợp tốt với ngôn ngữ C# nhằm hiện thực hóa gameplay.

Trong phạm vi đề tài này, Unity đóng vai trò là nền tảng phát triển chính của toàn bộ sản phẩm. Các scene, đối tượng, vật lý, camera, UI, vật phẩm, ánh sáng và hệ thống loading đều được tổ chức và quản lý trong Unity. Nhờ đó, việc kết nối giữa phần lập trình gameplay và phần thiết kế trực quan trong editor được thực hiện thuận tiện hơn.

### 1.2. Quá trình phát triển game trên Unity

Một project game trên Unity thường được phát triển theo các bước cơ bản sau:

1. Xác định ý tưởng và phạm vi gameplay.
2. Tạo scene, tổ chức môi trường và nhân vật.
3. Xây dựng cơ chế điều khiển và tương tác.
4. Bổ sung các thành phần gameplay như vật phẩm, AI, UI và hiệu ứng.
5. Kiểm thử, chỉnh sửa lỗi và cải thiện trải nghiệm người dùng.
6. Build sản phẩm ra phiên bản chạy thử.

Project hiện tại cũng được triển khai theo hướng như vậy. Trước tiên là tổ chức scene menu và scene gameplay, sau đó bổ sung hệ thống điều khiển nhân vật, tiếp theo là các cơ chế thu thập tài nguyên như chặt cây, săn lợn và câu cá. Sau khi có gameplay lõi, project tiếp tục được mở rộng bằng inventory, menu options và loading scene.

### 1.3. Một số thống kê và vai trò của Unity trong phát triển game

Unity là một trong những game engine phổ biến nhất hiện nay trong phát triển game indie, game học thuật và các ứng dụng mô phỏng tương tác. Lý do Unity được sử dụng rộng rãi là vì:

1. Có giao diện trực quan, dễ tiếp cận cho người mới bắt đầu.
2. Có kho tài liệu, cộng đồng và tài nguyên phong phú.
3. Hỗ trợ nhiều nền tảng triển khai khác nhau.
4. Có hệ thống package hỗ trợ mạnh như UI, navigation, render pipeline, animation và timeline.

Đối với sinh viên, Unity là một công cụ phù hợp để triển khai đồ án vì cho phép tạo ra sản phẩm có tính trực quan cao trong thời gian tương đối ngắn, đồng thời vẫn đủ chiều sâu để thể hiện tư duy thiết kế hệ thống và kỹ năng lập trình.

### 1.4. Ưu điểm của Unity

Unity có nhiều ưu điểm phù hợp với đề tài phát triển game 3D:

1. Hệ thống editor trực quan giúp thao tác với scene, camera, ánh sáng và vật thể nhanh chóng.
2. Mô hình Component giúp tách chức năng thành các module nhỏ, dễ quản lý.
3. Ngôn ngữ C# rõ ràng, mạnh về lập trình hướng đối tượng, phù hợp để xây dựng gameplay.
4. Hệ sinh thái package phong phú giúp bổ sung các tính năng như navigation, render, UI và video.
5. Thuận tiện cho việc kiểm thử nhanh vì có thể vừa chỉnh scene vừa chạy trực tiếp trong editor.

Trong project này, các ưu điểm đó được thể hiện khá rõ qua việc chia script thành các nhóm như `Player`, `Interact`, `Inventory`, `Menu`, `Cutscene`, `Dialogue` và `Walking Boar`.

**Gợi ý ảnh chèn:**
- Hình 1.1: Logo Unity hoặc ảnh chụp Unity Hub.
- Hình 1.2: Ảnh màn hình project đang mở trong Unity Editor.

## CHƯƠNG 2: TÌM HIỂU VỀ UNITY ENGINE

### 2.1. Các thành phần trong Unity Editor

Unity Editor là môi trường chính để xây dựng và kiểm thử game. Trong quá trình thực hiện đề tài, các cửa sổ làm việc quan trọng nhất bao gồm Scene, Hierarchy, Game, Project và Inspector.

#### 2.1.1. Cửa sổ Scene

Cửa sổ Scene được dùng để quan sát và bố trí không gian làm việc trong editor. Tại đây, người phát triển có thể sắp xếp địa hình, đối tượng cây cối, nước, camera và các vật thể tương tác. Với project này, Scene View đặc biệt hữu ích khi thiết kế môi trường gameplay trong scene `Suml`.

**Gợi ý ảnh chèn:**
- Hình 2.1: Ảnh Scene View của scene `Suml`, nhìn thấy terrain, khu vực nước và một phần nhân vật.

#### 2.1.2. Cửa sổ Hierarchy

Hierarchy hiển thị toàn bộ GameObject đang tồn tại trong scene hiện tại. Thông qua Hierarchy, có thể dễ dàng quản lý các nhóm đối tượng như nhân vật, camera, canvas, vùng nước, AI lợn rừng, vật phẩm hoặc các object môi trường.

Trong quá trình làm đề tài, Hierarchy giúp theo dõi cấu trúc scene rõ ràng hơn, nhất là khi scene có nhiều object như terrain, water volume, canvas, boar, door và các object dùng cho inventory UI.

**Gợi ý ảnh chèn:**
- Hình 2.2: Ảnh Hierarchy khi mở scene `Suml`, ưu tiên nhìn thấy `Canvas`, `player`, `CameraForCutscene`, `WildBoar`, `WaterVolume`.

#### 2.1.3. Cửa sổ Game

Game View là nơi hiển thị hình ảnh mà người chơi thực sự nhìn thấy khi chạy game. Đây là cửa sổ quan trọng để kiểm tra gameplay, prompt tương tác, giao diện inventory, hiệu ứng câu cá và loading scene.

Đối với project hiện tại, Game View là nơi kiểm tra trực tiếp tính đúng đắn của UI như menu chính, inventory, prompt giữa màn hình và giao diện mini game câu cá.

**Gợi ý ảnh chèn:**
- Hình 2.3: Ảnh Game View khi game đang chạy ở scene gameplay.

#### 2.1.4. Cửa sổ Project

Project Window chứa toàn bộ tài nguyên của game như scene, prefab, material, texture, animation, script và asset dữ liệu. Việc tổ chức tài nguyên hợp lý trong Project giúp giảm nhầm lẫn và hỗ trợ bảo trì project về sau.

Project này được tổ chức khá rõ theo các thư mục như `Assets/Scenes`, `Assets/scripts`, `Assets/Resources`, `Assets/model` và các package kèm theo. Cách tổ chức này giúp tách logic gameplay với tài nguyên trực quan.

**Gợi ý ảnh chèn:**
- Hình 2.4: Ảnh cửa sổ Project, ưu tiên nhìn thấy các thư mục `Scenes`, `scripts`, `Resources`, `model`.

#### 2.1.5. Cửa sổ Inspector

Inspector hiển thị thuộc tính chi tiết của GameObject hoặc asset đang được chọn. Thông qua Inspector, người phát triển có thể gán reference, thay đổi thông số chạy, chỉnh collider, camera, tốc độ di chuyển hoặc cấu hình UI.

Trong project này, Inspector đặc biệt quan trọng khi cấu hình các script như `PlayerMovement`, `FishingRob`, `InventoryUIController`, `CuttingTreeSystem` hoặc `Boar`.

**Gợi ý ảnh chèn:**
- Hình 2.5: Ảnh Inspector của object player hoặc camera, hiển thị các script chính đang gắn vào.

### 2.2. Các khái niệm cơ bản trong Unity

#### 2.2.1. GameObject

GameObject là đơn vị cơ bản nhất trong Unity. Mọi thành phần trong scene như nhân vật, camera, cây, cửa, lợn rừng, vật phẩm hay canvas UI đều được tổ chức dưới dạng GameObject.

Trong game này, `player`, `WildBoar`, `Canvas`, `WaterVolume`, `CameraForCutscene` và các panel inventory đều là các GameObject cụ thể trong scene.

#### 2.2.2. Component

Component là phần chức năng được gắn lên GameObject để định nghĩa hành vi hoặc dữ liệu. Ví dụ, một GameObject có thể có `Transform`, `Camera`, `Rigidbody`, `Collider` hoặc một script C# riêng.

Điểm mạnh của Unity là cho phép ghép nhiều component để tạo thành một đối tượng hoàn chỉnh. Player trong project là ví dụ rõ nhất khi đồng thời có movement, look, crouch, action, inventory và UI.

#### 2.2.3. Prefab

Prefab là mẫu đối tượng có thể tái sử dụng nhiều lần trong project. Prefab giúp tiết kiệm thời gian, giữ đồng nhất cấu hình và thuận tiện khi cần chỉnh sửa hàng loạt.

Trong project hiện tại, prefab được dùng cho một số vật phẩm và tài nguyên runtime như vật phẩm thịt, mô hình cá preview hoặc các object khác gắn vào gameplay.

#### 2.2.4. Animation

Animation là thành phần giúp đối tượng có chuyển động hoặc trạng thái trực quan hơn. Trong game, animation được dùng cho nhân vật khi di chuyển, cầm rìu, tấn công và một số cutscene hoặc trạng thái chuyển đổi đặc biệt.

Các script như `PlayerMovement` và `ActionScript` đều có liên hệ trực tiếp với Animator để chuyển đổi state.

#### 2.2.5. Sounds

Âm thanh giúp tăng cảm giác phản hồi và chiều sâu cho trải nghiệm. Dù chưa phải là phần được đầu tư mạnh nhất trong project, scene hiện tại vẫn đã xuất hiện một số object âm thanh như bước chân, đáp đất hoặc âm thanh môi trường.

Điều này cho thấy game đã bước đầu kết hợp giữa gameplay và phản hồi cảm giác, không chỉ dừng ở logic tương tác.

#### 2.2.6. Script

Script là nơi triển khai logic gameplay bằng ngôn ngữ C#. Trong project này, script là phần cốt lõi giúp game hoạt động, từ điều khiển nhân vật, raycast tương tác, inventory, menu đến AI lợn rừng và câu cá.

Đây cũng là phần phản ánh rõ nhất năng lực lập trình và tổ chức hệ thống trong đồ án.

#### 2.2.7. Scenes

Scene là một màn hoặc không gian làm việc trong Unity. Build Settings hiện tại của project đang bật hai scene chính là `MainMenu` và `Suml`. Điều này cho thấy game đã có luồng vào game rõ ràng từ menu sang gameplay.

#### 2.2.8. Assets

Assets là toàn bộ tài nguyên dùng trong game, bao gồm script, ảnh, prefab, scene, âm thanh, material, animation và dữ liệu ScriptableObject. Các asset như `WoodLog.asset`, `Meat.asset`, `Fish.asset` cho thấy project đã có bước tổ chức dữ liệu vật phẩm tương đối rõ ràng.

#### 2.2.9. Camera

Camera quyết định góc nhìn hiển thị của người chơi. Project có camera gameplay chính và camera riêng cho cutscene. Ngoài ra, camera còn được dùng trong hệ thống câu cá để thay đổi điểm nhìn cho phù hợp với trạng thái gameplay.

#### 2.2.10. Transform

Transform là component tồn tại trên mọi GameObject, dùng để quản lý vị trí, góc xoay và tỉ lệ. Đây là nền tảng cho toàn bộ thao tác di chuyển nhân vật, xoay cửa, đặt vị trí câu cá, spawn vật phẩm hoặc điều khiển camera trong game.

**Gợi ý ảnh chèn:**
- Hình 2.6: Ảnh một GameObject trong Inspector với nhiều component.
- Hình 2.7: Ảnh prefab hoặc asset vật phẩm trong Project.
- Hình 2.8: Ảnh camera gameplay và camera cutscene trong scene.

## CHƯƠNG 3: TỔNG QUAN ĐỀ TÀI

### 3.1. Giới thiệu ý tưởng và nội dung game

#### 3.1.1. Giới thiệu ý tưởng

Ý tưởng của đề tài là xây dựng một game 3D góc nhìn thứ nhất có yếu tố phiêu lưu và sinh tồn nhẹ. Người chơi được đặt trong một môi trường tự nhiên, có thể khám phá, tương tác với các đối tượng trong scene và thực hiện các hành động thu thập tài nguyên để tạo thành vòng lặp gameplay cơ bản.

Khác với các bài toán game chỉ dừng ở mức di chuyển thử nghiệm, đề tài hướng tới việc tạo ra một sản phẩm có đủ các vòng tương tác cốt lõi: di chuyển, quan sát, tác động vào môi trường, nhận vật phẩm và phản hồi qua giao diện. Điều này giúp project có tính thực tiễn hơn và thể hiện rõ tiến trình phát triển một game mẫu hoàn chỉnh.

#### 3.1.2. Nội dung game

Nội dung hiện tại của game tập trung vào các hoạt động chính sau:

1. Người chơi bắt đầu từ menu chính và vào scene gameplay.
2. Nhân vật điều khiển theo góc nhìn thứ nhất, có thể đi, chạy và cúi người.
3. Người chơi có thể trang bị rìu và thực hiện tấn công.
4. Từ hành động tấn công, người chơi có thể chặt cây để lấy gỗ.
5. Người chơi có thể đánh lợn rừng để lấy thịt.
6. Người chơi có thể tiếp cận vùng nước và tham gia hoạt động câu cá.
7. Các vật phẩm như gỗ, thịt và cá được lưu vào inventory.
8. Inventory có thể mở ra để quan sát số lượng vật phẩm đã thu thập.
9. Menu options cho phép tùy chỉnh âm lượng, độ nhạy chuột, display mode và chất lượng đồ họa.

Về bản chất, gameplay hiện tại tạo nên một vòng lặp khá rõ: khám phá môi trường -> tương tác -> nhận tài nguyên -> quan sát kết quả thông qua UI.

**Gợi ý ảnh chèn:**
- Hình 3.1: Ảnh tổng quan nhân vật đang đứng trong scene gameplay.
- Hình 3.2: Ảnh sơ đồ luồng game: `MainMenu -> Loading -> Suml`.
- Hình 3.3: Ảnh minh họa vòng lặp gameplay: di chuyển -> tương tác -> nhận vật phẩm -> mở inventory.

## CHƯƠNG 4: CƠ SỞ LÝ THUYẾT VÀ PHÂN TÍCH THIẾT KẾ

### 4.1. Giới thiệu về ngôn ngữ C#

C# là ngôn ngữ lập trình hướng đối tượng được Unity sử dụng để xây dựng logic gameplay. Ngôn ngữ này hỗ trợ tốt cho việc tổ chức lớp, đối tượng, event, collection và coroutine. Nhờ đó, C# rất phù hợp với bài toán game, nơi nhiều hệ thống phải hoạt động song song như nhân vật, UI, inventory, AI và scene management.

Trong project này, C# được dùng để triển khai gần như toàn bộ hệ thống lõi:

1. `PlayerMovement` điều khiển chuyển động nhân vật.
2. `PlayerLook` điều khiển góc nhìn chuột.
3. `ActionScript` điều khiển trạng thái cầm rìu và đánh.
4. `CuttingTreeSystem` xử lý chặt cây.
5. `Boar` xử lý AI lợn rừng và rơi vật phẩm.
6. `FishingRob` xử lý cơ chế câu cá.
7. `PlayerInventory` và `InventoryUIController` quản lý túi đồ.
8. `MainMenuController` và `LoadingManager` quản lý luồng giao diện.

**Gợi ý ảnh chèn:**
- Hình 4.1: Ảnh chụp code C# trong IDE, ưu tiên file `PlayerMovement.cs` hoặc `FishingRob.cs`.

### 4.2. Các công cụ sử dụng

#### 4.2.1. Unity 6.0.41f1

Theo thông tin từ `ProjectSettings/ProjectVersion.txt`, project đang sử dụng Unity phiên bản `6000.0.41f1`. Đây là công cụ chính dùng để tổ chức scene, gắn script, cấu hình UI, làm việc với terrain, lighting, navigation và build game.

Build Settings hiện tại cho thấy hai scene chính đang được bật là `MainMenu` và `Suml`, điều này phản ánh rõ cấu trúc vào game và gameplay của project.

#### 4.2.2. Môi trường lập trình C#

Project được phát triển với hệ sinh thái lập trình C# của Unity. Mặc dù trong repo không có tài liệu mô tả chi tiết IDE sử dụng xuyên suốt, các package tích hợp như Rider và Visual Studio đều có mặt trong project. Điều đó cho thấy quá trình code được hỗ trợ bởi các môi trường phát triển phổ biến dành cho Unity.

#### 4.2.3. Các package và thư viện hỗ trợ

Một số package đáng chú ý đang xuất hiện trong `Packages/manifest.json` gồm:

1. Universal Render Pipeline.
2. AI Navigation.
3. Cinemachine.
4. DOTween.
5. Input System.
6. Timeline.
7. UGUI.
8. Post Processing.

Trong thực tế project hiện tại, gameplay scripts chủ yếu vẫn đang dùng cách nhận input truyền thống qua `Input.GetKey` và `Input.GetAxis`, nhưng việc có sẵn các package hỗ trợ giúp project thuận tiện hơn nếu cần mở rộng về sau.

#### 4.2.4. Một số script sử dụng trong game

##### 4.2.4.1. Hệ thống điều khiển nhân vật

Nhóm script điều khiển nhân vật bao gồm `PlayerMovement`, `PlayerLook`, `Crouch`, `MouseMovement` và `ActionScript`. Trong đó:

1. `PlayerMovement` xử lý đi, chạy, trạng thái cutscene và animation di chuyển.
2. `PlayerLook` xử lý góc nhìn chuột và đọc độ nhạy đã lưu từ menu.
3. `Crouch` xử lý hạ đầu và collider khi nhân vật cúi người.
4. `ActionScript` xử lý cầm rìu, bỏ rìu và đánh.

Nhóm script này là nền tảng cho toàn bộ gameplay vì mọi hoạt động sau đó như chặt cây, đánh lợn hay câu cá đều phụ thuộc vào điều khiển nhân vật.

**Gợi ý ảnh chèn:**
- Hình 4.2: Ảnh Inspector của player với các script điều khiển chính.
- Hình 4.3: Ảnh code hoặc sơ đồ mô tả input `WASD`, `Shift`, `Ctrl`, `F`, `E`, `B`, `Q`.

##### 4.2.4.2. Hệ thống tương tác

`PickUpScript` triển khai raycast từ tâm màn hình để kiểm tra object mà người chơi đang nhìn vào. Nếu object đó là `Interactable`, game sẽ hiển thị prompt tương ứng. Khi người chơi nhấn `E`, đối tượng được kích hoạt tương tác.

Thiết kế này cho phép tái sử dụng chung cho nhiều loại object khác nhau như cửa, vật phẩm nhặt được hoặc điểm hiển thị nội dung tương tác.

**Gợi ý ảnh chèn:**
- Hình 4.4: Ảnh gameplay có prompt giữa màn hình khi người chơi nhìn vào vật thể.

##### 4.2.4.3. Hệ thống chặt cây

`CuttingTreeSystem` lắng nghe sự kiện đánh từ `ActionScript`, sau đó kiểm tra xem người chơi có đang nhắm đúng cây hay không. Nếu cây hợp lệ và đủ số lần tác động, hệ thống sẽ xóa cây khỏi Terrain và sinh ra vật phẩm gỗ.

Đây là một phần thể hiện tương đối rõ việc phối hợp giữa raycast, terrain data và spawn object trong Unity.

**Gợi ý ảnh chèn:**
- Hình 4.5: Ảnh người chơi đang chặt cây.
- Hình 4.6: Ảnh khúc gỗ sinh ra sau khi chặt xong.

##### 4.2.4.4. Hệ thống lợn rừng

`Boar` là script xử lý sinh vật AI đi lang thang trong scene. Đối tượng này có máu, nhận sát thương khi bị đánh đúng mục tiêu và sẽ rơi vật phẩm thịt khi bị tiêu diệt. Script cũng cho thấy khả năng dùng NavMesh nếu có dữ liệu dẫn đường hợp lệ, đồng thời có nhánh dự phòng khi không dùng được NavMesh.

**Gợi ý ảnh chèn:**
- Hình 4.7: Ảnh lợn rừng trong scene.
- Hình 4.8: Ảnh vật phẩm thịt rơi ra sau khi tiêu diệt lợn.

##### 4.2.4.5. Hệ thống câu cá

`FishingRob` là một trong những script phức tạp nhất trong project. Script này quản lý toàn bộ chu trình câu cá:

1. Xác định vùng nước hợp lệ.
2. Đưa người chơi vào trạng thái câu.
3. Chờ cá cắn câu với thời gian ngẫu nhiên.
4. Mở mini game móc cá.
5. Cộng vật phẩm cá vào inventory nếu thành công.

Ngoài logic gameplay, script còn tự dựng một số UI runtime và điều chỉnh camera khi câu cá. Điều này cho thấy mức độ hoàn thiện khá cao của module này so với nhiều phần khác trong project.

**Gợi ý ảnh chèn:**
- Hình 4.9: Ảnh người chơi bắt đầu câu cá tại vùng nước.
- Hình 4.10: Ảnh giao diện mini game câu cá.
- Hình 4.11: Ảnh vật phẩm cá trong inventory sau khi câu thành công.

##### 4.2.4.6. Hệ thống inventory

`PlayerInventory` quản lý dữ liệu các slot vật phẩm. `InventoryUIController` hiển thị inventory dưới dạng lưới, xử lý mở/tắt bằng phím `B`, khóa một số thao tác gameplay khi inventory mở và hiển thị hiệu ứng blur nền.

Các asset `WoodLog`, `Meat` và `Fish` trong `Assets/Resources/Inventory` cho thấy dữ liệu vật phẩm đã được tách khỏi script bằng ScriptableObject, đây là một hướng tổ chức hợp lý và dễ mở rộng.

**Gợi ý ảnh chèn:**
- Hình 4.12: Ảnh inventory đang mở.
- Hình 4.13: Ảnh asset vật phẩm trong Project Window.

##### 4.2.4.7. Hệ thống menu và loading

`MainMenuController` quản lý menu chính với các nút Play, Credits, Options và Quit. `MenuSettingsService` chịu trách nhiệm lưu các tùy chọn như âm lượng, độ nhạy chuột, chế độ hiển thị và chất lượng đồ họa. `LoadingManager` thực hiện nạp scene bất đồng bộ kèm overlay loading.

Ba thành phần này giúp sản phẩm có một luồng vào game hoàn chỉnh hơn, thay vì chỉ là một scene gameplay chạy thử.

**Gợi ý ảnh chèn:**
- Hình 4.14: Ảnh Main Menu.
- Hình 4.15: Ảnh panel Options.
- Hình 4.16: Ảnh loading overlay khi chuyển scene.

### 4.3. Phân tích thiết kế hệ thống

#### 4.3.1. Thiết kế luồng vào game

Luồng tổng quát của game hiện tại có thể mô tả như sau:

1. Người chơi mở game tại scene `MainMenu`.
2. Chọn `Play`.
3. `LoadingManager` hiển thị overlay loading và tải scene `Suml`.
4. Sau khi scene gameplay được nạp, người chơi bắt đầu điều khiển nhân vật.

Thiết kế này phù hợp với cấu trúc build hiện tại và tạo cảm giác sản phẩm có tổ chức thay vì chỉ là một bản demo trong editor.

#### 4.3.2. Thiết kế vòng lặp gameplay

Vòng lặp gameplay cơ bản hiện có thể mô tả theo chuỗi:

1. Di chuyển khám phá môi trường.
2. Nhìn vào đối tượng và nhận prompt.
3. Tương tác hoặc tấn công.
4. Nhận vật phẩm từ môi trường.
5. Mở inventory để kiểm tra kết quả.

Tùy theo mục tiêu của người chơi, vòng lặp này có thể diễn ra theo nhánh chặt cây, săn lợn hoặc câu cá.

#### 4.3.3. Thiết kế dữ liệu vật phẩm

Vật phẩm được tách thành ScriptableObject riêng, gồm thông tin mã định danh, tên hiển thị, mô tả, icon, số lượng stack tối đa và khả năng sử dụng. Cách tách dữ liệu này giúp:

1. Giảm việc hard-code trực tiếp trong script gameplay.
2. Dễ thêm vật phẩm mới.
3. Giúp inventory và pickup dùng chung một định dạng dữ liệu.

#### 4.3.4. Thiết kế giao diện và phản hồi người dùng

Game hiện có bốn lớp phản hồi quan trọng:

1. Prompt giữa màn hình khi nhìn vào object có thể tương tác.
2. Giao diện inventory khi người chơi mở túi đồ.
3. Giao diện câu cá và mini game.
4. Menu và loading ở giai đoạn bắt đầu game.

Những lớp giao diện này giúp người chơi luôn biết mình đang ở trạng thái nào và nên làm gì tiếp theo.

**Gợi ý ảnh chèn:**
- Hình 4.17: Sơ đồ luồng vào game.
- Hình 4.18: Sơ đồ vòng lặp gameplay.
- Hình 4.19: Sơ đồ mối quan hệ giữa `InventoryPickup`, `PlayerInventory`, `InventoryUIController`.

## CHƯƠNG 5: THIẾT KẾ GIAO DIỆN ĐỒ HỌA GAME

### 5.1. Tổng quan đồ họa màn chơi

#### 5.1.1. Menu Game

Menu chính là điểm vào đầu tiên của người chơi. Giao diện hiện có các chức năng cơ bản như bắt đầu game, mở phần cài đặt, xem credits và thoát game. Script menu còn bổ sung tween và hiệu ứng fade để phần hiển thị mượt hơn.

**Gợi ý ảnh chèn:**
- Hình 5.1: Màn hình Main Menu toàn cảnh.

#### 5.1.2. Màn gameplay chính

Scene gameplay `Suml` là không gian chính của trò chơi. Tại đây người chơi quan sát môi trường tự nhiên, di chuyển trong địa hình, tiếp cận vùng nước, khu vực có cây và lợn rừng để thực hiện các hoạt động gameplay.

**Gợi ý ảnh chèn:**
- Hình 5.2: Ảnh toàn cảnh scene gameplay.

#### 5.1.3. Options

Màn hình Options cho phép người chơi chỉnh các thông số quan trọng như âm lượng tổng, độ nhạy chuột, display mode và chất lượng đồ họa. Đây là phần tạo cảm giác chuyên nghiệp hơn cho sản phẩm, đồng thời cải thiện khả năng cá nhân hóa trải nghiệm.

**Gợi ý ảnh chèn:**
- Hình 5.3: Ảnh panel Options.

#### 5.1.4. Inventory

Inventory được thiết kế dưới dạng panel trung tâm với nhiều slot, hiển thị icon và số lượng vật phẩm. Khi inventory mở, chuột được mở khóa và một số thao tác gameplay bị tạm khóa để người chơi dễ thao tác hơn.

**Gợi ý ảnh chèn:**
- Hình 5.4: Ảnh inventory đang mở, nhìn rõ các ô vật phẩm.

#### 5.1.5. Giao diện câu cá

Khi chuyển sang trạng thái câu cá, game hiển thị một lớp UI riêng gồm nhãn hướng dẫn và mini game. Giao diện này đóng vai trò dẫn dắt người chơi theo từng bước của cơ chế câu cá.

**Gợi ý ảnh chèn:**
- Hình 5.5: Ảnh giao diện chờ cá cắn câu.
- Hình 5.6: Ảnh mini game khi cá đã cắn câu.

#### 5.1.6. Hộp tương tác và prompt

Prompt giữa màn hình và panel thông tin tương tác là lớp UI quan trọng giúp game truyền đạt ngắn gọn cho người chơi nên làm gì. Đây là một điểm tốt của project vì giao diện không quá phức tạp nhưng vẫn đủ vai trò hướng dẫn.

**Gợi ý ảnh chèn:**
- Hình 5.7: Ảnh prompt tương tác giữa màn hình.
- Hình 5.8: Ảnh panel hiển thị nội dung tương tác hoặc hình minh họa.

#### 5.1.7. Đối tượng đồ họa môi trường

Đồ họa môi trường trong game hiện tập trung vào các thành phần:

1. Terrain và cây cối.
2. Vùng nước dùng cho câu cá.
3. Lợn rừng đi lang thang.
4. Cửa và các vật thể tương tác.
5. Một số hiệu ứng ánh sáng, volume và object trang trí.

Đây là nền tảng để tạo cảm giác khám phá trong gameplay và cũng là nơi diễn ra hầu hết hành động thu thập tài nguyên.

**Gợi ý ảnh chèn:**
- Hình 5.9: Ảnh khu vực cây cối.
- Hình 5.10: Ảnh khu vực nước.
- Hình 5.11: Ảnh lợn rừng trong môi trường.

### 5.2. Chi tiết màn hình

#### 5.2.1. Màn hình Main Menu

Main Menu nên được trình bày như màn hình đầu tiên của sản phẩm. Khi mô tả trong báo cáo, cần nêu rõ:

1. Có các nút Play, Options, Credits, Quit.
2. Có hiệu ứng chuyển động giao diện.
3. Có khả năng điều hướng sang gameplay thông qua loading scene.

**Gợi ý ảnh chèn:**
- Hình 5.12: Ảnh chụp riêng màn hình Main Menu.

#### 5.2.2. Màn hình Loading

Màn hình loading được hiển thị dưới dạng overlay với dòng chữ loading khi chuyển từ menu sang gameplay. Dù đơn giản, đây là thành phần rất cần thiết để tránh việc chuyển scene quá đột ngột.

**Gợi ý ảnh chèn:**
- Hình 5.13: Ảnh loading overlay.

#### 5.2.3. Màn hình gameplay HUD

HUD gameplay hiện chủ yếu tập trung vào prompt giữa màn hình và các panel tương tác khi cần. Cách thiết kế này phù hợp với game góc nhìn thứ nhất vì không che khuất quá nhiều tầm nhìn của người chơi.

**Gợi ý ảnh chèn:**
- Hình 5.14: Ảnh gameplay thường khi nhân vật đang di chuyển.
- Hình 5.15: Ảnh gameplay khi đang nhắm vào đối tượng tương tác.

#### 5.2.4. Màn hình Inventory

Inventory là màn hình thể hiện rõ nhất kết quả của quá trình thu thập tài nguyên. Trong báo cáo, nên mô tả:

1. Số slot.
2. Kiểu hiển thị icon vật phẩm.
3. Cơ chế stack.
4. Hiệu ứng nền blur và khóa điều khiển khi mở.

**Gợi ý ảnh chèn:**
- Hình 5.16: Ảnh inventory với ít nhất 2 đến 3 vật phẩm đang có trong túi.

#### 5.2.5. Màn hình câu cá

Màn hình câu cá nên được mô tả ở hai trạng thái:

1. Trạng thái chờ và hướng dẫn câu.
2. Trạng thái mini game móc cá.

Đây là phần giao diện đặc trưng nhất của project vì nó gắn với một gameplay riêng, có trạng thái chuyển đổi rõ ràng.

**Gợi ý ảnh chèn:**
- Hình 5.17: Ảnh UI câu cá khi chờ.
- Hình 5.18: Ảnh UI câu cá khi vào mini game.

## CHƯƠNG 6: KẾT LUẬN

### 6.1. Kết quả

Sau quá trình thực hiện, đề tài đã xây dựng được một mẫu game 3D góc nhìn thứ nhất trên Unity với nhiều thành phần có thể hoạt động thành một luồng cơ bản. Kết quả đạt được nổi bật gồm:

1. Có menu chính, options và loading scene.
2. Có scene gameplay riêng trong build.
3. Có điều khiển nhân vật góc nhìn thứ nhất.
4. Có cơ chế trang bị rìu và tấn công.
5. Có cơ chế chặt cây lấy gỗ.
6. Có AI lợn rừng và cơ chế rơi vật phẩm thịt.
7. Có cơ chế câu cá với mini game.
8. Có hệ thống inventory hiển thị vật phẩm và số lượng.
9. Có hệ thống lưu cài đặt người dùng qua PlayerPrefs.

### 6.2. Đánh giá

#### 6.2.1. Những điểm làm được

Project có một số điểm mạnh đáng ghi nhận:

1. Đã hình thành một vòng lặp gameplay tương đối rõ.
2. Script được chia nhóm theo chức năng, thuận tiện cho bảo trì và mở rộng.
3. Module câu cá và inventory có mức hoàn thiện khá tốt.
4. Luồng từ menu đến gameplay tạo cảm giác sản phẩm hoàn chỉnh hơn.
5. Dữ liệu vật phẩm đã được tách riêng bằng ScriptableObject.

#### 6.2.2. Những điểm chưa làm được

Bên cạnh các kết quả đã đạt được, project vẫn còn một số hạn chế:

1. Dữ liệu hội thoại hiện còn ở mức mẫu thử, chưa thể xem là phần nội dung hoàn thiện.
2. Trigger hội thoại chưa khép kín hoàn toàn.
3. Hệ thống clue vision vẫn còn xung đột phím với trang bị rìu trong scene hiện tại.
4. Chưa có cơ chế lưu tiến trình tổng thể như vị trí nhân vật, trạng thái môi trường hoặc inventory qua nhiều phiên chơi.
5. Chưa có hệ thống nhiệm vụ, cốt truyện hoàn chỉnh hoặc màn chơi phân chương rõ ràng.

### 6.3. Hướng phát triển

Trong thời gian tới, đề tài có thể được phát triển tiếp theo các hướng sau:

1. Hoàn thiện hệ thống hội thoại và cốt truyện.
2. Bổ sung hệ thống nhiệm vụ để dẫn dắt người chơi.
3. Bổ sung cơ chế save/load đầy đủ.
4. Mở rộng số lượng vật phẩm và hoạt động tương tác.
5. Bổ sung hệ thống chế tạo hoặc nấu ăn từ các nguyên liệu thu thập được.
6. Cải thiện AI sinh vật, âm thanh, hiệu ứng hình ảnh và độ ổn định gameplay.

## TÀI LIỆU THAM KHẢO

[1] Unity Technologies, Unity Manual 6000.0, https://docs.unity3d.com/6000.0/Documentation/Manual/

[2] Unity Technologies, Unity Scripting API, https://docs.unity3d.com/ScriptReference/

[3] Unity Technologies, Scene Management in Unity, https://docs.unity3d.com/Manual/CreatingScenes.html

[4] Demigiant, DOTween Documentation, http://dotween.demigiant.com/documentation.php

[5] File mẫu báo cáo: `Assets/dath-do-an-tot-nghiep-game-bao-cao-tot-nghiep-chi-tiet.pdf`.

[6] File hướng dẫn quy cách đồ án: `Assets/03_Huong-dan-viet-DATN_2022.docx`.

## PHỤ LỤC: DANH SÁCH ẢNH CẦN CHÈN

Bạn có thể chụp và đặt tên ảnh theo danh sách sau để lúc đưa vào Word dễ quản lý:

1. `Hinh_1_1_Unity_Editor.png`: ảnh Unity Editor hoặc Unity Hub.
2. `Hinh_2_1_Scene_Suml.png`: Scene View của scene gameplay.
3. `Hinh_2_2_Hierarchy_Suml.png`: Hierarchy của scene gameplay.
4. `Hinh_2_3_GameView_Gameplay.png`: Game View khi đang chơi.
5. `Hinh_2_4_Project_Window.png`: cửa sổ Project.
6. `Hinh_2_5_Inspector_Player.png`: Inspector của player.
7. `Hinh_3_1_Gameplay_Overview.png`: ảnh tổng quan gameplay.
8. `Hinh_3_2_Game_Flow.png`: sơ đồ luồng `MainMenu -> Loading -> Suml`.
9. `Hinh_3_3_Gameplay_Loop.png`: sơ đồ vòng lặp gameplay.
10. `Hinh_4_1_Code_CSharp.png`: ảnh code C# trong IDE.
11. `Hinh_4_2_Player_Scripts.png`: script điều khiển player.
12. `Hinh_4_4_Prompt_Interact.png`: prompt giữa màn hình.
13. `Hinh_4_5_Chop_Tree.png`: chặt cây.
14. `Hinh_4_6_Wood_Drop.png`: khúc gỗ sau khi chặt.
15. `Hinh_4_7_Boar.png`: lợn rừng trong scene.
16. `Hinh_4_8_Meat_Drop.png`: thịt rơi ra sau khi hạ lợn.
17. `Hinh_4_9_Fishing_Start.png`: bắt đầu câu cá.
18. `Hinh_4_10_Fishing_Minigame.png`: mini game câu cá.
19. `Hinh_4_12_Inventory_Open.png`: inventory đang mở.
20. `Hinh_4_13_Item_Assets.png`: asset vật phẩm trong Project.
21. `Hinh_4_14_MainMenu.png`: Main Menu.
22. `Hinh_4_15_Options.png`: panel Options.
23. `Hinh_4_16_Loading.png`: overlay loading.
24. `Hinh_5_2_Gameplay_Scene.png`: toàn cảnh scene gameplay.
25. `Hinh_5_4_Inventory_UI.png`: UI inventory.
26. `Hinh_5_5_Fishing_Waiting.png`: UI câu cá trạng thái chờ.
27. `Hinh_5_6_Fishing_Hook.png`: UI câu cá trạng thái móc cá.
28. `Hinh_5_7_Interaction_Prompt.png`: prompt tương tác.
29. `Hinh_5_8_Interaction_Panel.png`: panel thông tin tương tác.
30. `Hinh_5_9_Tree_Area.png`: khu vực cây cối.
31. `Hinh_5_10_Water_Area.png`: khu vực nước.
32. `Hinh_5_11_Boar_Area.png`: khu vực lợn rừng.

> Gợi ý:
> - Với ảnh giao diện, nên chụp ở độ phân giải giống nhau để báo cáo đồng bộ hơn.
> - Với ảnh sơ đồ luồng và vòng lặp gameplay, bạn có thể tự vẽ bằng draw.io hoặc PowerPoint để báo cáo trông chuyên nghiệp hơn.
