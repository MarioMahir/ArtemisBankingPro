using ArtemisBankingPro.Core.Application.Interfaces.Repositories;
using ArtemisBankingPro.Core.Application.Mappings;
using AutoMapper;
using MockQueryable;
using Moq;

namespace ArtemisBankingPro.Tests.Unit.Services;

/// <summary>
/// Utilidades comunes para simular los repositorios genéricos y el UnitOfWork.
/// Query() se simula con MockQueryable para soportar los operadores async de EF
/// (FirstOrDefaultAsync, CountAsync, AnyAsync, SumAsync, ToListAsync) en memoria.
/// </summary>
public static class MockHelpers
{
    /// <summary>Repositorio simulado cuyo Query() devuelve la lista dada (con proveedor async).</summary>
    public static Mock<IGenericRepository<T>> Repo<T>(params T[] items) where T : class =>
        Repo(items.ToList());

    public static Mock<IGenericRepository<T>> Repo<T>(List<T> items) where T : class
    {
        var mock = new Mock<IGenericRepository<T>>();
        mock.Setup(r => r.Query()).Returns(items.BuildMock());
        mock.Setup(r => r.AddAsync(It.IsAny<T>()))
            .Callback<T>(items.Add)
            .Returns(Task.CompletedTask);
        mock.Setup(r => r.Delete(It.IsAny<T>())).Callback<T>(e => items.Remove(e));
        return mock;
    }

    /// <summary>Mapper REAL con el perfil Entidad→DTO (no un mock): valida los mapeos de producción.</summary>
    public static IMapper Mapper() =>
        new MapperConfiguration(cfg => cfg.AddProfile<EntityMappingProfile>()).CreateMapper();

    /// <summary>UnitOfWork simulado: ExecuteInTransactionAsync ejecuta la operación directamente.</summary>
    public static Mock<IUnitOfWork> UnitOfWork()
    {
        var mock = new Mock<IUnitOfWork>();
        mock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        mock.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((operation, _) => operation());
        return mock;
    }
}
