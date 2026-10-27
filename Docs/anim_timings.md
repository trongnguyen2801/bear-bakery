# Animation metadata đọc từ AnimationClip gốc

Nhân vật trong bộ asset là **animation theo frame** (mỗi frame là một GameObject con bật/tắt bằng curve `m_IsActive`). Bảng dưới là các mốc bật/tắt đã đọc; game dùng đúng các mốc này (FrameAnim.cs).
Chỉ có **một hướng nhìn** (chính diện, hơi nghiêng trái) → game dùng `flipX` khi đi sang phải; không có frame đi lùi/quay lưng.

## Bear_Idle  (`AnimationClip/Bear_Idle.anim`, dài 2.000s, loop=True, sampleRate 60)
- `Fast` m_IsActive: 0.000s→0.0
- `Work` m_IsActive: 0.000s→0.0
- `Rest` m_IsActive: 0.000s→0.0
- `Idle/Idle 2` m_IsActive: 0.000s→0.0, 1.000s→1.0, 2.000s→0.0
- `Idle/Idle 1` m_IsActive: 0.000s→1.0, 1.000s→0.0, 2.000s→1.0
- `Idle` m_IsActive: 0.000s→1.0

## Bear_Work  (`AnimationClip/Bear_Work.anim`, dài 1.333s, loop=True, sampleRate 60)
- `Rest` m_IsActive: 0.000s→0.0
- `Fast` m_IsActive: 0.000s→0.0
- `Work/Work 2` m_IsActive: 0.000s→0.0, 0.450s→1.0, 0.883s→0.0
- `Work/Work 1` m_IsActive: 0.000s→1.0, 0.450s→0.0, 1.333s→1.0
- `Work/Work 3` m_IsActive: 0.000s→0.0, 0.883s→1.0, 1.333s→0.0
- `Idle` m_IsActive: 0.000s→0.0
- `Work` m_IsActive: 0.000s→1.0

## Bear_Fast  (`AnimationClip/Bear_Fast.anim`, dài 0.667s, loop=True, sampleRate 60)
- `Work` m_IsActive: 0.000s→0.0
- `Rest` m_IsActive: 0.000s→0.0
- `Fast` m_IsActive: 0.000s→1.0
- `Fast/Fast 1` m_IsActive: 0.000s→0.0, 0.333s→1.0, 0.667s→0.0
- `Fast/Fast 2` m_IsActive: 0.000s→1.0, 0.333s→0.0, 0.667s→1.0
- `Idle` m_IsActive: 0.000s→0.0

## Bear_Rest  (`AnimationClip/Bear_Rest.anim`, dài 1.500s, loop=False, sampleRate 60)
- `Idle` m_IsActive: 0.000s→0.0
- `Fast` m_IsActive: 0.000s→0.0
- `Work` m_IsActive: 0.000s→0.0
- `Rest` m_IsActive: 0.000s→1.0
- `Rest/Rest 1` m_IsActive: 0.000s→1.0, 0.250s→0.0, 0.500s→1.0, 0.750s→0.0, 1.000s→1.0, 1.250s→0.0, 1.500s→1.0
- `Rest/Rest 2` m_IsActive: 0.000s→0.0, 0.250s→1.0, 0.500s→0.0, 0.750s→1.0, 1.000s→0.0, 1.250s→1.0, 1.500s→0.0

## LesserPanda_Idle  (`AnimationClip/LesserPanda_Idle.anim`, dài 2.000s, loop=True, sampleRate 60)
- `Fast` m_IsActive: 0.000s→0.0
- `Work` m_IsActive: 0.000s→0.0
- `Rest` m_IsActive: 0.000s→0.0
- `Idle` m_IsActive: 0.000s→1.0
- `Idle/Idle 2` m_IsActive: 0.000s→0.0, 1.000s→1.0, 2.000s→0.0
- `Idle/Idle 1` m_IsActive: 0.000s→1.0, 1.000s→0.0, 2.000s→1.0
- `Breads` m_IsActive: 0.000s→0.0

## LesserPanda_Work  (`AnimationClip/LesserPanda_Work.anim`, dài 1.167s, loop=True, sampleRate 60)
- `Fast` m_IsActive: 0.000s→0.0
- `Idle` m_IsActive: 0.000s→0.0
- `Rest` m_IsActive: 0.000s→0.0
- `Work` m_IsActive: 0.000s→1.0
- `Work/Work 1` m_IsActive: 0.000s→1.0, 0.300s→0.0, 1.167s→1.0
- `Breads` m_IsActive: 0.000s→1.0
- `Work/Work 2` m_IsActive: 0.000s→0.0, 0.300s→1.0, 0.583s→0.0
- `Work/Work 3` m_IsActive: 0.000s→0.0, 0.583s→1.0, 0.883s→0.0
- `Work/Work 4` m_IsActive: 0.000s→0.0, 0.883s→1.0, 1.167s→0.0

## LesserPanda_Fast  (`AnimationClip/LesserPanda_Fast.anim`, dài 0.500s, loop=True, sampleRate 60)
- `Idle` m_IsActive: 0.000s→0.0
- `Work` m_IsActive: 0.000s→0.0
- `Rest` m_IsActive: 0.000s→0.0
- `Fast` m_IsActive: 0.000s→1.0
- `Breads` m_IsActive: 0.000s→1.0
- `Fast/Fast 1` m_IsActive: 0.000s→1.0, 0.167s→0.0, 0.500s→1.0
- `Fast/Fast 2` m_IsActive: 0.000s→0.0, 0.167s→1.0, 0.333s→0.0
- `Fast/Fast 3` m_IsActive: 0.000s→0.0, 0.333s→1.0, 0.500s→0.0

## LesserPanda_Rest  (`AnimationClip/LesserPanda_Rest.anim`, dài 1.500s, loop=False, sampleRate 60)
- `Fast` m_IsActive: 0.000s→0.0
- `Idle` m_IsActive: 0.000s→0.0
- `Work` m_IsActive: 0.000s→0.0
- `Rest` m_IsActive: 0.000s→1.0
- `Rest/Rest 1` m_IsActive: 0.000s→1.0, 0.250s→0.0, 0.500s→1.0, 0.750s→0.0, 1.000s→1.0, 1.250s→0.0, 1.500s→1.0
- `Rest/Rest 2` m_IsActive: 0.000s→0.0, 0.250s→1.0, 0.500s→0.0, 0.750s→1.0, 1.000s→0.0, 1.250s→1.0, 1.500s→0.0
- `Breads` m_IsActive: 0.000s→0.0

## Sparrow_Idle  (`AnimationClip/Sparrow_Idle.anim`, dài 2.000s, loop=True, sampleRate 60)
- `Fast` m_IsActive: 0.000s→0.0
- `Work` m_IsActive: 0.000s→0.0
- `Rest` m_IsActive: 0.000s→0.0
- `path_0x64C26B81_UOUnouN` m_IsActive: 0.000s→1.0
- `Idle` m_IsActive: 0.000s→1.0
- `Idle/Idle 2` m_IsActive: 0.000s→0.0, 1.000s→1.0, 2.000s→0.0
- `Idle/Idle 1` m_IsActive: 0.000s→1.0, 1.000s→0.0, 2.000s→1.0

## Sparrow_Work  (`AnimationClip/Sparrow_Work.anim`, dài 4.000s, loop=True, sampleRate 60)
- `Fast` m_IsActive: 0.000s→0.0
- `Rest` m_IsActive: 0.000s→0.0
- `Idle` m_IsActive: 0.000s→0.0
- `Work` m_IsActive: 0.000s→1.0
- `Work/Work 1` m_IsActive: 0.000s→1.0, 0.500s→0.0, 1.000s→1.0, 1.500s→0.0
- `Work/Work 2` m_IsActive: 0.000s→0.0, 0.500s→1.0, 1.000s→0.0, 1.500s→1.0, 2.000s→0.0
- `Work/Work 3` m_IsActive: 0.000s→0.0, 2.000s→1.0, 2.500s→0.0, 3.000s→1.0, 3.500s→0.0, 4.000s→1.0
- `Work/Work 4` m_IsActive: 0.000s→0.0, 2.500s→1.0, 3.000s→0.0, 3.500s→1.0, 4.000s→0.0

## Sparrow_Fast  (`AnimationClip/Sparrow_Fast.anim`, dài 1.000s, loop=True, sampleRate 60)
- `Work` m_IsActive: 0.000s→0.0
- `Rest` m_IsActive: 0.000s→0.0
- `Idle` m_IsActive: 0.000s→0.0
- `path_0x64C26B81_UOUnouN` m_IsActive: 0.000s→1.0
- `Fast` m_IsActive: 0.000s→1.0
- `Fast/Fast 1` m_IsActive: 0.000s→1.0, 0.250s→0.0, 0.500s→0.0, 1.000s→1.0
- `Fast/Fast 2` m_IsActive: 0.000s→0.0, 0.250s→1.0, 0.500s→0.0
- `Fast/Fast 3` m_IsActive: 0.000s→0.0, 0.500s→1.0, 0.750s→0.0
- `Fast/Fast 4` m_IsActive: 0.000s→0.0, 0.750s→1.0, 1.000s→0.0

## Sparrow_Rest  (`AnimationClip/Sparrow_Rest.anim`, dài 1.500s, loop=True, sampleRate 60)
- `Fast` m_IsActive: 0.000s→0.0
- `Work` m_IsActive: 0.000s→0.0
- `Rest` m_IsActive: 0.000s→1.0
- `path_0x64C26B81_UOUnouN` m_IsActive: 0.000s→1.0
- `Idle` m_IsActive: 0.000s→0.0
- `Rest/Rest 1` m_IsActive: 0.000s→1.0, 0.250s→0.0, 0.500s→1.0, 0.750s→0.0, 1.000s→1.0, 1.250s→0.0, 1.500s→1.0
- `Rest/Rest 2` m_IsActive: 0.000s→0.0, 0.250s→1.0, 0.500s→0.0, 0.750s→1.0, 1.000s→0.0, 1.250s→1.0, 1.500s→0.0

## Walk  (`AnimationClip/Walk.anim`, dài 1.000s, loop=True, sampleRate 60)
- `Walk` ScaleCurves: 0.000s→(1.00,1.00), 0.483s→(1.00,1.00), 0.500s→(1.00,0.98)
- `Idle` m_IsActive: 0.000s→0.0
- `Walk` m_IsActive: 0.000s→1.0
- `Walk/Walk 2` m_IsActive: 0.000s→0.0, 0.500s→1.0, 1.000s→0.0
- `Walk/Walk 1` m_IsActive: 0.000s→1.0, 0.500s→0.0, 1.000s→1.0
- `'Pick_Walk '` m_IsActive: 0.000s→0.0, 1.000s→0.0
- `Pick_Hold` m_IsActive: 0.000s→0.0, 1.000s→0.0

## Chracters_Pickwalk  (`AnimationClip/Chracters_Pickwalk.anim`, dài 1.000s, loop=True, sampleRate 60)
- `'Pick_Walk '` ScaleCurves: 0.000s→(1.00,1.00), 0.483s→(1.00,1.00), 0.500s→(1.00,0.98)
- `'Pick_Walk '` m_IsActive: 0.000s→1.0
- `Pick_Walk /Pick_Walk 2` m_IsActive: 0.000s→0.0, 0.500s→1.0, 1.000s→0.0
- `Pick_Walk /Pick_Walk 1` m_IsActive: 0.000s→1.0, 0.500s→0.0, 1.000s→1.0
- `Idle` m_IsActive: 0.000s→0.0, 1.000s→0.0
- `Pick_Hold` m_IsActive: 0.000s→0.0, 1.000s→0.0
- `Walk` m_IsActive: 0.000s→0.0, 1.000s→0.0

## Chracters_Pickhold  (`AnimationClip/Chracters_Pickhold.anim`, dài 0.000s, loop=True, sampleRate 60)
- `Pick_Hold` m_IsActive: 0.000s→1.0
- `'Pick_Walk '` m_IsActive: 0.000s→0.0
- `Idle` m_IsActive: 0.000s→0.0
- `Walk` m_IsActive: 0.000s→0.0

## Chracters_Idle  (`AnimationClip/Chracters_Idle.anim`, dài 0.000s, loop=False, sampleRate 60)
- `Idle` m_IsActive: 0.000s→1.0
- `Walk` m_IsActive: 0.000s→0.0
- `'Pick_Walk '` m_IsActive: 0.000s→0.0
- `Pick_Hold` m_IsActive: 0.000s→0.0

## Oven_On  (`AnimationClip/Oven_On.anim`, dài 2.000s, loop=True, sampleRate 60)
- `OvenSmoke` ScaleCurves: 0.000s→(1.10,0.00), 0.383s→(1.42,0.37), 0.850s→(1.14,1.07), 0.883s→(1.14,1.10), 2.000s→(1.40,1.40)
- `OvenSlotClose` m_IsActive: 0.000s→1.0
- `OvenSlotOpen` m_IsActive: 0.000s→0.0
- `OvenFire` m_IsActive: 0.000s→1.0, 2.000s→1.0
- `OvenFire` m_Color.a: 0.000s→1.0, 1.000s→0.78431374, 2.000s→1.0
- `OvenSmoke` m_IsActive: 0.000s→1.0, 1.000s→1.0
- `OvenSmoke` m_AnchoredPosition.y: 0.000s→72.0, 1.000s→76.0, 2.000s→115.5
- `OvenSmoke` m_Color.a: 0.000s→1.0, 1.000s→1.0, 2.000s→0.0
- `Bubble_Oven` m_IsActive: 0.000s→1.0, 2.000s→1.0
- `OvenSmoke` m_AnchoredPosition.x: 0.000s→0.0, 2.000s→0.0

## Oven_Off  (`AnimationClip/Oven_Off.anim`, dài 0.000s, loop=True, sampleRate 60)
- `OvenFire` m_IsActive: 0.000s→0.0
- `OvenSmoke` m_IsActive: 0.000s→0.0
- `OvenSlotClose` m_IsActive: 0.000s→0.0
- `Bubble_Oven` m_IsActive: 0.000s→0.0
- `OvenSlotOpen` m_IsActive: 0.000s→1.0

## Oven_Complete  (`AnimationClip/Oven_Complete.anim`, dài 0.000s, loop=True, sampleRate 60)
- `OvenSlotClose` m_IsActive: 0.000s→1.0
- `OvenSlotOpen` m_IsActive: 0.000s→0.0
- `Bubble_Oven` m_IsActive: 0.000s→1.0

## Furniture  (`AnimationClip/Furniture.anim`, dài 5.867s, loop=True, sampleRate 60)
- `1` ScaleCurves: 0.000s→(1.00,1.00), 0.183s→(1.05,1.05), 0.367s→(1.07,0.95), 0.550s→(1.00,1.00), 0.650s→(1.01,0.95), 0.883s→(1.00,1.00), 5.867s→(1.00,1.00)
- `2` ScaleCurves: 1.167s→(1.00,1.00), 1.350s→(1.05,1.05), 1.533s→(1.07,0.95), 1.717s→(1.00,1.00), 1.817s→(1.01,0.95), 2.050s→(1.00,1.00)
- `3` ScaleCurves: 2.267s→(1.00,1.00), 2.450s→(1.05,1.05), 2.633s→(1.07,0.95), 2.817s→(1.00,1.00), 2.917s→(1.01,0.95), 3.150s→(1.00,1.00)
- `4` ScaleCurves: 3.367s→(1.00,1.00), 3.550s→(1.05,1.05), 3.733s→(1.07,0.95), 3.917s→(1.00,1.00), 4.017s→(1.01,0.95), 4.250s→(1.00,1.00)
- `5` ScaleCurves: 4.567s→(1.00,1.00), 4.750s→(1.05,1.05), 4.933s→(1.07,0.95), 5.117s→(1.00,1.00), 5.217s→(1.01,0.95), 5.450s→(1.00,1.00), 5.867s→(1.00,1.00)

## Earn_Money  (`AnimationClip/Earn_Money.anim`, dài 1.000s, loop=False, sampleRate 60)
- `Image_Coin` m_Color.a: 0.000s→1.0, 0.500s→1.0, 1.000s→0.0
- `Text_Price` m_Color.a: 0.000s→1.0, 0.500s→1.0, 1.000s→0.0
- `Text_Price` m_EffectColor.a: 0.000s→1.0, 0.500s→1.0, 1.000s→0.0

## Racco_Idle  (`AnimationClip/Racco_Idle.anim`, dài 2.000s, loop=True, sampleRate 60)
- `Idle` m_IsActive: 0.000s→1.0
- `PickWalk` m_IsActive: 0.000s→0.0
- `Idle/Idle 2` m_IsActive: 0.000s→0.0, 1.000s→1.0, 2.000s→0.0
- `Idle/Idle 1` m_IsActive: 0.000s→1.0, 1.000s→0.0, 2.000s→1.0

## Racco_PickWalk  (`AnimationClip/Racco_PickWalk.anim`, dài 1.000s, loop=True, sampleRate 60)
- `PickWalk` m_IsActive: 0.000s→1.0
- `Idle` m_IsActive: 0.000s→0.0
- `PickWalk/PickWalk 1` m_IsActive: 0.000s→1.0, 0.500s→0.0, 1.000s→1.0
- `PickWalk/PickWalk 2` m_IsActive: 0.000s→0.0, 0.500s→1.0, 1.000s→0.0
