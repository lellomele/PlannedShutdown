using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PlannedShutdown {
 static class AppInfo {
  internal const string Version="1.3";
  internal const string Copyright="(C) 2026 Prof. ing. Raffaele Mele";
 }
 static class Schedule {
  internal const string Name = "PlannedShutdown-6F3129A1";
  internal static readonly XNamespace Ns = "http://schemas.microsoft.com/windows/2004/02/mit/task";
  internal static string Arguments(bool hibernate, bool force) { return (hibernate ? "/h" : "/s /t 0") + (force ? " /f" : ""); }
  internal static DateTime Validate(DateTime when, DateTime now) {
   if (when <= now.AddSeconds(15)) throw new ArgumentException("Scegli un orario almeno 15 secondi nel futuro.");
   if (TimeZoneInfo.Local.IsInvalidTime(when)) throw new ArgumentException("Questo orario non esiste a causa del cambio dell'ora legale.");
   if (TimeZoneInfo.Local.IsAmbiguousTime(when)) throw new ArgumentException("Questo orario è ambiguo per il cambio dell'ora legale. Scegli un altro orario.");
   return when;
  }
  internal static string Xml(DateTime when, bool hibernate, bool force, bool recover) {
   return new XDocument(new XElement(Ns+"Task",new XAttribute("version","1.2"),
    new XElement(Ns+"RegistrationInfo",new XElement(Ns+"Description","Planned Shutdown • " + (hibernate?"Ibernazione":"Spegnimento") + (force?" forzato":""))),
    new XElement(Ns+"Triggers",new XElement(Ns+"TimeTrigger",new XElement(Ns+"StartBoundary",when.ToString("yyyy-MM-ddTHH:mm:sszzz",CultureInfo.InvariantCulture)),new XElement(Ns+"Enabled",true))),
    new XElement(Ns+"Principals",new XElement(Ns+"Principal",new XAttribute("id","System"),new XElement(Ns+"UserId","S-1-5-18"),new XElement(Ns+"RunLevel","HighestAvailable"))),
    new XElement(Ns+"Settings",new XElement(Ns+"MultipleInstancesPolicy","IgnoreNew"),new XElement(Ns+"DisallowStartIfOnBatteries",false),new XElement(Ns+"StopIfGoingOnBatteries",false),new XElement(Ns+"AllowHardTerminate",false),new XElement(Ns+"StartWhenAvailable",recover),new XElement(Ns+"RunOnlyIfNetworkAvailable",false),new XElement(Ns+"IdleSettings",new XElement(Ns+"StopOnIdleEnd",false),new XElement(Ns+"RestartOnIdle",false)),new XElement(Ns+"AllowStartOnDemand",false),new XElement(Ns+"Enabled",true),new XElement(Ns+"Hidden",false),new XElement(Ns+"RunOnlyIfIdle",false),new XElement(Ns+"WakeToRun",true),new XElement(Ns+"ExecutionTimeLimit","PT5M"),new XElement(Ns+"Priority",4)),
    new XElement(Ns+"Actions",new XAttribute("Context","System"),new XElement(Ns+"Exec",new XElement(Ns+"Command",System.IO.Path.Combine(Environment.SystemDirectory,"shutdown.exe")),new XElement(Ns+"Arguments",Arguments(hibernate,force)))))).ToString();
  }
  internal static dynamic Folder() { dynamic service=Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service")); service.Connect(); return service.GetFolder("\\"); }
  internal static dynamic Read() {
   dynamic folder=Folder(); try { return folder.GetTask(Name); }
   catch(Exception ex) { if(unchecked((uint)ex.HResult)==0x80070002) return null; throw; }
  }
  internal static void Save(string xml) {
   dynamic folder=Folder();
   // Validate with Windows before creating/replacing the single persistent task.
   folder.RegisterTask(Name,xml,1,"SYSTEM",null,5,null);
   folder.RegisterTask(Name,xml,6,"SYSTEM",null,5,null);
   try {
    dynamic task=folder.GetTask(Name);
    dynamic service=Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service"));service.Connect();
    dynamic expected=service.NewTask(0);expected.XmlText=xml;
    VerifyDefinition(task.Definition,expected);
    if(!(bool)task.Enabled) throw new InvalidOperationException("La pianificazione risulta disabilitata.");
   } catch(Exception e) {throw new SavedPlanVerificationException(e);}
  }
  // Read effective properties from Windows; XML may omit default-valued settings.
  internal static void VerifyDefinition(dynamic actual,dynamic expected) {
   if((int)actual.Triggers.Count!=1 || (int)actual.Actions.Count!=1) throw new InvalidOperationException("Numero di azioni o orari inatteso.");
   if((int)actual.Triggers.Item(1).Type!=1 || (int)actual.Actions.Item(1).Type!=0) throw new InvalidOperationException("Tipo di azione o orario inatteso.");
   DateTimeOffset actualTime,expectedTime;
   if(!DateTimeOffset.TryParse((string)actual.Triggers.Item(1).StartBoundary,CultureInfo.InvariantCulture,DateTimeStyles.None,out actualTime) ||
      !DateTimeOffset.TryParse((string)expected.Triggers.Item(1).StartBoundary,CultureInfo.InvariantCulture,DateTimeStyles.None,out expectedTime) || actualTime!=expectedTime)
    throw new InvalidOperationException("L'orario registrato non corrisponde a quello richiesto.");
   if(!String.Equals((string)actual.Actions.Item(1).Path,(string)expected.Actions.Item(1).Path,StringComparison.OrdinalIgnoreCase) ||
      (string)actual.Actions.Item(1).Arguments!=(string)expected.Actions.Item(1).Arguments)
    throw new InvalidOperationException("Il comando registrato non corrisponde a quello richiesto.");
   if((bool)actual.Settings.StartWhenAvailable!=(bool)expected.Settings.StartWhenAvailable ||
      (bool)actual.Settings.WakeToRun!=(bool)expected.Settings.WakeToRun ||
      (bool)actual.Settings.DisallowStartIfOnBatteries!=(bool)expected.Settings.DisallowStartIfOnBatteries ||
      (bool)actual.Settings.StopIfGoingOnBatteries!=(bool)expected.Settings.StopIfGoingOnBatteries ||
      !(bool)actual.Settings.Enabled || !(bool)actual.Triggers.Item(1).Enabled)
    throw new InvalidOperationException("Le impostazioni registrate non corrispondono a quelle richieste.");
  }
  internal static void Cancel() { Folder().DeleteTask(Name,0); }
 }
 internal class SavedPlanVerificationException:Exception {
  internal SavedPlanVerificationException(Exception inner):base("Windows ha registrato il piano, ma la verifica finale non è stata completata. Il piano potrebbe essere attivo. Per impedirne l'esecuzione usa Annulla piano.\n\nDettaglio: "+inner.Message,inner) {}
 }
 static class Program {
  [STAThread] static void Main() {
   System.Threading.Thread.CurrentThread.CurrentCulture=CultureInfo.GetCultureInfo("it-IT");
   System.Threading.Thread.CurrentThread.CurrentUICulture=CultureInfo.GetCultureInfo("it-IT");
   Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
   Application.ThreadException += delegate(object sender,System.Threading.ThreadExceptionEventArgs e) { MessageBox.Show(e.Exception.Message,"Planned Shutdown",MessageBoxButtons.OK,MessageBoxIcon.Error); };
   Application.Run(new MainForm(false));
  }
 }
 internal class ContrastButton:Button {
  protected override void OnPaint(PaintEventArgs e) {
   base.OnPaint(e);
   if(!Enabled) {
    using(var brush=new SolidBrush(BackColor)) e.Graphics.FillRectangle(brush,ClientRectangle);
    ControlPaint.DrawBorder(e.Graphics,ClientRectangle,Color.FromArgb(85,111,135),ButtonBorderStyle.Solid);
    TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,Color.FromArgb(159,177,193),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine);
   }
  }
 }
 internal class AdaptiveComboBox:ComboBox {
  public override Size GetPreferredSize(Size proposedSize) {
   Size size=base.GetPreferredSize(proposedSize);size.Height=Math.Max(size.Height,PreferredHeight);return size;
  }
 }
 internal class MainForm:Form {
  [DllImport("powrprof.dll")] [return:MarshalAs(UnmanagedType.U1)] static extern bool IsPwrHibernateAllowed();
  readonly Color muted=Color.FromArgb(159,177,193), mint=Color.FromArgb(64,217,192);
  DateTimePicker date=new DateTimePicker(), time=new DateTimePicker();
  ComboBox action=new AdaptiveComboBox(); CheckBox force=new CheckBox(), recover=new CheckBox();
  Label status, countdown, details; Button cancel; Timer timer=new Timer();
  DateTime? scheduled; bool preview; bool verificationWarning;
  internal MainForm(bool previewMode) {
   SuspendLayout();
   preview=previewMode; Text="Planned Shutdown";
   AutoScaleDimensions=new SizeF(96,96); AutoScaleMode=AutoScaleMode.Dpi;
   BackColor=Color.FromArgb(16,28,42); ForeColor=Color.White; Font=new Font("Segoe UI",10);
   ClientSize=new Size(720,800); MinimumSize=new Size(520,440);
   FormBorderStyle=FormBorderStyle.Sizable; StartPosition=FormStartPosition.CenterScreen;
   using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("AppIcon")) using(var icon=new Icon(stream)) Icon=(Icon)icon.Clone();
   Image artwork;
   using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("AppArtwork")) using(var original=Image.FromStream(stream)) artwork=new Bitmap(original);
   // Only the outer viewport scrolls. Every content row derives its height from its controls.
   var viewport=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(24)};
   Controls.Add(viewport);
   var body=Stack(); body.Dock=DockStyle.Top; body.Name="Content"; viewport.Controls.Add(body);
   var header=new TableLayoutPanel{AutoSize=true,Dock=DockStyle.Fill,ColumnCount=2,Margin=new Padding(0,0,0,22)};
   header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
   header.Controls.Add(new PictureBox{Size=new Size(52,52),Margin=new Padding(0,4,16,0),Image=artwork,SizeMode=PictureBoxSizeMode.Zoom},0,0);
   var heading=Stack(); AddRow(heading,TextLabel("Planned Shutdown",23,Color.White,0));
   AddRow(heading,TextLabel("Il tuo PC, al momento giusto.",10,muted,0));header.Controls.Add(heading,1,0);AddRow(body,header);
   AddRow(body,TextLabel("01  /  QUANDO",10,mint,12));
   var fields=new TableLayoutPanel{AutoSize=true,Dock=DockStyle.Fill,ColumnCount=2,Margin=new Padding(0,0,0,12)};
   fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,60));fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,40));
   var dateField=Stack();dateField.Margin=new Padding(0,0,16,0);var timeField=Stack();
   AddRow(dateField,TextLabel("Data",10,muted,5));AddRow(timeField,TextLabel("Ora",10,muted,5));
   // A compact, unambiguous format remains readable even in a narrow window.
   date.Format=DateTimePickerFormat.Custom;date.CustomFormat="dd / MM / yyyy";date.Dock=DockStyle.Top;date.Margin=Padding.Empty;
   time.Format=DateTimePickerFormat.Custom;time.CustomFormat="HH:mm:ss";time.ShowUpDown=true;time.Dock=DockStyle.Top;time.Margin=Padding.Empty;
   AddRow(dateField,date);AddRow(timeField,time);fields.Controls.Add(dateField,0,0);fields.Controls.Add(timeField,1,0);AddRow(body,fields);SetTime(DateTime.Now);
   var shortcuts=ButtonRow();shortcuts.Margin=new Padding(0,0,0,22);
   shortcuts.Controls.Add(ActionButton("Adesso",delegate{SetTime(DateTime.Now);},false));
   shortcuts.Controls.Add(ActionButton("+15 minuti",delegate{SetTime(DateTime.Now.AddMinutes(15));},false));
   shortcuts.Controls.Add(ActionButton("+1 ora",delegate{SetTime(DateTime.Now.AddHours(1));},false));AddRow(body,shortcuts);
   AddRow(body,TextLabel("02  /  COSA FARE",10,mint,12));
   action.AutoSize=true;action.DropDownStyle=ComboBoxStyle.DropDownList;action.Items.AddRange(new object[]{"Spegni il PC","Iberna il PC"});action.SelectedIndex=0;action.Dock=DockStyle.Top;action.Margin=new Padding(0,0,0,16);AddRow(body,action);
   force.Text="Forza l'operazione";force.AutoSize=true;force.Dock=DockStyle.Fill;force.Margin=new Padding(0,0,0,6);AddRow(body,force);
   AddRow(body,TextLabel("La forzatura può causare la perdita di dati non salvati.",9,Color.FromArgb(255,188,102),18));
   recover.Text="Recupera gli orari saltati";recover.AutoSize=true;recover.Dock=DockStyle.Fill;recover.Margin=new Padding(0,0,0,6);AddRow(body,recover);
   AddRow(body,TextLabel("Se attivo, il PC potrà spegnersi o ibernarsi al successivo avvio/risveglio.",9,muted,20));
   var commands=ButtonRow();commands.Margin=new Padding(0,0,0,20);
   commands.Controls.Add(ActionButton("Pianifica",delegate{Plan();},true));
   cancel=ActionButton("Annulla piano",delegate{CancelPlan();},false);commands.Controls.Add(cancel);AddRow(body,commands);
   var state=Stack();state.BackColor=Color.FromArgb(23,39,56);state.Padding=new Padding(16);state.Margin=new Padding(0,0,0,16);
   status=TextLabel("Nessuna pianificazione attiva",12,Color.White,8);AddRow(state,status);
   countdown=TextLabel("Scegli data, ora e operazione.",11,mint,8);AddRow(state,countdown);
   details=TextLabel("",9,muted,0);AddRow(state,details);AddRow(body,state);
   var footer=ButtonRow();footer.Controls.Add(ActionButton("Diagnostica e affidabilità",async delegate{await Diagnostics();},false));AddRow(body,footer);
   AddRow(body,TextLabel(AppInfo.Copyright,9,muted,6));
   AddRow(body,TextLabel("WINDOWS  •  PIANO PERSISTENTE",8,muted,0));
   if(!preview) {RefreshStatus();timer.Interval=1000;timer.Tick+=delegate{UpdateCountdown();};timer.Start();}
   else {cancel.Enabled=false;details.Text="Funziona anche con l'app chiusa. Risveglio richiesto a Windows.";}
   FormClosed+=delegate{timer.Dispose();artwork.Dispose();};
   ResumeLayout(true);
   Shown+=delegate {
    Rectangle work=Screen.FromControl(this).WorkingArea;
    Size=new Size(Math.Min(Width,work.Width),Math.Min(Height,work.Height));
    Location=new Point(work.Left+(work.Width-Width)/2,work.Top+(work.Height-Height)/2);
   };
  }
  static TableLayoutPanel Stack() {
   var table=new TableLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Dock=DockStyle.Fill,ColumnCount=1,RowCount=0,Margin=Padding.Empty,Padding=Padding.Empty};
   table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));return table;
  }
  static void AddRow(TableLayoutPanel table,Control child) {
   int row=table.RowCount++;table.RowStyles.Add(new RowStyle(SizeType.AutoSize));table.Controls.Add(child,0,row);
  }
  Label TextLabel(string text,float size,Color color,int gap) {
   return new Label{Text=text,AutoSize=true,Dock=DockStyle.Fill,Margin=new Padding(0,0,0,gap),Font=new Font("Segoe UI",size),ForeColor=color,UseMnemonic=false};
  }
  static FlowLayoutPanel ButtonRow() {
   return new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Dock=DockStyle.Fill,WrapContents=true,Margin=new Padding(0,0,0,10)};
  }
  Button ActionButton(string text,EventHandler click,bool primary) {
   var button=new ContrastButton{Text=text,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Padding=new Padding(16,8,16,8),Margin=new Padding(0,0,10,8),FlatStyle=FlatStyle.Flat,BackColor=primary?mint:Color.FromArgb(31,48,66),ForeColor=primary?Color.FromArgb(16,28,42):Color.White,Cursor=Cursors.Hand,UseVisualStyleBackColor=false};
   button.FlatAppearance.BorderColor=Color.FromArgb(85,111,135);button.FlatAppearance.BorderSize=1;button.Click+=click;return button;
  }
  void SetTime(DateTime value) {date.Value=value; time.Value=value;}
  void Plan() {try {
   DateTime when=Schedule.Validate(date.Value.Date+time.Value.TimeOfDay,DateTime.Now);
   bool hibernate=action.SelectedIndex==1;
   if(hibernate&&!IsPwrHibernateAllowed()) throw new InvalidOperationException("L'ibernazione non è disponibile su questo PC. Consulta Diagnostica e affidabilità per i dettagli di Windows.");
   string summary=(hibernate?"Ibernazione":"Spegnimento") + (force.Checked?" FORZATO":"") + "\n"+when.ToString("dddd dd MMMM yyyy 'alle' HH:mm:ss")+"\n\nIl piano resta attivo anche chiudendo l'app.";
   if(Schedule.Read()!=null) summary+="\nSostituirà il piano precedente.";
   if(force.Checked) summary+="\n\nATTENZIONE: i dati non salvati potrebbero andare persi.";
   if(recover.Checked) summary+="\n\nSe l'orario viene saltato, l'operazione sarà eseguita appena Windows potrà recuperarla, anche al prossimo avvio.";
   if(MessageBox.Show(this,summary,"Conferma pianificazione",MessageBoxButtons.OKCancel,MessageBoxIcon.Warning)!=DialogResult.OK) return;
   Schedule.Validate(when,DateTime.Now);
   Schedule.Save(Schedule.Xml(when,hibernate,force.Checked,recover.Checked)); verificationWarning=false; RefreshStatus();
  }catch(SavedPlanVerificationException e){
   verificationWarning=true;RefreshStatus();
   MessageBox.Show(this,e.Message,"Piano registrato: verifica incompleta",MessageBoxButtons.OK,MessageBoxIcon.Warning);
  }catch(Exception e){ShowError(e);} }
  void CancelPlan() {try {
   if(MessageBox.Show(this,"Rimuovere la pianificazione? L'annullamento è possibile solo prima che Windows avvii l'operazione.","Annulla piano",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes) return;
   Schedule.Cancel(); RefreshStatus();
  }catch(Exception e){ShowError(e);} }
  int ticks;
  void UpdateCountdown() {
   if(++ticks%10==0) {RefreshStatus(); return;}
   if(!scheduled.HasValue) return;
   TimeSpan left=scheduled.Value-DateTime.Now;
   if(left.TotalSeconds>0) countdown.Text="Tra " + (int)left.TotalDays + " g  " + left.Hours.ToString("00")+" h  "+left.Minutes.ToString("00")+" min  "+left.Seconds.ToString("00")+" s";
   else countdown.Text="Orario raggiunto • consulta l'esito di Windows qui sotto.";
  }
  void RefreshStatus() {try {
   dynamic task=Schedule.Read(); cancel.Enabled=task!=null; scheduled=null;
   if(task==null) {verificationWarning=false;status.Text="Nessuna pianificazione attiva";countdown.Text="Scegli data, ora e operazione.";details.Text="Funziona anche con l'app chiusa. Risveglio richiesto a Windows.";return;}
   var xml=XDocument.Parse((string)task.Xml); string args=xml.Descendants(Schedule.Ns+"Arguments").First().Value;
   DateTime when=DateTimeOffset.Parse(xml.Descendants(Schedule.Ns+"StartBoundary").First().Value,CultureInfo.InvariantCulture).LocalDateTime;
   bool enabled=(bool)task.Enabled;
   status.Text=(enabled?"Piano salvato: ":"Piano disabilitato: ")+(args.Contains("/h")?"ibernazione":"spegnimento")+(args.Contains("/f")?" forzato":"");
   if(verificationWarning) status.Text="Piano registrato • verifica incompleta";
   if(enabled) scheduled=when;
   DateTime last=(DateTime)task.LastRunTime; int result=(int)task.LastTaskResult;
   details.Text=when.ToString("dd/MM/yyyy HH:mm:ss")+" • anche a batteria • risveglio richiesto\n";
   details.Text+=last.Year>2000?"Ultimo avvio: "+last.ToString("dd/MM HH:mm")+" • codice Windows: 0x"+result.ToString("X8")+" (non prova lo spegnimento)":"Windows non ha ancora eseguito questo piano.";
   if(!enabled) countdown.Text="Riattiva il piano con una nuova pianificazione.";
   else { ticks=0;UpdateCountdown(); }
  } catch(Exception e){status.Text="Impossibile leggere il piano";countdown.Text="Verifica il servizio Utilità di pianificazione.";details.Text=e.Message;cancel.Enabled=false;scheduled=null;} }
  void ShowError(Exception e){MessageBox.Show(this,e.Message,"Operazione non completata",MessageBoxButtons.OK,MessageBoxIcon.Error);}
  static string RunInfo(string args) {
   using(var p=new Process{StartInfo=new ProcessStartInfo(System.IO.Path.Combine(Environment.SystemDirectory,"powercfg.exe"),args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true}}) {
    p.Start(); var output=p.StandardOutput.ReadToEndAsync(); var error=p.StandardError.ReadToEndAsync();
    if(!p.WaitForExit(10000)){p.Kill();return "Diagnostica scaduta.";} return output.Result+error.Result;
   }
  }
  async Task Diagnostics() {
   string info=await Task.Run(()=>RunInfo("/a")+"\r\nTIMER DI RIATTIVAZIONE (AC = rete, DC = batteria)\r\n"+RunInfo("/query SCHEME_CURRENT SUB_SLEEP RTCWAKE"));
   if(IsDisposed) return;
   using(var dialog=new Form{Text="Diagnostica e affidabilità",Size=new Size(760,670),StartPosition=FormStartPosition.CenterParent,Icon=Icon}) {
    var text=new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,Font=new Font("Segoe UI",10),BackColor=Color.White};
    text.Text="Il piano viene eseguito da Windows come SYSTEM: l'app può essere chiusa e non è necessario rimanere connessi.\r\n\r\n"+
     "Nessuna app può garantire lo spegnimento se Windows è bloccato, manca alimentazione o il firmware impedisce il risveglio. Il PC spento non può essere acceso da questa app. Il risveglio richiede supporto hardware e timer abilitati nel piano energetico, anche a batteria.\r\n\r\n"+
     "Senza forzatura, le applicazioni possono impedire lo spegnimento. Con forzatura, i dati non salvati possono andare persi. Per ibernare è necessario che l'ibernazione sia disponibile: un amministratore può abilitarla con powercfg /hibernate on. L'app non cambia queste impostazioni automaticamente.\r\n\r\n"+
     "Il recupero di un orario saltato è facoltativo: Windows potrebbe eseguirlo con ritardo al prossimo avvio. I codici di esito indicano il risultato del comando, non certificano che l'hardware sia effettivamente spento.\r\n\r\n"+info;
    dialog.Controls.Add(text);dialog.ShowDialog(this);
   }
  }
 }
}
