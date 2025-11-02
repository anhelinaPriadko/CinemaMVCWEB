using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using System.Text.Json.Serialization;

namespace CinemaInfrastructure.Services
{
    public class FilmSearchDTO
    {
        [SimpleField(IsKey = true)]
        public string Id { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true, IsSortable = true, AnalyzerName = LexicalAnalyzerName.Values.StandardLucene)]
        public string Name { get; set; } = string.Empty;

        [SearchableField(AnalyzerName = LexicalAnalyzerName.Values.StandardLucene)]
        public string Description { get; set; } = string.Empty;

        [SearchableField]
        public string FilmCategoryName { get; set; } = string.Empty;
    }
}
