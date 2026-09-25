# Planned Shutdown

App Windows in italiano per pianificare una singola operazione di spegnimento o ibernazione, anche forzata.

## Avvio

Scaricare il pacchetto dalla sezione Releases, estrarlo e aprire l’eseguibile. Per la compilazione locale, aprire `dist\PlannedShutdown-1.3.exe` e accettare la richiesta amministratore di Windows. Non richiede installazione: usa .NET Framework 4.x incluso in Windows 10/11. L'eseguibile non è firmato digitalmente.

La data e l'ora iniziali sono quelle correnti. Selezionare un momento futuro (almeno 15 secondi), oppure usare **+15 minuti** o **+1 ora**. Scegliere l'operazione, eventualmente la forzatura, quindi **Pianifica** e confermare il riepilogo. La data viene interpretata nel fuso orario locale e salvata con il relativo offset; gli orari inesistenti o ambigui nel cambio dell'ora legale sono rifiutati.

Il nuovo piano sostituisce quello precedente. Il conto alla rovescia e il codice di esito vengono aggiornati automaticamente. **Annulla piano** rimuove la pianificazione prima dell'esecuzione; non può fermare un'operazione già avviata da Windows. Chiudere l'app non annulla il piano. Eliminare l'eseguibile non annulla il piano: usare prima il pulsante di annullamento.

## Layout e leggibilità

La finestra è ridimensionabile. Righe, etichette e pulsanti si adattano al testo; gli avvisi vanno a capo e il contenuto scorre verticalmente quando lo spazio non basta. La data usa il formato compatto giorno/mese/anno. I bordi e il pulsante disabilitato hanno contrasto esplicito. La GUI attuale usa WinForms, non Qt5; per eventuali implementazioni Qt usare Qt6.

Verificato il layout con testo al 100%, 150% e 200%, anche a larghezza ridotta: nessuna sovrapposizione, nessuna etichetta troncata, fondo raggiungibile con lo scorrimento. Queste verifiche simulano l'ingrandimento dei font; non sostituiscono la prova su tutte le configurazioni multimonitor.

## Affidabilità e limiti reali

- Il piano è un'attività nativa di Windows eseguita come SYSTEM, anche senza sessione utente e con l'app chiusa.
- L'esecuzione è consentita anche a batteria, senza richiedere rete o inattività del PC.
- Viene richiesto il risveglio: firmware, hardware e impostazioni energetiche devono consentirlo. Un computer spento non viene acceso dall'app. La sezione **Diagnostica e affidabilità** mostra gli stati energetici disponibili e i timer del piano corrente.
- **Forza l'operazione** usa l'opzione Windows `/f`, che può causare perdita di dati non salvati. Senza forzatura le applicazioni possono impedire lo spegnimento.
- L'ibernazione viene accettata solo se Windows la dichiara disponibile. L'app non cambia automaticamente la configurazione energetica.
- **Recupera l'esecuzione** è disattivato inizialmente. Se abilitato, Windows può recuperare un orario saltato al prossimo avvio o risveglio, anche con ritardo. Lasciarlo disattivato per evitare uno spegnimento successivo inatteso.
- Nessun software può garantire l'operazione se Windows è bloccato, manca alimentazione, il servizio di pianificazione non funziona o il sistema impedisce il risveglio. Un codice di comando riuscito non è una certificazione dell'effettivo stato fisico della macchina.

L'attività si chiama `PlannedShutdown-6F3129A1` nell'Utilità di pianificazione. Rimane consultabile dopo l'orario previsto, per conservarne l'esito. Non contiene ripetizioni né tentativi automatici che possano innescare cicli di ibernazione al risveglio.

## Verifiche effettuate

Compilazione dell'eseguibile, 81 verifiche automatiche su comandi, date e tutte le combinazioni delle opzioni, parsing dei piani con il motore nativo di Windows e controllo visivo della GUI. Icona colorata incorporata nell'eseguibile e nella finestra; sorgenti e formati SVG, PNG e ICO in `assets`.

Non sono stati registrati piani né eseguiti spegnimenti o ibernazioni durante lo sviluppo. Il test completo di registrazione, annullamento, risveglio e operazione effettiva resta da effettuare sul PC destinatario, dopo aver salvato il lavoro.

## Sorgenti e compilazione

`build.ps1` rigenera le icone, compila l'app con il compilatore .NET Framework di Windows e avvia i test innocui. Non scarica dipendenze e non crea attività pianificate. Le anteprime vengono salvate in `tests\layout-*.png` (contenuto completo) e `tests\debug-layout-*.png` (finestra).

Documentazione Microsoft:

- [Comando shutdown](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/shutdown)
- [Risveglio richiesto dal Task Scheduler](https://learn.microsoft.com/en-us/windows/win32/taskschd/tasksettings-waketorun)
- [Impostazioni di pianificazione](https://learn.microsoft.com/en-us/windows/win32/taskschd/taskschedulerschema-settingstype-complextype)

## Correzione 1.1

La verifica dopo il salvataggio legge le proprietà effettive del motore Windows, senza presumere che il file XML contenga tutti i valori predefiniti. Se la registrazione riesce ma la verifica successiva fallisce, l'app avvisa esplicitamente che il piano potrebbe essere attivo e aggiorna subito lo stato. Verifiche aggiunte per valori XML omessi e per comandi o impostazioni realmente diversi.

## Versione 1.3

Rimosso il pulsante Informazioni su. Copyright mantenuto nella finestra principale e nelle proprietà dell'eseguibile: (C) 2026 Prof. ing. Raffaele Mele.
