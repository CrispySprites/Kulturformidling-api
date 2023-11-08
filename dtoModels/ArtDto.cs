namespace Kulturformidling_api.dtoModels
{
    public class ArtDto
    {
        public int ArtId { get; set; }
        public string ArtName { get; set; }
        public string ArtistName { get; set; }
        public int ArtistId { get; set; }
        public string Description { get; set; }
        public int TypeId { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string? Author { get; set; }

    }
}
