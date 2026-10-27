# Tiệm Bánh Gấu — prototype Unity (màn hình dọc)

Prototype merge + quản lý tiệm bánh, dựng hoàn toàn bằng bộ asset Bear Bakery được cung cấp
(`/Users/trongnp/UnityRE/BearBakery/export_project/ExportedProject`, không bị sửa).
Unity **6000.3.24f1**, Built-in renderer, uGUI, không dùng package ngoài.

## 1. Cách chạy

| Mục đích | Cách làm |
|---|---|
| Chơi ngay (macOS) | Mở `Builds/macOS/TiemBanhGau.app`. Cửa sổ dọc 390×844, kéo giãn được; nếu cửa sổ nằm ngang, game giữ khung dọc ở giữa. |
| Android | `Builds/Android/TiemBanhGau.apk` (nếu có trong thư mục — xem mục 7) |
| Mở source | Unity Hub → *Add* → `/Users/trongnp/BearBakery` (Unity 6000.3.24f1) → mở `Assets/Scenes/Main.unity` → Play. Đặt Game view tỉ lệ dọc (ví dụ 390×844). |
| Build lại | Menu **BearBakery → Build macOS / Build Android APK**, hoặc `_tools/unity.sh BB.EditorTools.ProjectSetup.BuildMac buildmac` |
| Kiểm thử model | `_tools/unity.sh BB.EditorTools.ModelTests.RunAll tests` → `Docs/test_results.md` |
| Mô phỏng nhịp 15 phút | `_tools/unity.sh BB.EditorTools.BalanceSim.Run balance` → `Docs/balance_sim.md` |
| Chụp màn hình tự động | `_tools/shots.sh 390 844 shots` (cũng 360 800 / 430 932) → `Docs/screens/` |

Tham số dòng lệnh của bản build: `-bbSlot <tên>` (khe lưu riêng), `-bbAuto shots|play|resume` (tự chơi để kiểm thử), `-bbOut <thư mục>`, `-bbSeconds <n>`, `-bbFrames` (ghi khung hình làm clip), `-bbBurn 1` (bật chế độ bánh cháy cho lần chạy đó).

## 2. Điều khiển (một tay, chỉ chạm/kéo)

- **Chạm Gấu**: giúp nhồi bột (tối đa 3 lần/mẻ, có giới hạn). Khi Gấu đi nghỉ: chạm bàn bếp để tự nhồi (6 chạm = 1 bột).
- **Kéo bột** → thả lên bột cùng cấp: ghép; vào ô trống: di chuyển; vào lò trống: nướng; thả chỗ khác: bột quay về ô cũ.
- **Chạm lò** có dấu tích: lấy bánh vào kho (giá kho cạnh tường bếp). Kho đầy thì bánh nằm lại trong lò.
- **Chạm kệ**: chọn bánh trong kho để bày; bật/tắt *Tự bổ sung* sau khi mở khóa.
- **Chạm quầy/Amber**: tính tiền nhanh hơn (hoặc tự tính khi Amber nghỉ). **Chạm heo đất**: nhận tip. **Chạm Tweetie**: mời khách (hồi 20 s). **Chạm khách**: xem món thích.
- **Chuyển khu bằng mũi tên hai bên màn hình**: mỗi khu (Phòng nghỉ ← Bếp → Tiệm) vừa đúng một màn hình; chạm mũi tên trái/phải (có ghi tên khu kế bên) để trượt ngang sang khu đó. Ở khu đầu/cuối mũi tên phía ngoài tự ẩn. Không còn vuốt dọc để cuộn. Trên máy tính: phím ←/→ (hoặc A/D).
- Thanh dưới: **Bếp / Tiệm** (chạm khi đang ở khu khác: nhảy tới khu đó; chạm lần nữa: mở bảng nâng cấp), **Nội thất** (tới phòng nghỉ + bảng nội thất/nhân viên), **Sổ tay** (nhiệm vụ, công thức, khách quen).
- Đóng bảng: nút ×, chạm vùng tối, hoặc phím Esc (desktop).

## 3. Cấu trúc

```
Assets/BearBakery/
  Resources/config.json          ← bảng cân bằng chung (giá, thời gian, phần thưởng, điều kiện mở khóa)
  Resources/asset_manifest.json  ← semantic id → file sprite (gameplay chỉ dùng semantic id)
  Resources/Art|Audio|Fonts      ← bản sao dẫn xuất từ bộ asset (truy vết ở Docs/asset_manifest.csv)
  Scripts/Model/                 ← gameplay thuần C#, không phụ thuộc UnityEngine
      GameModel.cs, GameModel.Shop.cs  (bàn/ghép/lò/kho/kệ/khách/thu ngân/nâng cấp/nhiệm vụ/offline/snapshot)
      GameConfig.cs, SaveData.cs, GameEvents.cs, BotPolicy.cs
  Scripts/View/                  ← renderer: World*.cs, FrameAnim (timing từ .anim gốc), Tween, Fx, Interaction (camera theo khu + input),
                                    AudioMan, SaveStore (JSON + backup), GameBoot (vòng lặp, save, resume/offline), AutoPilot
  Scripts/UI/                    ← UIRoot (khung dọc, safe area, HUD, thanh tab, nhãn bám thế giới), Sheets (bottom sheet, popup, bàn tay)
  Editor/                        ← ArtImporter (pivot/border gốc), ProjectSetup (scene, build), ModelTests, BalanceSim
_tools/                          ← script Python trích sprite, contact sheet, manifest; script chạy Unity/ảnh chụp
_derived/                        ← toàn bộ 3.316 sprite đã cắt + sprites_index.json + anim_metadata.json + contact sheet thô
Docs/                            ← manifest, audit, contact sheet, kết quả test, mô phỏng, ảnh chụp, clip
```

Model phát sự kiện (`Ev.*`); renderer chỉ đọc trạng thái và phát animation. Tiền chỉ thay đổi trong model (`Earn/Spend`), không dựa vào callback animation; đồng xu bay chỉ minh họa giao dịch đã ghi.

## 4. Vòng chơi đã hoàn tất

1. **Bột**: Gấu (frame Work 0.45/0.43/0.45 s từ `Bear_Work.anim`) đặt bột cấp 1 mỗi 5 s vào ô trống; bàn đầy → dừng, bubble "Bàn đầy!", không mất bột. Chạm hỗ trợ có giới hạn + frame Fast + bụi bột.
2. **Ghép**: 2 bột cùng cấp → 1 bột cấp kế (tiêu thụ đúng 2 đầu vào, ID mới); sai cấp/ngoài bàn → quay về; cấp 6 không ghép. Hiệu ứng: nâng khi kéo, highlight đích hợp lệ, hút vào tâm 0,12 s + squash + bụi + sao (~0,3 s, không khóa màn hình).
3. **Nướng**: kéo vào lò trống; lò giữ `recipe` + `jobId`; trạng thái empty/baking/ready(/burnt); thời gian mô phỏng 8–25 s × hệ số nâng cấp lò. Cửa lò đóng, lửa nhấp nháy, khói bay (đường cong từ `Oven_On.anim`), bubble tiến độ tròn.
4. **Lấy bánh**: chạm lò → bánh bay vào giá kho; kho đầy → giữ ở lò + cảnh báo. Spam chạm không nhân đôi.
5. **Bày bán**: chạm kệ → chọn bánh trong kho; kho giảm đúng số chuyển; kệ hiện tới 5 bánh (vị trí từ `StandSlot.prefab`) + "x/cap".
6. **Khách**: vào từ phố → cửa → đi theo lối đi (không xuyên kệ/quầy) → đứng trước kệ chọn (bubble món) → bưng khay → xếp hàng 6 chỗ trước quầy → Amber tính tiền (frame Work) → túi bánh, cảm ơn → ra về. Đặt chỗ (reservation) ngay khi khách chọn kệ.
7. **Thu nhập**: ghi tiền đúng một lần lúc thanh toán (txId, danh sách đã commit); số tiền nổi trên quầy được gộp khi nhiều giao dịch gần nhau; tip vào **heo đất** riêng, chạm để nhận.
8. **Đầu tư**: lò thứ 2–3, ô bàn 5–12, kệ 3–6 (đều hiện trong cảnh, rơi xuống + nảy + bụi + âm lắp đặt); 3 nhóm nâng cấp: Đầu bếp, Lò, Sức chứa trưng bày (kệ + kho); Tự bổ sung kệ (bật/tắt từng kệ, chỉ lấy từ kho).
9. **Khách quen**: 6 loài, mỗi loài có danh sách món thích; món thích luôn chọn trong công thức đã mở. Mua đúng món: +3 thiện cảm (thường +1), tip 15% + 10%/tim (trần 50%). Sổ khách: chân dung, tim, số lần mua, món thích.
10. **Phòng nghỉ**: năng lượng 3 nhân viên; < 30% làm chậm (×0,7, không dừng); ≤ 15% tự đi nghỉ ở chỗ nghỉ tốt nhất còn trống (Ghế lười/Sofa/Giường), thấy nhân vật ngủ trên đồ + "Zzz"; hết chỗ thì ngủ gật tại trạm. Máy cà phê giảm hao, Đèn tăng tốc hồi, Thảm tăng tip — tất cả có trần.
11. **Nhiệm vụ**: 8 bước hướng dẫn (tạo bột → ghép → lò → lấy bánh → bày kệ → bán → mua nâng cấp → xem phòng nghỉ) hoàn tất bằng sự kiện thật + 6 mục tiêu tiến trình; nhận thưởng một lần; bàn tay gợi ý chỉ hiện sau 5 s không thao tác, có thể tắt.
12. **Save/offline**: JSON cục bộ (ghi file tạm → đổi tên, giữ `.bak`), lưu mỗi 10 s và ~1 s sau giao dịch quan trọng, lưu khi tạm dừng/mất focus/thoát. Load và resume dùng chung `ApplyElapsed` (delta âm = 0, trần 2 giờ). Offline: hoàn tất mẻ đang nướng, bán hàng đang trên kệ (1 chiếc/12 s, không vượt tồn kho), tự bổ sung chỉ khi đã mở + bật; không tự tạo/ghép bột. Tiền offline thành khoản chờ có ID, nhận một lần qua popup.
    - Quy tắc khách khi thoát: khách *chỉ mới đặt chỗ* → trả lại chỗ; khách *đã cầm bánh* → lưu thành `pendingSales` và được thanh toán đúng một lần khi mở lại.
    - Save lỗi → dùng bản `.bak`; cả hai lỗi → giữ tệp lỗi (`*_corrupt_*.json`) và báo; save từ phiên bản mới hơn → chế độ chỉ đọc, không ghi đè.
13. **Bánh cháy**: `burn.burnEnabled` (mặc định **false**), `burnGraceSeconds` 45 s, chỉ tính khi đang chơi, cảnh báo "Sắp cháy!", chạm để dọn lò. Đây là điều chỉnh của prototype, không phải hành vi đã xác minh của bản gốc.

## 5. Kiểm chứng (chi tiết trong `Docs/verification.md`)

- `Docs/test_results.md`: 33 kiểm tra model (ghép đúng/sai, ngoài bàn, bàn đầy, cấp tối đa, lò bận, kho đầy, kệ hết, 2 khách tranh chiếc cuối, spam nhận/mua/thưởng, reload giữa nướng & thanh toán, offline, save lỗi…).
- Build macOS tự chơi qua **input giả lập thật** (kéo/chạm theo toạ độ màn hình) ở 360×800, 390×844, 430×932: kéo-ghép, thả ra ngoài, kéo vào lò đều đạt sau khi scale; mô phỏng không dừng khi mở bottom sheet/đổi khu.
- `Docs/balance_sim.md`: 3 lượt mô phỏng 15 phút; `Docs/playtest_build.md`: 10 phút tự chơi trong bản build + FPS.

## 6. Giới hạn thật

- Chỉ đo trên **macOS, Apple M4 (Mac16,10)**, bản build standalone: ~59 FPS trung bình (giới hạn 60), không phải đo trên điện thoại. APK Android chưa được chạy trên thiết bị thật.
- "Playtest 10 phút" là bot chơi tự động (bản build) + mô phỏng headless — **chưa có người chơi thật**.
- Nhân viên không có frame đi bộ → di chuyển tới phòng nghỉ bằng nhảy/mờ qua cửa nhân viên (tween). Khách không tránh va chạm lẫn nhau (có thể chồng nhau khi cùng lối), nhưng không xuyên nội thất.
- Chỉ một hướng nhìn cho mọi nhân vật (dùng lật ngang).
- Không có Event pop-up (giai đoạn mở rộng). Không có âm bước chân.
- Save chỉ nằm trên thiết bị (`Application.persistentDataPath`); xóa dữ liệu ứng dụng sẽ mất tiến trình.
- Chữ dùng LiberationSans vì font tròn KATURI của bộ asset thiếu dấu tiếng Việt.

## 7. Khác biệt so với game tham khảo

- Thông số (giá, thời gian, sức chứa, tip, năng lượng) là số khởi điểm của prototype, không lấy từ bản gốc; chỉ *thứ tự chuỗi bánh* và tên nhân vật lấy từ dữ liệu trong bộ asset.
- Mỗi mẻ nướng ra 3 chiếc; kho dùng chung; kệ chỉ chứa một loại tại một thời điểm.
- Ba khu riêng (Phòng nghỉ, Bếp, Tiệm + mặt tiền/phố), chuyển bằng mũi tên hai bên; mỗi khu tự co giãn camera để vừa màn hình. Bản gốc có nhiều khu và theme mùa hơn.
- Phòng nghỉ, heo đất tip, tự bổ sung kệ, năng lượng nhân viên được thiết kế theo mô tả công khai, không phải sao chép cơ chế đã kiểm tra.
- Không có gacha, IAP, quảng cáo, giao hàng, theme mùa, thú cưng/NPC dọn dẹp.
