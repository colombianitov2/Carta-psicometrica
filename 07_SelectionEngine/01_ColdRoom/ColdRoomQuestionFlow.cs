using System.Collections.Generic;

namespace FrioCalc.Psychrometrics.SelectionEngine.ColdRoom;

// ------------------------------ Sección 7.4.1 - Definición de preguntas y etapas ------------------------------

public sealed record ColdRoomQuestionDefinition(
    string Id,
    string Prompt,
    string? Unit,
    string SourceField,
    string SourceForm,
    bool IsRequired);

public sealed record ColdRoomWorkflowStepDefinition(
    string Id,
    string Title,
    IReadOnlyList<ColdRoomQuestionDefinition> Questions);

// ------------------------------ Fin de sección 7.4.1 ------------------------------


// ------------------------------ Sección 7.4.2 - Catálogo del flujo de preguntas ------------------------------

public static class ColdRoomQuestionFlow
{
    public static IReadOnlyList<ColdRoomWorkflowStepDefinition> Steps { get; } =
        new[]
        {
            new ColdRoomWorkflowStepDefinition(
                "7.4.2.1",
                "Dimensiones y entorno",
                new[]
                {
                    Q("room.length", "Longitud de la cámara", "m", "EditLength", "TCOLDROOMVALUEWIZARDDLG", true),
                    Q("room.width", "Ancho de la cámara", "m", "EditWidth", "TCOLDROOMVALUEWIZARDDLG", true),
                    Q("room.height", "Altura de la cámara", "m", "EditHeight", "TCOLDROOMVALUEWIZARDDLG", true),
                    Q("room.dimensionReference", "¿Las dimensiones son interiores o exteriores?", null, "RBInnerDim/RBOuterDim", "TCOLDROOMVALUEWIZARDDLG", true),
                    Q("surroundings.temperature", "Temperatura del entorno", "°C", "EditTRoomSurroundings", "TCOLDROOMVALUEWIZARDDLG", true),
                    Q("surroundings.relativeHumidity", "Humedad relativa del entorno", "%", "EditRHRoomInfiltration", "TCOLDROOMVALUEWIZARDDLG", true),
                    Q("floor.insulated", "¿El piso está aislado?", null, "CBFloor", "TCOLDROOMVALUEWIZARDDLG", true),
                    Q("floor.temperature", "Temperatura debajo del piso", "°C", "EditTFloorSurroundings", "TCOLDROOMVALUEWIZARDDLG", false)
                }),

            new ColdRoomWorkflowStepDefinition(
                "7.4.2.2",
                "Mercancía",
                new[]
                {
                    Q("goods.type", "Tipo de mercancía", null, "CBGoodsType", "TCOLDROOMVALUEWIZARDDLG", true),
                    Q("goods.totalMass", "Masa total almacenada", "kg", "EditGoodsMass", "TCOLDROOMVALUEWIZARDDLG", true),
                    Q("goods.dailyMass", "Cantidad de mercancía renovada por día", "kg/día", "EditGoodsExchangeMass", "TCOLDROOMVALUEWIZARDDLG", true),
                    Q("goods.inletTemperature", "Temperatura de entrada de la mercancía", "°C", "EditInletTemperature", "TCOLDROOMVALUEWIZARDDLG", true),
                    Q("goods.respirationHeat", "Tipo de carga de respiración", null, "RespHeatType", "07_TIPOS_Y_ESTRUCTURAS_DE_CALCULO", false),
                    Q("goods.storageType", "Tipo de almacenamiento", null, "StorageType", "07_TIPOS_Y_ESTRUCTURAS_DE_CALCULO", true)
                }),

            new ColdRoomWorkflowStepDefinition(
                "7.4.2.3",
                "Condiciones de la cámara y valores recomendados",
                new[]
                {
                    Q("room.temperature", "Temperatura interior de la cámara", "°C", "EditTRoom", "TCOLDROOMVALUEWIZARDDLG", true),
                    Q("room.relativeHumidity", "Humedad relativa interior", "%", "EditRHRoom", "TCOLDROOMVALUEWIZARDDLG", true),
                    Q("room.operatingHours", "Horas de operación por día", "h/día", "EditWorkingHours", "TCOLDROOMVALUEWIZARDDLG", true),
                    Q("panels.family", "Tipo de panel", null, "CBPanelType", "TCOLDROOMVALUEWIZARDDLG", true),
                    Q("panels.thickness", "Espesor del panel", "m", "CBPanelThickness", "TCOLDROOMVALUEWIZARDDLG", true)
                }),

            new ColdRoomWorkflowStepDefinition(
                "7.4.2.4",
                "Infiltración",
                new[]
                {
                    Q("infiltration.outdoorTemperature", "Temperatura del aire de infiltración", "°C", "EditTRoomInfiltration", "TCOMMERCIALCOLDROOMDLG", true),
                    Q("infiltration.outdoorRelativeHumidity", "Humedad relativa del aire de infiltración", "%", "EditRHRoomInfiltration", "TCOMMERCIALCOLDROOMDLG", true),
                    Q("infiltration.mode", "¿Definir por aperturas de puerta o por renovaciones de aire?", null, "RBDoorOpenings/RBAirExchangeRate", "TCOMMERCIALCOLDROOMDLG", true),
                    Q("infiltration.doorFrequency", "Frecuencia de apertura de puertas", null, "CBDoorOpenings", "TCOMMERCIALCOLDROOMDLG", false),
                    Q("infiltration.airChanges", "Renovaciones del volumen de la cámara por 24 horas", "1/día", "EditAirExchangeRate", "TCOMMERCIALCOLDROOMDLG", false)
                }),

            new ColdRoomWorkflowStepDefinition(
                "7.4.2.5",
                "Cargas adicionales",
                new[]
                {
                    Q("loads.lights", "Potencia de iluminación", "W", "EditLights", "TCOMMERCIALCOLDROOMDLG", true),
                    Q("loads.fans", "Potencia de ventiladores", "W", "EditFans", "TCOMMERCIALCOLDROOMDLG", true),
                    Q("loads.people", "Ocupación de personas", "h/día", "EditPeople", "TCOMMERCIALCOLDROOMDLG", true),
                    Q("loads.other", "Otras cargas", "W", "EditOther", "TCOMMERCIALCOLDROOMDLG", false),
                    Q("defrost.enabled", "¿Hay desescarche?", null, "CBDefrost", "TCOMMERCIALCOLDROOMDLG", true),
                    Q("defrost.method", "Método de desescarche", null, "RBNaturalDefrost/RBElectricDefrost", "TCOMMERCIALCOLDROOMDLG", false),
                    Q("defrost.power", "Potencia de desescarche", "W", "EditDefrostPower", "TCOMMERCIALCOLDROOMDLG", false),
                    Q("defrost.cycles", "Desescarches por día", "1/día", "EditNumDefrosts", "TCOMMERCIALCOLDROOMDLG", false),
                    Q("defrost.duration", "Duración de cada desescarche", "min", "EditDefrostTime", "TCOMMERCIALCOLDROOMDLG", false)
                }),

            new ColdRoomWorkflowStepDefinition(
                "7.4.2.6",
                "Condiciones para selección de componentes",
                new[]
                {
                    Q("selection.region", "Región de aplicación", null, "CBRegions", "TCOMMERCIALCOLDROOMDLG", true),
                    Q("selection.refrigerant", "Refrigerante", null, "CBRefrigerant", "TCOMMERCIALCOLDROOMDLG", true),
                    Q("selection.expansionValve", "Tipo de válvula de expansión", null, "RBElectronicExpansionValve/RBThermostaticExpansionValve", "TCOMMERCIALCOLDROOMDLG", true),
                    Q("selection.connection", "Familia de conexión", null, "CBConnectionFamily", "TCOMMERCIALCOLDROOMDLG", false),
                    Q("selection.pipeLength", "Longitud entre unidad condensadora y evaporador", "m", "EditDistance", "TCOMMERCIALCOLDROOMDLG", false)
                })
        };

    private static ColdRoomQuestionDefinition Q(
        string id,
        string prompt,
        string? unit,
        string sourceField,
        string sourceForm,
        bool required)
    {
        return
            new ColdRoomQuestionDefinition(
                id,
                prompt,
                unit,
                sourceField,
                sourceForm,
                required);
    }
}

// Este catálogo reproduce únicamente el orden lógico y los campos observados.
// No copia disposición visual, marcas ni textos comerciales.

// ------------------------------ Fin de sección 7.4.2 ------------------------------
