# Ke hoach cot truyen Mambo - 6 ngay va ending

Moi ngay duoc thiet ke khoang 3-5 phut: 1 doan thuc day, 2-3 nhiem vu ngan, 1 doan ket ngay. Thoai da duoc nhap vao `DialogueDatabase.asset` theo cap `DialogueDay` + `DialogueEventId`.

## Ngay 1 - To mo

- Cam xuc: Mambo nghi day, bat dau nghi ngo ve khu vuc minh dang canh giu.
- Gameplay: theo dau chim bay ve phia Tay, dat bay quanh ria rung, lap ho can.
- Vat pham: bay dam, coc danh dau, go lap ho.
- Dialogue events: `IntroWakeUp`, `DayStart`, `TreeApproach`, `InvestigationStart`, `InvestigationComplete`.
- Ket ngay: Mambo nhan ra co vet lop xe o ngoai bia rung, khong chi co dong vat di qua.

## Ngay 2 - Ngac nhien

- Cam xuc: su nghi ngo tang len khi dau vet doi huong sang phia Dong.
- Gameplay: kiem tra phia Dong, dat bay o loi mon moi, gap heo rung dang chay tron.
- Vat pham: day thep bay cu, coc danh dau, thit neu nguoi choi san duoc heo.
- Dialogue events: `IntroWakeUp`, `DayStart`, `BoarEncounter`, `InvestigationProgress`, `InvestigationComplete`.
- Ket ngay: Bo dam can Mambo khoa cua va khong mo cho bat ky tieng goi nao.

## Ngay 3 - Buon

- Cam xuc: nha khong con la noi an toan sau khi cua so bi vo tu phia trong.
- Gameplay: tim mat khau trong quan do, mo thung sua do, sua cua so phia Bac.
- Vat pham: thung sua do, manh go, bua, dinh.
- Dialogue events: `IntroWakeUp`, `DayStart`, `DoorHint`, `ItemPickup`, `InvestigationComplete`.
- Ket ngay: Mambo sua duoc cua so nhung thay vet tay moi tren bun.

## Ngay 4 - So

- Cam xuc: Mambo bi danh thuc giua dem, mat tri nho ngan ve viec da mo cua.
- Gameplay: lan theo dau chan quanh nha, tim lon bia da uong, phat hien mui thuoc tay va tro.
- Vat pham: lon bia rong, dau vet chan, chia khoa cua sau.
- Dialogue events: `IntroWakeUp`, `DayStart`, `InvestigationStart`, `ItemPickup`, `InvestigationComplete`.
- Ket ngay: Mambo nghi ngo minh da bi dan du bang giong noi gia.

## Ngay 5 - Ghe tom

- Cam xuc: Mambo phat hien minh bi theo doi ngay trong nha.
- Gameplay: dung clue vision hoac quan sat de tim camera bi giau, lan theo day tin hieu.
- Vat pham: camera nho, day nguon, hop luong kho.
- Dialogue events: `IntroWakeUp`, `DayStart`, `InvestigationStart`, `InvestigationProgress`, `InvestigationComplete`.
- Ket ngay: tin hieu camera dan ve phia Bac, noi Mambo chua tung duoc phep den.

## Ngay 6 - Tuc gian

- Cam xuc: Mambo biet minh la doi tuong bi dieu khien va quyet dinh pha vong lap.
- Gameplay: tim cua hang bo hoang, doc mat ma tu thu tu Tay-Dong-Bac, mo hom, lay ban do va may tinh.
- Vat pham: hom khoa, ban do, may tinh, tui do bi bo lai.
- Dialogue events: `IntroWakeUp`, `DayStart`, `DoorHint`, `InvestigationStart`, `InvestigationProgress`, `EndingStart`.
- Ket ngay: may tinh tiet lo cac ban ghi theo doi Mambo va giong bo dam gia lap.

## Ending - Thoat khoi khu rung chay

- Dieu kien de kich hoat: sau khi Mambo lay du ban do va may tinh o ngay 6, goi `DialogueController.RequestDialogue(DialogueEventId.EndingStart)`.
- Dien bien: Mambo chay theo long suoi can, bo lai can nha va bo dam gia.
- Ket qua: Mambo song sot, thoat khoi khu rung dang chay, nhung cau lenh "hay bao ve khu vuc duoc giao" van lap lai phia sau.

## Goi y gan trigger

- Khi player bat dau moi ngay: goi `IntroWakeUp`.
- Khi mo objective ngay: goi `DayStart`.
- Khi nhin/vao khu vuc cay, bay, vet dau tien: goi `TreeApproach` hoac `InvestigationStart`.
- Khi nhat vat pham cot truyen: goi `ItemPickup`.
- Khi hoan thanh objective ngay: goi `InvestigationComplete`.
- Khi den man cuoi ngay 6: goi `EndingStart`.
