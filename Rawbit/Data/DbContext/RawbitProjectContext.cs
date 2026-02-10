using System.IO;
using Microsoft.EntityFrameworkCore;
using Rawbit.Models;

namespace Rawbit.Data.DbContext;

public interface IProjectDbPathProvider
{
    string? DbPath { get; set; }
}

public sealed class ProjectDbPathProvider : IProjectDbPathProvider
{
    public string? DbPath { get; set; }
}

public class RawbitProjectContext : Microsoft.EntityFrameworkCore.DbContext
{
    private readonly IProjectDbPathProvider _pathProvider;
    public DbSet<Image> Images { get; set; }
    public DbSet<Adjustments> Adjustments { get; set; }
    
    public RawbitProjectContext(
        DbContextOptions<RawbitProjectContext> options,
        IProjectDbPathProvider pathProvider)
        : base(options)
    {
        _pathProvider = pathProvider;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured && !string.IsNullOrWhiteSpace(_pathProvider.DbPath))
        {
            options.UseSqlite($"Data Source={_pathProvider.DbPath}");
        }
    }
}
