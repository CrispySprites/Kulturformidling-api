namespace Kulturformidling_api.Data.Model
{
    public class Art
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public virtual Type Type { get; set; }
        public string Description { get; set; }
        public virtual User Artist { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string Author { get; set; }
    }
}
