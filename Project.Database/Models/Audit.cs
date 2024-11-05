using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Database.Models
{
    public partial class Audit
    {
        public int Id { get; set; }

        public string? Operation { get; set; }

        public string? TableName { get; set; }

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        public int? RecordId { get; set; }

        public DateTime? ChangeDate { get; set; }

        public int? ChangedById { get; set; }

        public virtual ICollection<AuditEntry> AuditEntries { get; set; } = new List<AuditEntry>();
    }
}
