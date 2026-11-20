using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FrioCalc.Psychrometrics.Core;
using FrioCalc.Psychrometrics.Processes;

namespace FrioCalc.Psychrometrics.UI;

// [5.8] Diccionario de magnitudes y símbolos de las ecuaciones visibles.
public sealed class VariableReferencePanel : UserControl
{
    private sealed record Entry(string Symbol,string Name,string Definition,string Origin,string SI,string IP,string Formulas);
    private static readonly List<Entry> Entries=new();
    private readonly StackPanel _rows=new();
    private readonly TextBox _search=new(){Margin=new Thickness(0,8,0,12),MinHeight=30};
    private UnitSystem _units=UnitSystem.SI;

    static VariableReferencePanel()
    {
        void Add(string symbol,string name,string definition,string origin,string si,string ip,string formulas="")=>
            Entries.Add(new(symbol,name,definition,origin,si,ip,formulas));
        const string mass="kg",massIP="lb",flow="kg/s",flowIP="lb/h";
        const string temp="°C",tempIP="°F",pressure="kPa abs",pressureIP="psia";
        const string h="kJ/kg da",hIP="Btu/lb da",waterH="kJ/kg agua",waterHIP="Btu/lb agua";
        const string w="kg/kg da",wIP="lb/lb da",cp="kJ/(kg·K)",cpIP="Btu/(lb·°R)";
        const string heat="kW",heatIP="Btu/h",dim="1";
        Add("m_{da}","Masa de aire seco","Masa de los gases secos, sin vapor de agua.","Pesaje o balance de masa seca.",mass,massIP,"mDa|Wmass");
        Add("mᵥ","Masa de vapor","Masa de agua presente en fase gaseosa.","Balance de agua o masa seca multiplicada por W.",mass,massIP,"mV|Wmass");
        Add("P","Presión absoluta total","Presión barométrica local medida respecto al vacío.","Barómetro o presión de referencia por altitud.",pressure,pressureIP,"Dalton");
        Add("p_{da}","Presión parcial de aire seco","Contribución del aire seco a la presión total.","Por diferencia entre P y presión del vapor.",pressure,pressureIP,"pDa|Dalton");
        Add("pᵥ","Presión parcial de vapor","Contribución del vapor de agua a la presión total.","Se deduce de W y P, o HR y saturación.",pressure,pressureIP,"pv|RH");
        Add("p_{ws}","Presión de saturación","Presión del vapor en equilibrio con líquido o hielo a una temperatura.","Correlación termodinámica de saturación del agua, evaluada a la temperatura indicada.",pressure,pressureIP,"PwsIce|PwsWater|PwsExp|Ws|RH|Tdp");
        Add("L","Logaritmo de presión de saturación","Logaritmo natural del valor numérico de la presión de saturación en las unidades seleccionadas.","Correlación sobre hielo o líquido, según temperatura respecto al punto triple.",dim,dim,"PwsIce|PwsWater|PwsExp");
        Add("t / Tbs","Temperatura de bulbo seco","Temperatura medida sin evaporación ni radiación directa.","Termómetro calibrado o solución del estado.",temp,tempIP,"TK|MixT");
        Add("T / T_{K}","Temperatura absoluta","Temperatura desde el cero absoluto. T se expresa en Kelvin o Rankine según el sistema.","Conversión desde la temperatura seca.","K","°R","TK|EntropyT");
        Add("T* / Tbh","Temperatura de bulbo húmedo","Temperatura de saturación adiabática del aire.","Resolver el balance del saturador con P, t y W.",temp,tempIP,"Tbh");
        Add("Tdp","Temperatura de rocío","Temperatura en la cual comienza la condensación a W y P constantes.","Invertir la presión de saturación a pᵥ.",temp,tempIP,"Tdp");
        Add("W","Razón de humedad","Masa de vapor por masa de aire seco; la variable de las fórmulas es una razón de masas.","Balance de masas o ley de Dalton y gases ideales.",w,wIP,"Wmass|W|hInverse|vInverse|MixW|BFW");
        Add("Wₛ","Razón de humedad saturada","Máxima masa de vapor por masa seca en equilibrio a P y temperatura dados.","Evaluar p de saturación y sustituir en W.",w,wIP,"Ws");
        Add("γ","Humedad específica","Masa de vapor dividida por masa total de aire húmedo.","Definición de masa de mezcla.", "kg/kg mezcla","lb/lb mezcla","SpecificHumidity");
        Add("ρᵥ","Densidad de vapor","Masa de vapor por volumen de aire húmedo.","Dividir W entre el volumen específico.","kg/m³","lb/ft³","VaporDensity");
        Add("φ / HR","Humedad relativa","Porcentaje de la presión parcial de vapor respecto de la de saturación a la misma temperatura.","Higrómetro o relación de presiones.","%","%","RH");
        Add("h","Entalpía específica de aire húmedo","Suma de las entalpías del aire seco y del vapor en base a masa seca. Referencia del motor: aire seco a 0 °C (32 °F).","Modelo de calor específico constante; se obtiene con temperatura y W.",h,hIP,"h|MixH|SteamH");
        Add("hₛ","Entalpía de aire saturado","Entalpía de aire húmedo con HR=100 % a la temperatura y presión indicadas.","Evaluar la ecuación de h con Wₛ.",h,hIP,"h|Ws|Tbh");
        Add("h_{w} / h_{agua}","Entalpía del agua suministrada","Entalpía específica por masa de agua, líquida o vapor según el proceso.","Temperatura, presión y fase del suministro; tablas de propiedades del agua.",waterH,waterHIP,"SteamH|Tbh");
        Add("h_{liq}","Entalpía de condensado líquido","Entalpía del líquido retirado del aire.","Temperatura del condensado y referencia de entalpía común; el motor usa calor específico líquido.",waterH,waterHIP,"Coil");
        Add("v","Volumen específico","Volumen total de mezcla por unidad de masa de aire seco.","Ley de gas ideal de la mezcla.", "m³/kg da","ft³/lb da","v");
        Add("V̇","Caudal volumétrico","Volumen de aire que atraviesa una sección por unidad de tiempo.","Medir o especificar flujo real del conducto.","m³/s","ft³/h","Mdot");
        Add("ṁ_{da}","Caudal másico seco","Masa de aire seco que fluye por unidad de tiempo.","Dividir caudal volumétrico por volumen específico.",flow,flowIP,"Mdot|Heat");
        Add("ṁ_{agua}","Caudal másico de agua","Flujo de agua suministrada o de agua de enfriamiento, según el balance.","Diferencia de razón de humedad o balance del circuito de agua.",flow,flowIP,"SteamW|WaterFlow");
        Add("ṁ_{cond}","Caudal de condensado","Masa de agua líquida separada en un enfriamiento con condensación.","Balance de vapor entre estados de entrada y salida.",flow,flowIP,"Condensate|Coil");
        Add("Q̇","Potencia térmica","Velocidad de transferencia de energía térmica. Positiva hacia el aire en el balance general.","Caudal seco y variación de entalpía; contabilizar agua adicional.",heat,heatIP,"Heat|Coil");
        Add("Q̇ₛ / Q̇_{sensible}","Potencia sensible","Transferencia térmica asociada a variación de temperatura, con W fija en el modelo sensible.","Balance energético sensible.",heat,heatIP,"Sensible");
        Add("Q̇_{total}","Potencia total","Suma de componentes sensible y latente del mismo balance.","Balance de energía de la frontera considerada.",heat,heatIP,"TotalPower|Heat|SHR");
        Add("Q̇_{latente}","Potencia latente","Componente térmica asociada al cambio de contenido de agua.","Diferencia entre potencia total y sensible del mismo balance.",heat,heatIP,"TotalPower|SHR");
        Add("Q̇_{ext}","Potencia extraída","Transferencia de energía retirada en un serpentín, positiva al extraer.","Balance incluyendo la entalpía del condensado.",heat,heatIP,"Coil");
        Add("FCS / SHR","Factor de calor sensible","Fracción sensible de la potencia total del balance.","Cociente de cargas con frontera y signos coherentes.",dim,dim,"SHR");
        Add("RSH","Calor sensible del recinto","Carga sensible correspondiente a la frontera del local.","Balance térmico del recinto.",heat,heatIP,"RSHF");
        Add("RLH","Calor latente del recinto","Carga latente correspondiente a humedad de la frontera del local.","Balance de masa de vapor del recinto.",heat,heatIP,"RSHF");
        Add("GSH","Calor sensible global","Carga sensible total de la frontera del sistema.","Balance del sistema, incluida ventilación cuando corresponde.",heat,heatIP,"GSHF");
        Add("GLH","Calor latente global","Carga latente total de la frontera del sistema.","Balance de humedad del sistema.",heat,heatIP,"GSHF");
        Add("RSHF","Factor sensible del recinto","Fracción sensible de la carga total del recinto.","Cociente entre RSH y suma de RSH y RLH.",dim,dim,"RSHF");
        Add("GSHF","Factor sensible global","Fracción sensible de la carga total del sistema.","Cociente entre GSH y suma de GSH y GLH.",dim,dim,"GSHF");
        Add("ε","Efectividad de saturación","Descenso térmico real dividido por descenso máximo al bulbo húmedo inicial.","Modelo de enfriamiento evaporativo.",dim,dim,"Evap");
        Add("ADP","Punto de rocío del aparato","Temperatura de saturación efectiva del serpentín ideal.","Dato del equipo o intersección de la trayectoria prolongada con saturación.",temp,tempIP,"BF|BFW");
        Add("BF","Factor de bypass","Fracción idealizada de aire que evita el contacto con el estado ADP.","Modelo de mezcla ideal de serpentín, por entalpía o razón de humedad.",dim,dim,"BF|BFW");
        Add("h_{ADP}","Entalpía al ADP","Entalpía del estado saturado a la temperatura ADP.","Evaluar h y Wₛ a ADP y P.",h,hIP,"h|BF");
        Add("W_{ADP}","Razón de humedad al ADP","Razón de humedad de saturación a la temperatura ADP.","Correlación de saturación y P.",w,wIP,"Ws|BFW");
        Add("c_{p,da}","Calor específico del aire seco","Energía por masa seca y diferencia de temperatura a presión constante.","Coeficiente constante del modelo: 1,006 kJ/(kg·K), equivalente a 0,240279 Btu/(lb·°R).",cp,cpIP,"h|Sensible|Entropy");
        Add("c_{p,v}","Calor específico del vapor","Energía por masa de vapor y diferencia de temperatura a presión constante.","Coeficiente constante del modelo: 1,86 kJ/(kg·K), equivalente a 0,444253 Btu/(lb·°R).",cp,cpIP,"h|Entropy");
        Add("c_{p,agua}","Calor específico del agua líquida","Energía por masa líquida y diferencia de temperatura.","Propiedad del agua a las condiciones de operación.",cp,cpIP,"WaterFlow");
        Add("R_{da}","Constante de gas del aire seco","Constante específica en la ecuación de estado ideal.","Constante universal dividida por masa molar del aire seco; 0,287042 en SI energético, o 0,370489 psia·ft³/(lb·°R) para volumen IP.",cp,cpIP,"v|Entropy");
        Add("Rᵥ","Constante de gas del vapor","Constante específica del vapor de agua, usada en modelo de gas ideal.","Constante universal dividida por masa molar del agua.",cp,cpIP,"Entropy|v");
        Add("0,621945","Razón de masas molares","Masa molar del vapor dividida por la del aire seco.","Composición molecular del aire seco y agua.",dim,dim,"W|pv");
        Add("1,607858","Razón de constantes de gas","Coeficiente que multiplica W en la ecuación de volumen específico.","Cociente aproximado entre constante del vapor y del aire seco.",dim,dim,"v");
        Add("Δh","Desviación de entalpía","En esta familia auxiliar: h del estado menos h saturada a igual bulbo húmedo. En un tramo puede designar h₂−h₁.","Diferenciar la referencia saturada del balance entre dos estados.",h,hIP,"DeltaH|Heat");
        Add("s*","Entropía específica relativa","Suma de contribuciones de entropía del aire seco y vapor con referencia fija.","Modelo de mezcla de gases ideales.","kJ/(kg da·K)","Btu/(lb da·°R)","Entropy");
        Add("θ","Razón de temperaturas absolutas","Temperatura del estado dividida por la de referencia.","Ambas en escala absoluta, no en Celsius ni Fahrenheit.",dim,dim,"EntropyT");
        Add("π_{da}","Razón de presiones parciales secas","Presión parcial del aire seco entre su referencia.","Ambas absolutas y en las mismas unidades.",dim,dim,"EntropyPda");
        Add("πᵥ","Razón de presiones parciales de vapor","Presión parcial de vapor entre su referencia.","Ambas absolutas y en las mismas unidades.",dim,dim,"EntropyPv");
        Add("T_{ref}","Temperatura absoluta de referencia","Temperatura fija respecto de la cual se calcula entropía relativa.","Referencia del modelo: 273,15 K = 491,67 °R; no temperatura de salida.","K","°R","EntropyT");
        Add("p_{da,ref}","Presión parcial seca de referencia","Presión absoluta parcial de aire seco en el estado de referencia.","Referencia del motor: 101,325 kPa = 14,69595 psia.",pressure,pressureIP,"EntropyPda");
        Add("p_{v,ref}","Presión parcial de vapor de referencia","Presión absoluta parcial de vapor en el estado de referencia.","Referencia del motor: 0,611657 kPa ≈ 0,088713 psia.",pressure,pressureIP,"EntropyPv");
        Add("ln","Logaritmo natural","Operador matemático de base e, aplicado a una razón positiva adimensional.","Integración de diferenciales de entropía ideal.",dim,dim,"Entropy");
        Add("t_{ent}","Temperatura de entrada del agua","Temperatura del agua que entra al equipo.","Sensor o dato del circuito de agua.",temp,tempIP,"WaterFlow");
        Add("t_{sal}","Temperatura de salida del agua","Temperatura del agua que sale del equipo.","Sensor o dato del circuito de agua.",temp,tempIP,"WaterFlow");
        Add("T_{bh,1}","Bulbo húmedo de entrada","Temperatura de bulbo húmedo del estado inicial.","Resolver el balance a las condiciones de entrada.",temp,tempIP,"Tbh|Evap");
        for(int i=1;i<=3;i++)
        {
            string index=i==1?"₁":i==2?"₂":"₃";
            string state=i==1?"entrada o primera corriente":i==2?"salida o segunda corriente (en mezcla)":"salida de mezcla";
            Add("W"+index,"Razón de humedad — "+state,"Masa de vapor por masa seca en la corriente indicada.","Cálculo del estado o balance de humedad.",w,wIP,i==3?"MixW":"W|SteamW|Condensate");
            Add("h"+index,"Entalpía — "+state,"Entalpía por masa seca de la corriente indicada.","Cálculo del estado o balance de energía.",h,hIP,i==3?"MixH":"h|Heat|SteamH");
            if(i<3)
            {
                Add("t"+index,"Temperatura — "+state,"Temperatura de bulbo seco en la corriente indicada.","Medición o solución del estado.",temp,tempIP,"TK|Sensible|Evap");
                Add("ṁ"+index,"Caudal seco — "+state,"Caudal másico de aire seco de esa corriente.","Caudal volumétrico dividido por su propio volumen específico.",flow,flowIP,"Mdot|MixW|MixH");
            }
        }
    }

    public VariableReferencePanel()
    {
        var host=new StackPanel{Margin=new Thickness(16),MaxWidth=1500};
        host.Children.Add(new TextBlock{Text="VARIABLES",FontSize=20,FontWeight=FontWeights.Bold});
        host.Children.Add(new TextBlock{Text="Símbolos, unidades, definición, obtención y ecuaciones según el modelo. da = aire seco; v = vapor cuando es subíndice; s = saturación; ref = referencia; 1, 2 y 3 identifican los estados.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,6,0,4)});
        host.Children.Add(new TextBlock{Text="Buscar por símbolo o nombre",FontWeight=FontWeights.SemiBold});
        host.Children.Add(_search);host.Children.Add(_rows);
        Content=new ScrollViewer{Content=host,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
        _search.TextChanged+=(_,_)=>Refresh();
        Loaded+=(_,_)=>Refresh();
    }

    public void SetUnits(UnitSystem units){_units=units;if(IsLoaded)Refresh();}

    private void Refresh()
    {
        _rows.Children.Clear();
        var headers=new[]{"Variable","Unidad","Definición","Obtención / origen","Fórmula y modelo"};
        var weights=new[]{1.0,0.9,1.6,1.6,3.0};
        Grid Row()
        {
            var grid=new Grid();
            foreach(double weight in weights)grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(weight,GridUnitType.Star)});
            return grid;
        }
        void Cell(Grid row,int col,UIElement content,bool header=false)
        {
            var border=new Border{BorderBrush=Brushes.LightGray,BorderThickness=new Thickness(.5),
                Background=header?Brushes.AliceBlue:Brushes.White,Padding=new Thickness(8),Child=content};
            Grid.SetColumn(border,col);row.Children.Add(border);
        }
        TextBlock Txt(string value,bool bold=false)
        {
            var t=new TextBlock{TextWrapping=TextWrapping.Wrap,FontWeight=bold?FontWeights.SemiBold:FontWeights.Normal};
            AshraeStudyPanel.FormatMath(t,value);return t;
        }
        var head=Row();
        for(int i=0;i<headers.Length;i++)Cell(head,i,Txt(headers[i],true),true);
        _rows.Children.Add(head);
        string query=_search.Text.Trim();
        foreach(var e in Entries.Where(x=>query.Length==0 || (x.Name+" "+x.Symbol).Contains(query,StringComparison.CurrentCultureIgnoreCase)))
        {
            var row=Row();
            var variable=new StackPanel();variable.Children.Add(Txt(e.Symbol,true));variable.Children.Add(Txt(e.Name));
            Cell(row,0,variable);Cell(row,1,Txt(_units==UnitSystem.IP?e.IP:e.SI));
            Cell(row,2,Txt(e.Definition));Cell(row,3,Txt(e.Origin));
            var formulas=new StackPanel();
            foreach(string key in e.Formulas.Split('|'))
                if(AshraeStudyCatalog.FindEquation(key) is AshraeEquation raw)
                {
                    var eq=EducationalUnits.Equation(raw,_units);
                    var block=Txt(eq.Title,true);block.Margin=new Thickness(0,6,0,2);
                    formulas.Children.Add(block);
                    var visual=AshraeStudyPanel.RenderMathematicalFraction(eq);
                    void Fit(UIElement element)
                    {
                        if(element is TextBlock math){math.MaxWidth=270;math.FontSize=17;}
                        else if(element is Panel panel)foreach(UIElement child in panel.Children)Fit(child);
                    }
                    Fit(visual);formulas.Children.Add(visual);
                    formulas.Children.Add(Txt(eq.Origin));
                }
            if(formulas.Children.Count==0)formulas.Children.Add(Txt("Magnitud medida o dato de referencia."));
            Cell(row,4,formulas);_rows.Children.Add(row);
        }
    }
}
