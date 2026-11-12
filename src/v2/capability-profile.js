/**
 * ⚠️ 此文件已废弃，不要引用或运行
 *
 * 原因（18条问题中的 #6-10）：
 * - 读取了 inventory 回包里不存在的字段：item.pick / createTile / createWall / damage / healLife
 * 真实回包字段（Commands.cs:4639-4646）：{slot, hotbar, type, name, stack, prefix}
 * - AVAILABLE_COMMANDS 只有 22 条，真实有 123 条
 * - reach:7 是过时值，实测攻击距离横向 5 格、纵向 4 格
 * - clearance 净空检查永远返回 ok（只有 TODO 注释）
 * - 整个预验证思路已被 ARCHITECTURE_REDESIGN_V2.md 否定
 *
 * 对着真实数据喂进去必然误判：
 * - 有铁镐也会报 matched:false, missing:["pickaxe"]
 * - checkMobility 永远通过
 *
 * 不要修这个文件，这个思路本身就是错的。
 */
