using System;
using System.Collections.Generic;
using System.Text;

namespace Dfc.CourseDirectory.Models.Interfaces.Providers
{
    public interface IBulkUploadStatus
    {
        bool InProgress { get; set; }
        DateTime? StartedTimestamp { get; set; }
        int? TotalRowCount { get; set; }
    }
}
