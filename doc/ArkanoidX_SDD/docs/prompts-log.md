# prompts-log.md - Tracabilitat resumida del proces SDD

> **Metodologia:** Spec-Driven Development amb suport d'IA  
> **Projecte:** ArkanoidX  
> **Feature acotada:** bucle principal de joc centrat en pilota, pala, blocs i mode IA  
> **Objectiu del log:** registrar de forma breu i cronologica com s'ha definit, implementat i corregit la funcionalitat.

---

## 1. Resum de la feature

La funcionalitat escollida no es tot el projecte, sino una part concreta del joc:
- comportament de la pilota
- moviment de la pala
- destruccio de blocs
- recompte de blocs restants
- comportament del mode IA amb `ArkanoidAgent`

Aquesta eleccio compleix el requisit d'una feature concreta i acotada, i permet verificar facilment si la IA segueix o no l'especificacio.

---

## 2. Tracabilitat cronologica

| Pas | Fase | Prompt resumit | Problema detectat | Correccio aplicada | Resultat |
|---|---|---|---|---|---|
| 1 | `opsx:propose` | Definir `foundations.md` per a la feature de gameplay | El primer material disponible descrivia una altra feature del projecte | Es va redefinir l'abast nomes al gameplay loop i mode IA | Context i restriccions alineats amb la part real implementada |
| 2 | `opsx:propose` | Generar `spec.md` amb comportament de pilota, pala, blocs i IA | L'especificacio antiga no servia perque era de CRUD/auth | Es va crear una especificacio nova centrada nomes en runtime gameplay | Comportament esperat definit per seccions funcionals |
| 3 | `opsx:propose` | Generar `plan.md` per implementar nomes aquesta part | El pla antic era massa ampli i fora d'abast | Es va dividir en 4 fases: pilota/pala, blocs, flow de ronda, mode IA | Estrategia d'implementacio acotada i verificable |
| 4 | `opsx:apply` | Revisar `Block.cs` i `GameManager.cs` respecte al spec de blocs | El nivell es podia donar per acabat quan encara quedava 1 bloc visible | El bloc es desactiva abans de notificar i el recompte es recalcula des de l'escena real | El final de nivell nomes passa amb 0 blocs actius |
| 5 | `opsx:apply` | Verificar el comportament de la pilota | Risc que la pilota perdi velocitat o es quedi aturada | Es valida el reset, el `Launch()` i la normalitzacio de velocitat | La pilota mante una velocitat estable durant la partida |
| 6 | `opsx:apply` | Verificar el comportament de la pala | Risc que la pala surti dels limits o barregi controls | Es mantenen limits basats en camera i collider; en mode IA es desactiva control manual | La pala queda limitada a l'area jugable |
| 7 | `opsx:apply` | Revisar el mode IA amb `ArkanoidAgent` | Calia demostrar que la IA segueix una logica observable i no magica | Es documenten observacions, accions i recompenses del model | El mode IA queda tracable dins de l'especificacio |

---

## 3. Prompts utilitzats

### 3.1 Prompts per generar l'especificacio

**Prompt A - foundations.md**
```text
Defineix una feature acotada d'ArkanoidX centrada nomes en el bucle principal de joc.
Inclou context, objectius, restriccions i fora d'abast.
La feature ha d'explicar com funcionen la pilota, la pala, els blocs i el mode IA.
```

**Prompt B - spec.md**
```text
Genera un spec.md per a OpenSpec que descrigui el comportament esperat de:
- pilota
- pala del jugador
- blocs
- flow de ronda amb vides i respawn
- mode IA amb ArkanoidAgent
No incloguis login, backend ni funcionalitats fora del gameplay.
```

**Prompt C - plan.md**
```text
Genera un plan.md per implementar aquesta feature en fases curtes.
Separa la feina entre fisica de pilota, moviment de pala, destruccio de blocs,
control d'estat de la ronda i mode IA.
```

### 3.2 Prompts per a la implementacio

**Prompt D - correccio del recompte de blocs**
```text
Compara la implementacio actual amb l'spec de destruccio de blocs.
Corregeix el flux perque el nivell nomes es completi quan no quedi cap bloc actiu real a l'escena.
```

**Prompt E - revisio del flow de gameplay**
```text
Verifica que la pilota es reinicia be, es llanca correctament i mante velocitat estable.
No afegeixis funcionalitats noves; nomes corregeix desviacions respecte a l'spec.
```

**Prompt F - revisio del mode IA**
```text
Revisa el mode IA i assegura que el paddle controlat per ArkanoidAgent rep
observacions, accions i recompenses coherents amb l'especificacio.
```

### 3.3 Prompts de refinament/correccio

**Prompt G - desviacio detectada en els blocs**
```text
[CORRECCIO] Encara hi ha una desviacio respecte a l'spec: el joc pot reaccionar abans que el bloc desaparegui de l'estat actiu.
Fes que el GameManager treballi amb l'estat real dels blocs actius i no amb un comptador desfasat.
```

---

## 4. Errors detectats i com s'han corregit

### Error 1 - L'especificacio original no corresponia a la feature real
- Problema: el material inicial parlava de CRUD de perfils, no del gameplay.
- Impacte: no servia per justificar la implementacio real del joc.
- Solucio: es va redefinir la feature com a "bucle principal de joc + mode IA".
- Canvi en el prompt: es va restringir explicitament l'abast a pilota, pala, blocs i agent IA.

### Error 2 - El nivell es completava quan encara podia quedar un bloc visible
- Problema: el recompte de blocs depenia d'un estat que podia desajustar-se.
- Impacte: final de nivell prematur.
- Solucio: recalcular els blocs restants a partir dels objectes actius reals de l'escena.
- Canvi en el prompt: es va demanar comparar la implementacio amb la seccio d'`spec.md` sobre block counting i level completion.

### Error 3 - Risc de barreja entre control huma i control IA
- Problema: en mode IA cal evitar que scripts manuals interfereixin amb l'agent.
- Impacte: comportament inconsistent del paddle.
- Solucio: documentar i verificar que en mode IA el control manual queda desactivat.
- Canvi en el prompt: es va forcar a revisar nomes el comportament de control i no la resta del joc.

---

## 5. Valoracio breu del proces

- La IA ha seguit millor l'especificacio quan el prompt era curt i amb abast molt concret.
- Les desviacions mes clares han aparegut quan l'abast era massa ampli o quan la implementacio real no coincidia amb l'especificacio inicial.
- Ha calgut modificar l'especificacio perque la primera no descrivia la feature correcta.
- Un cop acotada la feature, les iteracions han estat mes controlades i utils.

---

## 6. Conclusio

Aquest proces mostra que la IA es mes util quan:
- la feature esta ben acotada
- l'especificacio correspon realment al que es vol implementar
- cada correccio es formula contra una desviacio concreta

La tracabilitat no s'ha basat en "provar prompts fins que funcioni", sino en:
1. definir una especificacio
2. comparar el codi amb aquesta especificacio
3. corregir nomes les desviacions detectades
