using Dfc.CourseDirectory.Models.Interfaces.Providers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dfc.CourseDirectory.Models.Models.Providers
{
    public class BulkUploadStatus : IBulkUploadStatus
    {
        public bool InProgress { get; set; }
        public DateTime? StartedTimestamp { get; set; }
        public int? TotalRowCount { get; set; }
        public bool PublishInProgress { get; set; }
    }
}
