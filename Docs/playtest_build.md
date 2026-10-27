# Playtest 10 phút từ save mới (bản build macOS, bot tự chơi)

Người chơi ở đây là **bot** `AutoPilot` thao tác qua input giả lập (kéo/chạm thật trên màn hình) với chính sách `BotPolicy` (ưu tiên: nhận thưởng → lấy bánh → bày kệ → ghép cặp thấp nhất → nướng → mua món có giá trị/chi phí tốt nhất → chạm hỗ trợ). Bot phản xạ nhanh hơn người (0,25–0,35 s/thao tác), nên mốc thời gian dưới đây là **cận dưới**. Mô phỏng nhịp người hơn (1,2 s/thao tác, +1 s khi đổi khu) ở `Docs/balance_sim.md`.

Log đầy đủ: `Docs/screens/autolog_390x844_play.txt`; ảnh mỗi phút: `Docs/screens/390x844_*_t*.png`; clip 80 giây đầu: `Docs/video/gameplay_first80s_390x844.mp4`.

## Mốc chính

| Mốc | Bản build (bot nhanh) | Mô phỏng nhịp người (3 seed) | Mục tiêu |
|---|---|---|---|
| Bán lần đầu | 0:26 | 0:30 | < 1:00 |
| Mua nâng cấp đầu | 0:51 (Đầu bếp cấp 2) | 0:51 – 1:00 | 0:45 – 1:30 |
| Mở lò thứ hai | 2:52 | 3:42 – 3:50 | 3:00 – 5:00 |
| Mở công thức cấp 4 / 5 / 6 | 0:46 / 5:50 / 6:25 | 1:57–2:32 / 9:33–9:50 / 10:23–10:41 | tiến triển trong 10–15 phút |
| Sau 10 phút / 15 phút | 98 bánh, cấp tiệm 5, 2 lò, 4 kệ, 11 ô bàn, 4 nội thất (10 phút) | 138–140 bánh, cấp 5, 2 lò, 4 kệ, 11 ô bàn (15 phút) | |

## Dòng thời gian (bản build)

```
0:05 xong "Có 2 cục bột"          0:08 ghép lần đầu → mở Bánh vòng (cấp 2), đưa vào lò
0:12 lấy bánh                     0:13 bày kệ
0:26 BÁN LẦN ĐẦU                  0:46 mở Bánh xoắn (cấp 4)
0:51 mua Đầu bếp cấp 2            0:52 xem phòng nghỉ (hoàn tất 8 bước hướng dẫn)
1:11 mở kệ thứ 3                  1:16 tiệm cấp 2
1:27 Sức chứa cấp 2               1:47 Lò nướng cấp 2
1:56 tiệm cấp 3, bán đủ 15 bánh   2:52 MỞ LÒ THỨ HAI (650 xu)
3:41 tiệm cấp 4                   4:28 Đầu bếp cấp 3
5:13 mua Sofa, khách quen đạt 2 tim
5:50 mở Muffin (cấp 5)            6:25 mở Bánh mứt dâu (cấp 6)
6:26 mở kệ thứ 4                  6:32 Thảm cỏ   7:22 Đèn đứng
7:57 Lò nướng cấp 3               8:15 tiệm cấp 5, Máy cà phê
```

## Khoảng chờ không có việc

Mô phỏng nhịp người: seed 2 và 3 không có khoảng nào > 8 s không có việc hữu ích; seed 1 có 2 khoảng (8:56 dài 11 s, 12:42 dài 8 s) — lúc bot đang dồn bột để mở cấp 5–6 và kho/kệ đã đầy, chỉ còn chờ khách. Cuối phiên, nút thắt chuyển sang **số khách** (≈ 1 khách/5–9 s): bánh dồn ở kho và lò — người chơi giải quyết bằng ghép bánh cấp cao (giá trị/khách cao hơn), mua kệ, nâng Sức chứa, hoặc nhờ Tweetie mời khách.

## Quan sát

- Giá trị trên mỗi cục bột cấp 1 tăng nhẹ theo cấp ghép (15 → 22,5 xu/bột) nên ghép không bao giờ lỗ; giá trị trên mỗi giây lò và mỗi khách tăng mạnh theo cấp → lò và khách là nút thắt khuyến khích ghép, nhưng giữ bánh thấp cấp giúp kệ không trống (khách buồn bỏ đi khi kệ hết).
- Thu nhập giảm tạm thời khi người chơi dồn bột để mở cấp 5–6 (phút 5–9 trong mô phỏng) — đúng đánh đổi "ghép hay nướng ngay".
- Năng lượng: nhân viên đi nghỉ lần đầu quanh phút 4–5, mỗi lần 15–20 s; trong lúc đó người chơi có việc thay (tự nhồi / tự tính tiền).
