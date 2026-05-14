# Kịch bản thuyết trình 10 phút

Tài liệu này viết lời trình bày theo từng slide. Khi thuyết trình, bạn có thể đọc gần như nguyên văn, nhưng nên nói tự nhiên, nhìn vào slide để nhắc ý chính và tránh đọc quá nhanh.

## Slide 1: Xây dựng game điều tra - sinh tồn 3D trên Unity (0:30)

Kính thưa quý thầy cô, em xin trình bày báo cáo đồ án tốt nghiệp với đề tài: “Xây dựng game điều tra - sinh tồn 3D trên nền tảng Unity”.

Đề tài của em tập trung xây dựng một trò chơi 3D góc nhìn thứ nhất, trong đó người chơi vừa khám phá môi trường, vừa thực hiện các nhiệm vụ điều tra và sinh tồn theo từng ngày. Project được phát triển bằng Unity, sử dụng ngôn ngữ C# và các hệ thống hỗ trợ như UI, âm thanh, nhiệm vụ, hội thoại, inventory và tương tác trong môi trường 3D.

Trong phần trình bày hôm nay, em sẽ giới thiệu ngắn gọn lý do chọn đề tài, mục tiêu, công nghệ sử dụng, thiết kế hệ thống, các chức năng đã cài đặt, kết quả kiểm thử và hướng phát triển tiếp theo.

## Slide 2: Nội dung trình bày (0:30)

Bài trình bày của em gồm năm nội dung chính.

Thứ nhất, em trình bày lý do chọn đề tài, mục tiêu và phạm vi thực hiện. Phần này giúp làm rõ vì sao em chọn hướng xây dựng game 3D điều tra - sinh tồn và đồ án cần đạt được những kết quả nào.

Thứ hai, em giới thiệu các công nghệ sử dụng trong project, gồm Unity, C#, URP, TextMeshPro, Unity UI, NavMeshAgent và ScriptableObject.

Thứ ba, em trình bày ngắn gọn phần thiết kế đồ họa của game, bao gồm cách xây dựng môi trường 3D, vật phẩm 3D và quy trình đưa asset vào Unity để sử dụng trong gameplay.

Thứ tư, em trình bày phần phân tích và thiết kế hệ thống, tập trung vào vòng lặp gameplay, các module chính và cách các module phối hợp với nhau.

Cuối cùng, em trình bày phần cài đặt, kiểm thử, đánh giá kết quả đạt được, hạn chế hiện tại và hướng phát triển trong tương lai.

## Slide 3: Lý do chọn đề tài (0:50)

Lý do em chọn đề tài này là vì game 3D là một dạng sản phẩm có tính tổng hợp cao. Để xây dựng được một game có thể chơi được, người phát triển cần kết hợp nhiều kiến thức của ngành công nghệ thông tin như lập trình hướng đối tượng, xử lý đồ họa, thiết kế giao diện, xử lý âm thanh, quản lý dữ liệu và tổ chức hệ thống phần mềm.

Với thể loại nhập vai, người chơi không chỉ di chuyển trong môi trường 3D mà còn phải quan sát, tìm manh mối, thu thập tài nguyên và hoàn thành nhiệm vụ. Điều này tạo ra một vòng lặp gameplay rõ ràng và phù hợp để thể hiện nhiều kỹ thuật khác nhau trong Unity.

Ngoài ra, Unity là một nền tảng phù hợp cho đồ án vì hỗ trợ đầy đủ các thành phần cần thiết như scene, Đối tượng trò chơi, component, physics, UI Canvas, animation, terrain, NavMesh và âm thanh. Vì vậy, đề tài này không chỉ là một sản phẩm giải trí, mà còn là cơ hội để em vận dụng kiến thức lập trình và thiết kế hệ thống vào một project tương tác thời gian thực.

## Slide 4: Mục tiêu và phạm vi (0:55)

Về mục tiêu, đồ án hướng đến việc xây dựng một prototype game 3D góc nhìn thứ nhất trên nền tảng Unity. Người chơi có thể di chuyển, chạy, cúi, xoay camera, quan sát môi trường và tương tác với các object trong scene.

Bên cạnh phần điều khiển nhân vật, project cần cài đặt các hệ thống gameplay chính như tương tác bằng raycast, hiển thị prompt, nhặt vật phẩm, quản lý inventory, chặt cây, săn lợn rừng, câu cá, đặt campfire, nấu ăn, hội thoại và nhiệm vụ theo ngày.

Về phạm vi, project tập trung vào bản demo phục vụ đồ án. Game có hai scene chính là MainMenu và InGame. Scene MainMenu dùng để bắt đầu game và thiết lập một số cài đặt cơ bản, còn scene InGame là màn chơi chính chứa môi trường rừng núi, hồ, nhân vật, vật phẩm, UI, nhiệm vụ, hội thoại và các hệ thống gameplay.

Trong phạm vi đồ án, hệ thống lưu trạng thái mới dừng ở mức đơn giản bằng PlayerPrefs, ví dụ lưu ngày hiện tại hoặc một số cài đặt. 

## Slide 5: Công nghệ sử dụng (0:55)

Project được xây dựng bằng Unity phiên bản 6000.0.41f1. Unity đóng vai trò là game engine chính, hỗ trợ quản lý Màn chơi, Đối tượng trò chơi, physics, animation, terrain, UI và âm thanh.

Ngôn ngữ lập trình chính là C#. Các script trong project thường kế thừa MonoBehaviour để tham gia vào vòng đời của Unity. Cách tổ chức này giúp các chức năng gameplay có thể được gắn trực tiếp vào Đối tượng trò chơi trong Màn chơi.

Về mặt hiển thị, dự án sử dụng Universal Render Pipeline (URP) để hỗ trợ xuất ảnh (render) môi trường 3D. Giao diện người dùng sử dụng Unity UI và TextMeshPro để hiển thị menu, HUD (bảng thông tin trên màn hình), thông báo tương tác (prompt), kho đồ (inventory), nhiệm vụ (quest) và lời thoại (dialogue).

Ngoài ra, project còn sử dụng AI Navigation và NavMeshAgent cho hành vi di chuyển của lợn rừng. Một số công nghệ hỗ trợ khác gồm DOTween, Timeline và Shader Graph cho hiệu ứng, chuyển cảnh và xử lý hình ảnh.

## Slide 6: Thiết kế đồ họa và môi trường 3D (0:40)

Bên cạnh phần lập trình gameplay, đồ án cũng chú trọng đến phần thiết kế đồ họa để tạo ra cảm giác nhập vai trong môi trường rừng núi. Trong scene gameplay, em xây dựng địa hình, cây cối, mặt nước, cabin, campfire và các vật thể tương tác sao cho vừa phục vụ thẩm mỹ, vừa hỗ trợ người chơi dễ nhận biết khu vực và mục tiêu cần tương tác.

Đối với các tài sản 3D như rìu, gỗ, cá, thịt, máy tính, campfire và một số đạo cụ trong cabin, quy trình thực hiện gồm dựng khối cơ bản trong Blender, bổ sung chi tiết khi cần, tối ưu lưới, trải UV, sau đó tạo vật liệu và texture trước khi import vào Unity. Cách làm này giúp asset có hình dạng rõ ràng, đồng thời vẫn phù hợp với hiệu năng của một project Unity thời gian thực.

Về tổng thể, phần thiết kế đồ họa không chỉ nhằm làm game đẹp hơn, mà còn gắn trực tiếp với gameplay. Ví dụ, vật phẩm phải đủ nổi bật để người chơi nhận ra, môi trường phải định hướng được đường đi, còn các đạo cụ như cabin, PC hay campfire phải vừa đúng bối cảnh, vừa hỗ trợ kể chuyện và dẫn dắt nhiệm vụ.

## Slide 7: Vòng lặp gameplay tổng quát (0:55)

Vòng lặp gameplay của game bắt đầu từ Main Menu. Khi người chơi bấm New Game, game chuyển sang scene InGame, đây là scene gameplay chính.

Trong scene Ingame, người chơi điều khiển nhân vật ở góc nhìn thứ nhất để khám phá môi trường. Camera được dùng để quan sát, đồng thời cũng là hướng raycast để phát hiện đối tượng có thể tương tác. Khi nhìn vào đối tượng của trò chơi hợp lệ, hệ thống hiển thị thông báo tương tác và người chơi có thể bấm phím E để thực hiện tương tác.

Các tương tác trong game bao gồm mở cửa, nhặt vật phẩm, kích hoạt hội thoại, chặt cây, câu cá, đặt campfire hoặc xử lý các object nhiệm vụ. Sau khi tương tác, dữ liệu trong Túi đồ, nhiệm vụ hoặc hội thoại có thể được cập nhật.

Vòng lặp chính của game là: khám phá môi trường, tương tác với đối tượng, thu thập tài nguyên, hoàn thành điều kiện nhiệm vụ, xem hội thoại và chuyển sang tiến trình ngày tiếp theo. Cách tổ chức này giúp game có mục tiêu rõ ràng thay vì chỉ là một scene để di chuyển tự do.

## Slide 8: Thiết kế hệ thống chính (1:00)

Về thiết kế hệ thống, project được chia thành nhiều nhóm module chính để dễ quản lý và mở rộng.

Nhóm Player xử lý điều khiển nhân vật, camera, chạy, cúi, stamina và hành động cầm rìu. Nhóm Interact xử lý raycast, prompt và các object có thể tương tác. Nhóm Inventory quản lý vật phẩm, ô chứa, UI túi đồ và thao tác với vật phẩm như gỗ, thịt, cá.

Nhóm Quest quản lý nhiệm vụ theo ngày, điều kiện hoàn thành và Giao diện tiến độ. Nhóm Dialogue quản lý hội thoại theo ngày và theo sự kiện. Nhóm Audio xử lý nhạc nền, hiệu ứng âm thanh một lần và âm thanh lặp. 



## Slide 9: Các chức năng đã cài đặt (1:05)

Ở phần cài đặt, project đã hoàn thành các chức năng gameplay chính.

Trước hết là hệ thống điều khiển nhân vật. Người chơi có thể đi, chạy, cúi và xoay camera bằng chuột. Các trạng thái di chuyển được kết hợp với animation để tạo cảm giác phản hồi trong game.

Tiếp theo là hệ thống tương tác bằng raycast. Khi camera nhìn vào object có thể tương tác, thông báo sẽ hiện trên màn hình. Người chơi bấm phím E để thực hiện hành động như mở cửa, nhặt vật phẩm hoặc kích hoạt hội thoại.

Dự án cũng cài đặt hệ thống trang bị rìu bằng phím F. Khi đang cầm rìu, người chơi có thể tấn công bằng chuột trái. Nếu hướng nhìn hợp lệ và đủ số lần đánh, cây sẽ bị loại khỏi terrain và sinh vật phẩm gỗ.

Đối với tài nguyên sinh tồn, game có cơ chế săn lợn rừng để nhận thịt, câu cá để nhận cá, và đặt campfire để nấu ăn. Inventory quản lý các vật phẩm này theo slot và số lượng stack.

Bên cạnh đó, dự án có hệ thống dialogue và quest theo ngày. Người chơi nhận nhiệm vụ, theo dõi tiến độ trên HUD và hoàn thành điều kiện để mở hội thoại tiếp theo. Các tính năng bổ sung âm thanh theo hành động, mưa theo ngày và mini game nấu ăn.

## Slide 10: Tiến trình nhiệm vụ theo ngày (0:50)

Hệ thống nhiệm vụ được thiết kế theo tiến trình từng ngày để tạo cảm giác phát triển câu chuyện.


Như vậy, mỗi ngày không chỉ là một nhiệm vụ riêng lẻ, mà còn giới thiệu hoặc kết hợp thêm một nhóm chức năng gameplay mới. Điều này giúp dự án thể hiện được cả phần kỹ thuật lẫn phần thiết kế trải nghiệm người chơi.

## Slide 11: Kiểm thử và kết quả (0:50)

Sau khi cài đặt, em tiến hành kiểm thử theo từng nhóm chức năng chính.

Đầu tiên là kiểm thử Main Menu và chuyển scene. Khi bấm New Game, game cần chuyển đúng sang scene Suml. Tiếp theo là kiểm thử điều khiển nhân vật, bao gồm đi, chạy, cúi, xoay camera và cập nhật trạng thái animation.

Đối với hệ thống tương tác, em kiểm thử việc nhìn vào object có Interactable, hiển thị prompt và bấm E để object xử lý đúng hành vi. Các trường hợp như mở cửa, nhặt vật phẩm và kích hoạt hội thoại đều được kiểm tra theo luồng sử dụng thực tế.

Các chức năng gameplay như chặt cây, săn lợn rừng, câu cá và nấu ăn cũng được kiểm thử riêng. Ví dụ, khi chặt cây đủ số hit thì cây phải biến mất và sinh vật phẩm gỗ; khi săn lợn thành công thì lợn bị hạ và rơi thịt; khi câu cá thành công thì cá được thêm vào inventory.

Ngoài ra, em kiểm thử quest HUD, dialogue, clue vision, âm thanh và stamina. Kết quả là các vòng lặp gameplay chính đã hoạt động và có thể kết hợp thành một bản demo chơi được, đáp ứng mục tiêu của đồ án.

## Slide 12: Đánh giá (0:40)

Về kết quả đạt được, project đã xây dựng được một prototype game 3D có đầy đủ các hệ thống gameplay cốt lõi. Các module như Player, Interact, Inventory, Quest, Dialogue và Audio được tách tương đối rõ theo chức năng.

Một điểm mạnh của project là dữ liệu item, nhiệm vụ và hội thoại không bị hard-code hoàn toàn trong logic gameplay, mà được tách thành asset hoặc cấu hình riêng. Điều này giúp việc thêm item, nhiệm vụ hoặc đoạn hội thoại mới thuận tiện hơn.

Tuy nhiên, project vẫn còn một số hạn chế. Hệ thống save/load hiện mới lưu các trạng thái nhẹ bằng PlayerPrefs, chưa lưu đầy đủ toàn bộ thế giới game. AI của lợn rừng và cân bằng gameplay còn đơn giản. Một số phần giao diện và hiệu ứng hình ảnh vẫn cần tiếp tục hoàn thiện nếu phát triển thành sản phẩm lớn hơn.

Nhìn chung, đồ án đã đạt được mục tiêu chính là xây dựng một bản demo có thể chơi được, có luồng gameplay, có nhiệm vụ và có nhiều module kỹ thuật phối hợp với nhau.

## Slide 13: Kết luận và hướng phát triển (0:40)

Tổng kết lại, đồ án đã xây dựng được prototype game điều tra - sinh tồn 3D trên Unity. Người chơi có thể di chuyển trong môi trường 3D, tương tác với object, thu thập tài nguyên, sử dụng inventory, theo dõi nhiệm vụ, xem hội thoại và trải nghiệm các hoạt động sinh tồn như chặt cây, săn lợn, câu cá và nấu ăn.

Thông qua project này, em đã vận dụng được các kiến thức về lập trình C#, tổ chức project Unity, thiết kế hệ thống gameplay, xử lý UI, âm thanh, dữ liệu cấu hình và kiểm thử chức năng.

Trong hướng phát triển tiếp theo, em mong muốn hoàn thiện hệ thống save/load, mở rộng cốt truyện, bổ sung thêm màn chơi, cải thiện AI, tối ưu hiệu năng và hoàn thiện UI/UX để sản phẩm có trải nghiệm tốt hơn.

Phần trình bày của em đến đây là kết thúc. Em xin cảm ơn quý thầy cô đã lắng nghe và mong nhận được ý kiến đóng góp từ quý thầy cô.
