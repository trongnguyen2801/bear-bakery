# Asset audit — Tiệm Bánh Gấu (prototype)

Nguồn: `/Users/trongnp/UnityRE/BearBakery/export_project/ExportedProject` (không chỉnh sửa).
Bảng truy vết từng sprite đã tích hợp: `Docs/asset_manifest.csv` (semantic id → PNG trong game → `Sprite/*.asset` gốc → atlas `Texture2D/*.png` → rect/pivot/border).
Bằng chứng nạp được trong Unity: `Docs/art_load_check.txt` (236/236 sprite nạp qua `Resources.Load<Sprite>`, 0 thiếu). Bản build chạy tự động cũng báo `missingSprites=0`.

## 1. Dùng được và đã tích hợp

| Nhóm | Số lượng | Ghi chú |
|---|---|---|
| Nền/kiến trúc (`bg.*`) | 30 | tường bếp/tiệm/phòng nghỉ, sàn (tile), cửa sổ, tủ lạnh, giá kho, bàn nhồi bột, quầy, mặt tiền "BAKERY", cửa nhân viên |
| Thiết bị (`equip.*`) | 9 | lò + cửa lò mở/đóng + lửa + khói (offset theo `OvenSlot.prefab`), khay ghép (`Img_Table_Merge801` theo `MergeTray.prefab`), kệ bán (`Img_Table_Sell801`, 5 vị trí bánh theo `StandSlot.prefab`) |
| Bột/bánh (`dough.*`, `bread.*`, `breadBig.*`) | 18 | 6 cấp × (bột, bánh nhỏ, bánh lớn) — mỗi cấp là một sprite **khác nhau** thật |
| Nhân viên | 29 frame | Gấu 9, Amber 12 (kể cả giỏ bánh), Tweetie 8 |
| Khách | 36 frame | 6 loài × 6 frame (Idle, Hold, Walk 1–2, Pickwalk 1–2) |
| Đạo cụ nhân vật | 10 | bóng đổ, khay có/không bánh, túi bánh, hóa đơn, bubble tim/chấm than/buồn |
| Nội thất | 8 | 6 dùng trong gameplay (Ghế lười, Sofa, Giường, Máy cà phê, Đèn đứng, Thảm cỏ) |
| UI / VFX | ~95 | nút, icon tab, coin, khung popup 9-slice (border gốc), heo đất 3 trạng thái, bàn tay hướng dẫn, bụi ghép, sao, lấp lánh, "Zzz", minh họa chân dung khách/nhân viên |
| Âm thanh | 24 `.ogg` | nhạc nền `bgm_Main`, nhồi bột, đặt bột, mở/xong lò, bày kệ, bán, coin, nâng cấp, lắp đặt, giọng Gấu/Amber/Tweetie… |
| Font | 2 | LiberationSans (chữ Việt), KATURI (chỉ số) |

## 2. Cần cắt / đổi định dạng (đã làm, lưu ở thư mục dẫn xuất)

- **Toàn bộ sprite nằm trong atlas** (`sactx-*-ASTC 6x6-Atlas_*.png`). Đã đọc metadata `Sprite.asset` (`m_Rect`, `m_RD.textureRect`, `textureRectOffset`, `settingsRaw`, `m_Pivot`, `m_Border`, `m_PixelsToUnits`) để cắt đúng khung; không chia lưới đều.
- `settingsRaw` của cả 3.316 sprite cho thấy *packed, rectangle, rotation = 0* → không cần xoay; không sprite nào bị trim lệch khung nguồn.
- PNG đã cắt: `_derived/sprites/<atlas>/<tên>.png`; bản dùng trong game: `Assets/BearBakery/Resources/Art/<nhóm>/<tên>__<atlas>.png`.
- Pivot/border gốc được giữ khi import (`Editor/ArtImporter.cs` đọc `Editor/import_settings.json`). Ví dụ nhân vật có pivot ở chân (y = 0), Gấu/Amber có pivot riêng giúp các frame Idle/Work/Fast/Rest khớp nhau.
- Sprite trùng tên giữa atlas (ví dụ `Croissant` nhỏ/lớn) được phân biệt bằng hậu tố atlas.

## 3. Thiếu frame / thiếu hướng (đã xử lý thay thế, ghi rõ)

| Thiếu | Ảnh hưởng | Cách xử lý trong prototype |
|---|---|---|
| Gấu, Amber, Tweetie **không có frame đi bộ** | không thể đi bộ thật tới phòng nghỉ | nhảy nhẹ + mờ dần ở trạm làm, hiện ra ở cửa nhân viên rồi nhảy tới chỗ nghỉ (tween, không phải frame) |
| `Sparrow_Work.anim` tham chiếu Work 3/Work 4 nhưng atlas không có sprite | | lặp Work01/Work02 |
| Không có tư thế ngồi/nằm trên ghế, giường | | dùng frame **Rest** gốc (ngủ gật) đặt lên đồ nội thất |
| Khách chỉ có **1 hướng** (chính diện, hơi nghiêng trái), không có quay lưng | | `flipX` khi đi sang phải; không kéo méo sprite |
| Khách không có frame "trả tiền"/"cảm ơn" | | frame Hold + bubble tim/túi bánh + Amber dùng frame Fast (đưa túi, lấp lánh) |
| Thu ngân: không có frame vẫy tay riêng | | Fast01–03 (đưa bánh) làm cử chỉ cảm ơn |

## 4. Thiếu asset chức năng

| Thiếu | Thay thế tạm |
|---|---|
| Font tròn dễ thương có dấu tiếng Việt (KATURI thiếu 134 ký tự) | LiberationSans cho chữ; KATURI chỉ cho số |
| Logo "Tiệm Bánh Gấu" tiếng Việt | logo `UI_Popup_Logo1` ("B.BAKERY") + biển "BAKERY" trên mặt tiền + chữ tiêu đề; icon app = `Illust_Bear01` |
| Phòng nghỉ nhân viên mùa chính (nền riêng) | ghép từ `Wallpaper002` + `Sng_Main_Tile001` + cửa `Img_Loading_Door01` |
| Gạch vỉa hè/đường phố mùa chính | mảng màu phẳng |
| Cầu thang/lối đi giữa bếp và phòng nghỉ | không có — nhân viên "vào cửa nhân viên" |
| Hiệu ứng hạt (ParticleSystem trong `FX_Merge.prefab`, `Get_Coin.prefab`) | dựng lại bằng tween sprite thật (`Img_MergeDust01`, `Image_StarEffect`, `Img_Twinkle01/02`, `UI_Coin`) |
| Âm bước chân, âm heo đất | không có; dùng `se_Etc_GetCoin` khi nhận tip |
| Biến thể hình theo cấp cho lò/kệ/nội thất mùa chính | không có; nâng cấp hiển thị bằng hiệu ứng lắp đặt/squash + sao, thông số đổi thật |

## 5. Có trong bộ nhưng không dùng (ngoài phạm vi)

Gacha, cửa hàng IAP/VIP, boutique/phòng thử đồ, 8 bộ theme mùa (Halloween, Xmas, Hawaii…), 4 loài khách còn lại (Fox, Otter, Sheep, Hamster — có đủ 6 frame trong bộ; muốn dùng cần thêm vào manifest, FrameAnim và config), Raccoon/Dog (NPC dọn dẹp/giao hàng), AssetBundles, mã decompile trong `Scripts/`.
