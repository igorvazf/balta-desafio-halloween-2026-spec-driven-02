using LinkShortener.Api.Application;
using LinkShortener.Api.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LinkShortener.Api.Data;

public sealed class LinkRepository(AppDbContext context) : ILinkRepository
{
    private const int SqliteConstraintUnique = 2067;

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken) =>
        context.Links.AnyAsync(l => l.Code == code, cancellationToken);

    public async Task<bool> TryAddAsync(Link link, CancellationToken cancellationToken)
    {
        context.Links.Add(link);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteConstraintUnique })
        {
            context.Entry(link).State = EntityState.Detached;
            return false;
        }
    }

    public Task<Link?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        context.Links.AsNoTracking().SingleOrDefaultAsync(l => l.Code == code, cancellationToken);

    public Task IncrementAccessCountAsync(int linkId, CancellationToken cancellationToken) =>
        context.Links
            .Where(l => l.Id == linkId)
            .ExecuteUpdateAsync(set => set.SetProperty(l => l.AccessCount, l => l.AccessCount + 1), cancellationToken);
}
