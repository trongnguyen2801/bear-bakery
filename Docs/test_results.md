# Kết quả kiểm thử model (tự động)

Chạy: 2026-10-14 16:50 · Unity 6000.3.24f1 · batchmode · **33/33 đạt**

| # | Kiểm tra | Kết quả | Ghi chú |
|---|---|---|---|
| 1 | Ghép đúng: 2 bột cùng cấp -> 1 bột cấp kế, tiêu thụ đúng 2 đầu vào | ĐẠT |  |
| 2 | Ghép sai cấp: không đổi trạng thái (trở về ô cũ) | ĐẠT |  |
| 3 | Thả vào ô trống: di chuyển giữ nguyên ID | ĐẠT |  |
| 4 | Thả ra ngoài / ô khóa / chính nó: không đổi trạng thái | ĐẠT |  |
| 5 | Cấp tối đa không ghép tiếp | ĐẠT |  |
| 6 | Bàn đầy: đầu bếp dừng, không mất bột; mở chỗ thì làm tiếp | ĐẠT |  |
| 7 | Hỗ trợ đầu bếp có giới hạn mỗi chu kỳ | ĐẠT |  |
| 8 | Lò bận: không nhận bột thứ hai, bột vẫn ở bàn | ĐẠT |  |
| 9 | Nướng dùng thời gian mô phỏng; giữ recipeId | ĐẠT |  |
| 10 | Kho đầy: bánh ở lại lò, không mất, không nhân đôi | ĐẠT |  |
| 11 | Spam nhận bánh: chỉ cộng một lần | ĐẠT |  |
| 12 | Chế độ cháy (bật qua config): chỉ tính khi chơi, có dọn lò | ĐẠT |  |
| 13 | Mặc định tắt cháy: bánh chờ lâu không mất | ĐẠT |  |
| 14 | Bày kệ: kho giảm đúng số chuyển, không quá sức chứa | ĐẠT |  |
| 15 | Hai khách cùng chọn đơn vị cuối: chỉ một người lấy được | ĐẠT |  |
| 16 | Kệ hết hàng: không sinh khách mua; khách buồn ghé không lấy gì | ĐẠT |  |
| 17 | Thanh toán ghi tiền đúng một lần; tip vào heo đất riêng | ĐẠT |  |
| 18 | Món thích luôn nằm trong công thức đã mở | ĐẠT |  |
| 19 | Spam mua nâng cấp: không trừ/cộng hai lần | ĐẠT |  |
| 20 | Nhận thưởng nhiệm vụ đúng một lần | ĐẠT |  |
| 21 | Nhiệm vụ hướng dẫn hoàn tất bằng sự kiện thật (không có Next) | ĐẠT |  |
| 22 | Reload giữa lúc nướng: không mất/nhân đôi job | ĐẠT |  |
| 23 | Reload khi khách đang cầm bánh chờ thanh toán: tính tiền đúng một lần | ĐẠT |  |
| 24 | Reservation chưa lấy hàng được trả lại khi lưu/tải | ĐẠT |  |
| 25 | Offline: không bán vượt tồn kho, tối đa 2 giờ, nhận một lần | ĐẠT |  |
| 26 | Offline có tự bổ sung (đã mở+bật): chỉ lấy từ kho, không tạo hàng | ĐẠT |  |
| 27 | Offline không tự tạo hoặc ghép bột | ĐẠT |  |
| 28 | Delta âm (đồng hồ lùi) = 0 | ĐẠT |  |
| 29 | Resume và load dùng chung cơ chế: không tính thời gian hai lần | ĐẠT |  |
| 30 | Năng lượng thấp: chậm hơn nhưng vẫn chạy; nghỉ trên nội thất hồi phục | ĐẠT |  |
| 31 | Mua nội thất thay đổi tốc độ hồi (có trần) | ĐẠT |  |
| 32 | Không số âm sau 30 phút mô phỏng có bot | ĐẠT |  |
| 33 | Save lỗi: dùng bản sao lưu; cả hai lỗi: giữ tệp lỗi, không xóa âm thầm | ĐẠT |  |
