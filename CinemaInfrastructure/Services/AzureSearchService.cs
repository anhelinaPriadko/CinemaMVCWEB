using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using CinemaDomain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CinemaInfrastructure.Services
{
    public class AzureSearchService
    {
        private readonly SearchIndexClient _indexClient;
        private readonly SearchClient _searchClient;
        private readonly CinemaContext _context;
        private readonly string _indexName;

        public AzureSearchService(SearchIndexClient indexClient, SearchClient searchClient, CinemaContext context, IConfiguration config)
        {
            _indexClient = indexClient;
            _searchClient = searchClient;
            _context = context;
            _indexName = config["AzureSearch:IndexName"]!;
        }

        public async Task InitializeAndIndexAsync()
        {
            // 1. Створення індексу (Якщо він не існує, або оновлення)
            var searchIndex = new SearchIndex(_indexName)
            {
                Fields = new FieldBuilder().Build(typeof(FilmSearchDTO))
            };
            await _indexClient.CreateOrUpdateIndexAsync(searchIndex);

            // 2. Завантаження даних
            await IndexAllFilmsAsync();
        }

        public async Task IndexAllFilmsAsync()
        {
            // Використовуйте AsNoTracking для ефективності
            var films = await _context.Films.AsNoTracking().Include(f => f.FilmCategory).ToListAsync();

            var actions = films
            .Select(f => IndexDocumentsAction.Upload<FilmSearchDTO>(new FilmSearchDTO
            {
                Id = f.Id.ToString(),
                Name = f.Name,
                Description = f.Description ?? "",
                FilmCategoryName = f.FilmCategory!.Name
            }))
            .ToList();

            if (actions.Any())
            {
                var batch = IndexDocumentsBatch.Create<FilmSearchDTO>(actions.ToArray());
                await _searchClient.IndexDocumentsAsync(batch);
            }
        }
    }
}