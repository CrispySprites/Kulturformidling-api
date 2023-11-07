namespace Kulturformidling_api.dtoModels
{
    public class UserRequestDto
    {
        public int RequestId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
    }
}
