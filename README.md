# Der GitHub-Lern-Bot von LernMoment.de
Ein Bot der dich beim Kennenlernen von GitHub unterstützt.

Für mich ist dieses ein Lernprojekt. Zum einen möchte ich zukünftig interaktivere Kurse entwickeln und zum anderen möchte ich gerne die verschiedenen Möglichkeiten von ASP.NET Core kennenlernen.

## Funktionalitäten
Momentan kann der Bot eine im jeweiligen Projekt definierte Nachricht als *Issue* erstellen, wenn ein Benutzer zum ersten Mal ein Issue an dem Verzeichnis eröffnet. Darüberhinaus würde ich gerne eine komplette "Lernanwendung" damit erstellen. Der Bot soll also ähnlich wie das [Github Learning Lab](https://lab.github.com) eine interaktive Möglichkeiten bieten um Teilnehmer mit GitHub bekannt zu machen. Eine scheinbar noch ausgefeiltere (insbesondere bezogen auf die Storyline) Version ist [Online DevOps Dojo](https://dxc-technology.github.io/about-devops-dojo/). 

## Entwicklung
Ich habe das Projekt gestartet um mich etwas mehr in Themen rund um Webentwicklung mit .NET Core einzuarbeiten. Daher ist dieser Bot in mehreren Iterationen entstanden und ich werde ihn nach und nach mit anderen Technologien weiter entwickeln.

In der momentanen Version wird er mithilfe eine *Azure Function* ausgeführt. Diese ist bei den wenigen Ausführung (max. 1-2 pro Tag) kostenlos und ich brauche keinen eigenen Server, VM, ... verwalten. Dazu verwende ich den *Azure Key Vault* um Token usw. für die Ausführung zu speichern. Auf GitHub ist das ganze als *GitHub App* verpackt. Diese gehört meinem Benutzer und ist als organisationsweite App für die *LernMoment-Organization* installiert. Verwendet wird die *GitHub-App* im [Taschenrechner-Projekt](https://github.com/LernMoment/einstieg-csharp-taschenrechner). 

### Lokale Entwicklung
Da die Anwendung in *Azure Functions* nicht wirklich zu testen ist, kommt auch hier der lokalen Entwicklung eine hohe Bedeutung zu. Wichtig ist dabei, dass die nicht so optimale Unterstützung für die lokale Entwicklung, mit der Art des Deployments zusammen hängt. Ich habe mich für die CI-Variante entschieden. Es gibt auch die Möglichkeit, dass direkt via *Visual Studio* das Deployment gemacht wird. So wie ich es bisher verstehe, ist dann auch das lokale Entwickeln wesentlich einfacher.

#### Eigene GitHub-Anwendung für die lokale Entwicklung
Der wichtigste Schritt ist, dass ich von irgendwo einmal das korrekte Event des WebHooks inkl. einer gültigen Payload bekomme. Es gibt bei WebHooks verschiedene Sicherheitsmechanism (z.B. die Verifzierung der Signature). Diese kann nur erfolgen, wenn ich eine gültige Anfrage von GitHub bekomme. Daher habe ich ein [privates Repository](https://github.com/suchja/LernBotTest) und eine [zusätzliche GitHub-App](https://github.com/settings/apps/lokale-version-lernbot-github) aufgesetzt.

Die "lokale GH-App" ist ähnlich wie der offizielle LernBot konfiguriert, aber verwendet eine andere URL. Diese URL ist nicht von Azure, sondern wird über [smee.io](https://smee.io) an meine lokal laufende Instanz vom Bot weitergeleitet. Darüber ist es möglich, dass ich einerseits über GitHub die korrekten Events des WebHooks auslösen kann UND über smee.io kann ich diese Events dann immer wieder abspielen.

**WICHTIG:** Für diese GH-APP gibt es nicht nur einen anderen Private-Key, sondern natürlich auch eine andere GH-APP-Id. Diese hatte ich bei den ersten Tests nicht geändert im Vergleich zu der Produktivversion und habe dann lange nach dem Verbindungsproblem gesucht!!!

#### Smee.io - Verbindung von GitHub mit lokaler Instanz
Über die Webseite [smee.io](https://smee.io) kann eine Art Kanal geöffnet werden. Dieser Kanal verwendet die [Server-Sent Events](https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events/Using_server-sent_events) Technologie, welche vom Konzept her ähnlich wie *WebSockets* ist, aber nur eine Ein-Weg-Kommunikation erlaubt. Zusätzlich zu dem Kanal (welcher durch eine URL identifiziert wird), habe ich auf meinem Rechner den *smee-Client* installiert.

In der GitHub-App wird nun die eindeutige smee-URL für meinen Kanal eingetragen. Außerdem starte ich den *smee-Client* auf meinem Rechner mit der eindeutigen URL des *smee-Kanals*. Somit ist es nun mögliche, dass ein Event vom GitHub-WebHook an den *smee-Kanal* auf dem *smee-Server* weitergeleitet wird. Von dort wird die Anfrage an den registrieten *smee-Client* geschickt und dieser leitet es als Anfrage an mein lokal laufendes Projekt weiter.

Der *smee-Client* wird über folgenden Befehl gestartet:

```sh
smee --url https://smee.io/{Kanal-URL} --target http://localhost:{Port}/{Route}
```

Die konkreten Werte für meine laufende Instanz habe ich in Evernote dokumentiert.

### Azure Function
Die *Azure Function* die den Bot ausführt wird direkt aus dem `master`-Branch von [diesem Verzeichnis](https://github.com/LernMoment/github-lernbot-app) erstellt. Ein push in den Branch reicht und die *Function* wird neu gebaut und direkt gestartet.

Alle notwendigen Informationen wie das *Secret*, der *Token* und auch die *App-Id* liegen im *Azure KeyVault*. Damit kann die Anwendung zum einen verifizieren, dass die Anfrage tatsächlich vom passenden GitHub-Verzeichnis kommt und zusätzlich werden die Informationen benötigt, damit der Bot sich gegenüber *GitHub* authentifiziern kann. Das ist nötig, damit der Bot schreibend auf das Verzeichnis zurgeifen kann.

Das Testen der *Azure Function* über diesen Weg funktioniert (soweit ich bisher weiß) nur über die praktische Ausführung. In dem verwendeten Verzeichnis muss also ein neuer Benutzer sein erstes Issue anlegen. Dann wird die *Azure Function* ausgeführt und über das *Logging* in *Azure* gibt es dann ein paar Informationen was passiert ist. Daher sollte die Entwicklung besser mit der "lokalen Variante" gemacht werden.

#### Configure
Entgegen anderen Projektarten in *ASP.NET Core*, gibt es für *Azure Functions* (bisher?) keinen `DefaultBuilder`, der die Konfiguration erstellt. Dinge wie die Verwendungen von *Dependency Injection* oder auch *Options* müssen in der Klasse `Startup` und dort `public override void Configure(IFunctionsHostBuilder builder)` konfiguriert werden. Bisher habe ich das [so gelöst](https://github.com/LernMoment/github-lernbot-app/blob/55849b37277fc64422180f76f35e0770673f5c10/GitHubLernBotApp/Startup.cs#L20).

Allerdings stellt sich mir die Frage ob ich zwischen einer Konfiguration für lokale Entwicklung mit *User-Secrets* und einer Produktivumgebung mit *Azure KeyVault* unterscheiden muss. Momentan läuft es so wie ich es eingestellt habe in beiden Szenarien. Allerdings denke ich, dass es daran liegt, dass `AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true, reloadOnChange: true)` als optional markiert ist und danach noch die `AddEnvironmentVariables()` hinzugefügt werden über die die passenden Daten vom *KeyVault* kommen sollten.

Die grundlegende Idee für diesen Ansatz habe ich von [hier](https://damienbod.com/2020/07/12/azure-functions-configuration-and-secrets-management/)

#### Dependency Injection
Um die *Dependency Injection* (wie üblich in ASP.NET Core Projekten) in *Azure Functions* zu verwenden, ist es notwendig diese Zeile `[assembly: FunctionsStartup(typeof(GitHubLernBotApp.Startup))]` in das `Startup.cs` einzufügen und zwar vor dem `namespace`. Mehr Details dazu gibt es [hier](https://docs.microsoft.com/de-de/azure/azure-functions/functions-dotnet-dependency-injection).

Für *Azure Functions* scheint die Lebenszeit für die von der *DI* bereitgestellten Objekt wie folgt zu sein:

> Service Lifetime
>
> Transient: Transient services are created upon each request of the service.
>
> Scoped: The scoped service lifetime matches a function execution lifetime. Scoped services are created once per execution.
>
> Singleton: The singleton service lifetime matches the host lifetime and is reused across function executions on that instance.
> - [siehe hier](https://rmauro.dev/native-dependency-injection-in-azure-functions-with-csharp/)

Daraus folgt, dass *Scoped* wohl die passende Lebenslänge für die Services im LernBot ist.

#### Speichern von Daten
Auch wenn die bisherige Funktionalität ohne das Speichern von Daten auskommt, sehe ich nicht, wie es möglich ist einen kompletten Bot so zu bauen. Grundsätzlich könnte ich mir folgende Wege vorstellen um beispielsweise mehrere Lernschritte an einem *Issue*, *PullRequest* oder auch einer Kombination umzusetzen:

1. Es wird keinerlei Zustand für einen Benutzer gespeichert. Dann müsste aus allen *"Posts"* die ein Benutzer bisher an einem *Repository* gemacht hat und allen *"Posts"* die der *LernBot* daran gemacht ermittelt werden an welchem Schritt der *LernBot* gerade steht. Für das *Willkommen heißen* könnte der *LernBot* also ermitteln wie oft er selber schon an dem fraglichen Issue gepostet hat und daraus ableiten was der nächste Schritt ist.
2. *Azure* bietet unterschiedliche Lösungen zum Speichern von Daten an. Nach einer ersten schnellen Suche denke ich, dass [Azure Table Storage](https://docs.microsoft.com/de-de/azure/storage/tables/table-storage-overview#what-is-table-storage) wohl eine praktische Lösung wäre. Wie das genau geht und ob es wirklich funktioniert müsste ich ausprobieren.
3. Eine weitere Möglichkeit ist, dass ich innerhalb des `.github` Verzeichnis eine Datei anlegen in der für jeden Benutzer festgehalten wird wo er gerade beim *LernBot* steht. Für den aktuellen Stand sollte das gehen. Allerdings hätte ich die Befürchtung, dass bei einem höheren Datenvolumen (sprich mehr Teilnehmer), sich die Änderungen überscheiden und es dann häufiger *Merge-Konflikte* gibt die ich manuell auslösen muss.
4. Grundsätzlich ist es auch möglich sich mit einer DB in *Azure* zu verbinden (siehe [hier](https://docs.microsoft.com/en-us/azure/azure-functions/functions-scenario-database-table-cleanup)). Ich habe allerdings das Gefühl, dass dieses eher für die Administration der DB gedacht ist und nicht um damit aus einer *Azure Function* die Daten zu speichern.
5. Ich sollte mir noch die bereits existierenden Lösungen wie *GitHub Learning Lab* (siehe Funktionalitäten-Kapitel) anschauen. Dort bekomme ich sicherlich noch andere Ideen wie so etwas umgesetzt werden kann.

### Private-Key (aus .pem-Datei) in user secrets speichern
Für die Entwicklung ist es wichtig, dass der zur GH-App gehörende *private key* in der Anwendung verfügbar ist. Dieser wird benötigt um mithilfe von `octokit` einen authentifizierten `Client` zu erstellen der dann wieder auf das Verzeichnis, aus dem das Event des WebHooks kam, zugreifen kann.

Allerdings war es nicht ganz trivial den Inhalt der Datei in das *user secret* zu bekommen. Der Schlüssel ist in der Datei auf mehreren Zeilen verteilt und damit kann er nicht direkt als *user secret* gespeichert werden. Mehr Details dazu [hier](https://github.com/dotnet/AspNetCore.Docs.de-de/issues/70). Die Lösung für dieses Problem habe ich durch [eine Frage auf StackOverflow](https://stackoverflow.com/questions/63170612/storing-multiline-rsa-key-in-net-core-user-secrets-json/63172227#63172227) bekommen.

Mit einer Powershell im Verzeichnis in dem die *.pem-Datei* liegt kann diese eingelesen und in einen `string` ohne Zeilenumbruch konvertiert werden. Natürlich würde dieses auch über ein kleines C#-Programm gehen, aber das scheint mir nicht wirklich praktikabel. Folgende Befehle müssen ausgeführt werden:

```sh
$fileName = "{Dateipfad}\{Dateiname}.pem"
$multiVal = Get-Content $fileName -Raw
```

Damit ist die Datei in den string `multival` eingelesen. Nun kann ebenfalls in Powershell in das Verzeichnis mit dem Projekt gewechselt werden (ACHTUNG die pem-Datei darf nicht in die Versionsverwaltung kommen!!!). Dort erlaubt folgender Befehl den string (ohne Zeilenumbruch) in den `user screts` zu speichern:

```sh
dotnet user-secrets set "{Name des Schlüssels}" " $multiVal"
```

**WICHTIG:** Ich denke das zusätzliche Leerzeichen von `$multiVal` ist notwendig. Ansonsten gibt es Probleme beim Einsetzen durch `dotnet user-secrets`. Das müsste allerdgins nochmals überprüft werden.

In den *user secrets* muss nun noch der Anfang und das Ende des Schlüssels entfernt werden. Obwohl das eigentlich nicht notwendig sein sollte (glaube ich), hat bei mir der Schlüssel mit `-----BEGIN RSA PRIVATE KEY-----` und `-----END RSA PRIVATE KEY-----` nicht funktioniert.

