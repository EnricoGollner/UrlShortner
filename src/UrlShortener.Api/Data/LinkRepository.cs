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
        return await _context.Links
            .FirstOrDefaultAsync(l => EF.Functions.Like(l.Code, code), cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Links
            .AnyAsync(l => EF.Functions.Like(l.Code, code), cancellationToken);
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
