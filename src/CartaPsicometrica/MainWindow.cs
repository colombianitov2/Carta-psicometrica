using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CartaPsicometrica;

public sealed class MainWindow : Window
{
    private readonly PsychrometricCalculator calc = new();
    private readonly ChartRenderer chart = new();
    private readonly ObservableCollection<Row> rows = new();
    private readonly Canvas canvas = new();
    private readonly DataGrid grid = new();
    private readonly TextBlock summary = new();
    private readonly TextBlock status = new();
    private readonly TextBox pressure = Box("101325");
    private readonly TextBox flow = Box("1.00");
    private readonly TextBox ta = Box("26");
    private readonly TextBox rha = Box("55");
    private readonly TextBox tb = Box("14");
    private readonly TextBox rhb = Box("90");
    private State? a;
    private State? b;

    public MainWindow()
    {
        Title = "[ En desarrollo] Carta psicométrica";
        Width = 1320;
        Height = 860;
        MinWidth = 1100;
        MinHeight = 740;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = Brush("#F4F7FB");
        Content = BuildUi();
        Loaded += (_, _) => Calculate();
        canvas.SizeChanged += (_, _) => Draw();
    }

    private UIElement BuildUi()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var head = new Border { Background = Brush("#0F172A"), Padding = new Thickness(18, 14, 18, 14) };
        head.Child = new DockPanel
        {
            Children =
            {
                status,
                new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = "[ En desarrollo] Carta psicométrica", Foreground = Brushes.White, FontSize = 22, FontWeight = FontWeights.SemiBold },
                        new TextBlock { Text = "Base inicial: puntos psicrométricos, proceso A→B y carta SI.", Foreground = Brush("#CBD5E1"), FontSize = 13, Margin = new Thickness(0,4,0,0) }
                    }
                }
            }
        };
        status.Foreground = Brush("#BFDBFE");
        status.VerticalAlignment = VerticalAlignment.Center;
        DockPanel.SetDock(status, Dock.Right);
        Grid.SetRow(head, 0);
        root.Children.Add(head);

        var body = new Grid { Margin = new Thickness(16) };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(380) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(body, 1);
        body.Children.Add(LeftPanel());
        body.Children.Add(RightPanel());
        root.Children.Add(body);

        var foot = new Border { Background = Brush("#E2E8F0"), Padding = new Thickness(14, 8, 14, 8) };
        foot.Child = new TextBlock { Text = "Próxima etapa: selección con clic, múltiples puntos A→B→C→A, unidades IP y exportación.", Foreground = Brush("#334155") };
        Grid.SetRow(foot, 2);
        root.Children.Add(foot);
        return root;
    }

    private UIElement LeftPanel()
    {
        var panel = new StackPanel();
        panel.Children.Add(Card("Condición base", Label("Presión atmosférica [Pa]", pressure), Label("Flujo de aire seco [kg/s]", flow)));
        panel.Children.Add(Card("Punto A", Label("Temperatura de bulbo seco [°C]", ta), Label("Humedad relativa [%]", rha)));
        panel.Children.Add(Card("Punto B", Label("Temperatura de bulbo seco [°C]", tb), Label("Humedad relativa [%]", rhb)));
        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        buttons.Children.Add(Button("Calcular y dibujar", Calculate, 170, "#2563EB"));
        buttons.Children.Add(Button("Restablecer", Reset, 120, "#475569"));
        panel.Children.Add(buttons);
        summary.TextWrapping = TextWrapping.Wrap;
        summary.Foreground = Brush("#64748B");
        panel.Children.Add(Card("Resultado del proceso A→B", summary));
        return new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }

    private UIElement RightPanel()
    {
        var right = new Grid { Margin = new Thickness(16,0,0,0) };
        right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });
        right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var ccard = Card("Carta psicrométrica", canvas);
        Grid.SetRow(ccard, 0);
        right.Children.Add(ccard);
        grid.ItemsSource = rows;
        grid.AutoGenerateColumns = false;
        grid.IsReadOnly = true;
        grid.CanUserAddRows = false;
        grid.Columns.Add(new DataGridTextColumn { Header = "Propiedad", Binding = new System.Windows.Data.Binding(nameof(Row.Prop)), Width = new DataGridLength(2, DataGridLengthUnitType.Star) });
        grid.Columns.Add(new DataGridTextColumn { Header = "Punto A", Binding = new System.Windows.Data.Binding(nameof(Row.A)), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        grid.Columns.Add(new DataGridTextColumn { Header = "Punto B", Binding = new System.Windows.Data.Binding(nameof(Row.B)), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        grid.Columns.Add(new DataGridTextColumn { Header = "Unidad", Binding = new System.Windows.Data.Binding(nameof(Row.Unit)), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        var gcard = Card("Propiedades calculadas", grid);
        gcard.Margin = new Thickness(0, 14, 0, 0);
        Grid.SetRow(gcard, 1);
        right.Children.Add(gcard);
        Grid.SetColumn(right, 1);
        return right;
    }

    private void Calculate()
    {
        try
        {
            double p = Read(pressure), m = Read(flow);
            a = calc.FromTdbRh("A", Read(ta), Read(rha), p);
            b = calc.FromTdbRh("B", Read(tb), Read(rhb), p);
            var pr = calc.Process(a, b, m);
            FillRows(a, b);
            summary.Text = $"Tipo: {pr.Type}\nΔT={pr.Dt:0.00} °C | ΔW={pr.Dw*1000:0.000} g/kg | Δh={pr.Dh:0.00} kJ/kg a.s.\nTotal={pr.TotalKw:0.00} kW | Sensible={pr.SensibleKw:0.00} kW | Latente={pr.LatentKw:0.00} kW | SHR={pr.Shr:0.000}\nCondensado={pr.CondensateKgH:0.000} kg/h | Humidificación={pr.HumidificationKgH:0.000} kg/h";
            status.Text = "Cálculo actualizado";
            Draw();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Entrada no válida", MessageBoxButton.OK, MessageBoxImage.Warning);
            status.Text = "Revise las entradas";
        }
    }

    private void Reset()
    {
        pressure.Text = "101325"; flow.Text = "1.00"; ta.Text = "26"; rha.Text = "55"; tb.Text = "14"; rhb.Text = "90";
        Calculate();
    }

    private void Draw() => chart.Draw(canvas, TryRead(pressure, 101325), a, b);

    private void FillRows(State x, State y)
    {
        rows.Clear();
        Add("Presión atmosférica", x.Pa, y.Pa, "Pa", "0");
        Add("Bulbo seco", x.Tdb, y.Tdb, "°C", "0.00");
        Add("Humedad relativa", x.Rh*100, y.Rh*100, "%", "0.00");
        Add("Razón de humedad", x.W*1000, y.W*1000, "g/kg a.s.", "0.000");
        Add("Bulbo húmedo", x.Twb, y.Twb, "°C", "0.00");
        Add("Punto de rocío", x.Tdp, y.Tdp, "°C", "0.00");
        Add("Entalpía", x.H, y.H, "kJ/kg a.s.", "0.00");
        Add("Presión parcial de vapor", x.Pv, y.Pv, "Pa", "0.0");
        Add("Presión de vapor saturado", x.Pvs, y.Pvs, "Pa", "0.0");
        Add("Calor específico aprox.", x.Cp, y.Cp, "kJ/kg a.s.·K", "0.000");
        Add("Volumen específico", x.V, y.V, "m³/kg a.s.", "0.0000");
        Add("Densidad", x.Rho, y.Rho, "kg/m³", "0.000");
        Add("Grado de saturación", x.Mu*100, y.Mu*100, "%", "0.00");
    }

    private void Add(string prop, double x, double y, string unit, string fmt) => rows.Add(new Row(prop, x.ToString(fmt, CultureInfo.InvariantCulture), y.ToString(fmt, CultureInfo.InvariantCulture), unit));
    private static double Read(TextBox b) => double.TryParse(b.Text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : throw new FormatException($"Valor inválido: {b.Text}");
    private static double TryRead(TextBox b, double fallback) => double.TryParse(b.Text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : fallback;
    private static TextBox Box(string text) => new() { Text = text, Height = 34, Margin = new Thickness(0,4,0,10), Padding = new Thickness(8,4,8,4), BorderBrush = Brush("#D8E0EA") };
    private static Button Button(string text, Action action, double width, string color) { var b = new Button { Content = text, Width = width, Height = 36, Margin = new Thickness(0,8,8,0), Foreground = Brushes.White, Background = Brush(color), BorderBrush = Brush(color), FontWeight = FontWeights.SemiBold }; b.Click += (_, _) => action(); return b; }
    private static StackPanel Label(string text, UIElement element) { var p = new StackPanel(); p.Children.Add(new TextBlock { Text = text }); p.Children.Add(element); return p; }
    private static Border Card(string title, params UIElement[] items) { var s = new StackPanel(); s.Children.Add(new TextBlock { Text = title, FontSize = 17, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0,0,0,8) }); foreach (var i in items) s.Children.Add(i); return new Border { Child = s, Background = Brushes.White, BorderBrush = Brush("#D8E0EA"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(14), Padding = new Thickness(16), Margin = new Thickness(0,0,0,14) }; }
    private static SolidColorBrush Brush(string hex) => (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
}

public sealed record Row(string Prop, string A, string B, string Unit);
public sealed record State(string Name, double Pa, double Tdb, double Rh, double W, double Twb, double Tdp, double H, double Pv, double Pvs, double Cp, double V, double Rho, double Mu);
public sealed record Proc(double Dt, double Dw, double Dh, double TotalKw, double SensibleKw, double LatentKw, double Shr, double CondensateKgH, double HumidificationKgH, string Type);

public sealed class PsychrometricCalculator
{
    private const double Eps = 0.621945;
    private const double Rda = 287.042;
    private const double Wmin = 1e-7;
    public State FromTdbRh(string name, double tdb, double rhPercent, double pa)
    {
        double rh = Math.Clamp(rhPercent/100.0, 0, 1), pvs = Pws(tdb), pv = rh*pvs, w = WFromPv(pv, pa);
        return Build(name, tdb, w, pa);
    }
    public Proc Process(State a, State b, double m)
    {
        double dt=b.Tdb-a.Tdb, dw=b.W-a.W, dh=b.H-a.H, total=m*dh, sensible=m*(1.006+1.86*(a.W+b.W)/2)*dt, latent=total-sensible;
        return new Proc(dt,dw,dh,total,sensible,latent,Math.Abs(total)<1e-9?0:sensible/total,Math.Max(0,-dw*m*3600),Math.Max(0,dw*m*3600),Kind(dt,dw));
    }
    public double Ws(double tdb, double pa) => WFromPv(Pws(tdb), pa);
    private State Build(string name, double tdb, double w, double pa)
    {
        w=Math.Max(w,Wmin); double pv=PvFromW(w,pa), pvs=Pws(tdb), ws=Ws(tdb,pa), h=H(tdb,w), v=Rda*(tdb+273.15)*(1+1.607858*w)/pa;
        return new State(name,pa,tdb,Math.Clamp(pv/pvs,0,1),w,Twb(tdb,w,pa),Tdp(pv,tdb),h,pv,pvs,1.006+1.86*w,v,(1+w)/v,ws<=0?0:w/ws);
    }
    private static string Kind(double dt,double dw){const double e=1e-6; bool heat=dt>e,cool=dt<-e,hum=dw>e,dehum=dw<-e; if(heat&&!hum&&!dehum)return"Calentamiento sensible"; if(cool&&!hum&&!dehum)return"Enfriamiento sensible"; if(hum&&!heat&&!cool)return"Humidificación"; if(dehum&&!heat&&!cool)return"Deshumidificación"; if(heat&&hum)return"Calentamiento con humidificación"; if(heat&&dehum)return"Calentamiento con deshumidificación"; if(cool&&hum)return"Enfriamiento con humidificación"; if(cool&&dehum)return"Enfriamiento con deshumidificación"; return"Sin cambio apreciable";}
    private static double H(double t,double w)=>1.006*t+w*(2501+1.86*t);
    private static double WFromPv(double pv,double pa)=>Math.Max(Wmin,Eps*pv/(pa-pv));
    private static double PvFromW(double w,double pa)=>pa*Math.Max(w,Wmin)/(Eps+Math.Max(w,Wmin));
    public static double Pws(double tdb){double t=tdb+273.15; double l=tdb<=0.01 ? -5.6745359e3/t+6.3925247-9.677843e-3*t+0.62215701e-6*t*t+2.0747825e-9*t*t*t-9.484024e-13*t*t*t*t+4.1635019*Math.Log(t) : -5.8002206e3/t+1.3914993-4.8640239e-2*t+4.1764768e-5*t*t-1.4452093e-8*t*t*t+6.5459673*Math.Log(t); return Math.Exp(l);}
    private static double Tdp(double pv,double upper){double lo=-80,hi=Math.Min(upper,100); for(int i=0;i<80;i++){double mid=(lo+hi)/2; if(Pws(mid)>pv)hi=mid; else lo=mid;} return (lo+hi)/2;}
    private double Twb(double t,double w,double pa){double h=H(t,w),lo=Tdp(PvFromW(w,pa),t),hi=t; for(int i=0;i<80;i++){double mid=(lo+hi)/2; if(H(mid,Ws(mid,pa))>h)hi=mid; else lo=mid;} return (lo+hi)/2;}
}

public sealed class ChartRenderer
{
    private const double Tmin=0,Tmax=50,Wmin=0,Wmax=0.030; private readonly PsychrometricCalculator calc=new();
    public void Draw(Canvas c,double pa,State? a,State? b){c.Children.Clear(); c.Background=Brushes.White; double w=Math.Max(c.ActualWidth,900),h=Math.Max(c.ActualHeight,560); Rect r=new(70,32,w-110,h-94); Rect(c,0,0,w,h,Brushes.White,Br("#DEE4EC")); Rect(c,r.Left,r.Top,r.Width,r.Height,Br("#FAFCFF"),Br("#CBD5E1")); Grid(c,r); Rh(c,r,pa); Sat(c,r,pa); if(a!=null&&b!=null&&Inside(a)&&Inside(b)) Line(c,P(r,a),P(r,b),Br("#DC2626"),2.4,true); if(a!=null) Point(c,r,a,Br("#3730A3")); if(b!=null) Point(c,r,b,Br("#166534")); Text(c,"Temperatura de bulbo seco, °C",r.Left+r.Width/2-100,r.Bottom+32,13,Br("#64748B")); Text(c,"Razón de humedad, g/kg aire seco",r.Right-210,r.Top-25,13,Br("#64748B"));}
    private static bool Inside(State s)=>s.Tdb>=Tmin&&s.Tdb<=Tmax&&s.W>=Wmin&&s.W<=Wmax;
    private void Grid(Canvas c,Rect r){for(double t=Tmin;t<=Tmax;t+=5){var p=P(r,t,Wmin); Line(c,new Point(p.X,r.Top),new Point(p.X,r.Bottom),Br("#E2E8F0"),1,false); Text(c,t.ToString("0"),p.X-8,r.Bottom+9,11,Br("#64748B"));} for(double w=Wmin;w<=Wmax+0.0001;w+=0.005){var p=P(r,Tmax,w); Line(c,new Point(r.Left,p.Y),new Point(r.Right,p.Y),Br("#E2E8F0"),1,false); Text(c,(w*1000).ToString("0"),r.Right+8,p.Y-8,11,Br("#64748B"));}}
    private void Rh(Canvas c,Rect r,double pa){for(double rh=.1;rh<=.9;rh+=.1){Polyline l=new(){Stroke=Br("#94A3B8"),StrokeThickness=.9,Opacity=.85}; for(double t=Tmin;t<=Tmax;t+=.5){double w=rh*calc.Ws(t,pa); if(w>=Wmin&&w<=Wmax)l.Points.Add(P(r,t,w));} c.Children.Add(l);}}
    private void Sat(Canvas c,Rect r,double pa){Polyline l=new(){Stroke=Br("#2563EB"),StrokeThickness=2.2}; for(double t=Tmin;t<=Tmax;t+=.25){double w=calc.Ws(t,pa); if(w<=Wmax)l.Points.Add(P(r,t,w));} c.Children.Add(l); Text(c,"100% HR",r.Left+30,r.Top+22,12,Br("#2563EB"));}
    private void Point(Canvas c,Rect r,State s,Brush br){if(!Inside(s))return; var p=P(r,s); Ellipse e=new(){Width=14,Height=14,Fill=Brushes.White,Stroke=br,StrokeThickness=3}; Canvas.SetLeft(e,p.X-7); Canvas.SetTop(e,p.Y-7); c.Children.Add(e); Text(c,s.Name,p.X+9,p.Y-17,13,br);}
    private static Point P(Rect r,State s)=>P(r,s.Tdb,s.W); private static Point P(Rect r,double t,double w)=>new(r.Left+(t-Tmin)/(Tmax-Tmin)*r.Width,r.Bottom-(w-Wmin)/(Wmax-Wmin)*r.Height);
    private static SolidColorBrush Br(string hex)=>(SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
    private static void Rect(Canvas c,double x,double y,double w,double h,Brush f,Brush s){var r=new Rectangle{Width=w,Height=h,Fill=f,Stroke=s,StrokeThickness=1};Canvas.SetLeft(r,x);Canvas.SetTop(r,y);c.Children.Add(r);} private static void Line(Canvas c,Point a,Point b,Brush br,double th,bool dash){var l=new Line{X1=a.X,Y1=a.Y,X2=b.X,Y2=b.Y,Stroke=br,StrokeThickness=th}; if(dash)l.StrokeDashArray=new DoubleCollection{7,4}; c.Children.Add(l);} private static void Text(Canvas c,string tx,double x,double y,double fs,Brush br){var t=new TextBlock{Text=tx,FontSize=fs,Foreground=br};Canvas.SetLeft(t,x);Canvas.SetTop(t,y);c.Children.Add(t);} }
