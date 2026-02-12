# Timeline Phát Triển Game - 10 Tuần

## Tổng Quan Dự Án
**Game Điều Tra Cốt Truyện Ngắn (Narrative Investigation Game)**

**Concept:** Nhân vật tỉnh dậy trên giường, nhận cuộc gọi điều tra một khu vực. Đến hiện trường tìm manh mối bằng cơ chế điều tra đặc biệt. Kết thúc với cảnh animation combat quicktime event.

**Core Mechanics:**
- First-Person Investigation
- Evidence Collection & Examination
- Story-driven Narrative
- Quicktime Event Combat System

---

## **TUẦN 1: Thiết Lập & Story Design**
**Mục tiêu:** Hoàn thiện cốt truyện và thiết lập nền tảng

### Công việc chính:
- ✅ Hoàn thiện hệ thống First-Person Controller (đã có)
- ✅ Hoàn thiện hệ thống Pick Up/Throw (đã có)
- ✅ Hoàn thiện hệ thống tương tác cửa (đã có)
- 📝 Viết cốt truyện đầy đủ (opening, investigation, climax, ending)
- 📝 Thiết kế dialogue system và phone call system
- 📝 Tạo storyboard cho opening cutscene (tỉnh dậy trên giường + cuộc gọi)
- 📝 Thiết kế storyboard cho ending combat sequence
- 📋 Tạo GDD (Game Design Document) với focus vào narrative
- 📋 Thiết kế investigation locations và evidence placement

### Deliverables:
- GDD v1.0 với cốt truyện hoàn chỉnh
- Storyboard cho opening và ending
- Danh sách evidence và clues cần thiết
- Dialogue script cơ bản

---

## **TUẦN 2: Investigation Mechanics & Evidence System**
**Mục tiêu:** Phát triển hệ thống điều tra và thu thập manh mối

### Công việc chính:
- 🔍 Phát triển hệ thống Evidence Collection (nhặt và lưu trữ manh mối)
- 🔍 Tạo Evidence Examination System (zoom, rotate, inspect chi tiết)
- 🔍 Thêm Notebook/Journal System để ghi chép findings
- 🔍 Phát triển Investigation Mode (highlight interactive objects, clues)
- 🔍 Tạo hệ thống Evidence Connection (liên kết manh mối với nhau)
- 🔧 Cải thiện raycast system cho việc phát hiện clues nhỏ
- 🔧 Thêm visual feedback khi phát hiện evidence (highlight, glow effect)

### Deliverables:
- Evidence Collection System hoạt động
- Evidence Examination UI hoàn chỉnh
- Notebook/Journal System
- Investigation Mode prototype

---

## **TUẦN 3: Opening Scene & Bedroom Environment**
**Mục tiêu:** Xây dựng cảnh mở đầu và môi trường phòng ngủ

### Công việc chính:
- 🎬 Dựng phòng ngủ (bedroom) với assets và props
- 🎬 Tạo opening cutscene (nhân vật tỉnh dậy animation)
- 🎬 Implement Phone Call System (UI, audio, dialogue)
- 🎬 Thiết kế camera sequence cho opening
- 🎨 Tối ưu hóa lighting cho atmosphere (moody, mysterious)
- 🎨 Thêm post-processing effects phù hợp với tone game
- 🎨 Tạo transition từ bedroom đến investigation location
- 🔊 Thêm ambient sounds và music cho opening scene

### Deliverables:
- Opening scene hoàn chỉnh (bedroom + phone call)
- Phone Call System hoạt động
- Cutscene system cơ bản
- Transition system giữa scenes

---

## **TUẦN 4: Investigation Location & Evidence Placement**
**Mục tiêu:** Xây dựng hiện trường điều tra và đặt manh mối

### Công việc chính:
- 🗺️ Thiết kế và dựng Investigation Location (crime scene/area)
- 🗺️ Đặt evidence và clues theo story design
- 🗺️ Tạo multiple investigation areas nếu cần
- 🗺️ Thiết kế environmental storytelling (visual clues, atmosphere)
- 🎨 Tối ưu hóa lighting cho investigation area (forensic feel)
- 🎨 Thêm particle effects (dust, light rays) để tăng atmosphere
- 🔍 Test và balance evidence placement (không quá dễ/quá khó tìm)
- 🎬 Tạo investigation area entry cutscene/animation

### Deliverables:
- Investigation Location hoàn chỉnh
- Tất cả evidence được đặt và test
- Environmental storytelling elements
- Entry sequence cho investigation area

---

## **TUẦN 5: Audio & Sound Design**
**Mục tiêu:** Tích hợp hệ thống âm thanh

### Công việc chính:
- 🔊 Thiết kế và tích hợp ambient sounds
- 🔊 Thêm sound effects cho các hành động (pick up, throw, door, v.v.)
- 🔊 Tạo background music system
- 🔊 Implement audio mixer và volume controls
- 🔊 Thêm spatial audio cho vật thể 3D
- 🔊 Tối ưu hóa audio performance

### Deliverables:
- Audio system hoàn chỉnh
- Sound library cơ bản
- Music tracks cho các khu vực

---

## **TUẦN 6: Quicktime Event Combat System**
**Mục tiêu:** Phát triển hệ thống quicktime event cho combat ending

### Công việc chính:
- ⚔️ Thiết kế Quicktime Event System (QTE framework)
- ⚔️ Tạo QTE UI (button prompts, timing indicators, feedback)
- ⚔️ Implement QTE input detection và validation
- ⚔️ Thiết kế combat sequence với multiple QTEs
- ⚔️ Tạo success/failure states cho QTEs
- ⚔️ Thêm visual và audio feedback cho QTE actions
- 🎬 Storyboard và plan ending combat animation sequence
- 🎬 Tạo transition từ investigation phase sang combat phase

### Deliverables:
- Quicktime Event System hoàn chỉnh
- QTE UI và feedback system
- Combat sequence prototype
- Transition system vào combat

---

## **TUẦN 7: Level Design Expansion**
**Mục tiêu:** Tạo thêm levels và nội dung

### Công việc chính:
- 🗺️ Thiết kế và dựng Level 2 và Level 3
- 🗺️ Tạo các khu vực đa dạng với gameplay khác nhau
- 🗺️ Thêm secrets và hidden areas
- 🗺️ Thiết kế boss area hoặc climax area (nếu có)
- 🗺️ Tối ưu hóa performance cho các level lớn
- 🗺️ Thêm transitions giữa các levels

### Deliverables:
- Level 2 và Level 3 hoàn chỉnh
- Tối thiểu 3 khu vực gameplay khác nhau
- Level transitions hoạt động

---

## **TUẦN 8: Audio & Sound Design**
**Mục tiêu:** Tích hợp hệ thống âm thanh đầy đủ

### Công việc chính:
- 🔊 Thiết kế và tích hợp ambient sounds cho tất cả locations
- 🔊 Thêm sound effects cho investigation actions (pick up evidence, examine, v.v.)
- 🔊 Tạo background music system với dynamic music
- 🔊 Implement phone call audio (voice acting hoặc text-to-speech)
- 🔊 Thêm combat music và QTE sound effects
- 🔊 Implement audio mixer và volume controls
- 🔊 Thêm spatial audio cho 3D objects và clues
- 🔊 Tối ưu hóa audio performance

### Deliverables:
- Audio system hoàn chỉnh
- Sound library đầy đủ
- Music tracks cho tất cả scenes
- Voice acting hoặc audio cho phone calls

---

## **TUẦN 9: Testing & Balancing**
**Mục tiêu:** Test toàn diện và cân bằng game

### Công việc chính:
- 🧪 Playtesting với người chơi thật
- 🧪 Thu thập feedback và phân tích
- 🧪 Cân bằng difficulty và pacing
- 🧪 Fix critical bugs được phát hiện
- 🧪 Test trên nhiều cấu hình máy khác nhau
- 🧪 Kiểm tra edge cases và crash scenarios
- 🧪 Tạo build demo để test

### Deliverables:
- Playtest report
- Balanced gameplay
- Stable build

---

## **TUẦN 10: Testing, Balancing & Release Prep**
**Mục tiêu:** Test toàn diện, cân bằng và chuẩn bị release

### Công việc chính:
- 🧪 Playtesting với người chơi thật (focus vào narrative flow)
- 🧪 Test investigation mechanics (evidence có dễ tìm không?)
- 🧪 Test QTE difficulty và timing
- 🧪 Thu thập feedback về story và pacing
- 🧪 Cân bằng narrative pacing và investigation difficulty
- 🧪 Fix critical bugs được phát hiện
- 🧪 Test trên nhiều cấu hình máy khác nhau
- 🧪 Kiểm tra edge cases và crash scenarios
- 🚀 Final bug fixes và polish
- 🚀 Tạo trailer và screenshots (highlight investigation & QTE)
- 🚀 Viết documentation và credits
- 🚀 Chuẩn bị store page (nếu release)
- 🚀 Tạo final build và test
- 🚀 Backup và archive project

### Deliverables:
- Playtest report với feedback về narrative
- Balanced investigation difficulty
- Balanced QTE difficulty
- Final release build
- Marketing materials
- Documentation hoàn chỉnh
- Game sẵn sàng để release

---

## **Milestones Quan Trọng**

| Tuần | Milestone | Trạng thái |
|------|-----------|------------|
| Tuần 2 | Core Mechanics Hoàn Thành | 🟡 In Progress |
| Tuần 4 | UI/UX Hoàn Thành | ⚪ Pending |
| Tuần 6 | Gameplay Loop Hoàn Chỉnh | ⚪ Pending |
| Tuần 8 | Alpha Build | ⚪ Pending |
| Tuần 9 | Beta Build | ⚪ Pending |
| Tuần 10 | Release Candidate | ⚪ Pending |

---

## **Rủi Ro & Giảm Thiểu**

### Rủi ro tiềm ẩn:
1. **Narrative pacing issues** - Cốt truyện quá dài hoặc quá ngắn
   - *Giảm thiểu:* Test narrative flow sớm, có người đọc script và feedback

2. **Investigation quá khó/dễ** - Người chơi không tìm được clues hoặc quá dễ
   - *Giảm thiểu:* Playtest investigation mechanics sớm, có visual hints rõ ràng

3. **QTE timing issues** - Quicktime events quá khó hoặc không responsive
   - *Giảm thiểu:* Test QTE system nhiều lần, có difficulty options

4. **Performance issues** - Game chạy chậm với nhiều evidence và effects
   - *Giảm thiểu:* Profiling sớm, optimize evidence rendering, LOD system

5. **Story coherence** - Các manh mối không kết nối logic với nhau
   - *Giảm thiểu:* Story review với người khác, test investigation flow

6. **Scope creep** - Thêm quá nhiều investigation areas hoặc evidence
   - *Giảm thiểu:* Tuân thủ GDD, focus vào quality hơn quantity

---

## **Ghi Chú**

- Timeline này có thể điều chỉnh dựa trên tiến độ thực tế
- Mỗi tuần nên có review meeting để đánh giá tiến độ
- Ưu tiên tính năng core trước khi mở rộng
- Luôn giữ build playable sau mỗi tuần

---

**Ngày tạo:** 4 tháng 2, 2026  
**Cập nhật lần cuối:** 4 tháng 2, 2026  
**Phiên bản:** 2.0 (Narrative Investigation Game)
