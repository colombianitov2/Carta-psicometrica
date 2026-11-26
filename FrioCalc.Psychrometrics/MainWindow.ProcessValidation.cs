using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FrioCalc.Psychrometrics.Chart;
using FrioCalc.Psychrometrics.Core;
using FrioCalc.Psychrometrics.Points;
using FrioCalc.Psychrometrics.Processes;

namespace FrioCalc.Psychrometrics;

// [5.1C.23] Validación reactiva y sin efectos secundarios del editor.
public partial class MainWindow
{
    private bool _processValidationReady;
    private sealed record EquipmentPrediction(ChartPoint Output,string Note,double? Effectiveness=null,
        double? Bypass=null,double? SteamGrams=null,double? SteamEnthalpy=null,double? Adp=null,
        double? RemovedGrams=null,double? OutletT=null);

    private void InitializeProcessValidation()
    {
        foreach(var box in new[]{DryFlowTextBox,CondensateTemperatureTextBox,EffectivenessTextBox,
            ApparatusDewPointTextBox,BypassFactorTextBox,SteamAddedTextBox,SteamEnthalpyTextBox,
            DesiccantRemovedTextBox,DesiccantTemperatureTextBox,MixFlowsListTextBox,
            MixFlowFirstTextBox,MixFlowSecondTextBox,BranchFractionTextBox})
            box.TextChanged+=(_,_)=>UpdateProcessCompatibility();
        CloseCycleCheckBox.Checked+=(_,_)=>UpdateProcessCompatibility();
        CloseCycleCheckBox.Unchecked+=(_,_)=>UpdateProcessCompatibility();
        _processValidationReady=true;
        UpdateProcessCompatibility();
    }

    private static void ShowAssessment(TextBlock target,ProcessAssessment result)
    {
        target.Text=(result.Status switch {
            ProcessCompatibilityStatus.Compatible=>"Compatible: ",
            ProcessCompatibilityStatus.Incomplete=>"Datos insuficientes: ",
            _=>"Proceso no posible: "})+result.Message;
        target.Foreground=result.Status switch {
            ProcessCompatibilityStatus.Compatible=>Brushes.DarkGreen,
            ProcessCompatibilityStatus.Incomplete=>Brushes.DarkGoldenrod,
            _=>Brushes.Firebrick};
    }

    private void UpdateProcessCompatibility()
    {
        if(!_processValidationReady)return;
        var assessment=AssessProcessEditor(out _);
        ShowAssessment(ProcessCompatibilityText,assessment);
        SaveProcessGroupButton.IsEnabled=assessment.CanCalculate;
    }

    private ProcessAssessment AssessProcessEditor(out EquipmentPrediction? prediction)
    {
        prediction=null;
        string raw=(ProcessTypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString()??"Automatic";
        if(!Enum.TryParse(raw,out ProcessType type))
            return ProcessAssessment.Invalid("Método de proceso no reconocido.");
        var states=_selectedPointIds.Select(id=>PsychrometricChart.ActivePoints.FirstOrDefault(p=>p.Id==id)).ToArray();
        if(states.Any(s=>s is null))return ProcessAssessment.Invalid("Un punto seleccionado ya no existe.");
        var points=states.OfType<ChartPoint>().ToArray();
        var assessment=ProcessCompatibility.AssessSelection(type,points);
        if(!assessment.CanCalculate)return assessment;
        if(!TryReadOptionalFlow(DryFlowTextBox.Text,out double? flow,out string error))
            return ProcessAssessment.Invalid(error);
        double? cond=null;
        if(type is ProcessType.Automatic or ProcessType.CoolingDehumidification)
        {
            if(!string.IsNullOrWhiteSpace(CondensateTemperatureTextBox.Text))
            {
                if(!TryReadFinite(CondensateTemperatureTextBox.Text,out double t)||
                    TemperatureToSI(t)< -50||TemperatureToSI(t)>100)
                    return ProcessAssessment.Invalid($"Temperatura del condensado fuera del intervalo {TemperatureFromSI(-50):0.##} a {TemperatureFromSI(100):0.##} {TempUnit}.");
                cond=TemperatureToSI(t);
            }
        }
        if(type==ProcessType.Branching)
        {
            if(flow is null||string.IsNullOrWhiteSpace(BranchFractionTextBox.Text))
                return ProcessAssessment.Need("Introduzca caudal total positivo y fracción de la primera rama.");
            if(!TryReadUnitInterval(BranchFractionTextBox.Text,out double fraction)||fraction<=0||fraction>=1)
                return ProcessAssessment.Invalid("La fracción de la primera rama debe estar estrictamente entre 0 y 1.");
            return ProcessCalculator.TryBranchBalance(points[0],points[1],points[2],flow.Value,fraction,out _,out error)
                ?ProcessAssessment.Ok(error):ProcessAssessment.Invalid(error);
        }
        if(type==ProcessType.AdiabaticMixing)
        {
            var flowAssessment=ReadMixFlows(points.Length,out var flows);
            if(!flowAssessment.CanCalculate)return flowAssessment;
            var streams=points.Select((state,index)=>(State:state,
                DryAirMassFlowKgS:flows[index]/3600/state.SpecificVolumeM3PerKgDryAir)).ToArray();
            if(!ProcessCalculator.TryMix(streams,0,points[0].ColorHex,out var mixed,out error)||mixed is null)
                return ProcessAssessment.Invalid(error);
            return AssessGeneratedRange(mixed,"Mezcla compatible: los balances determinan una salida físicamente admisible.");
        }
        bool equipment=type is ProcessType.SteamHumidification or ProcessType.EvaporativeHumidification or ProcessType.Bypass
            || (type==ProcessType.Desiccant&&points.Length==1);
        if(equipment)
        {
            var eq=ReadEquipmentPrediction(type,points[0],out prediction);
            if(!eq.CanCalculate||prediction is null)return eq;
            if(points.Length==2)
            {
                if(!ProcessCompatibility.SameState(prediction.Output,points[1]))
                    return ProcessAssessment.Invalid($"{points[0].Name} → {points[1].Name}: los parámetros del equipo no producen el estado final seleccionado.");
                return ProcessAssessment.Ok("El estado final concuerda con los parámetros del equipo.");
            }
            return AssessGeneratedRange(prediction.Output,"La salida calculada es físicamente admisible.");
        }
        if(!TryReadSegmentSettings(type,out var settings,out error))return ProcessAssessment.Invalid(error);
        var ids=_selectedPointIds.ToList();
        var route=points.ToList();
        if(CloseCycleCheckBox.IsChecked==true&&ids[0]!=ids[^1])
        {
            if(type!=ProcessType.Automatic||ids.Distinct().Count()<3)
                return ProcessAssessment.Invalid("Cerrar el ciclo requiere método Automático y tres estados distintos.");
            route.Add(route[0]);ids.Add(ids[0]);
        }
        for(int i=0;i<route.Count-1;i++)
        {
            var segment=ProcessSegmentSetting.Find(settings,ids,i);
            if(!ProcessCalculator.TryCalculate(route[i],route[i+1],segment?.Type??type,
                segment?.DryAirMassFlowKgS??flow,segment?.CondensateTemperatureC??cond,out _,out error))
                return ProcessAssessment.Invalid(error.StartsWith(route[i].Name+" →",StringComparison.Ordinal)
                    ?error:route[i].Name+" → "+route[i+1].Name+": "+error);
        }
        return route.Count>2?ProcessAssessment.Ok($"Los {route.Count-1} tramos son compatibles."):
            type==ProcessType.Automatic&&settings.Count>0&&settings[0].Type!=ProcessType.Automatic
                ?ProcessCompatibility.AssessPair(route[0],route[1],settings[0].Type):assessment;
    }

    private ProcessAssessment AssessGeneratedRange(ChartPoint state,string message)
    {
        // Mismos límites activos que la incorporación de estados en la carta.
        if(!PsychrometricChart.IsWithinActiveChartDomain(state))
            return ProcessAssessment.Invalid("La salida es físicamente válida, pero está fuera del dominio gráfico activo. Revise el encuadre o desactive Carta estándar.");
        return ProcessAssessment.Ok(message);
    }

    private ProcessAssessment ReadMixFlows(int count,out List<double> flows)
    {
        flows=new List<double>();
        if(!string.IsNullOrWhiteSpace(MixFlowsListTextBox.Text))
        {
            foreach(string part in MixFlowsListTextBox.Text.Split(new[]{';','\n'},StringSplitOptions.RemoveEmptyEntries))
            {
                if(!TryReadPositive(part,out double v))return ProcessAssessment.Invalid("Cada caudal de mezcla debe ser positivo.");
                flows.Add(VolumeFlowToSI(v));
            }
        }
        else if(count==2)
        {
            if(string.IsNullOrWhiteSpace(MixFlowFirstTextBox.Text)||string.IsNullOrWhiteSpace(MixFlowSecondTextBox.Text))
                return ProcessAssessment.Need($"Introduzca dos caudales ({VolumeFlowUnit}), uno por entrada.");
            if(!TryReadPositive(MixFlowFirstTextBox.Text,out double v1)||!TryReadPositive(MixFlowSecondTextBox.Text,out double v2))
                return ProcessAssessment.Invalid("Los dos caudales de mezcla deben ser positivos.");
            flows.Add(VolumeFlowToSI(v1));flows.Add(VolumeFlowToSI(v2));
        }
        if(flows.Count!=count)return ProcessAssessment.Need($"Introduzca exactamente {count} caudales ({VolumeFlowUnit}), uno por corriente.");
        return ProcessAssessment.Ok("Caudales válidos.");
    }

    private ProcessAssessment ReadEquipmentPrediction(ProcessType type,ChartPoint inlet,out EquipmentPrediction? prediction)
    {
        prediction=null;
        ChartPoint? output;
        string note;
        switch(type)
        {
            case ProcessType.EvaporativeHumidification:
                if(string.IsNullOrWhiteSpace(EffectivenessTextBox.Text))
                    return ProcessAssessment.Need("Introduzca la efectividad evaporativa.");
                if(!TryReadUnitInterval(EffectivenessTextBox.Text,out double e))
                    return ProcessAssessment.Invalid("La efectividad evaporativa debe estar entre 0 y 1.");
                if(!ProcessCalculator.TryEvaporativeOutlet(inlet,e,0,inlet.ColorHex,out output,out note)||output is null)
                    return ProcessAssessment.Invalid(note);
                prediction=new(output,note,Effectiveness:e);
                break;
            case ProcessType.Bypass:
                if(string.IsNullOrWhiteSpace(ApparatusDewPointTextBox.Text)||string.IsNullOrWhiteSpace(BypassFactorTextBox.Text))
                    return ProcessAssessment.Need($"Introduzca ADP ({TempUnit}) y factor de bypass.");
                if(!TryReadFinite(ApparatusDewPointTextBox.Text,out double adp)||!TryReadUnitInterval(BypassFactorTextBox.Text,out double bf))
                    return ProcessAssessment.Invalid("Se requiere ADP numérico y factor de bypass entre 0 y 1.");
                adp=TemperatureToSI(adp);
                if(!ProcessCalculator.TryBypassOutlet(inlet,adp,bf,0,inlet.ColorHex,out output,out note)||output is null)
                    return ProcessAssessment.Invalid(note);
                prediction=new(output,note,Bypass:bf,Adp:adp);
                break;
            case ProcessType.SteamHumidification:
                if(string.IsNullOrWhiteSpace(SteamAddedTextBox.Text)||string.IsNullOrWhiteSpace(SteamEnthalpyTextBox.Text))
                    return ProcessAssessment.Need("Introduzca masa de vapor añadida y su entalpía.");
                if(!TryReadNonNegative(SteamAddedTextBox.Text,out double grams)||!TryReadPositive(SteamEnthalpyTextBox.Text,out double hv))
                    return ProcessAssessment.Invalid("El vapor añadido no puede ser negativo y su entalpía debe ser positiva.");
                grams=WaterToSI(grams);hv=WaterEnthalpyToSI(hv);
                if(!ProcessCalculator.TrySteamOutlet(inlet,grams/1000,hv,0,inlet.ColorHex,out output,out note)||output is null)
                    return ProcessAssessment.Invalid(note);
                prediction=new(output,note,SteamGrams:grams,SteamEnthalpy:hv);
                break;
            case ProcessType.Desiccant:
                if(string.IsNullOrWhiteSpace(DesiccantRemovedTextBox.Text)||string.IsNullOrWhiteSpace(DesiccantTemperatureTextBox.Text))
                    return ProcessAssessment.Need("Introduzca humedad retirada y temperatura de salida.");
                if(!TryReadNonNegative(DesiccantRemovedTextBox.Text,out double removed)||!TryReadFinite(DesiccantTemperatureTextBox.Text,out double t))
                    return ProcessAssessment.Invalid("La humedad retirada no puede ser negativa y la temperatura debe ser numérica.");
                removed=WaterToSI(removed);t=TemperatureToSI(t);
                if(!ProcessCalculator.TryDesiccantOutlet(inlet,removed/1000,t,0,inlet.ColorHex,out output,out note)||output is null)
                    return ProcessAssessment.Invalid(note);
                prediction=new(output,note,RemovedGrams:removed,OutletT:t);
                break;
            default:
                return ProcessAssessment.Invalid("Método predictivo no reconocido.");
        }
        return ProcessAssessment.Ok("Parámetros compatibles con el modelo de equipo.");
    }
}
