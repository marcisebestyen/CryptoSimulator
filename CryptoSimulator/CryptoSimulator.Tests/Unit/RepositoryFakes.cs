using System.Linq.Expressions;
using CryptoSimulator.Repositories;
using NSubstitute;

namespace CryptoSimulator.Tests.Unit;

internal static class RepositoryFakes
{
    // Makes GetAsync apply the real predicate to an in-memory list.
    public static IRepository<T> WithData<T>(params T[] items) where T : class
    {
        var repo = Substitute.For<IRepository<T>>();
        repo.GetAsync(Arg.Any<Expression<Func<T, bool>>>(), Arg.Any<string[]?>())
            .Returns(call =>
            {
                var predicate = call.Arg<Expression<Func<T, bool>>>().Compile();
                return Task.FromResult<IEnumerable<T>>(items.Where(predicate).ToList());
            });
        return repo;
    }
}
