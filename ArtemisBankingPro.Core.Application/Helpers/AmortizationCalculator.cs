namespace ArtemisBankingPro.Core.Application.Helpers;

public record AmortizationEntry(
    int Number,
    DateTime DueDate,
    decimal Value,
    decimal InterestAmount,
    decimal PrincipalAmount);

/// <summary>
/// Cálculo de cuotas por sistema de amortización francés (cuota fija).
/// Verificación del spec: 100,000 a 12 meses al 12% anual → cuota 8,884.88, total 106,618.56.
/// </summary>
public static class AmortizationCalculator
{
    /// <summary>
    /// C = P·r(1+r)^n / ((1+r)^n − 1), con r = (tasaAnual/100)/12.
    /// Caso especial: tasa 0% → C = P/n. Redondeo a 2 decimales.
    /// </summary>
    public static decimal CalculateMonthlyInstallment(decimal principal, decimal annualRatePercent, int termMonths)
    {
        if (annualRatePercent == 0)
            return Math.Round(principal / termMonths, 2, MidpointRounding.AwayFromZero);

        var r = annualRatePercent / 100m / 12m;
        var factor = Pow(1m + r, termMonths);
        var installment = principal * r * factor / (factor - 1m);
        return Math.Round(installment, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Genera la tabla completa. La primera cuota vence el MISMO DÍA del mes siguiente
    /// a la fecha de creación (préstamo del 5-jul → 1ª cuota 5-ago).
    /// La cuota es fija; en la última se ajusta la división interés/capital para que
    /// el capital amortizado sume exactamente el monto prestado.
    /// </summary>
    public static List<AmortizationEntry> BuildSchedule(
        decimal principal, decimal annualRatePercent, int termMonths, DateTime creationDate)
    {
        var installmentValue = CalculateMonthlyInstallment(principal, annualRatePercent, termMonths);
        var r = annualRatePercent / 100m / 12m;
        var schedule = new List<AmortizationEntry>(termMonths);
        var remaining = principal;

        for (var i = 1; i <= termMonths; i++)
        {
            var dueDate = creationDate.Date.AddMonths(i);
            decimal interest, capital, value;

            if (i == termMonths)
            {
                // Última cuota: el capital salda exactamente el balance restante.
                capital = Math.Round(remaining, 2, MidpointRounding.AwayFromZero);
                if (annualRatePercent == 0)
                {
                    interest = 0m;
                    value = capital;
                }
                else
                {
                    interest = installmentValue - capital;
                    value = installmentValue;
                }
            }
            else
            {
                interest = Math.Round(remaining * r, 2, MidpointRounding.AwayFromZero);
                capital = installmentValue - interest;
                value = installmentValue;
            }

            remaining -= capital;
            schedule.Add(new AmortizationEntry(i, dueDate, value, interest, capital));
        }

        return schedule;
    }

    /// <summary>Suma de todas las cuotas: el "total a pagar" usado en la evaluación de riesgo.</summary>
    public static decimal TotalToPay(decimal principal, decimal annualRatePercent, int termMonths) =>
        BuildSchedule(principal, annualRatePercent, termMonths, DateTime.Today).Sum(e => e.Value);

    /// <summary>Potencia decimal exacta con exponente entero (evita el redondeo binario de Math.Pow).</summary>
    private static decimal Pow(decimal baseValue, int exponent)
    {
        var result = 1m;
        for (var i = 0; i < exponent; i++) result *= baseValue;
        return result;
    }
}
