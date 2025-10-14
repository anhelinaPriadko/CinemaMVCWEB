﻿using CinemaDomain.Model;
namespace CinemaInfrastructure.Services
{
    public interface IExportService<TEntity>
        where TEntity : Entity
    {
        Task WriteToAsync(Stream stream, CancellationToken cancellationToken);
    }
}