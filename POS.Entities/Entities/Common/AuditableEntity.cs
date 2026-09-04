namespace POS.Entities.Common
{
    /// <summary>
    /// Standard audit + soft-delete fields required on every table per the DB design doc.
    /// </summary>
    public abstract class AuditableEntity
    {
        public int Id { get; set; }

        public int? AddedBy { get; set; }
        public DateTime AddedOn { get; set; } = DateTime.Now;

        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }

        public int? DeletedBy { get; set; }
        public DateTime? DeletedOn { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
