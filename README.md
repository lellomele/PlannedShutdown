# Planned Shutdown

Applicazione Windows in italiano per pianificare lo spegnimento o l'ibernazione del PC, anche in modalità forzata.

## Installazione e avvio

Scaricare il pacchetto dalla sezione [Releases](https://github.com/lellomele/PlannedShutdown/releases/latest), estrarlo e aprire l'eseguibile, accettando la richiesta di autorizzazione amministratore.

L'app è portabile e non richiede installazione. Utilizza .NET Framework 4.x, incluso in Windows 10/11. L'eseguibile non è firmato digitalmente.

## Utilizzo

1. Selezionare data e ora, inizialmente impostate al momento corrente. L'orario deve essere almeno 15 secondi nel futuro; i pulsanti **+15 minuti** e **+1 ora** permettono una selezione rapida.
2. Scegliere **Spegni il PC** oppure **Iberna il PC**.
3. Attivare, se necessario, **Forza l'operazione** o **Recupera gli orari saltati**.
4. Premere **Pianifica** e confermare il riepilogo.

Il nuovo piano sostituisce quello precedente. La finestra mostra il conto alla rovescia e l'esito restituito da Windows.

**Annulla piano** rimuove la pianificazione prima dell'esecuzione; non può fermare un'operazione già avviata. Chiudere l'app o eliminare l'eseguibile non annulla il piano.

## Funzionamento e limiti

- La pianificazione viene salvata nell'Utilità di pianificazione di Windows e rimane attiva anche con l'app chiusa o senza una sessione utente aperta.
- L'esecuzione è consentita anche a batteria, senza richiedere una connessione di rete.
- Il risveglio da sospensione o ibernazione richiede hardware compatibile e timer di riattivazione abilitati. L'app non accende un PC spento. **Diagnostica e affidabilità** mostra gli stati energetici disponibili e le impostazioni dei timer.
- **Forza l'operazione** può causare la perdita di dati non salvati. Senza forzatura, le applicazioni possono impedire lo spegnimento.
- L'ibernazione deve essere disponibile in Windows. L'app non modifica automaticamente la configurazione energetica.
- **Recupera gli orari saltati**, disattivato inizialmente, permette a Windows di eseguire un piano mancato al successivo avvio o risveglio, anche con ritardo.
- L'operazione non è garantita se Windows è bloccato, manca alimentazione o il sistema impedisce il risveglio. Il codice di esito del comando non certifica l'effettivo spegnimento della macchina.

La data e l'ora sono interpretate nel fuso orario locale. Gli orari inesistenti o ambigui durante il cambio dell'ora legale vengono rifiutati.

L'attività è identificata come `PlannedShutdown-6F3129A1` nell'Utilità di pianificazione e resta consultabile dopo l'esecuzione.

## Compilazione

Da PowerShell, nella cartella del progetto:

```powershell
.\build.ps1
```

Lo script genera le icone, compila l'eseguibile nella cartella `dist` ed esegue i test. I sorgenti dell'app si trovano in `src`; le risorse dell'icona in `assets`.

## Copyright

(C) 2026 Prof. ing. Raffaele Mele
