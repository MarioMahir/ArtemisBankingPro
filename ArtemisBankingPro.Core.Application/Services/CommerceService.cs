using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Commerces;
using ArtemisBankingPro.Core.Application.Interfaces.Repositories;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Core.Domain.Entities;
using AutoMapper;
using ArtemisBankingPro.Core.Application.Extensions;

namespace ArtemisBankingPro.Core.Application.Services;

public class CommerceService(
    IGenericRepository<Commerce> commerceRepository,
    IAccountService accountService,
    IUnitOfWork unitOfWork,
    IMapper mapper) : GenericService<Commerce, CommerceDto>(commerceRepository, mapper), ICommerceService
{
    protected override string NotFoundMessage => Mensajes.ComercioNoExiste;

    public Task<PagedResult<CommerceDto>> GetPagedAsync(int page, bool? isActive)
    {
        // null = todos; el default "activos" lo decide el llamador.
        var query = Repository.Query();
        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        return ToPagedAsync(query, page, c => c.CreatedAt);
    }

    public async Task<ServiceResult<CommerceDto>> CreateAsync(SaveCommerceRequest request)
    {
        if (await Repository.Query().AnyAsync(c => c.Rnc == request.Rnc))
            return ServiceResult<CommerceDto>.Fail(Mensajes.ComercioRncDuplicado);

        if (await Repository.Query().AnyAsync(c => c.Email == request.Email))
            return ServiceResult<CommerceDto>.Fail(Mensajes.ComercioCorreoDuplicado);

        var commerce = new Commerce
        {
            Name = request.Name,
            Description = request.Description,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Rnc = request.Rnc,
            IsActive = true, // nace Activo y SIN usuario asociado
            CreatedAt = DateTime.Now
        };

        await Repository.AddAsync(commerce);
        await unitOfWork.SaveChangesAsync();

        return ServiceResult<CommerceDto>.Ok(Mapper.Map<CommerceDto>(commerce));
    }

    public async Task<ServiceResult> UpdateAsync(int id, SaveCommerceRequest request)
    {
        var commerce = await Repository.Query().FirstOrDefaultAsync(c => c.Id == id);
        if (commerce is null)
            return ServiceResult.Fail(Mensajes.ComercioNoExiste);

        if (await Repository.Query().AnyAsync(c => c.Rnc == request.Rnc && c.Id != id))
            return ServiceResult.Fail(Mensajes.ComercioRncDuplicado);

        if (await Repository.Query().AnyAsync(c => c.Email == request.Email && c.Id != id))
            return ServiceResult.Fail(Mensajes.ComercioCorreoDuplicado);

        commerce.Name = request.Name;
        commerce.Description = request.Description;
        commerce.Email = request.Email;
        commerce.PhoneNumber = request.PhoneNumber;
        commerce.Rnc = request.Rnc;

        Repository.Update(commerce);
        await unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SetStatusAsync(int id, bool isActive)
    {
        var commerce = await Repository.Query().FirstOrDefaultAsync(c => c.Id == id);
        if (commerce is null)
            return ServiceResult.Fail(Mensajes.ComercioNoExiste);

        commerce.IsActive = isActive;
        Repository.Update(commerce);
        await unitOfWork.SaveChangesAsync();

        // Desactivar el comercio inactiva sus usuarios; reactivarlo NO los reactiva.
        if (!isActive)
            await accountService.DeactivateUsersByCommerceAsync(id);

        return ServiceResult.Ok();
    }

}
