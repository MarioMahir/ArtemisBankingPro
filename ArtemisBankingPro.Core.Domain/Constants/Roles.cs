namespace ArtemisBankingPro.Core.Domain.Constants;

public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Cajero = "Cajero";
    public const string Cliente = "Cliente";
    public const string Comercio = "Comercio";

    public static readonly string[] All = [Administrador, Cajero, Cliente, Comercio];

    public static readonly string[] WebAppRoles = [Administrador, Cajero, Cliente];

    public static readonly string[] ApiRoles = [Administrador, Comercio];
}
