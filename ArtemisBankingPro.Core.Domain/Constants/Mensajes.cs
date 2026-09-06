namespace ArtemisBankingPro.Core.Domain.Constants;

/// <summary>
/// Textos EXACTOS exigidos por el documento funcional (ver docs/mensajes.md).
/// No modificar la redacción: la evaluación compara literalmente.
/// </summary>
public static class Mensajes
{
    // Login y acceso
    public const string CredencialesInvalidas = "Los datos de acceso son inválidos.";
    public const string CuentaInactiva = "Su cuenta se encuentra inactiva. Debe activar su cuenta mediante el enlace enviado a su correo electrónico registrado para poder acceder al sistema.";
    public const string SinPermisosWeb = "Este usuario no tiene permisos para acceder a la aplicación web.";
    public const string NoAutenticado = "No tiene permiso para acceder a esta sección.";
    public const string AccesoDenegado = "No posee permisos para acceder a esta sección.";

    // Activación de cuenta
    public const string CuentaActivada = "Su cuenta ha sido activada correctamente. Ya puede iniciar sesión.";
    public const string EnlaceActivacionInvalido = "El enlace de activación no es válido.";
    public const string EnlaceActivacionUsado = "Este enlace de activación ya fue utilizado.";

    // Restablecimiento de contraseña
    public const string UsuarioNoExiste = "No existe un usuario registrado con este nombre de usuario.";
    public const string UsuarioSinCorreo = "Este usuario no tiene un correo electrónico registrado. No es posible enviar la solicitud de restablecimiento.";
    public const string ResetEnviado = "Se ha enviado un enlace de restablecimiento de contraseña al correo electrónico registrado.";
    public const string EnlaceResetInvalido = "El enlace de restablecimiento no es válido.";
    public const string EnlaceResetExpirado = "El enlace de restablecimiento ha expirado. Solicite un nuevo restablecimiento de contraseña.";
    public const string EnlaceResetUsado = "Este enlace de restablecimiento ya fue utilizado.";
    public const string ContrasenasNoCoinciden = "La contraseña y la confirmación de contraseña deben coincidir.";
    public const string ContrasenaRestablecida = "Su contraseña ha sido restablecida correctamente. Ya puede iniciar sesión.";

    // Usuarios
    public const string CedulaDuplicada = "Ya existe un usuario registrado con esta cédula.";
    public const string CorreoDuplicado = "Ya existe un usuario registrado con este correo electrónico.";
    public const string UsuarioDuplicado = "Ya existe un usuario registrado con este nombre de usuario.";
    public const string MontoInicialNegativo = "El monto inicial no puede ser negativo.";
    public const string CorreoActivacionFallido = "No fue posible enviar el correo de activación. Intente nuevamente más tarde.";
    public const string NoEditarPropiaCuenta = "No puede editar su propia cuenta desde este módulo.";
    public const string NoModificarPropioEstado = "No puede modificar el estado de su propia cuenta.";
    public const string ConfirmarInactivarUsuario = "¿Está seguro que desea inactivar este usuario?";
    public const string ConfirmarActivarUsuario = "¿Está seguro que desea activar este usuario?";

    // Préstamos
    public const string ClienteNoExistePorCedula = "No existe un cliente registrado con esta cédula.";
    public const string ClienteSinPrestamos = "Este cliente no tiene préstamos registrados.";
    public const string DebeSeleccionarCliente = "Debe seleccionar un cliente para continuar.";
    public const string ClienteConPrestamoActivo = "Este cliente ya tiene un préstamo activo asignado.";
    public const string PlazoInvalido = "El plazo seleccionado no es válido.";
    public const string MontoPrestamoInvalido = "El monto a prestar debe ser mayor que cero.";
    public const string TasaNegativa = "La tasa de interés anual no puede ser negativa.";
    public const string ClienteAltoRiesgoActual = "Este cliente se considera de alto riesgo, ya que su deuda actual supera el promedio del sistema.";
    public const string ClienteAltoRiesgoProyectado = "Asignar este préstamo convertirá al cliente en un cliente de alto riesgo, ya que su deuda superará el umbral promedio del sistema.";
    public const string ClienteSinCuentaPrincipal = "El cliente no tiene una cuenta de ahorro principal activa para recibir el desembolso del préstamo.";
    public const string PrestamoCreadoCorreoFallido = "El préstamo fue creado correctamente, pero no fue posible enviar el correo de notificación.";
    public const string PrestamoNoExiste = "El préstamo seleccionado no existe.";
    public const string SoloTasaPrestamosActivos = "Solo se puede modificar la tasa de interés de préstamos activos.";
    public const string SinCuotasFuturas = "No existen cuotas futuras pendientes para recalcular.";

    // Tarjetas de crédito
    /// <summary>Plantilla dictada por el spec; {0} = últimos 4 dígitos.</summary>
    public const string ConfirmarCancelarTarjeta = "¿Está seguro que desea cancelar la tarjeta {0}?";
    public const string SoloTarjetasClientesActivos = "Solo se puede asignar tarjetas de crédito a clientes activos.";
    public const string LimiteInvalido = "El límite de crédito debe ser mayor que cero.";
    public const string TarjetaNoExiste = "La tarjeta seleccionada no existe.";
    public const string TarjetaCanceladaNoModificable = "No se puede modificar una tarjeta cancelada.";
    public const string LimiteTarjetaInvalido = "El límite de la tarjeta debe ser mayor que cero.";
    public const string LimiteMenorQueDeuda = "El límite de la tarjeta no puede ser inferior al monto adeudado actualmente.";
    public const string TarjetaConDeudaNoCancelable = "Para cancelar esta tarjeta, el cliente debe saldar la totalidad de la deuda pendiente.";

    // Cuentas de ahorro
    /// <summary>Plantilla dictada por el spec; {0} = número de cuenta (9 dígitos).</summary>
    public const string ConfirmarCancelarCuenta = "¿Está seguro que desea cancelar la cuenta {0}?";
    public const string SoloCuentasClientesActivos = "Solo se puede asignar cuentas de ahorro a clientes activos.";
    public const string RequierePrincipalActiva = "El cliente debe tener una cuenta de ahorro principal activa antes de asignarle una cuenta secundaria.";
    public const string BalanceInicialNegativo = "El balance inicial no puede ser negativo.";
    public const string PrincipalNoCancelable = "Las cuentas principales no pueden ser canceladas.";
    public const string CuentaNoExiste = "La cuenta seleccionada no existe.";
    public const string CuentaYaCancelada = "La cuenta seleccionada ya se encuentra cancelada.";
    public const string SinPrincipalParaFondos = "No es posible cancelar la cuenta porque el cliente no tiene una cuenta principal activa para recibir los fondos.";

    // Cliente
    public const string SinProductos = "No posee productos financieros activos.";
    public const string CuentaInvalida = "El número de cuenta ingresado no corresponde a una cuenta válida.";
    public const string BeneficiarioCancelado = "No puede agregar una cuenta cancelada como beneficiario.";
    public const string BeneficiarioPropio = "No puede agregar una cuenta propia como beneficiario. Utilice la opción Transferencia para mover fondos entre sus cuentas.";
    public const string BeneficiarioDuplicado = "Esta cuenta ya se encuentra registrada como beneficiario.";
    public const string BeneficiarioAgregado = "Beneficiario agregado correctamente.";
    public const string BeneficiarioEliminado = "Beneficiario eliminado correctamente.";
    public const string ConfirmarEliminarBeneficiario = "¿Está seguro que desea eliminar este beneficiario?";
    public const string FondosInsuficientes = "El monto ingresado excede el saldo disponible de la cuenta seleccionada.";
    public const string DestinoIgualOrigen = "La cuenta destino no puede ser la misma cuenta de origen.";
    public const string ConfirmarTransaccion = "¿Está seguro de que desea realizar esta transacción?";
    public const string TarjetaSinDeuda = "La tarjeta seleccionada no tiene deuda pendiente.";
    public const string PrestamoSinCuotasPendientes = "El préstamo seleccionado no tiene cuotas pendientes de pago.";
    public const string SinBeneficiarios = "No tiene beneficiarios registrados.";
    public const string BeneficiarioNoDisponible = "La cuenta del beneficiario no se encuentra disponible.";
    public const string SinFondosParaTransaccion = "No dispone de fondos suficientes para realizar esta transacción.";
    public const string TarjetaNoActiva = "La tarjeta seleccionada no se encuentra activa.";
    public const string TarjetaVencida = "La tarjeta seleccionada se encuentra vencida.";
    public const string CuentaAhorroNoActiva = "La cuenta de ahorro seleccionada no se encuentra activa.";
    public const string MontoAvanceInvalido = "El monto del avance debe ser mayor que cero.";
    public const string AvanceExcedeDisponible = "El avance solicitado excede el crédito disponible de la tarjeta seleccionada.";
    public const string RequiereDosCuentas = "Debe tener al menos dos cuentas de ahorro activas para realizar una transferencia entre cuentas.";
    public const string OrigenIgualDestino = "La cuenta de origen y la cuenta de destino no pueden ser la misma.";
    public const string MontoTransferenciaInvalido = "El monto a transferir debe ser mayor que cero.";
    public const string SinMontoRequerido = "No dispone del monto requerido en la cuenta seleccionada.";
    public const string ConfirmarTransferencia = "¿Está seguro que desea realizar esta transferencia?";

    // Cajero
    public const string MontoDepositoInvalido = "El monto a depositar debe ser mayor que cero.";
    public const string ConfirmarDeposito = "¿Está seguro que desea realizar este depósito?";
    public const string DepositoCorreoFallido = "El depósito fue realizado correctamente, pero no fue posible enviar el correo de notificación.";
    public const string RetiroExcedeSaldo = "El monto ingresado excede el saldo disponible de la cuenta.";
    public const string ConfirmarRetiro = "¿Está seguro que desea realizar este retiro?";
    public const string TarjetaInvalidaCajero = "El número de tarjeta ingresado no corresponde a una tarjeta válida.";
    public const string ConfirmarPago = "¿Está seguro que desea realizar este pago?";
    public const string PrestamoInvalidoCajero = "El número de préstamo ingresado no corresponde a un préstamo válido.";
    public const string CuentaOrigenInvalida = "El número de cuenta origen ingresado no corresponde a una cuenta válida.";
    public const string CuentaDestinoInvalida = "El número de cuenta destino ingresado no corresponde a una cuenta válida.";
    public const string OrigenIgualDestinoCajero = "La cuenta origen y la cuenta destino no pueden ser la misma.";
    public const string TransaccionCorreoFallido = "La transacción fue realizada correctamente, pero no fue posible enviar una o más notificaciones por correo.";

    // Comercios (el documento funcional exige RNC y correo únicos con "mensajes claros";
    // estos textos no están dictados literalmente por el spec)
    public const string ComercioNoExiste = "El comercio seleccionado no existe.";
    public const string ComercioRncDuplicado = "Ya existe un comercio registrado con este RNC.";
    public const string ComercioCorreoDuplicado = "Ya existe un comercio registrado con este correo electrónico.";
    public const string ComercioInactivo = "El comercio se encuentra inactivo y no puede procesar pagos.";
    public const string ComercioSinUsuario = "El comercio no tiene un usuario asociado para procesar pagos.";
    public const string ComercioSinCuentaPrincipal = "El comercio no tiene una cuenta de ahorro principal activa para recibir pagos.";

    // Mensajes de apoyo (no dictados literalmente por el spec, centralizados por convención del repo)
    public const string UsuarioSeleccionadoNoExiste = "El usuario seleccionado no existe.";
    public const string MontoRetiroInvalido = "El monto a retirar debe ser mayor que cero.";
    public const string MontoPagoInvalido = "El monto a pagar debe ser mayor que cero.";
    public const string MontoAdicionalNegativo = "El monto adicional no puede ser negativo.";

    // API
    public const string ApiNoAutorizado = "No tiene autorización para acceder a este recurso.";
    public const string ApiAccesoDenegado = "Acceso denegado. No tiene permisos para utilizar este recurso.";
    public const string ApiCuentaInactiva = "Su cuenta se encuentra inactiva. Debe activar su cuenta antes de iniciar sesión.";
    public const string ApiExcedeCreditoDisponible = "El monto de la transacción excede el crédito disponible de la tarjeta.";
}
