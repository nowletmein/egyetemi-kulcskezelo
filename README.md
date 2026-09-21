# egyetemi-kulcskezelo
Egyetemi Kulcs- és Teremnyilvántartó Rendszer PROJEKT LABOR - 2026/27/1 Csoport munka

---

## 1. A projekt celja es attekintese

Az egyetemi oktatasi termek, laborok, valamint a hozzajuk tartozo fizikai kulcsok nyilvantartasa gyakran manualisan, papiralapon tortenik, ami adminisztracios terhet, kulcselvesztest es foglalasi utkozeseket eredmenyez.

A rendszer celja egy kozpontositott, digitalis felulet biztositasa, amely:
- Valos idoben koveti a fizikai kulcsok kiadasat es visszavetelet.
- Kezeli az oktatok idopontalapu teremfoglalasait es a termek kapacitasat.
- Digitalis hibajegykezelo modult nyujt a serult kulcsok, zarak vagy teremi eszkozok bejelentesere.
- Adminisztratori engedelyhez koti a mesterkulcsok felvetelet es audit naplot vezet a folyamatokrol.

---

## 2. Szerepkorok es jogosultsagok

| Szerepkor | Jogosultsagok es feladatok |
| :--- | :--- |
| **Oktato** | Szabad termek keresese kapacitas alapjan, teremfoglalas inditasa, sajat foglalasok megtekintese, hibajegyek bejelentese. |
| **Portas** | Kulcsok fizikai kiadasanak es visszavetelenek gyors rogzitese, kulcsstatuszok lekerdezese (benn/kint), serulesek es vesztesegek azonnali jelzese. |
| **Admin** | Felhasznaloi fiokok es szerepkorok kezelese, uj termek es kulcsok felvitele, karbantartasok elrendelese, mesterkulcs-jogok kiosztasa, teljes audit log elerese. |
| **Igazgato** | Statisztikai kimutatasok, kihasznaltsagi riportok es hibanaplok megtekintese (olvasasi jog). |

---

## 3. Rendszerarchitektura (C4 Container szint)

```mermaid
graph TB
    user["<b>Felhasznalo</b><br/><small>Oktato, Portas, Admin, Igazgato</small>"]

    subgraph system_boundary ["Egyetemi Kulcs- es Teremnyilvantarto Rendszer"]
        direction TB
        
        spa["<b>Kliens (UI)</b><br/><i>[React]</i><br/><small>Bejelentkezes, naptar, kulcskezelo, riportok</small>"]
        backend["<b>Backend API (Auth + Uzleti Logika)</b><br/><i>[C# / .NET]</i><br/><small>Autentikacio, RBAC, utkozesvizsgalat, audit log, ertesitesek</small>"]
        database[("<b>Adatbazis</b><br/><i>[MSSQL]</i><br/><small>3NF adatmodell: termek, kulcsok, foglalások, audit</small>")]
    end

    user -->|"Hasznalja<br/>HTTPS"| spa
    spa -->|"REST API keresek<br/>(JWT Token / HTTPS)"| backend
    backend -->|"Adatkezeles es Auth check<br/>SQL / ORM"| database

    classDef person fill:#08427b,stroke:#073b6f,color:#fff;
    classDef container fill:#438dd5,stroke:#3c7fc0,color:#fff;
    classDef db fill:#23a2d9,stroke:#1e8bc0,color:#fff;
    classDef boundary fill:none,stroke:#b1b1b1,stroke-dasharray: 5 5,color:#444;

    class user person;
    class spa,backend container;
    class database db;
    class system_boundary boundary;
```

---

## 4. Adatbazis sema (ER Diagram)

```mermaid
erDiagram
  FELHASZNALO ||--o{ FOGLALAS : letrehoz
  FELHASZNALO ||--o{ KULCSMOZGAS : vegzi
  FELHASZNALO ||--o{ HIBAJEGY : bejelent
  FELHASZNALO ||--o{ KARBANTARTAS : elrendel
  FELHASZNALO ||--o{ MESTERKULCS_JOG : kap
  TEREM ||--o{ KULCS : tartalmaz
  TEREM ||--o{ FOGLALAS : erint
  TEREM ||--o{ HIBAJEGY : erint
  TEREM ||--o{ KARBANTARTAS : erint
  KULCS ||--o{ KULCSMOZGAS : mozog
  KULCS ||--o{ HIBAJEGY : erint
  KULCS ||--o{ MESTERKULCS_JOG : jogosit
  FOGLALAS |o--o{ KULCSMOZGAS : kivalthat

  FELHASZNALO {
    int id PK
    string nev
    string email
    password jelszo
    string szerepkor
  }
  TEREM {
    string id PK
    string epulet
    int szint
    string nev
    int ferohely
    string felszereltseg
  }
  KULCS {
    int id PK
    string terem_id FK
    string tipus
  }
  FOGLALAS {
    int id PK
    int oktato_id FK
    string terem_id FK
    datetime kezdet
    datetime veg
    string statusz
  }
  KULCSMOZGAS {
    int id PK
    int kulcs_id FK
    int felhasznalo_id FK
    int foglalas_id FK
    string tipus
    datetime idobelyeg
    string azonositas_mod
  }
  HIBAJEGY {
    int id PK
    int kulcs_id FK
    string terem_id FK
    int bejelento_id FK
    string leiras
    string statusz
  }
  KARBANTARTAS {
    int id PK
    string terem_id FK
    int admin_id FK
    datetime kezdet
    datetime veg
  }
  MESTERKULCS_JOG {
    int id PK
    int kulcs_id FK
    int felhasznalo_id FK
    int admin_id FK
    date datum
  }