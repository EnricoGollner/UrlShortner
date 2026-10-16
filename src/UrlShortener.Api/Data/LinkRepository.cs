using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Data;

public class LinkRepository : ILinkRepository
{
    private readonly AppDbContext _context;

    public LinkRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Link?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToLower();
        return await _context.Links
            .FirstOrDefaultAsync(l => l.Code.ToLower() == normalized, cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToLower();
        return await _context.Links
            .AnyAsync(l => l.Code.ToLower() == normalized, cancellationToken);
    }

    public async Task AddAsync(Link link, CancellationToken cancellationToken = default)
    {
        await _context.Links.AddAsync(link, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
