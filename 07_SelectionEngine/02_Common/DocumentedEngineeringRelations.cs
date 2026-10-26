using System;
using System.Collections.Generic;

namespace FrioCalc.Psychrometrics.SelectionEngine.Common;

// ------------------------------ Sección 7.6.1 - Relaciones documentadas comunes ------------------------------

public static class DocumentedEngineeringRelations
{
    public const double StandardAtmosphericPressureBar = 1.01325;

    public static double AbsolutePressureFromGaugeBar(
        double gaugePressureBar,
        double atmosphericPressureBar)
    {
        ValidateFinite(
            gaugePressureBar,
            nameof(gaugePressureBar));

        ValidatePositive(
            atmosphericPressureBar,
            nameof(atmosphericPressureBar));

        double absolutePressureBar =
            gaugePressureBar
            + atmosphericPressureBar;

        if (absolutePressureBar <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gaugePressureBar),
                "La presión absoluta resultante debe ser mayor que cero.");
        }

        return absolutePressureBar;
    }

    public static double HotGasDefrostMassFlowKgPerS(
        double refrigerationCapacityKw,
        double enthalpyCKJPerKg,
        double enthalpyDKJPerKg,
        double defrostFactor = 2.0)
    {
        ValidateNonNegative(
            refrigerationCapacityKw,
            nameof(refrigerationCapacityKw));

        ValidateFinite(
            enthalpyCKJPerKg,
            nameof(enthalpyCKJPerKg));

        ValidateFinite(
            enthalpyDKJPerKg,
            nameof(enthalpyDKJPerKg));

        ValidatePositive(
            defrostFactor,
            nameof(defrostFactor));

        double enthalpyDifference =
            enthalpyCKJPerKg
            - enthalpyDKJPerKg;

        ValidatePositive(
            enthalpyDifference,
            "hC-hD");

        return
            defrostFactor
            * refrigerationCapacityKw
            / enthalpyDifference;
    }

    public static IReadOnlyList<double> FlowAfterJunctionChanges(
        double inletFlow,
        IEnumerable<double> changesPercentOfOriginalInlet)
    {
        ValidateNonNegative(
            inletFlow,
            nameof(inletFlow));

        List<double> outputs =
            new();

        double cumulativeFraction =
            0.0;

        foreach (double changePercent
                 in changesPercentOfOriginalInlet)
        {
            ValidateFinite(
                changePercent,
                nameof(changesPercentOfOriginalInlet));

            cumulativeFraction +=
                changePercent / 100.0;

            double remainingFraction =
                1.0
                + cumulativeFraction;

            if (remainingFraction < -1e-12)
            {
                throw new InvalidOperationException(
                    "Las uniones retiran más caudal del disponible.");
            }

            outputs.Add(
                inletFlow
                * Math.Max(
                    0.0,
                    remainingFraction));
        }

        return outputs;
    }

    private static void ValidateFinite(
        double value,
        string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "El valor debe ser finito.");
        }
    }

    private static void ValidatePositive(
        double value,
        string parameterName)
    {
        ValidateFinite(
            value,
            parameterName);

        if (value <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "El valor debe ser mayor que cero.");
        }
    }

    private static void ValidateNonNegative(
        double value,
        string parameterName)
    {
        ValidateFinite(
            value,
            parameterName);

        if (value < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "El valor no puede ser negativo.");
        }
    }
}

// Fuentes documentales:
// - Pressure reference.pdf pp. 2-3: P_abs = P_gauge + P_atm.
// - Dimensioning mass flow in hot gas defrost lines.pdf p. 5:
//   m_dot = factor × Q_frío / (h_C - h_D).
// - Junctions Explained.pdf pp. 2-4:
//   cada variación porcentual se refiere al caudal ORIGINAL de entrada.
//
// Estas relaciones no seleccionan equipos ni sustituyen un motor termodinámico.

// ------------------------------ Fin de sección 7.6.1 ------------------------------
