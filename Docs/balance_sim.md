# Mô phỏng nhịp chơi 15 phút (headless, bot "người chơi hợp lý")

Bot dùng đúng các lệnh người chơi có (ghép, nướng, lấy bánh, bày kệ, mua, chạm hỗ trợ). Phản xạ 1,2 s/thao tác, +1 s khi chuyển giữa Bếp và Tiệm, chạm hỗ trợ tối đa 1 lần/3 s; đường đi khách dùng thời lượng mặc định. Số liệu là **mô phỏng**, không phải đo trên người thật.

## Lượt seed 1

- Bán lần đầu: **0:30** (mục tiêu < 60 s)
- Mua nâng cấp đầu: **0:52** (mục tiêu 45–90 s)
- Mở lò thứ hai: **3:48** (mục tiêu 3–5 phút)
- Mở công thức: cấp 1 @ 0:00, cấp 2 @ 0:12, cấp 3 @ 0:36, cấp 4 @ 1:57, cấp 5 @ 9:50, cấp 6 @ 10:41
- Sau 15 phút: bán 140 bánh, tổng thu 4782 xu, cấp tiệm 5, 550 thao tác (~37 /phút), ô bàn 11, lò 2, kệ 4, nâng cấp [4,3,3], nội thất mua 4
- Thu nhập theo phút: p1:119 · p2:286 · p3:360 · p4:435 · p5:209 · p6:197 · p7:120 · p8:145 · p9:320 · p10:148 · p11:420 · p12:494 · p13:759 · p14:344 · p15:426
- Khoảng chờ không có việc hữu ích > 8 s: 8:56 (11s), 12:42 (8s)

<details><summary>Dòng thời gian</summary>

    0:07  xong nhiệm vụ t1_dough
    0:12  mở công thức cấp 2
    0:12  xong nhiệm vụ t2_merge
    0:12  xong nhiệm vụ t3_bake
    0:15  xong nhiệm vụ t4_collect
    0:18  xong nhiệm vụ t5_stock
    0:30  BÁN LẦN ĐẦU
    0:30  xong nhiệm vụ t6_sell
    0:36  mở công thức cấp 3
    0:52  mở rộng ô bàn #5 (70 xu)
    0:52  xong nhiệm vụ t7_buy
    1:01  xong nhiệm vụ t8_break
    1:04  mua nâng cấp chef → cấp 2 (80 xu)
    1:24  mua nâng cấp capacity → cấp 2 (70 xu)
    1:34  tiệm lên cấp 2
    1:41  mở rộng ô bàn #6 (80 xu)
    1:57  mở công thức cấp 4
    1:59  xong nhiệm vụ p1_sell15
    2:46  tiệm lên cấp 3
    3:48  mở rộng LÒ #2 (650 xu)
    3:48  xong nhiệm vụ p2_oven2
    3:48  xong nhiệm vụ p3_recipe4
    3:53  mở rộng KỆ #3 (110 xu)
    3:54  mua nâng cấp oven → cấp 2 (90 xu)
    4:36  mở rộng ô bàn #7 (100 xu)
    4:44  cashier đi nghỉ
    4:47  chef đi nghỉ
    5:02  chef đi nghỉ
    5:46  tiệm lên cấp 4
    6:12  mở rộng ô bàn #8 (130 xu)
    6:44  promoter đi nghỉ
    7:24  mua nâng cấp chef → cấp 3 (160 xu)
    8:26  cashier đi nghỉ
    8:39  mua nội thất sofa (160 xu)
    8:39  xong nhiệm vụ p4_furn
    8:39  xong nhiệm vụ p5_hearts
    8:43  mua nâng cấp capacity → cấp 3 (190 xu)
    9:15  chef đi nghỉ
    9:50  mở công thức cấp 5
    9:57  mua nội thất rug (180 xu)
    10:41  mở công thức cấp 6
    10:41  xong nhiệm vụ p6_recipe6
    10:45  mở rộng KỆ #4 (280 xu)
    11:00  mở rộng ô bàn #9 (170 xu)
    11:58  mua nâng cấp oven → cấp 3 (240 xu)
    12:01  mua nội thất lamp (200 xu)
    12:13  cashier đi nghỉ
    12:20  tiệm lên cấp 5
    12:27  mua nội thất coffee (260 xu)
    12:33  mua nâng cấp chef → cấp 4 (400 xu)
    13:13  promoter đi nghỉ
    13:17  mở rộng ô bàn #10 (220 xu)
    13:24  chef đi nghỉ
    14:43  mở rộng ô bàn #11 (280 xu)
</details>

## Lượt seed 2

- Bán lần đầu: **0:30** (mục tiêu < 60 s)
- Mua nâng cấp đầu: **0:51** (mục tiêu 45–90 s)
- Mở lò thứ hai: **3:50** (mục tiêu 3–5 phút)
- Mở công thức: cấp 1 @ 0:00, cấp 2 @ 0:12, cấp 3 @ 0:36, cấp 4 @ 2:32, cấp 5 @ 9:47, cấp 6 @ 10:37
- Sau 15 phút: bán 138 bánh, tổng thu 4527 xu, cấp tiệm 5, 553 thao tác (~37 /phút), ô bàn 11, lò 2, kệ 4, nâng cấp [4,3,3], nội thất mua 4
- Thu nhập theo phút: p1:119 · p2:260 · p3:325 · p4:435 · p5:227 · p6:217 · p7:146 · p8:120 · p9:346 · p10:108 · p11:455 · p12:162 · p13:1003 · p14:245 · p15:359
- Khoảng chờ không có việc hữu ích > 8 s: không có

<details><summary>Dòng thời gian</summary>

    0:07  xong nhiệm vụ t1_dough
    0:12  mở công thức cấp 2
    0:12  xong nhiệm vụ t2_merge
    0:12  xong nhiệm vụ t3_bake
    0:15  xong nhiệm vụ t4_collect
    0:18  xong nhiệm vụ t5_stock
    0:30  BÁN LẦN ĐẦU
    0:30  xong nhiệm vụ t6_sell
    0:36  mở công thức cấp 3
    0:51  mở rộng ô bàn #5 (70 xu)
    0:51  xong nhiệm vụ t7_buy
    1:00  xong nhiệm vụ t8_break
    1:03  mua nâng cấp chef → cấp 2 (80 xu)
    1:18  mua nâng cấp capacity → cấp 2 (70 xu)
    1:29  tiệm lên cấp 2
    1:36  mở rộng ô bàn #6 (80 xu)
    2:00  xong nhiệm vụ p1_sell15
    2:32  mở công thức cấp 4
    2:54  tiệm lên cấp 3
    3:50  mở rộng LÒ #2 (650 xu)
    3:50  xong nhiệm vụ p2_oven2
    3:50  xong nhiệm vụ p3_recipe4
    4:02  mở rộng KỆ #3 (110 xu)
    4:03  mua nâng cấp oven → cấp 2 (90 xu)
    4:45  cashier đi nghỉ
    4:47  chef đi nghỉ
    5:03  chef đi nghỉ
    5:06  mở rộng ô bàn #7 (100 xu)
    5:44  tiệm lên cấp 4
    6:20  mở rộng ô bàn #8 (130 xu)
    6:44  promoter đi nghỉ
    7:33  mua nâng cấp chef → cấp 3 (160 xu)
    8:28  cashier đi nghỉ
    8:48  mua nội thất sofa (160 xu)
    8:49  xong nhiệm vụ p4_furn
    8:49  xong nhiệm vụ p5_hearts
    8:54  mua nâng cấp capacity → cấp 3 (190 xu)
    9:15  chef đi nghỉ
    9:47  mở công thức cấp 5
    9:58  mở rộng ô bàn #9 (170 xu)
    10:37  mở công thức cấp 6
    10:38  xong nhiệm vụ p6_recipe6
    10:42  mở rộng KỆ #4 (280 xu)
    11:08  mua nội thất rug (180 xu)
    12:25  mua nội thất lamp (200 xu)
    12:29  cashier đi nghỉ
    12:29  mua nâng cấp oven → cấp 3 (240 xu)
    12:46  tiệm lên cấp 5
    12:46  mua nâng cấp chef → cấp 4 (400 xu)
    12:59  mua nội thất coffee (260 xu)
    13:07  promoter đi nghỉ
    13:20  chef đi nghỉ
    13:41  mở rộng ô bàn #10 (220 xu)
    14:37  mở rộng ô bàn #11 (280 xu)
</details>

## Lượt seed 3

- Bán lần đầu: **0:30** (mục tiêu < 60 s)
- Mua nâng cấp đầu: **1:00** (mục tiêu 45–90 s)
- Mở lò thứ hai: **3:42** (mục tiêu 3–5 phút)
- Mở công thức: cấp 1 @ 0:00, cấp 2 @ 0:12, cấp 3 @ 0:36, cấp 4 @ 2:13, cấp 5 @ 9:33, cấp 6 @ 10:23
- Sau 15 phút: bán 140 bánh, tổng thu 4600 xu, cấp tiệm 5, 548 thao tác (~37 /phút), ô bàn 11, lò 2, kệ 4, nâng cấp [3,3,3], nội thất mua 4
- Thu nhập theo phút: p1:89 · p2:290 · p3:332 · p4:490 · p5:190 · p6:228 · p7:120 · p8:145 · p9:358 · p10:120 · p11:463 · p12:475 · p13:754 · p14:260 · p15:286
- Khoảng chờ không có việc hữu ích > 8 s: không có

<details><summary>Dòng thời gian</summary>

    0:07  xong nhiệm vụ t1_dough
    0:12  mở công thức cấp 2
    0:12  xong nhiệm vụ t2_merge
    0:12  xong nhiệm vụ t3_bake
    0:15  xong nhiệm vụ t4_collect
    0:18  xong nhiệm vụ t5_stock
    0:30  BÁN LẦN ĐẦU
    0:30  xong nhiệm vụ t6_sell
    0:36  mở công thức cấp 3
    1:00  mua nâng cấp chef → cấp 2 (80 xu)
    1:00  xong nhiệm vụ t7_buy
    1:02  xong nhiệm vụ t8_break
    1:05  mở rộng ô bàn #5 (70 xu)
    1:17  mua nâng cấp capacity → cấp 2 (70 xu)
    1:30  tiệm lên cấp 2
    1:37  mở rộng ô bàn #6 (80 xu)
    2:00  xong nhiệm vụ p1_sell15
    2:13  mở công thức cấp 4
    2:58  tiệm lên cấp 3
    3:42  mở rộng LÒ #2 (650 xu)
    3:42  xong nhiệm vụ p2_oven2
    3:42  xong nhiệm vụ p3_recipe4
    3:47  mở rộng KỆ #3 (110 xu)
    4:08  mua nâng cấp oven → cấp 2 (90 xu)
    4:09  mở rộng ô bàn #7 (100 xu)
    4:46  cashier đi nghỉ
    4:48  chef đi nghỉ
    5:04  chef đi nghỉ
    5:28  mở rộng ô bàn #8 (130 xu)
    5:43  tiệm lên cấp 4
    6:44  promoter đi nghỉ
    7:22  mua nâng cấp chef → cấp 3 (160 xu)
    8:25  cashier đi nghỉ
    8:25  mua nội thất sofa (160 xu)
    8:26  xong nhiệm vụ p4_furn
    8:26  xong nhiệm vụ p5_hearts
    8:29  mua nâng cấp capacity → cấp 3 (190 xu)
    9:17  chef đi nghỉ
    9:27  mở rộng ô bàn #9 (170 xu)
    9:33  mở công thức cấp 5
    10:23  mở công thức cấp 6
    10:23  xong nhiệm vụ p6_recipe6
    10:27  mở rộng KỆ #4 (280 xu)
    10:37  mua nội thất rug (180 xu)
    11:35  mua nội thất lamp (200 xu)
    11:52  mua nâng cấp oven → cấp 3 (240 xu)
    12:02  tiệm lên cấp 5
    12:02  cashier đi nghỉ
    12:04  mua nội thất coffee (260 xu)
    12:12  mở rộng ô bàn #10 (220 xu)
    13:17  promoter đi nghỉ
    13:37  chef đi nghỉ
    13:42  mở rộng ô bàn #11 (280 xu)
</details>

