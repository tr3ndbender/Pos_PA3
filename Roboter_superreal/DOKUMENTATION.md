# Roboter-Interpreter — Dokumentation & Lernhilfe

Diese Datei erklärt, wie das Roboter-Programm funktioniert und wie du so etwas
selbst von Null bauen kannst. Sie ist als Nachschlagewerk gedacht — lies sie
immer wieder durch, bis dir das Muster in Fleisch und Blut übergeht.

---

## 1. Worum geht es überhaupt?

Du hast eine eigene, kleine **Programmiersprache** erfunden, mit der man einen
Roboter über ein Feld steuert. Ein Programm in dieser Sprache sieht so aus:

```
REPEAT 2 {
    MOVE RIGHT
}
COLLECT
```

Der Computer versteht diesen Text **nicht direkt**. Ein **Interpreter** ist ein
Programm, das so einen Text liest, versteht und ausführt. Das macht er in
**3 Phasen**:

```
Text  →  [1. Tokenizer]  →  Tokens  →  [2. Parser]  →  Baum  →  [3. Run]  →  Roboter bewegt sich
```

Diese 3 Phasen findest du als Kommentare ("Schritt 1/2/3") in
`MainWindow.xaml.cs`.

---

## 2. Die Grammatik (die Regeln der Sprache)

Bevor man Code schreibt, legt man die **Regeln** fest. Das nennt man Grammatik.
Für dieses Programm lautet sie:

```
Programm   = Anweisung*                  (beliebig viele Anweisungen hintereinander)
Anweisung  = MOVE Richtung
           | REPEAT Zahl Block
           | COLLECT
Block      = { Programm }                 (geschweifte Klammern, innen wieder ein Programm)
Richtung   = LEFT | RIGHT | UP | DOWN
```

Bedeutung der Zeichen:
- `*`  = "beliebig oft" (0, 1, 2, … mal)
- `|`  = "oder"

**Das Wichtigste:** Ein `Block` enthält wieder ein ganzes `Programm`. Dadurch
kann man Befehle **ineinander verschachteln** (z. B. ein REPEAT in einem REPEAT).
Das nennt man **Rekursion** und ist der Kern der ganzen Sache.

**Goldene Regel:** *Jede Zeile dieser Grammatik wird im Code zu genau einer Klasse.*

| Grammatik-Regel       | Klasse im Code         |
|-----------------------|------------------------|
| `Programm`            | `Programm.cs`          |
| `MOVE Richtung`       | `MoveExpression.cs`    |
| `REPEAT Zahl Block`   | `RepeatExpression.cs`  |
| `Block = { … }`       | `BlockExpression.cs`   |
| `COLLECT`             | `CollectExpression.cs` |

---

## 3. Phase 1 — Tokenizer (Text in "Wörter" zerlegen)

**Datei:** `MainWindow.xaml.cs`, Methode `Button_Click`, Schritt 1.

Der rohe Text wird in einzelne Bausteine — **Tokens** — zerlegt. Jedes Token hat
einen **Wert** (z. B. `"MOVE"`) und einen **Typ** (z. B. `Keyword`).

### Die Token-Klasse (`Token.cs`)

```csharp
class Token {
    public enum TokenType { Keyword, Number, Direction, OpenBracket, CloseBracket, Error }
    public string Value { get; set; }
    public TokenType Type { get; set; } = TokenType.Error;   // Standard = Fehler
}
```

### Wie zerlegt wird

Ein **regulärer Ausdruck** (Regex) schneidet den Text in einzelne Stücke:

```csharp
private Regex regex = new Regex(@"REPEAT|MOVE|COLLECT|LEFT|RIGHT|UP|DOWN|\d+|{|}|\S+");
```

- `\d+`  = eine oder mehrere Ziffern (eine Zahl)
- `{` und `}`  = die geschweiften Klammern
- `\S+`  = "irgendein Wort ohne Leerzeichen" (Auffangnetz für unbekannten Müll)

Danach bekommt jedes Stück seinen Typ über eine `switch`-Anweisung:

```csharp
foreach (Match match in regex.Matches(Code.Text)) {
    Token token = new Token() { Value = match.Value };
    tokens.Add(token);
    switch (match.Value) {
        case var _ when numberRegex.IsMatch(match.Value):   token.Type = TokenType.Number;    break;
        case var _ when keywordRegex.IsMatch(match.Value):  token.Type = TokenType.Keyword;   break;
        case var _ when directionRegex.IsMatch(match.Value):token.Type = TokenType.Direction; break;
        case "{":  token.Type = TokenType.OpenBracket;  break;
        case "}":  token.Type = TokenType.CloseBracket; break;
        // alles andere bleibt Error
    }
}
```

### Beispiel

Aus dem Text `REPEAT 2 {` werden drei Tokens:

| Value    | Type        |
|----------|-------------|
| `REPEAT` | Keyword     |
| `2`      | Number      |
| `{`      | OpenBracket |

*Schritt 1.5 im Code prüft nur, ob ein Token den Typ `Error` hat (= unbekanntes
Wort) und zeigt es dann an.*

---

## 4. Phase 2 — Parser (Tokens in einen Baum verwandeln)

Jetzt wird aus der **flachen Liste** von Tokens eine **Baumstruktur** gebaut, die
der Grammatik entspricht. Pro Grammatik-Regel gibt es eine Klasse, und alle erben
von `Expression`.

### Die gemeinsame Basis (`Expression.cs`)

```csharp
abstract class Expression {
    public static List<string> Errors { get; set; } = new();  // Fehlersammlung
    public abstract void Parse(List<Token> tokens);           // Pflicht: muss jede Klasse haben
    public virtual void Run(RobotField robot) { }             // optional überschreibbar
}
```

`Parse` baut den Baum auf. `Run` führt ihn später aus.

### Das Herzstück: `Programm.Parse` (`Programm.cs`)

```csharp
while (tokens.Count > 0 && tokens[0].Type != TokenType.CloseBracket) {
    Token token = tokens[0];
    if (token.Type == TokenType.Keyword) {
        Expression expression = null;
        switch (token.Value) {
            case "MOVE":    expression = new MoveExpression();    break;
            case "REPEAT":  expression = new RepeatExpression();  break;
            case "COLLECT": expression = new CollectExpression(); break;
        }
        if (expression == null) {
            Errors.Add("Unbekanntes Keyword " + token.Value);
        } else {
            tokens.RemoveAt(0);          // Keyword "aufessen"
            expression.Parse(tokens);    // die Klasse parst sich selbst weiter
            expressions.Add(expression); // im Baum speichern
        }
    } else {
        Errors.Add("Unerwarteter Token-Typ " + token.Type);
        tokens.RemoveAt(0);
    }
}
```

### DAS MUSTER, das sich überall wiederholt

> **1. Schau Token[0] an.**
> **2. Ist es das, was ich erwarte?**
>    - **Ja** → Wert merken und `tokens.RemoveAt(0)` ("aufessen").
>    - **Nein** → `Errors.Add(...)`.

Jede einzelne `Parse`-Methode macht nur das. Beispiel `MoveExpression.cs`:

```csharp
class MoveExpression : Expression {
    private Token direction;
    public override void Parse(List<Token> tokens) {
        if (tokens.Count > 0 && tokens[0].Type == TokenType.Direction) {
            direction = tokens[0];     // merken
            tokens.RemoveAt(0);        // aufessen
        } else {
            Errors.Add("Richtung erwartet");
        }
    }
}
```

### Die Rekursion (der einzige knifflige Teil)

`REPEAT` braucht eine Zahl **und** einen Block, und der Block enthält wieder ein
ganzes Programm. So sieht das aus:

`RepeatExpression.cs`:
```csharp
class RepeatExpression : Expression {
    private int _count;
    private Expression _block = new BlockExpression();
    public override void Parse(List<Token> tokens) {
        if (tokens.Count > 0 && tokens[0].Type == TokenType.Number) {
            _count = int.Parse(tokens[0].Value);  // Zahl merken
            tokens.RemoveAt(0);                    // Zahl aufessen
            _block.Parse(tokens);                  // Block parsen lassen
        } else {
            Errors.Add("Zahl erwartet");
        }
    }
}
```

`BlockExpression.cs`:
```csharp
class BlockExpression : Expression {
    private Programm _programm = new Programm();
    public override void Parse(List<Token> tokens) {
        if (tokens.Count > 0 && tokens[0].Type == TokenType.OpenBracket) {
            tokens.RemoveAt(0);          // '{' aufessen
            _programm.Parse(tokens);     // <-- HIER geht es wieder von vorne los!
            // danach muss ein '}' kommen:
            if (tokens.Count > 0 && tokens[0].Type == TokenType.CloseBracket) {
                tokens.RemoveAt(0);      // '}' aufessen
            } else {
                Errors.Add("'}' erwartet");
            }
        } else {
            Errors.Add("'{' erwartet");
        }
    }
}
```

`BlockExpression` ruft `Programm.Parse` auf — und das ist genau die Methode, mit
der alles angefangen hat. Dadurch kann sich alles beliebig tief verschachteln.

*Schritt 2.5 im Code zeigt gesammelte Parse-Fehler an.*

---

## 5. Phase 3 — Run (den Baum ausführen)

**Datei:** `MainWindow.xaml.cs`, Schritt 3 (läuft im `ThreadPool`, damit das
Fenster nicht einfriert).

Jetzt wird der fertige Baum ausgeführt. Jede Klasse weiß selbst, was zu tun ist:

```csharp
// Programm: führt alle Anweisungen der Reihe nach aus
public override void Run(RobotField robot) {
    foreach (Expression e in expressions) e.Run(robot);
}

// MoveExpression: bewegt den Roboter
public override void Run(RobotField robot) {
    switch (direction.Value) {
        case "LEFT":  robot.Move(RobotField.Direction.Left);  break;
        case "RIGHT": robot.Move(RobotField.Direction.Right); break;
        // ...
    }
}

// RepeatExpression: führt den Block _count-mal aus
public override void Run(RobotField robot) {
    for (int i = 0; i < _count; i++) _block.Run(robot);
}

// CollectExpression: sammelt einen Buchstaben auf
public override void Run(RobotField robot) {
    string letter = robot.Collect();
    if (string.IsNullOrEmpty(letter)) Errors.Add("Nichts zum Sammeln");
}
```

*Schritt 3.5 zeigt Laufzeitfehler an (z. B. "Roboter konnte sich nicht bewegen").*

---

## 6. Beispiel komplett durchgespielt

Eingabe: `REPEAT 2 { MOVE RIGHT }`

### Phase 1 — Tokenizer
```
[REPEAT|Keyword] [2|Number] [{|OpenBracket] [MOVE|Keyword] [RIGHT|Direction] [}|CloseBracket]
```

### Phase 2 — Parser (so schrumpft die Token-Liste)

| Schritt | Aktion                                            | Übrige Tokens                          |
|---------|---------------------------------------------------|----------------------------------------|
| 1 | `Programm.Parse`: sieht `REPEAT` → neue RepeatExpr, isst `REPEAT` | `2 { MOVE RIGHT }`         |
| 2 | `RepeatExpr.Parse`: sieht `2` → merkt count=2, isst `2`         | `{ MOVE RIGHT }`           |
| 3 | ruft `BlockExpr.Parse`: sieht `{`, isst `{`                     | `MOVE RIGHT }`             |
| 4 | `BlockExpr` ruft `Programm.Parse`: sieht `MOVE` → MoveExpr, isst `MOVE` | `RIGHT }`           |
| 5 | `MoveExpr.Parse`: sieht `RIGHT` → merkt Richtung, isst `RIGHT`  | `}`                        |
| 6 | inneres `Programm` stoppt (nächstes ist `}`)                   | `}`                        |
| 7 | zurück in `BlockExpr`: sieht `}`, isst `}`                     | *(leer)*                   |

Fertiger Baum:
```
Programm
└─ RepeatExpression (count = 2)
   └─ BlockExpression
      └─ Programm
         └─ MoveExpression (RIGHT)
```

### Phase 3 — Run
`RepeatExpression` führt seinen Block 2× aus → Roboter geht 2× nach rechts.

---

## 7. Wie baue ich so ein Programm von Null? (Rezept)

Immer dieselbe Reihenfolge:

1. **Grammatik aufschreiben** (auf Papier): Welche Befehle, wie kombinierbar?
2. **`Token`-Klasse + `enum TokenType`** anlegen.
3. **Tokenizer** schreiben (Regex → Tokens mit Typ).
4. **`abstract class Expression`** mit `Parse()` und `Run()`.
5. **`class Programm`** mit der while-Schleife (das Herzstück).
6. **Pro Befehl eine Klasse**, jede nach dem Muster
   "Token[0] anschauen → merken + aufessen → sonst Fehler".
7. **`Run()`** in jeder Klasse umsetzen.
8. **Im Button** zusammenstecken: Tokenize → Parse → Run.

---

## 8. Einen neuen Befehl hinzufügen (Übung & Testfrage!)

Angenommen, du willst einen Befehl `TURN` hinzufügen. Du musst genau **3 Stellen**
anfassen:

1. **Tokenizer:** das Wort in den `keywordRegex` aufnehmen.
2. **`Programm.Parse`:** einen `case "TURN":` in den switch einbauen.
3. **Neue Klasse `TurnExpression : Expression`** schreiben (Vorlage: eine
   bestehende Expression-Klasse kopieren und anpassen).

> Merke: Wenn du einen Befehl selbst hinzufügen kannst, hast du das Prinzip
> verstanden. Genau das wird im Test oft verlangt.

---

## 9. Die wichtigsten Sätze zum Auswendigmerken

- **3 Phasen:** Tokenize → Parse → Run.
- **Jede Grammatik-Regel = eine Klasse.** Jede Klasse hat `Parse()` und `Run()`.
- **Parse-Muster:** "Token[0] anschauen → passt? → merken + `RemoveAt(0)` → sonst Fehler."
- **Rekursion:** Ein Block ruft wieder `Programm.Parse` auf — dadurch klappt das Verschachteln.
- **`Run()`:** jede Klasse macht ihre eigene Aktion; Container (Programm, Block, Repeat) rufen die `Run()` ihrer Kinder auf.

---

## 10. So lernst du es am schnellsten

1. **Diese Doku einmal langsam durchlesen.**
2. **Beispiel aus Abschnitt 6 selbst auf Papier durchspielen** mit einem anderen
   Programm, z. B. `MOVE UP COLLECT`.
3. **Im Debugger zuschauen** (Breakpoint in `Programm.Parse`, F5, dann F10/F11)
   und beobachten, wie die `tokens`-Liste schrumpft.
4. **Eine abgespeckte Version aus dem Kopf abtippen** (ohne zu spicken). Wenn du
   stecken bleibst: kurz nachschauen, Zettel weglegen, weiter. 2–3× wiederholen.
5. **Übungsaufgabe:** Baue eine Mini-Sprache mit `SAY <Zahl>` (gibt die Zahl aus)
   und `REPEAT <Zahl> { … }`. Das enthält alles Wichtige, nur kleiner.
