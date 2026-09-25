using System;
using System.Linq;
using System.Xml.Linq;
using System.Windows.Forms;
using System.Drawing;
namespace PlannedShutdown {
 static class Checks {
  static void Assert(bool value,string description){if(!value)throw new Exception(description);Console.WriteLine("PASS: "+description);}
  static System.Collections.Generic.IEnumerable<Control> Descendants(Control parent) {
   foreach(Control child in parent.Controls) {yield return child;foreach(var nested in Descendants(child))yield return nested;}
  }
  static void LayoutPreview(float factor,int width,string name) {
   using(var form=new MainForm(true)) {
    var fonts=Descendants(form).Select(c=>new {Control=c,Font=c.Font}).ToArray();
    foreach(var item in fonts)item.Control.Font=new Font(item.Font.FontFamily,item.Font.Size*factor,item.Font.Style);
    form.ClientSize=new Size(width,800);form.Show();Application.DoEvents();form.PerformLayout();Application.DoEvents();
    using(var shot=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(shot,new Rectangle(0,0,form.Width,form.Height));shot.Save("tests\\debug-"+name+".png");}
    foreach(Control c in Descendants(form)) {
     if(c is Label || c is Button || c is CheckBox) {
      Size preferred=c.GetPreferredSize(new Size(c.Width,0));
      if(c.Height<preferred.Height)throw new Exception(name+": testo tagliato: "+c.Text+" ("+c.Height+" < "+preferred.Height+")");
     }
     var grid=c as TableLayoutPanel;
     if(grid!=null) {
      var children=grid.Controls.Cast<Control>().ToArray();
      for(int i=0;i<children.Length;i++)for(int j=i+1;j<children.Length;j++)
       if(children[i].Bounds.IntersectsWith(children[j].Bounds))throw new Exception(name+": sovrapposizione fra "+children[i].Text+" "+children[i].Bounds+" e "+children[j].Text+" "+children[j].Bounds);
     }
    }
    var content=Descendants(form).First(c=>c.Name=="Content");
    using(var bmp=new Bitmap(content.Width,content.Height)){content.DrawToBitmap(bmp,new Rectangle(0,0,content.Width,content.Height));bmp.Save("tests\\"+name+".png");}
    var viewport=(Panel)content.Parent;
    Assert(!viewport.HorizontalScroll.Visible,name+": nessun taglio orizzontale");
    var last=content.Controls[content.Controls.Count-1];viewport.ScrollControlIntoView(last);Application.DoEvents();
    Assert(viewport.RectangleToScreen(viewport.ClientRectangle).Contains(last.RectangleToScreen(last.ClientRectangle)),name+": fondo raggiungibile");
    Assert(true,name+": testi interi e nessuna sovrapposizione");form.Close();
   }
  }
  [STAThread] static int Main(){try{
   Assert(Schedule.Arguments(false,false)=="/s /t 0","Spegnimento normale senza forzatura implicita");
   Assert(Schedule.Arguments(false,true)=="/s /t 0 /f","Spegnimento forzato");
   Assert(Schedule.Arguments(true,false)=="/h","Ibernazione normale");
   Assert(Schedule.Arguments(true,true)=="/h /f","Ibernazione forzata senza timeout non supportato");
   var now=new DateTime(2026,10,1,12,0,0);bool rejected=false;
   try{Schedule.Validate(now,now);}catch(ArgumentException){rejected=true;}
   Assert(rejected,"Rifiuto date presenti/passate");
   Schedule.Validate(now.AddMinutes(1),now);
   foreach(bool h in new[]{false,true}) foreach(bool f in new[]{false,true}) foreach(bool r in new[]{false,true}) {
    var xml=XDocument.Parse(Schedule.Xml(now.AddHours(1),h,f,r));var ns=Schedule.Ns;
    Assert(xml.Descendants(ns+"Exec").Count()==1 && xml.Descendants(ns+"Arguments").Single().Value==Schedule.Arguments(h,f),"Singola azione corretta");
    Assert(xml.Descendants(ns+"WakeToRun").Single().Value=="true" && xml.Descendants(ns+"DisallowStartIfOnBatteries").Single().Value=="false" && xml.Descendants(ns+"StopIfGoingOnBatteries").Single().Value=="false","Risveglio e alimentazione");
    Assert(xml.Descendants(ns+"StartWhenAvailable").Single().Value==(r?"true":"false"),"Recupero esplicito");
    dynamic service=Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service")); service.Connect();
    dynamic definition=service.NewTask(0); definition.XmlText=xml.ToString();
    Assert((string)definition.Actions.Item(1).Arguments==Schedule.Arguments(h,f),"XML accettato dal parser nativo di Windows");
    dynamic actual=service.NewTask(0);actual.XmlText=(string)definition.XmlText;
    Schedule.VerifyDefinition(actual,definition);
    Assert(true,"Verifica dopo serializzazione nativa Windows");
    var omitted=XDocument.Parse((string)actual.XmlText);
    if(!r)omitted.Descendants(ns+"StartWhenAvailable").Remove();
    actual.XmlText=omitted.ToString();Schedule.VerifyDefinition(actual,definition);
    Assert(true,"Valori predefiniti omessi dall'XML accettati");
    actual.Actions.Item(1).Arguments="/r /t 0";
    bool mismatch=false;try{Schedule.VerifyDefinition(actual,definition);}catch(InvalidOperationException){mismatch=true;}
    Assert(mismatch,"Comando diverso rifiutato dalla verifica");
    actual.XmlText=(string)definition.XmlText;actual.Settings.WakeToRun=false;
    mismatch=false;try{Schedule.VerifyDefinition(actual,definition);}catch(InvalidOperationException){mismatch=true;}
    Assert(mismatch,"Impostazione risveglio diversa rifiutata");

   }
   Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
   LayoutPreview(1,720,"layout-100");
   LayoutPreview(1.5f,720,"layout-150");
   LayoutPreview(2,720,"layout-200");
   LayoutPreview(2,520,"layout-200-narrow");
   Console.WriteLine("Verifiche completate. Nessun comando di spegnimento eseguito, nessun piano registrato.");return 0;
  }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 }
}
