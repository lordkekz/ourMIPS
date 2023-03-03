---

# Feel free to add content and custom Front Matter to this file.

# To modify the layout, see https://jekyllrb.com/docs/themes/#overriding-theme-defaults

layout: home
title: Downloads
---

- [Downloads are in this folder](http://dl.lkekz.de/ourmips/)

# Spezifikation und Funktionsübersicht
## Registergröße
**32-Bit** Register, **32-Bit** Wortgröße, **32-Bit** Addressbusgröße

## Zahlendarstellung
### Dezimal
#### Philosonline
- \>=2^15 => Zweierkomplement
- VZ & <2^15 => B&V

#### Yapjoma
- VZ & in range `[-2^15, 2^15-1]`

### Binär

#### Philosonline
- Prefix `0b` oder Suffix `b`
- Genau 16Bit => Zweierkomplement
- VZ & <16Bit => B&V

#### Yapjoma
- Prefix `0b`
- Genau 16Bit, Zweierkomplement vorzeichenerweitert auf 32Bit, kein explizites VZ

### Hexadezimal

#### Philosonline
- Prefix `0x` oder Suffix `h`
- Genau 16Bit => Zweierkomplement
- VZ & <16Bit => B&V

#### Yapjoma
- Prefix `0x`
- Genau 16Bit, Zweierkomplement vorzeichenerweitert auf 32Bit, kein explizites VZ

### Standard
- Wie Philosonline, weil das ein Superset von Yapjoma ist

## Anweisungscodierung
- Pro Anweisung 1 Wort (32 Bit)
- **6 Bit** Opcode
- **5 Bit** erster Registeroperand
- **5 Bit** zweiter Registeroperand
- **16 Bit** immediate Operand

## Speicheradressierung
- Wortbasierte Adressierung (Alle 4 Byte eine Adresse)

## Speicherinitialisierung
- Normalerweise alles zufällige Daten in Speicher und Registern

### Philosonline
- JSON-Datei

### Yapjoma
- Eigenes format


## Bezeichnerausdrücke
- Keywords sind case-insensitiv
- Vor nicht-Alias-Registerausdrücken darf ein `$` stehen.
- Benutzerdefinierte Bezeichner müssen mit einem ASCII-Buchstaben beginnen
- Benutzerdefinierte Bezeichner müssen ansonsten aus ASCII-Buchstaben oder ASCII-Ziffern oder dem Unterstrich `_` bestehen

### Philosonline
- Benutzerdefinierte Bezeichner sind case-sensitiv

### Yapjoma
- Benutzerdefinierte Bezeichner sind case-insensitiv
- Makroargumentennamen sind eingeschränkt (siehe Makros)
- Namen, die für Makroargumente erlaubt sind, sind für keine anderen Bezeichner erlaubt.

### Standard
- Benutzerdefinierte Bezeichner sind case-insensitiv

## Label
```ourMIPS
label_name:
	instructions
```
- Können vor Definition verwendet werden.
- Doppelpunkt ist **notwendig**

## Aliasse

### Philosonline
```ourMIPS
alias name = wert

```
- Aliasse werden aufgelöst, indem jedes Vorkommen durch den Wert ersetzt wird. Aliasse dürfen nicht mit `$` geprefixt werden, auch wenn sie auf Register verweisen.

### Yapjoma
- Werden nicht unterstützt

## Makros
- Label, die in Makros deklariert werden, werden nur innerhalb desselben Makros aufgelöst
- Label von außen, auf die in Makros verwiesen wird, werden unverändert aufgelöst
- Gleiches gilt für Aliasse (Philosonline)

### Philosonline
```ourMIPS
macro name args:
	instructions
endmacro
```
- Makros **müssen vor** ihrer Verwendung definiert werden.
- Der Programmfluss muss Makros **nicht** vor ihrer Verwendung erreichen (denn sie werden zur Compilezeit aufgelöst)
- Der **Doppelpunkt ist optional**.
- Makro-Definitionen können geschachtelt werden, Makros sind nur innerhalb ihres Elternmakros gültig.
- Makros können innerhalb von anderen Makros eingesetzt werden, solange nicht rekursiv

### Yapjoma
```ourMIPS
macro name args
	instructions
mend
```
- Makros **dürfen vor** ihrer Definition verwendet werden.
- Der Programmfluss muss Makros **nicht** vor ihrer Verwendung erreichen (denn sie werden zur Compilezeit aufgelöst)
- Nach dem letzten Argument darf **kein Doppelpunkt** stehen.
- Argumente müssen je nach ihrem Typ Namen der Form `regX`, `constX` oder `labelX` haben, wobei `X` eine Zahl von 0 bis 99 ist.
- Makro-Definitionen können **nicht** geschachtelt werden
- Makros können innerhalb von anderen Makros eingesetzt werden, solange nicht rekursiv

Anweisungen, die in einem Makro stehen, werden normalerweise übersprungen. Wenn aber die macro-Anwesiung in der allerersten Zeile des Programms steht, wird diese übersehen. Der erzeugte Programmspeicher wird aber korrekt angezeigt.

```ourMIPS
macro testmakro
  sysout "makro"
mend

sysout "startet"
testmakro
sysout "endet"
systerm
```

### Standard
- Doppelpunkt optional
- Makros dürfen vor ihrer Definition verwendet werden
- `endmacro` und `mend` sind beide zulässig
- Makros dürfen in Makros verwendet werden, solange nicht rekursiv
- Makrodefinitionen dürfen nicht geschachtelt werden. Auch nicht im Kompatibilitätsmodus für Philosonline!


## Instruktionen
```ourMIPS
befehl arg1 arg2 arg2
; ODER
befehl arg1, arg2, arg3
```

## Flags

### Overflow-Flag


## Bugs

### Philosonline `bgt`
- Philosonline scheint für bgt die eine Zahl von der anderen abzuziehen. Wenn die Differenz der Zahlen größer als `2^31 - 1` ist, kommt es zum Overflow und Philos trifft die falsche Entscheidung.

### Philosonline Doku
- `bgt` Verhalten (insbesondere Bug) nicht genau spezifiziert
- Bytecode-Kodierung von magic instructions nicht genau definiert
- `bo` Instruction wird aufgeführt als ob es 2 Register und ein Label annimmt; tatsächlich ist nur ein Label korrekt.