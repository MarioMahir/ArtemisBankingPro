using ArtemisBankingPro.Core.Application.Helpers;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;

namespace ArtemisBankingPro.Tests.Unit.Helpers;

public class PaymentDistributorTests
{
    private static LoanInstallment Cuota(
        int numero, decimal pendiente, InstallmentStatus estado = InstallmentStatus.Pendiente,
        bool atrasada = false, DateTime? vencimiento = null) => new()
    {
        Number = numero,
        DueDate = vencimiento ?? new DateTime(2026, 1, 1).AddMonths(numero - 1),
        Value = pendiente,
        PendingAmount = pendiente,
        Status = estado,
        IsOverdue = atrasada
    };

    // ---------------- EffectiveAmount (anti-sobrepago) ----------------

    [Fact]
    public void EffectiveAmount_Deuda500Pago1000_Devuelve500()
    {
        // Ejemplo EXACTO del spec.
        Assert.Equal(500m, PaymentDistributor.EffectiveAmount(1_000m, 500m));
    }

    [Fact]
    public void EffectiveAmount_Pendiente2000Pago3000_Devuelve2000()
    {
        // Ejemplo EXACTO del spec.
        Assert.Equal(2_000m, PaymentDistributor.EffectiveAmount(3_000m, 2_000m));
    }

    [Fact]
    public void EffectiveAmount_PagoMenorQueLaDeuda_DevuelveElPago()
    {
        Assert.Equal(300m, PaymentDistributor.EffectiveAmount(300m, 500m));
    }

    [Fact]
    public void EffectiveAmount_PagoIgualALaDeuda_DevuelveElMonto()
    {
        Assert.Equal(500m, PaymentDistributor.EffectiveAmount(500m, 500m));
    }

    // ---------------- ApplyPayment (cascada de cuotas) ----------------

    [Fact]
    public void ApplyPayment_PagoParcial_SeAplicaALaCuotaMasAntiguaYQuedaParcialmentePagada()
    {
        // Arrange
        var cuotas = new List<LoanInstallment> { Cuota(1, 1_000m), Cuota(2, 1_000m) };

        // Act
        var todasPagadas = PaymentDistributor.ApplyPayment(cuotas, 400m);

        // Assert
        Assert.False(todasPagadas);
        Assert.Equal(600m, cuotas[0].PendingAmount);
        Assert.Equal(InstallmentStatus.ParcialmentePagada, cuotas[0].Status);
        Assert.Equal(1_000m, cuotas[1].PendingAmount);
        Assert.Equal(InstallmentStatus.Pendiente, cuotas[1].Status);
    }

    [Fact]
    public void ApplyPayment_ElSobranteCaeEnCascadaALaSiguienteCuota()
    {
        // Arrange
        var cuotas = new List<LoanInstallment> { Cuota(1, 1_000m), Cuota(2, 1_000m), Cuota(3, 1_000m) };

        // Act: 1,500 → salda la 1ª y deja la 2ª a la mitad.
        var todasPagadas = PaymentDistributor.ApplyPayment(cuotas, 1_500m);

        // Assert
        Assert.False(todasPagadas);
        Assert.Equal(InstallmentStatus.Pagada, cuotas[0].Status);
        Assert.Equal(0m, cuotas[0].PendingAmount);
        Assert.Equal(InstallmentStatus.ParcialmentePagada, cuotas[1].Status);
        Assert.Equal(500m, cuotas[1].PendingAmount);
        Assert.Equal(InstallmentStatus.Pendiente, cuotas[2].Status);
    }

    [Fact]
    public void ApplyPayment_CuotaAtrasadaPagadaCompleta_PierdeElIndicadorDeAtraso()
    {
        // Arrange
        var cuotas = new List<LoanInstallment> { Cuota(1, 1_000m, atrasada: true) };

        // Act
        PaymentDistributor.ApplyPayment(cuotas, 1_000m);

        // Assert
        Assert.Equal(InstallmentStatus.Pagada, cuotas[0].Status);
        Assert.False(cuotas[0].IsOverdue);
    }

    [Fact]
    public void ApplyPayment_CuotaAtrasadaPagadaParcialmente_SigueAtrasada()
    {
        // Arrange
        var cuotas = new List<LoanInstallment> { Cuota(1, 1_000m, atrasada: true) };

        // Act
        PaymentDistributor.ApplyPayment(cuotas, 200m);

        // Assert: solo se quita el atraso al SALDAR la cuota.
        Assert.Equal(InstallmentStatus.ParcialmentePagada, cuotas[0].Status);
        Assert.True(cuotas[0].IsOverdue);
    }

    [Fact]
    public void ApplyPayment_TodasLasCuotasSaldadas_DevuelveTrue()
    {
        // Arrange
        var cuotas = new List<LoanInstallment> { Cuota(1, 500m), Cuota(2, 500m) };

        // Act
        var todasPagadas = PaymentDistributor.ApplyPayment(cuotas, 1_000m);

        // Assert: el préstamo debe pasar a Completado.
        Assert.True(todasPagadas);
        Assert.All(cuotas, c => Assert.Equal(InstallmentStatus.Pagada, c.Status));
        Assert.All(cuotas, c => Assert.Equal(0m, c.PendingAmount));
    }

    [Fact]
    public void ApplyPayment_QuedaAlgunaCuotaPendiente_DevuelveFalse()
    {
        // Arrange
        var cuotas = new List<LoanInstallment> { Cuota(1, 500m), Cuota(2, 500m) };

        // Act
        var todasPagadas = PaymentDistributor.ApplyPayment(cuotas, 500m);

        // Assert
        Assert.False(todasPagadas);
    }

    [Fact]
    public void ApplyPayment_IgnoraCuotasYaPagadasYAplicaALaSiguientePendiente()
    {
        // Arrange
        var pagada = Cuota(1, 0m, InstallmentStatus.Pagada);
        var pendiente = Cuota(2, 800m);
        var cuotas = new List<LoanInstallment> { pendiente, pagada }; // desordenadas a propósito

        // Act
        var todasPagadas = PaymentDistributor.ApplyPayment(cuotas, 800m);

        // Assert
        Assert.True(todasPagadas);
        Assert.Equal(InstallmentStatus.Pagada, pendiente.Status);
    }

    [Fact]
    public void ApplyPayment_OrdenaPorVencimiento_AplicaPrimeroALaMasAntigua()
    {
        // Arrange: la lista llega desordenada; debe pagarse primero la de vencimiento más antiguo.
        var reciente = Cuota(2, 1_000m, vencimiento: new DateTime(2026, 5, 1));
        var antigua = Cuota(1, 1_000m, vencimiento: new DateTime(2026, 1, 1));
        var cuotas = new List<LoanInstallment> { reciente, antigua };

        // Act
        PaymentDistributor.ApplyPayment(cuotas, 1_000m);

        // Assert
        Assert.Equal(InstallmentStatus.Pagada, antigua.Status);
        Assert.Equal(InstallmentStatus.Pendiente, reciente.Status);
    }

    [Fact]
    public void ApplyPayment_ContinuaSobreUnaParcialmentePagada_AntesDeLasPendientes()
    {
        // Arrange
        var parcial = Cuota(1, 300m, InstallmentStatus.ParcialmentePagada);
        var pendiente = Cuota(2, 1_000m);
        var cuotas = new List<LoanInstallment> { parcial, pendiente };

        // Act: 500 → salda los 300 de la parcial y abona 200 a la siguiente.
        PaymentDistributor.ApplyPayment(cuotas, 500m);

        // Assert
        Assert.Equal(InstallmentStatus.Pagada, parcial.Status);
        Assert.Equal(800m, pendiente.PendingAmount);
        Assert.Equal(InstallmentStatus.ParcialmentePagada, pendiente.Status);
    }
}
