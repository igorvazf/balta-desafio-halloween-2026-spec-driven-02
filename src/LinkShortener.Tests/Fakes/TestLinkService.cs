using LinkShortener.Api.Application;
using Microsoft.Extensions.Caching.Memory;

namespace LinkShortener.Tests.Fakes;

public static class TestLinkService
{
    public static LinkService Create(
        ILinkRepository repository,
        TimeProvider clock,
        ICodeGenerator? codeGenerator = null,
        LinkResolutionCache? cache = null) =>
        new(
            repository,
            codeGenerator ?? new RandomCodeGenerator(),
            new TargetUrlValidator(new FakeHostAddressResolver()),
            cache ?? NewCache(),
            clock);

    public static LinkResolutionCache NewCache() => new(new MemoryCache(new MemoryCacheOptions()));
}
