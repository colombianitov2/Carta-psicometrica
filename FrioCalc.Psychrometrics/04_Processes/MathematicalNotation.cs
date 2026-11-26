using System;
using System.Text.RegularExpressions;

namespace FrioCalc.Psychrometrics.Processes;

// [4.20] Presentación matemática. Las divisiones explícitas se delimitan
// con ⟦numerador¦denominador⟧ para no confundirlas con unidades como kJ/kg.
public static class MathematicalNotation
{
    private static readonly (string From,string To)[] Ratios =
    {
        ("(10000/3600)/v1","⟦⟦10000¦3600⟧¦v1⟧"),
        ("(3000/3600)/v_i","⟦⟦3000¦3600⟧¦v_i⟧"),
        ("(3000/3600)/","⟦3000¦3600⟧/"),
        ("3000/(3600·v1)","⟦3000¦3600·v1⟧"),
        ("500/(3600·v2)","⟦500¦3600·v2⟧"),
        ("RSH/[cp_da,mezcla·(T_local−T_imp)]","⟦RSH¦cp_da,mezcla·(T_local−T_imp)⟧"),
        ("RSH/[cp_s·(T_R−T_S)]","⟦RSH¦cp_s·(T_R−T_S)⟧"),
        ("Q̇coil/[cp_agua·(12−6)]","⟦Q̇coil¦cp_agua·(12−6)⟧"),
        ("Q̇_serpentín/[4,186·(12−6)]","⟦Q̇_serpentín¦4,186·(12−6)⟧"),
        ("(ṁ1W1+ṁ2W2)/(ṁ1+ṁ2)","⟦ṁ1·W1+ṁ2·W2¦ṁ1+ṁ2⟧"),
        ("(ṁ1h1+ṁ2h2)/(ṁ1+ṁ2)","⟦ṁ1·h1+ṁ2·h2¦ṁ1+ṁ2⟧"),
        ("(h3−2501·W3)/(1,006+1,86·W3)","⟦h3−2501·W3¦1,006+1,86·W3⟧"),
        ("0,621945·p_v/(P−p_v)","⟦0,621945·p_v¦P−p_v⟧"),
        ("0,621945·Pv/(P−Pv)","⟦0,621945·p_v¦P−p_v⟧"),
        ("0,621945·p_ws(T)/(P−p_ws(T))","⟦0,621945·p_ws(T)¦P−p_ws(T)⟧"),
        ("P·W/(0,621945+W)","⟦P·W¦0,621945+W⟧"),
        ("100·p_v/p_ws(Tbs)","⟦100·p_v¦p_ws(Tbs)⟧"),
        ("100·p_v/p_ws(T2)","⟦100·p_v¦p_ws(T2)⟧"),
        ("Pv/Pws(Tbs)·100","⟦100·p_v¦p_ws(Tbs)⟧"),
        ("R_da·T_K·(1+1,607858·W)/P","⟦R_da·T_K·(1+1,607858·W)¦P⟧"),
        ("R_da·T_K·(1+1,607858W)/P","⟦R_da·T_K·(1+1,607858·W)¦P⟧"),
        ("Q_s,local/Q_total,local","⟦Q̇_{s,local}¦Q̇_{total,local}⟧"),
        ("Q_s,global/Q_total,global","⟦Q̇_{s,global}¦Q̇_{total,global}⟧"),
        ("Q_sensible/Q_total","⟦Q̇_{sensible}¦Q̇_{total}⟧"),
        ("Q sensible/Q total","⟦Q̇_{sensible}¦Q̇_{total}⟧"),
        ("Σ(ṁda·W)/Σṁda","⟦Σ(ṁda·W)¦Σṁda⟧"),
        ("Σ(ṁda·h)/Σṁda","⟦Σ(ṁda·h)¦Σṁda⟧"),
        ("Σ(ṁda W)/Σṁda","⟦Σ(ṁda·W)¦Σṁda⟧"),
        ("Σ(ṁda h)/Σṁda","⟦Σ(ṁda·h)¦Σṁda⟧"),
        ("Σṁi·Wi/Σṁi","⟦Σ(ṁ_i·W_i)¦Σṁ_i⟧"),
        ("Σṁi·hi/Σṁi","⟦Σ(ṁ_i·h_i)¦Σṁ_i⟧"),
        ("ṁagua/ṁda","⟦ṁ_{agua}¦ṁ_{da}⟧"),
        ("m_cond/m_da","⟦m_cond¦m_da⟧"),
        ("m_agua/m_da","⟦m_agua¦m_da⟧"),
        ("m_agua,ṁ/ṁda","⟦ṁ_{agua}¦ṁ_{da}⟧"),
        ("masa de agua retirada/masa de aire seco","⟦masa de agua retirada¦masa de aire seco⟧"),
        ("(Tbs1−Tbs2)/(Tbs1−Tbh1)","⟦Tbs1−Tbs2¦Tbs1−Tbh1⟧"),
        ("300·8/1000","⟦300·8¦1000⟧"),
        ("V̇_i/v_i","⟦V̇_i¦v_i⟧"),
        ("V̇/v_ext","⟦V̇¦v_ext⟧"),
        ("V̇/v1","⟦V̇¦v1⟧"),
        ("V̇/v","⟦V̇¦v⟧"),
        ("5/v1","⟦5¦v1⟧"),
        ("150/200","⟦150¦200⟧"),
        ("ln(T/T*)","ln(⟦T¦T*⟧)"),
        ("ln(p_da/p_da*)","ln(⟦p_da¦p_da*⟧)"),
        ("ln(p_v/p_v*)","ln(⟦p_v¦p_v*⟧)"),
    };
    private static readonly (string From,string To)[] Symbols =
    {
        ("cp_da,mezcla","c_{p,da,mezcla}"),
        ("c_p,da","c_{p,da}"),
        ("c_p,v","c_{p,v}"),
        ("cp_da","c_{p,da}"),
        ("cp_v","c_{p,v}"),
        ("cp_s","c_{p,s}"),
        ("cp_agua","c_{p,agua}"),
        ("p_da,ref","p_{da,ref}"),
        ("p_v,ref","p_{v,ref}"),
        ("p da,ref","p_{da,ref}"),
        ("p v,ref","p_{v,ref}"),
        ("T ref","T_{ref}"),
        ("ṁ da","ṁ_{da}"),
        ("ṁ cond","ṁ_{cond}"),
        ("h liq","h_{liq}"),
        ("h agua","h_{agua}"),
        ("W ADP","W_{ADP}"),
        ("R da","R_{da}"),
        ("p da","p_{da}"),
        ("c p,agua","c_{p,agua}"),
        ("T bh,1","T_{bh,1}"),
        ("ṁda,total","ṁ_{da,total}"),
        ("ṁda,total","ṁ_{da,total}"),
        ("ṁda,1","ṁ_{da,1}"),
        ("ṁda,2","ṁ_{da,2}"),
        ("ṁda,entrada","ṁ_{da,entrada}"),
        ("ṁda,salida","ṁ_{da,salida}"),
        ("ṁda1","ṁ_{da,1}"),
        ("ṁda2","ṁ_{da,2}"),
        ("ṁda","ṁ_{da}"),
        ("ṁda","ṁ_{da}"),
        ("ṁcond","ṁ_{cond}"),
        ("ṁcond","ṁ_{cond}"),
        ("ṁagua","ṁ_{agua}"),
        ("Q̇aire","Q̇_{aire}"),
        ("Q̇coil","Q̇_{coil}"),
        ("Wmix","W_{mix}"),
        ("hmix","h_{mix}"),
        ("WADP","W_{ADP}"),
        ("hADP","h_{ADP}"),
        ("T_K","T_{K}"),
        ("W_sat","W_{sat}"),
        ("h_sat","h_{sat}"),
        ("Tbs1","T_{bs,1}"),
        ("Tbs2","T_{bs,2}"),
        ("Tbs3","T_{bs,3}"),
        ("Tbh1","T_{bh,1}"),
        ("Trocío1","T_{rocío,1}"),
        ("P_intermedio","P_{intermedio}"),
        ("ṁ_agua fría","ṁ_{agua fría}"),
        ("ṁ_da","ṁ_{da}"),
        ("ṁ_cond","ṁ_{cond}"),
        ("ṁ_i","ṁ_{i}"),
        ("ṁi","ṁ_{i}"),
    };
    public static string Prepare(string text)
    {
        // No tocar URLs ni rutas bibliográficas.
        return Regex.Replace(text,@"https?://[^\s]+|[^\r\n]+",match=>
        {
            if(match.Value.StartsWith("https://",StringComparison.Ordinal)||match.Value.StartsWith("http://",StringComparison.Ordinal))return match.Value;
            // Los URL que aparezcan al final de una línea se preservan.
            int url=match.Value.IndexOf("https://",StringComparison.Ordinal);
            if(url<0)url=match.Value.IndexOf("http://",StringComparison.Ordinal);
            string body=url<0?match.Value:match.Value[..url];
            foreach(var (from,to) in Ratios)body=body.Replace(from,to,StringComparison.Ordinal);
            return NormalizeSymbols(body)+(url<0?"":match.Value[url..]);
        });
    }

    public static string NormalizeSymbols(string text)
    {
        foreach(var (from,to) in Symbols)text=text.Replace(from,to,StringComparison.Ordinal);
        // Índices compactos del material docente: h1, W2, HR3, etc.
        text=Regex.Replace(text,@"(?<![\p{L}\p{N}_])(?:HR|Tbs|Tbh|Tdp|[ThWvtpṁm])([123])(?=$|[^\p{L}\p{N}_])",
            m=>m.Value[..^1]+"_{"+m.Groups[1].Value+"}");
        text=Regex.Replace(text,@"_(?!\{)([\p{L}][\p{L}\p{N}]*(?:,[\p{L}\p{N}]+)*)",
            m=>"_{"+m.Groups[1].Value+"}");
        // Superíndices de las correlaciones de saturación, sin modificar unidades.
        text=Regex.Replace(text,@"(10|T)([⁻⁺⁰¹²³⁴⁵⁶⁷⁸⁹]+)",m=>
        {
            const string old="⁻⁺⁰¹²³⁴⁵⁶⁷⁸⁹",plain="−+0123456789";
            string power="";
            foreach(char ch in m.Groups[2].Value)power+=plain[old.IndexOf(ch)];
            return m.Groups[1].Value+"^{"+power+"}";
        });
        return text;
    }
}
