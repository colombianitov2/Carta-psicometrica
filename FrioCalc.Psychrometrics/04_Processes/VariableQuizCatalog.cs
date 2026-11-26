using System;
using System.Collections.Generic;
using System.Linq;

namespace FrioCalc.Psychrometrics.Processes;

// [4.19] Preguntas originales basadas en temas A03 del material docente y
// fundamentos complementarios de la carta. No son transcripciones de diapositivas.
public sealed record VariableQuestion(string Prompt,string[] Answers,int CorrectIndex,string Explanation,string TopicId);
public static class VariableQuizCatalog
{
    private static readonly Dictionary<string,VariableQuestion[]> Bank=new()
    {
        ["Tbs"]=new VariableQuestion[]
        {
            new("En una calefacción sensible, ¿qué ocurre con la temperatura de bulbo seco?",new[]{"Aumenta","Permanece constante","Disminuye"},0,"Se aporta energía sin añadir vapor; aumenta la temperatura de bulbo seco.","A03-02"),
            new("Al enfriar aire de 35 a 25 °C sin condensación, ¿qué propiedad permanece constante?",new[]{"Entalpía","Humedad absoluta W","Humedad relativa"},1,"El enfriamiento sensible conserva la masa de vapor por masa seca, no la entalpía ni la humedad relativa.","A03-02"),
            new("¿Cómo se representa un calentamiento sensible en una carta con temperatura horizontal y W vertical?",new[]{"Línea vertical ascendente","Curva de saturación","Línea horizontal hacia la derecha"},2,"La temperatura aumenta mientras W se mantiene constante.","A03-02"),
        },
        ["Tbh"]=new VariableQuestion[]
        {
            new("En el lavador evaporativo directo ideal, ¿hacia qué temperatura se aproxima el aire?",new[]{"Temperatura de rocío","Bulbo húmedo inicial","Temperatura exterior máxima"},1,"La efectividad de saturación mide la aproximación al bulbo húmedo inicial.","A03-07"),
            new("Si el aire entra a 30 °C con bulbo húmedo de 15 °C y efectividad de 70 %, ¿cuál es la temperatura de salida según el modelo ideal?",new[]{"19,5 °C (67,1 °F)","15 °C (59 °F)","25,5 °C (77,9 °F)"},0,"El descenso es el 70 % de la diferencia entre bulbo seco y húmedo: 0,70 × 15 = 10,5 °C.","A03-07"),
            new("¿Qué ocurre con el bulbo seco y húmedo cuando el aire está saturado en el modelo de equilibrio?",new[]{"El bulbo húmedo es mayor","El bulbo seco es siempre mayor","Coinciden"},2,"A saturación no hay potencial de enfriamiento por evaporación en ese estado.","A03-07"),
        },
        ["Tdp"]=new VariableQuestion[]
        {
            new("¿Qué indica el punto de rocío durante el enfriamiento a presión y W constantes?",new[]{"La temperatura de inicio de la condensación","La temperatura de ebullición del agua","La temperatura de salida del refrigerante"},0,"Al alcanzar el rocío, la presión parcial del vapor iguala la de saturación.","A03-08"),
            new("Un aire con rocío de 28 °C se enfría hasta 21 °C y queda saturado. ¿Qué debe ocurrir con W?",new[]{"Aumentar","Disminuir","Permanecer constante"},1,"El estado final saturado a menor temperatura contiene menos vapor por masa seca.","A03-08"),
            new("Durante una calefacción sensible sin intercambio de vapor y a presión constante, ¿cómo cambia el rocío?",new[]{"Aumenta con el bulbo seco","Disminuye con HR","Permanece constante"},2,"Se conservan W y presión parcial de vapor, por lo que el rocío no cambia.","A03-08"),
        },
        ["RH"]=new VariableQuestion[]
        {
            new("Si se calienta aire sin añadir ni retirar agua a presión constante, ¿qué ocurre con HR?",new[]{"Aumenta","Disminuye","Permanece constante"},1,"Aumenta la presión de saturación mientras la presión parcial de vapor se mantiene.","A03-02"),
            new("¿Qué significa una humedad relativa de 100 %?",new[]{"El aire contiene únicamente agua","Se duplicó la masa de vapor","La presión parcial de vapor iguala la de saturación"},2,"La humedad relativa compara presiones a la misma temperatura.","A03-02"),
            new("¿Una caída de HR demuestra necesariamente que se retiró vapor?",new[]{"No, también puede resultar de un aumento de temperatura","Sí, en cualquier proceso","Sí, si la presión es constante"},0,"La masa de vapor se verifica con W; HR depende también de la temperatura.","A03-02"),
        },
        ["W"]=new VariableQuestion[]
        {
            new("¿Qué mide la humedad absoluta W empleada por la carta?",new[]{"Masa de vapor por masa de aire seco","Masa de vapor por volumen total","Porcentaje de presión de saturación"},0,"W es una razón de masas con base de aire seco.","A03-05"),
            new("Si W disminuye durante una transformación, ¿qué ocurre con la masa de vapor por masa de aire seco?",new[]{"Se conserva","Aumenta","Disminuye"},2,"La disminución de W identifica retirada de vapor, independientemente de HR.","A03-05"),
            new("Al mezclar corrientes con diferentes W sin condensación, ¿cómo se obtiene W de la mezcla?",new[]{"Promedio de temperaturas","Promedio de W ponderado por caudal de aire seco","Promedio de caudales volumétricos"},1,"El balance de vapor se pondera por masa de aire seco de cada corriente.","A03-05"),
        },
        ["h"]=new VariableQuestion[]
        {
            new("En un enfriamiento sensible a W constante, ¿qué signo tiene el cambio de entalpía de salida menos entrada?",new[]{"Positivo","Negativo","Siempre cero"},1,"La entalpía disminuye al descender la temperatura a W constante.","A03-03"),
            new("¿Qué información hace falta para convertir una diferencia de entalpía específica en potencia del flujo de aire?",new[]{"Caudal másico de aire seco","Solo HR","Solo temperatura exterior"},0,"La potencia de cambio de entalpía es el producto del caudal seco por la diferencia de entalpía.","A03-03"),
            new("En un serpentín con condensación, ¿qué término adicional requiere el balance de energía del equipo?",new[]{"La masa molar del aire seco únicamente","La entropía absoluta del refrigerante únicamente","La energía transportada por el condensado"},2,"La energía que sale con el agua líquida debe contabilizarse en la frontera del serpentín.","A03-03"),
        },
        ["v"]=new VariableQuestion[]
        {
            new("Para obtener caudal másico de aire seco a partir de caudal volumétrico, ¿qué se utiliza?",new[]{"La entalpía","El volumen específico de la corriente","El factor sensible"},1,"Se divide el caudal volumétrico real entre el volumen específico referido a masa seca.","A03-12"),
            new("Dos corrientes tienen igual caudal volumétrico, pero diferente volumen específico. ¿Tienen necesariamente igual caudal seco?",new[]{"No","Sí","Solo si ambas son saturadas"},0,"La masa por unidad de tiempo depende del volumen específico de cada corriente.","A03-12"),
            new("¿Qué volumen específico debe usarse con un caudal volumétrico medido en la entrada?",new[]{"El del aire a 0 °C","El del estado de salida","El del estado de entrada"},2,"El volumen y la propiedad deben corresponder a las mismas condiciones.","A03-12"),
        },
        ["P"]=new VariableQuestion[]
        {
            new("¿Qué presión se utiliza en las ecuaciones psicrométricas?",new[]{"Manométrica","Absoluta","Diferencial del ventilador"},1,"La presión de estado de la mezcla se refiere al vacío.","BASE-11"),
            new("Si cambia la presión de trabajo, ¿qué debe hacerse con los estados de una carta calculados previamente?",new[]{"Recalcularlos con la nueva presión y las propiedades independientes","Mantener siempre sus propiedades","Cambiar solamente el color de las líneas"},0,"La presión interviene en las relaciones de humedad, volumen y saturación.","BASE-11"),
            new("En una mezcla adiabática simple, ¿qué condición de presión exige el modelo del programa?",new[]{"Una presión distinta por corriente","Presión manométrica nula en todas","Presión absoluta común"},2,"Un cambio de presión necesita un modelo adicional.","BASE-11"),
        },
        ["SHR"]=new VariableQuestion[]
        {
            new("Con carga sensible de 150 kW y latente de 50 kW, ¿cuál es el FCS del recinto?",new[]{"0,75","0,25","3,00"},0,"La carga total es 200 kW y la fracción sensible es 150 dividido por 200.","A03-13"),
            new("¿Puede determinarse una única condición de impulsión conociendo únicamente FCS?",new[]{"Sí, siempre","No, se necesita otra condición independiente","Sí, si el aire es exterior"},1,"El factor define una dirección o relación de cargas, no un único estado.","A03-13"),
            new("¿Cuál es la diferencia entre RSHF y GSHF?",new[]{"RSHF es temperatura y GSHF presión","No existe diferencia","Corresponden a fronteras de balance distintas: recinto y sistema"},2,"El factor global incluye las cargas de la frontera del sistema, que pueden diferir de las del recinto.","A03-13"),
        },
        ["BF"]=new VariableQuestion[]
        {
            new("¿Qué representa un BF de 0,20 en el modelo ideal del serpentín?",new[]{"Una fracción idealizada del 20 % de aire sin contacto","Una humedad relativa de 20 %","Una eficiencia térmica del 20 %"},0,"La salida ideal se pondera con 20 % del estado de entrada y 80 % del estado ADP.","A03-17"),
            new("¿Qué significa ADP en el modelo de serpentín?",new[]{"Presión diferencial del aire","Temperatura de saturación efectiva del aparato","Caudal total del agua fría"},1,"El ADP es el punto de rocío efectivo del aparato, asociado al estado saturado ideal.","A03-17"),
            new("¿Es suficiente BF por sí solo para obtener la salida?",new[]{"Sí, porque define la temperatura","Sí, si vale 0,20","No, también se requieren entrada y ADP"},2,"BF es una fracción; necesita los estados que se mezclan.","A03-17"),
        },
        ["s"]=new VariableQuestion[]
        {
            new("¿Qué distingue la entropía específica relativa de una diferencia de entalpía?",new[]{"Son la misma magnitud","La entropía depende de temperatura absoluta, presiones y referencias de la mezcla","La entropía se expresa en kW"},1,"La entropía relativa usa relaciones de temperatura y presión con una referencia definida; no representa calor directamente.","BASE-10"),
            new("¿Qué escala de temperatura debe emplearse en el cociente de temperaturas de la ecuación de entropía?",new[]{"Absoluta, Kelvin o Rankine","Celsius directamente","Fahrenheit directamente"},0,"Los cocientes termodinámicos de temperatura requieren valores absolutos positivos.","BASE-10"),
            new("¿Cómo deben elegirse las referencias al comparar entropías relativas de dos estados con el mismo modelo?",new[]{"Cambiar la referencia en cada punto","Usar HR como referencia","Mantener referencias coherentes"},2,"La referencia común permite interpretar la diferencia dentro del mismo modelo.","BASE-10"),
        },
        ["dh"]=new VariableQuestion[]
        {
            new("¿Qué representa la familia auxiliar de desviación de entalpía de la carta?",new[]{"Entalpía del estado menos entalpía saturada a su bulbo húmedo","Diferencia de temperatura seca","Potencia de refrigeración"},0,"La referencia es el estado saturado a igual bulbo húmedo, no otro punto elegido libremente.","BASE-10"),
            new("¿Es la desviación auxiliar de entalpía igual al cambio de entalpía entre dos puntos de un proceso?",new[]{"Siempre","No, tienen referencias diferentes","Solo si la presión es atmosférica"},1,"El cambio entre estados compara sus entalpías; la desviación auxiliar compara contra una referencia saturada.","BASE-10"),
            new("¿Qué unidades corresponden a la desviación de entalpía en SI?",new[]{"kW","kJ/(kg·K)","kJ/kg de aire seco"},2,"Es una diferencia de energía específica, no una potencia ni una entropía.","BASE-10"),
        },
    };

    public static IReadOnlyList<VariableQuestion> For(string key)=>
        Bank.TryGetValue(key,out var questions)?questions:Array.Empty<VariableQuestion>();
}
