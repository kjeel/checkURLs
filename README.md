Kurze Erklärung zum Projekt:
Das ganze wurde mit C# umgesetzt es werden verschiedene URLs ausgelesen und folgende Werte ermittelt und in ein JSON geschrieben:
Url: URL der Website
Status: HTTP Status Code der zurückgeliefert wird von der Website (200, 404 etc.)
Reachable: true oder false (false wenn z.B 500 zurückgeschickt wird oder Domain nicht existiert)
Timestamp: Zeitstempel wann das ganze ausgeführt wurde
ResponseTime: Zeit in ms wie lange der Server braucht um zu antworten
certificatePresent: true/false
certificateExpiry: Ablaufdatum des SSL Zertifikates

  
in der test_urls.json sind Websiten die getestet werden

Zusätzliche Packages werden noch benötigt: npm install -g azure-functions-core-tools@4 --unsafe-perm true

Das ganze Lokal starten kann man folgendermaßen:
App starten: func start
Danach triggern:
curl -X POST http://localhost:7071/api/UrlHealthCheck \
     -H "Content-Type: application/json" \
     -d @test_urls.json

Um das JSON "lesbarer" machen haben wir ein node.js package verwendet welches man mit "npm install -g prettier" installieren kann
Dann kann man mit folgenden Befehl das JSON formatieren: prettier --parser json results.json --write pretty_results.json
Ansonsten ist alles in einer Zeile. (war nicht gegeben sieht aber schöner aus)

in Root Ordner wird automatisch ein File logs.txt angelegt, nach jeder Ausführung wird dort zusätzlich mitgeloggt, wann die Funktion gestartet wurde und welche Websiten getestet wurden.



