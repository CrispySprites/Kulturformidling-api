namespace Kulturformidling_api.Data.Model
{
    public class RoleRequest
    {
        public int Id { get; set; }
        public virtual Role Role { get; set; }
        public virtual User User { get; set; }
        public DateTime? DateAccepted { get; set; }
    }
}
