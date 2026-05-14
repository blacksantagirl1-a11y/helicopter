PHẦN BỔ SUNG CHO MỤC PHÂN CÔNG

2. Sinh viên Nguyễn Minh Đức
Phụ trách xây dựng môi trường và các hệ thống gameplay còn lại của game, bao gồm:
- Xây dựng scene gameplay và môi trường 3D.
- Thiết kế nhân vật, chuyển động nhân vật và các vật phẩm trong môi trường game 3D.
- Thiết kế đồ họa và xây dựng tài sản 3D bằng Blender, ZBrush và Substance 3D Painter.
- Thực hiện quy trình tạo asset từ dựng hình khối cơ bản, điêu khắc chi tiết, tối ưu lưới, trải UV, tô chất liệu đến xuất mô hình sang Unity.
- Tối ưu mô hình, material và texture để bảo đảm sự cân bằng giữa chất lượng hiển thị và hiệu năng khi chạy game.
- Điều khiển nhân vật và camera.
- Hệ thống tương tác môi trường.
- Cơ chế chặt cây và thu thập tài nguyên.
- Hệ thống lợn rừng và AI cơ bản.
- Hệ thống hội thoại và nhiệm vụ theo ngày.
- Clue Vision và các hiệu ứng môi trường.
- Thiết kế sơ đồ hệ thống, tổ chức dữ liệu và tích hợp gameplay tổng thể.


PHẦN BỔ SUNG CHO THÂN BÁO CÁO

4.2.12. Thiết kế đồ họa và xây dựng vật phẩm 3D

Trong quá trình phát triển game, phần thiết kế đồ họa 3D giữ vai trò quan trọng vì ảnh hưởng trực tiếp đến tính thẩm mỹ, khả năng nhận diện vật phẩm và trải nghiệm nhập vai của người chơi. Đối với đề tài này, các vật phẩm và đối tượng môi trường như rìu, gỗ, thịt, cá, campfire, bàn, máy tính, các đạo cụ trong cabin và một số chi tiết trang trí được xây dựng theo quy trình thiết kế asset 3D gồm các bước: dựng hình khối cơ bản, điêu khắc chi tiết, tối ưu mô hình, tô vật liệu và đưa vào Unity để sử dụng trong scene gameplay.

Blender được sử dụng làm công cụ chính để dựng mô hình cơ bản (blocking và low-poly modeling). Ở giai đoạn này, các vật thể được tạo ra từ những khối hình học đơn giản, sau đó chỉnh sửa bằng các thao tác extrude, inset, bevel, loop cut và chỉnh sửa vertex để tạo nên hình dáng tổng quát. Blender cũng được dùng để kiểm soát tỉ lệ mô hình, căn chỉnh pivot, đặt đúng kích thước tương đối với nhân vật và môi trường trong game. Ngoài ra, đây cũng là công cụ thuận tiện để thực hiện retopology, tối ưu số lượng polygon và trải UV cho mô hình trước khi chuyển sang các bước xử lý chi tiết hơn.

Đối với các tài sản cần bề mặt có nhiều chi tiết nhỏ hoặc hình khối hữu cơ, ZBrush được sử dụng để điêu khắc mô hình high-poly. Công cụ này hỗ trợ bổ sung các vết nứt, độ gồ ghề bề mặt, nếp gấp, vết mòn và những đặc điểm hình học khó thể hiện nếu chỉ dựng bằng low-poly thông thường. Việc điêu khắc trong ZBrush giúp mô hình có chiều sâu thị giác tốt hơn, đặc biệt phù hợp với các vật thể như thân cây, đá, xương, vật phẩm cũ kỹ hoặc những chi tiết mang tính sinh tồn trong bối cảnh rừng núi. Sau khi hoàn thiện bản high-poly, mô hình được đưa trở lại quy trình tối ưu hóa để phục vụ cho game thời gian thực.

Sau bước dựng hình và điêu khắc, Substance 3D Painter được sử dụng để tạo chất liệu và texture theo chuẩn PBR. Tại đây, nhóm thực hiện bake các bản đồ cần thiết như Normal Map, Ambient Occlusion, Curvature và World Space Normal từ mô hình high-poly sang low-poly. Dựa trên các dữ liệu đó, vật liệu của từng vật phẩm được xây dựng bằng cách kết hợp màu nền, độ nhám, độ phản sáng, vết bẩn, vết trầy xước và lớp phủ bề mặt nhằm tăng độ chân thực. Substance 3D Painter đặc biệt hữu ích trong việc mô phỏng các loại chất liệu như gỗ, kim loại, vải, da, đá hoặc bề mặt bị phong hóa, từ đó giúp các vật phẩm trong game có cảm giác trực quan và thống nhất hơn với bối cảnh sinh tồn.

Quy trình tổng quát để xây dựng một vật phẩm 3D trong đề tài có thể mô tả như sau: đầu tiên, mô hình được phác thảo và dựng khối cơ bản trong Blender; tiếp theo, nếu cần tăng chi tiết hình học thì mô hình được đưa sang ZBrush để sculpt; sau đó quay lại Blender để retopology, giảm số polygon và trải UV; cuối cùng, mô hình low-poly cùng UV được đưa vào Substance 3D Painter để bake và tô texture. Sau khi hoàn thiện, asset được xuất dưới dạng FBX và các texture map tương ứng để import vào Unity, tạo material và gán vào prefab hoặc object trong scene.

Việc áp dụng kết hợp Blender, ZBrush và Substance 3D Painter giúp đề tài xây dựng được quy trình sản xuất tài sản 3D tương đối đầy đủ, gần với cách làm trong thực tế phát triển game. Blender phù hợp cho phần dựng hình và tối ưu mô hình, ZBrush hỗ trợ xử lý các chi tiết hình khối phức tạp, còn Substance 3D Painter cho phép hoàn thiện phần chất liệu theo hướng trực quan và có độ chân thực cao. Nhờ đó, các vật phẩm trong game không chỉ đáp ứng yêu cầu sử dụng trong gameplay mà còn góp phần nâng cao chất lượng hình ảnh tổng thể của sản phẩm.

Bên cạnh yếu tố thẩm mỹ, nhóm cũng chú ý đến yêu cầu tối ưu để các asset có thể vận hành ổn định trong Unity. Các mô hình sau khi hoàn thiện đều được kiểm soát số lượng polygon ở mức hợp lý, hạn chế các chi tiết hình học không cần thiết và ưu tiên thể hiện chi tiết thông qua normal map và texture. Cách tiếp cận này giúp giảm chi phí xử lý khi hiển thị nhiều đối tượng trong scene, đồng thời vẫn giữ được chất lượng hình ảnh phù hợp với phạm vi của một đồ án tốt nghiệp.


DANH SÁCH HÌNH ẢNH CẦN CHÈN

Nên dùng cùng một vật phẩm đại diện xuyên suốt quy trình, ví dụ: rìu, campfire, thùng gỗ, bàn làm việc hoặc máy tính trong cabin. Cách này giúp phần trình bày mạch lạc hơn vì người đọc nhìn thấy toàn bộ quy trình từ lúc dựng khối đến khi đưa vào game.

Phần nên bổ sung vào DANH MỤC HÌNH VẼ:

Hình 4-11. Quy trình tổng quát xây dựng asset 3D từ Blender, ZBrush, Substance 3D Painter đến Unity
Hình 4-12. Dựng hình khối cơ bản của vật phẩm trong Blender
Hình 4-13. Mô hình high-poly sau khi điêu khắc chi tiết trong ZBrush
Hình 4-14. Trải UV và tối ưu mô hình low-poly trong Blender
Hình 4-15. Tô chất liệu và texture PBR trong Substance 3D Painter
Hình 4-16. Bộ texture xuất ra dùng cho asset gồm Base Color, Normal, Roughness hoặc Metallic
Hình 4-17. Asset 3D sau khi import vào Unity và gán material trong scene
Hình 4-18. Một số vật phẩm 3D hoàn thiện trong môi trường gameplay

Gợi ý vị trí chèn trong báo cáo:

- Chèn Hình 4-11 ngay sau đoạn mở đầu của mục `4.2.12. Thiết kế đồ họa và xây dựng vật phẩm 3D`.
- Chèn Hình 4-12 sau đoạn mô tả vai trò của Blender trong bước dựng hình khối cơ bản.
- Chèn Hình 4-13 sau đoạn mô tả vai trò của ZBrush trong bước sculpt mô hình high-poly.
- Chèn Hình 4-14 sau đoạn mô tả retopology, tối ưu polygon và trải UV trong Blender.
- Chèn Hình 4-15 và Hình 4-16 sau đoạn mô tả quá trình bake map và tô texture trong Substance 3D Painter.
- Chèn Hình 4-17 sau đoạn mô tả export FBX, import asset vào Unity và tạo material.
- Chèn Hình 4-18 ở cuối mục `4.2.12` hoặc bổ sung thêm vào phần `4.4. Minh họa sản phẩm thực tế`.

Nội dung cụ thể nên chụp cho từng hình:

Hình 4-11:
Chụp sơ đồ quy trình bằng SmartArt hoặc shapes trong Word với luồng:
`Blender dựng khối -> ZBrush sculpt -> Blender retopology và UV -> Substance 3D Painter bake và texture -> Unity import và sử dụng`.

Hình 4-12:
Chụp cửa sổ làm việc trong Blender ở giai đoạn mô hình mới dựng khối xong, nên để thấy clearly lưới mesh, viewport và hình dáng tổng thể của vật phẩm.

Hình 4-13:
Chụp mô hình trong ZBrush sau khi thêm chi tiết bề mặt như vết xước, nếp gồ ghề, cạnh mòn hoặc chi tiết hữu cơ. Nên dùng chế độ hiển thị làm nổi khối sculpt.

Hình 4-14:
Chụp màn hình Blender thể hiện low-poly sau retopology và layout UV. Nếu được, có thể ghép 2 ảnh nhỏ trong cùng một hình: bên trái là mesh low-poly, bên phải là UV unwrap.

Hình 4-15:
Chụp giao diện Substance 3D Painter trong lúc đang tô material. Nên để thấy model ở giữa và các layer hoặc texture set ở bên phải để thể hiện quy trình làm chất liệu.

Hình 4-16:
Chụp các texture map đã bake hoặc đã export, ví dụ: Base Color, Normal, Roughness, Ambient Occlusion. Có thể ghép nhiều ảnh nhỏ thành một hình tổng hợp.

Hình 4-17:
Chụp Inspector hoặc Scene view trong Unity khi asset đã được import, gán material, đặt vào prefab hoặc scene gameplay. Ảnh này giúp thể hiện bước tích hợp sản phẩm vào game.

Hình 4-18:
Chụp một góc trong scene gameplay có các vật phẩm 3D hoàn thiện đang được sử dụng thực tế. Nên ưu tiên các vật phẩm có tính tương tác như rìu, campfire, gỗ, cá, máy tính hoặc đạo cụ trong cabin.

Đoạn mô tả ngắn có thể đặt dưới từng hình:

Hình 4-11 mô tả quy trình tổng quát xây dựng tài sản 3D được áp dụng trong đề tài, từ bước dựng hình đến bước tích hợp vào Unity.
Hình 4-12 thể hiện giai đoạn dựng hình khối cơ bản của vật phẩm trong Blender trước khi bổ sung chi tiết.
Hình 4-13 thể hiện mô hình high-poly sau bước điêu khắc chi tiết trong ZBrush.
Hình 4-14 thể hiện bước tối ưu mô hình và trải UV để chuẩn bị cho quá trình bake texture.
Hình 4-15 thể hiện quá trình xây dựng vật liệu và tô texture PBR trong Substance 3D Painter.
Hình 4-16 thể hiện các texture map được xuất ra để sử dụng cho asset trong Unity.
Hình 4-17 thể hiện asset sau khi được import vào Unity và gán material hoàn chỉnh.
Hình 4-18 thể hiện các vật phẩm 3D sau khi được đưa vào scene gameplay thực tế.
