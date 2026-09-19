using System;
using System.Collections.Generic;
using System.Linq;
using ImageCleaner.Models;

namespace ImageCleaner.Services;

public class DeleteConditionEvaluator
{
	public DeleteEvaluationResult Evaluate(MonitorFolderProfile profile, IList<CleanupTarget> items, DiskInfo diskInfo)
	{
		DeleteConditions conditions = profile.DeleteConditions;
		DateTime now = DateTime.UtcNow;
		double thresholdSeconds = Math.Max(1.0, conditions.StorageTimeSeconds);
		TimeSpan threshold = TimeSpan.FromSeconds(thresholdSeconds);
		List<CleanupTarget> expired = (from f in items
			where now - f.CreationTimeUtc > threshold
			orderby f.CreationTimeUtc, f.Depth descending
			select f).ToList();
		bool ageMet = expired.Count > 0;
		bool countMet = !conditions.ImageCountEnabled || items.Count > conditions.ImageCountThreshold;
		bool diskMet = !conditions.DiskSpaceEnabled || diskInfo.FreeSpaceGb < conditions.DiskSpaceThresholdGb;
		return new DeleteEvaluationResult
		{
			AgeConditionMet = ageMet,
			ImageCountConditionMet = countMet,
			DiskSpaceConditionMet = diskMet,
			AllowDelete = (ageMet && countMet && diskMet),
			ExpiredFiles = expired
		};
	}

	public bool ShouldContinueDeleting(MonitorFolderProfile profile, IList<CleanupTarget> remainingItems, DiskInfo diskInfo)
	{
		return Evaluate(profile, remainingItems, diskInfo).AllowDelete;
	}
}
