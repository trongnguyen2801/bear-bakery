# Kết quả kiểm chứng

Môi trường đo: **Mac16,10 (Apple M4), macOS 27.0**, Unity 6000.3.24f1, bản build standalone macOS (cửa sổ, `targetFrameRate = 60`).
Chưa đo trên điện thoại thật.

## 1. Kiểm thử tự động

| Nguồn | Nội dung | Kết quả |
|---|---|---|
| `Docs/test_results.md` | 33 kiểm tra luật trên model (batchmode) | **33/33 đạt** |
| `Docs/screens/autolog_*_shots.txt` | Bản build tự chơi bằng **input giả lập theo toạ độ màn hình** (đi qua camera, EventSystem, InputRouter như chuột/ngón tay) | xem bảng 2 |
| `Docs/screens/autolog_390x844_play.txt` | Bản build tự chơi 10 phút liên tục | 98 bánh bán, mở đủ 6 công thức, không kẹt |
| `Docs/screens/autolog_390x844_resume.txt` | Mở lại bản lưu sau khi đóng app | lượt 1 (save của lượt 10 phút, đóng 65 s): bán 5 chiếc offline (+130), 1 khách đang cầm bánh được thanh toán đúng 1 lần (+12); lượt 2 (save của lượt chụp ảnh, đóng 21 phút): kệ chỉ còn 3 chiếc → bán đúng 3 (+78), 1 mẻ nướng xong; nhận thưởng một lần ở cả hai lượt (log hiện giữ lượt 2) |
| `Docs/art_load_check.txt` | Nạp mọi sprite trong manifest | 236/236 |

## 2. Kéo thả đúng sau khi scale (bản build)

| Độ phân giải | Mũi tên chuyển khu (Bếp→Tiệm→Bếp→Phòng nghỉ, mũi tên ngoài cùng ẩn) | Vuốt dọc không cuộn | Kéo-ghép | Thả ra ngoài bàn → trả về | Kéo vào lò | Mô phỏng chạy khi mở sheet + đổi khu |
|---|---|---|---|---|---|---|
| 360×800 | ĐẠT | ĐẠT | ĐẠT | ĐẠT | ĐẠT | ĐẠT (lò 10,4 s → 7,9 s; bột 2 → 3) |
| 390×844 | ĐẠT | ĐẠT | ĐẠT | ĐẠT | ĐẠT | ĐẠT |
| 430×932 | ĐẠT | ĐẠT | ĐẠT | ĐẠT | ĐẠT | ĐẠT |

Bấm mũi tên được kiểm tra bằng raycast uGUI thật tại toạ độ màn hình của mũi tên (mũi tên phải là phần tử trên cùng ở vị trí đó).

Ảnh so sánh cùng trạng thái ở 3 độ phân giải: `Docs/screens/compare_*.png`. HUD + thanh tab chiếm khoảng 23% chiều cao; phần thế giới ≈ 77% khi đóng bảng phụ.

## 3. Đối chiếu danh sách kiểm tra bắt buộc

| Yêu cầu | Bằng chứng |
|---|---|
| Ghép đúng/sai, thả ra ngoài, bảng đầy, cấp tối đa | test 1–6; self-test bản build |
| Multi-touch | `InputRouter` chỉ theo dõi một `fingerId` chính, các ngón khác bị bỏ qua; mất focus khi đang kéo → huỷ kéo, bột về chỗ cũ. **Chưa thử trên màn hình cảm ứng thật** (máy đo không có cảm ứng). |
| Lò bận, kho đầy, kệ hết hàng, hai khách cùng chọn món cuối | test 8, 10, 16, 15 |
| Spam nhận bánh / nhận thưởng / mua nâng cấp | test 11, 17, 20, 19 |
| Không số âm; không yêu cầu recipe chưa mở | test 32 (30 phút bot), test 18 |
| Đổi khu / mở bottom sheet không dừng mô phỏng | self-test bản build ở 3 độ phân giải |
| Reload khi đang nướng / thanh toán | test 22, 23, 24; kịch bản `resume` của bản build |
| Offline không bán vượt tồn kho, không nhận lại thưởng | test 25–29; `resume`: "offline claim once: PASS (+130, second +0)" |
| Save lỗi / phiên bản cũ / mới hơn | test 33 (backup, giữ tệp lỗi, migrate v1→v2, chế độ chỉ đọc) |
| Đường đi không xuyên quầy/kệ; sắp lớp theo chân | khách đi theo waypoint (cửa → hành lang → lối đi giữa các cột kệ → hàng chờ); thứ tự vẽ theo y của chân (SortingGroup). Khách **có thể chồng lên nhau** (không có tránh va chạm giữa khách). |
| Animation đổi đúng trạng thái | Gấu Work/Idle(bàn đầy)/Fast(khi chạm)/Rest; Amber Work khi tính tiền, Fast khi trao túi; khách Walk → Idle/Hold khi chọn → Pickwalk + khay → Hold khi chờ → Walk + túi khi về; lò đóng/mở cửa, lửa, khói chỉ khi nướng |
| FPS | xem mục 4 |

## 4. Hiệu năng (đo trong bản build macOS)

| Lượt | Khung hình | FPS trung bình | p95 | p99 | Tệ nhất |
|---|---|---|---|---|---|
| Tự chơi 10 phút, 390×844 | 35.816 | 59,5 | 17,5 ms | 17,6 ms | 49,9 ms |
| Chụp màn hình 390×844 | ~3.250 | 59,0 | 17,6 ms | 18,6 ms | 58,8 ms |
| Ghi clip (chụp 15 khung/s) | 4.753 | 58,5 | 17,0 ms | 32,8 ms | 49,9 ms |

Giá trị trên chỉ áp dụng cho máy Mac M4; **không khẳng định 60 FPS trên điện thoại**.

## 5. Lỗi / rủi ro còn tồn tại

- Khách không tránh nhau, có lúc 2 khách đứng chồng ở cùng lối đi.
- Cuối phiên (≈ phút 8–10) sản lượng vượt nhu cầu: kho đầy, lò chờ lấy. Đây là nút thắt có chủ đích (nâng Sức chứa / mua kệ / ghép cấp cao hơn) nhưng có thể làm người chơi thấy phải chờ; cần playtest người thật.
- Bước hướng dẫn "Mua 1 nâng cấp" dựa vào việc chạm tab Bếp lần 2 (đã có gợi ý chữ + bàn tay) — cần xác nhận người chơi thật hiểu.
- Nhân viên không có frame đi bộ nên di chuyển tới phòng nghỉ bằng nhảy + mờ qua cửa.
- Tiếng Việt dùng LiberationSans (font tròn của bộ asset thiếu dấu).
