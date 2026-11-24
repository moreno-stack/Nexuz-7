using System;
using Nexus7.Director;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static void Main(){
    string header = TelemetryModel.Header();
    Check(header.StartsWith("fecha;fase;dificultad") && header.Split(';').Length==TelemetryModel.Columns.Length, "Cabecera con una columna por dato, separada por «;»");
    var r = new PhaseReportModel("Fase 2 · Almacén de carga");
    for (int i=0;i<120;i++) r.Sample(0.5f, 0.55f, 0.1f);
    for (int i=0;i<4;i++) r.RecordShot("pistola_plasma");
    r.RecordHit(1); r.RecordKill(); r.RecordLifeLost(); r.RecordHeatGuard();
    string row = TelemetryModel.Row(r, new DateTime(2026,11,24,6,30,5), "Normal", 1);
    var cols = row.Split(';');
    Check(cols.Length==TelemetryModel.Columns.Length, "La fila tiene tantas columnas como la cabecera");
    Check(cols[0]=="2026-11-24 06:30:05" && cols[1]=="Fase 2 · Almacén de carga" && cols[2]=="Normal" && cols[3]=="1", "Fecha, fase, dificultad y reinicios");
    Check(cols[4]=="60" && cols[5]=="1" && cols[8]=="4" && cols[9]=="1" && cols[10]=="0.25", "Duración, vidas, disparos, aciertos y precisión (punto decimal)");
    Check(cols[13]=="pistola_plasma" && cols[14]=="1" && cols[16]=="0.55", "Arma dominante, su uso y la tensión media");
    Check(cols[23]=="1" && cols[24]==PhaseReportModel.StyleName(r.Style), "Vigilancias y estilo");
    Check(TelemetryModel.Field("a;b")=="\"a;b\"" && TelemetryModel.Field("di \"hola\"")=="\"di \"\"hola\"\"\"", "Escapa «;» y comillas");
    Check(TelemetryModel.Field(1.23456f)=="1.23" && TelemetryModel.Field(-1f)=="-1" && TelemetryModel.Field(null)=="", "Números redondeados a 2 decimales y vacíos");
    var empty = new PhaseReportModel("x");
    Check(TelemetryModel.Row(empty, DateTime.MinValue, "Facil", 0).Split(';')[10]=="-1", "Sin disparos la precisión es −1 (sin dato)");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
