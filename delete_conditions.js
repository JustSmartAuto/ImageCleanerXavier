// ImageCleaner 删除判定脚本（QuickJS）
// 将此文件放在 imagecleaner jar 同目录即生效；不放置时使用内置 AND 逻辑。
//
// 约定：定义 evaluate(context) 函数，返回 true 表示允许继续删除最旧目标，false 表示停止。
// context 字段：
//   nowMs                当前 UTC 毫秒
//   path                 监控路径
//   cleanupMode          "Image" 或 "Folder"
//   storageTimeSeconds   保存时间阈值（秒）
//   expiredCount         已过期目标数（创建时间早于阈值）
//   imageCount           当前目标总数
//   imageCountEnabled    是否启用数量条件
//   imageCountThreshold  数量阈值
//   freeSpaceGb          磁盘剩余空间 GB
//   diskSpaceEnabled     是否启用磁盘条件
//   diskSpaceThresholdGb 磁盘余量阈值 GB
//
// 默认逻辑（与未放置本文件时一致）：
function evaluate(c) {
    var ageMet = c.expiredCount > 0;
    var countMet = !c.imageCountEnabled || c.imageCount > c.imageCountThreshold;
    var diskMet = !c.diskSpaceEnabled || c.freeSpaceGb < c.diskSpaceThresholdGb;
    return ageMet && countMet && diskMet;
}

// 自定义示例：磁盘低于 5GB 时无视保存时间直接清理最旧文件夹
// function evaluate(c) {
//     if (c.freeSpaceGb < 5) return c.imageCount > 0;
//     return c.expiredCount > 0 && (!c.imageCountEnabled || c.imageCount > c.imageCountThreshold);
// }
