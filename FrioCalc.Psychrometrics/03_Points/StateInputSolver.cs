using System;
using System.Collections.Generic;
using System.Linq;
using FrioCalc.Psychrometrics.Core;

namespace FrioCalc.Psychrometrics.Points;

// [3.4] Solucionador inverso: presión conocida y dos propiedades independientes.
public enum StateProperty
{
    DryBulb, WetBulb, DewPoint, RelativeHumidity, HumidityRatio, Enthalpy, SpecificVolume
}

public enum StateSolveMethod { Automatic, Direct, Iterative }

public static class StateInputSolver
{
    public static bool TrySolve(IReadOnlyDictionary<StateProperty, double> conditions,
        double pressureKPa, int id, string colorHex, out ChartPoint? point, out string diagnostic)
        => TrySolve(conditions,pressureKPa,id,colorHex,StateSolveMethod.Automatic,out point,out diagnostic);

    public static bool TrySolve(
        IReadOnlyDictionary<StateProperty, double> conditions,
        double pressureKPa, int id, string colorHex, StateSolveMethod method,
        out ChartPoint? point, out string diagnostic)
    {
        point = null;
        diagnostic = "";
        if (!double.IsFinite(pressureKPa) || pressureKPa <= 0)
        {
            diagnostic = "La presión absoluta debe ser positiva."; return false;
        }
        if (conditions.Count < 2)
        {
            diagnostic = "Indique al menos dos propiedades independientes del aire húmedo."; return false;
        }
        if (conditions.Any(x => !double.IsFinite(x.Value)))
        {
            diagnostic = "Una propiedad no es numérica o no es finita."; return false;
        }
        if (conditions.TryGetValue(StateProperty.RelativeHumidity, out double rh)
            && (rh < 0 || rh > 100))
        {
            diagnostic = "La humedad relativa debe estar entre 0 y 100 %."; return false;
        }
        if (conditions.TryGetValue(StateProperty.HumidityRatio, out double w)
            && w < 0)
        {
            diagnostic = "La humedad absoluta no puede ser negativa."; return false;
        }
        if (conditions.TryGetValue(StateProperty.SpecificVolume, out double v)
            && v <= 0)
        {
            diagnostic = "El volumen específico debe ser positivo."; return false;
        }

        var pairs = conditions.ToArray();
        var candidates = new List<ChartPoint>();
        for (int i = 0; i < pairs.Length && candidates.Count == 0; i++)
        for (int j = i + 1; j < pairs.Length && candidates.Count == 0; j++)
        {
            FindForPair(pairs[i], pairs[j], pressureKPa, id, colorHex, method, candidates);
        }
        if (candidates.Count == 0)
        {
            diagnostic = "No existe un estado físico único para esas dos propiedades. Revise independencia, presión y rangos.";
            return false;
        }
        if (candidates.Count > 1)
        {
            diagnostic = "Los datos admiten varios estados posibles. Introduzca otra propiedad independiente.";
            return false;
        }

        ChartPoint solution = candidates[0];
        foreach (var condition in conditions)
        {
            double computed = ValueOf(condition.Key, solution);
            double tolerance = Tolerance(condition.Key, condition.Value);
            if (!double.IsFinite(computed) || Math.Abs(computed - condition.Value) > tolerance)
            {
                diagnostic = $"Condición incompatible: {condition.Key}. Dato {condition.Value:G6}, cálculo {computed:G6}; tolerancia {tolerance:G3}.";
                return false;
            }
        }
        point = solution with { Method = method.ToString(), Origin = "Entrada numérica",
            InputConditions = new Dictionary<StateProperty,double>(conditions) };
        diagnostic = "Estado calculado y comprobado con todas las propiedades introducidas.";
        return true;
    }

    private static void FindForPair(
        KeyValuePair<StateProperty, double> a,
        KeyValuePair<StateProperty, double> b,
        double pressure, int id, string color, StateSolveMethod method, List<ChartPoint> results)
    {
        if (a.Key == b.Key) return;
        bool hasDry = a.Key == StateProperty.DryBulb || b.Key == StateProperty.DryBulb;
        if (method == StateSolveMethod.Direct && !hasDry) return;
        if (hasDry)
        {
            var dry = a.Key == StateProperty.DryBulb ? a : b;
            var other = a.Key == StateProperty.DryBulb ? b : a;
            if (method == StateSolveMethod.Iterative)
            {
                SolveOrientation(other,dry,pressure,id,color,results);
            }
            else if (TryHumidityRatioAtTemperature(other.Key, other.Value, dry.Value, pressure, out double humidity)
                && PointPropertyCalculator.TryCreatePoint(id, $"P{id}", color, pressure, dry.Value, humidity, out ChartPoint? p)
                && p != null)
            {
                results.Add(p);
            }
            return;
        }

        // Invert one constraint as W(T) and solve the other by bracketing.
        // Search both orientations; physically identical roots are deduplicated.
        SolveOrientation(a, b, pressure, id, color, results);
        if (results.Count == 0) SolveOrientation(b, a, pressure, id, color, results);
    }

    private static void SolveOrientation(
        KeyValuePair<StateProperty, double> anchor,
        KeyValuePair<StateProperty, double> target,
        double pressure, int id, string color, List<ChartPoint> results)
    {
        const double minT = -50.0, maxT = 110.0;
        double previousT = minT, previousError = double.NaN;
        for (double temperature = minT; temperature <= maxT + .00001; temperature += 1.0)
        {
            double error = ErrorAt(temperature, anchor, target, pressure, id, color);
            if (double.IsFinite(error))
            {
                if (Math.Abs(error) <= Tolerance(target.Key, target.Value) * .001)
                    AddCandidate(temperature, anchor, pressure, id, color, results);
                if (double.IsFinite(previousError) && Math.Sign(previousError) != Math.Sign(error))
                {
                    double lo = previousT, hi = temperature;
                    double loErr = previousError;
                    for (int k = 0; k < 65; k++)
                    {
                        double mid = (lo + hi) / 2.0;
                        double midErr = ErrorAt(mid, anchor, target, pressure, id, color);
                        if (!double.IsFinite(midErr)) break;
                        if (Math.Sign(midErr) == Math.Sign(loErr))
                        { lo = mid; loErr = midErr; }
                        else hi = mid;
                    }
                    AddCandidate((lo + hi) / 2.0, anchor, pressure, id, color, results);
                }
                previousError = error;
            }
            else previousError = double.NaN;
            previousT = temperature;
        }
    }

    private static double ErrorAt(double t, KeyValuePair<StateProperty, double> anchor,
        KeyValuePair<StateProperty, double> target, double pressure, int id, string color)
    {
        if (!TryHumidityRatioAtTemperature(anchor.Key, anchor.Value, t, pressure, out double w)
            || !PointPropertyCalculator.TryCreatePoint(id, $"P{id}", color, pressure, t, w, out ChartPoint? point)
            || point == null) return double.NaN;
        return ValueOf(target.Key, point) - target.Value;
    }

    private static void AddCandidate(double temperature, KeyValuePair<StateProperty, double> anchor,
        double pressure, int id, string color, List<ChartPoint> results)
    {
        if (!TryHumidityRatioAtTemperature(anchor.Key, anchor.Value, temperature, pressure, out double w)
            || !PointPropertyCalculator.TryCreatePoint(id, $"P{id}", color, pressure, temperature, w, out ChartPoint? point)
            || point == null) return;
        if (results.Any(p => Math.Abs(p.DryBulbC - temperature) < .02)) return;
        results.Add(point);
    }

    private static bool TryHumidityRatioAtTemperature(
        StateProperty property, double value, double dryBulbC, double pressure, out double humidityRatio)
    {
        humidityRatio = double.NaN;
        if (!double.IsFinite(dryBulbC) || dryBulbC < -50 || dryBulbC > 110) return false;
        try
        {
            humidityRatio = property switch
            {
                StateProperty.HumidityRatio => value, // kg/kg dry air (canonical)
                StateProperty.RelativeHumidity =>
                    RelativeHumidity.HumidityRatioFromDryBulbAndRelativeHumidity(dryBulbC, value / 100.0, pressure),
                StateProperty.WetBulb =>
                    value <= dryBulbC ? WetBulbHumidityRatio.FromDryBulbAndWetBulb(dryBulbC, value, pressure) : double.NaN,
                StateProperty.DewPoint =>
                    value <= dryBulbC ?
                    SaturationHumidityRatio.FromPressuresKPa(pressure, SaturationVaporPressure.GetPressureKPa(value)) : double.NaN,
                StateProperty.Enthalpy =>
                    MoistAirEnthalpy.HumidityRatioFromEnthalpyAndDryBulb(value, dryBulbC),
                StateProperty.SpecificVolume =>
                    MoistAirSpecificVolume.HumidityRatioFromDryBulbAndSpecificVolume(dryBulbC, value, pressure),
                _ => double.NaN
            };
        }
        catch (ArgumentException) { return false; }
        catch (InvalidOperationException) { return false; }
        return double.IsFinite(humidityRatio) && humidityRatio >= 0;
    }

    private static double ValueOf(StateProperty kind, ChartPoint p) => kind switch
    {
        StateProperty.DryBulb => p.DryBulbC,
        StateProperty.WetBulb => p.WetBulbC,
        StateProperty.DewPoint => p.DewPointC,
        StateProperty.RelativeHumidity => p.RelativeHumidityPercent,
        StateProperty.HumidityRatio => p.HumidityRatioKgPerKg,
        StateProperty.Enthalpy => p.EnthalpyKJPerKgDryAir,
        StateProperty.SpecificVolume => p.SpecificVolumeM3PerKgDryAir,
        _ => double.NaN
    };

    private static double Tolerance(StateProperty property, double value) => property switch
    {
        StateProperty.HumidityRatio => 0.00015,
        StateProperty.RelativeHumidity => 0.35,
        StateProperty.Enthalpy => 0.3,
        StateProperty.SpecificVolume => 0.003,
        _ => 0.12
    };
}
