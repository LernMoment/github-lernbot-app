# Der GitHub-Lern-Bot von LernMoment.de
Ein Bot der dich beim Kennenlernen von GitHub unterstützt.

## Funktionalitäten
Momentan kann der Bot eine im jeweiligen Projekt definierte Nachricht als *Issue* erstellen, wenn ein Benutzer zum ersten Mal ein Issue an dem Verzeichnis eröffnet. Darüberhinaus würde ich gerne eine komplette "Lernanwendung" damit erstellen. Der Bot soll also ähnlich wie das [Github Learning Lab](https://lab.github.com) eine interaktive Möglichkeiten bieten um Teilnehmer mit GitHub bekannt zu machen. Eine scheinbar noch ausgefeiltere (insbesondere bezogen auf die Storyline) Version ist [Online DevOps Dojo](https://dxc-technology.github.io/about-devops-dojo/). 

## Entwicklung
Ich habe das Projekt gestartet um mich etwas mehr in Themen rund um Webentwicklung mit .NET Core einzuarbeiten. Daher ist dieser Bot in mehreren Iterationen entstanden und ich werde ihn nach und nach mit anderen Technologien weiter entwickeln.

In der momentanen Version wird er mithilfe eine *Azure Function* ausgeführt. Diese ist bei den wenigen Ausführung (max. 1-2 pro Tag) kostenlos und ich brauche keinen eigenen Server, VM, ... verwalten. Dazu verwende ich den *Azure Key Vault* um Token usw. für die Ausführung zu speichern. Auf GitHub ist das ganze als *GitHub App* verpackt. Diese gehört meinem Benutzer und ist als organisationsweite App für die *LernMoment-Organization* installiert. Verwendet wird die *GitHub-App* im [Taschenrechner-Projekt](https://github.com/LernMoment/einstieg-csharp-taschenrechner). 

### Lokale Entwicklung



### Azure Function
Die *Azure Function* die den Bot ausführt wird direkt aus dem `master`-Branch von [diesem Verzeichnis](https://github.com/LernMoment/github-lernbot-app) erstellt. Ein push in den Branch reicht und die *Function* wird neu gebaut und direkt gestartet.

Alle notwendigen Informationen wie das *Secret*, der *Token* und auch die *App-Id* liegen im *Azure KeyVault*. Damit kann die Anwendung zum einen verifizieren, dass die Anfrage tatsächlich vom passenden GitHub-Verzeichnis kommt und zusätzlich werden die Informationen benötigt, damit der Bot sich gegenüber *GitHub* authentifiziern kann. Das ist nötig, damit der Bot schreibend auf das Verzeichnis zurgeifen kann.

Das Testen der *Azure Function* über diesen Weg funktioniert (soweit ich bisher weiß) nur über die praktische Ausführung. In dem verwendeten Verzeichnis muss also ein neuer Benutzer sein erstes Issue anlegen. Dann wird die *Azure Function* ausgeführt und über das *Logging* in *Azure* gibt es dann ein paar Informationen was passiert ist. Daher sollte die Entwicklung besser mit der "lokalen Variante" gemacht werden.

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
