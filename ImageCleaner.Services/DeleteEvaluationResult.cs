using System.Collections.Generic;

namespace ImageCleaner.Services;

public class DeleteEvaluationResult
{
	public bool AllowDelete { get; set; }

	public bool AgeConditionMet { get; set; }

	public bool ImageCountConditionMet { get; set; }

	public bool DiskSpaceConditionMet { get; set; }

	public List<CleanupTarget> ExpiredFiles { get; set; } = new List<CleanupTarget>();
}
