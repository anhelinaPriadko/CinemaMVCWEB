using System.Text.Json.Serialization;

namespace CinemaInfrastructure.ViewModel
{
    public class TokenResponseDTO
    {
        public string? Token { get; set; }

        public string Status { get; set; } = "Ok";
    }
}