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

| Szerepkor    | Jogosultsagok es feladatok                                                                                                                                      |
| :----------- | :-------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Oktato**   | Szabad termek keresese kapacitas alapjan, teremfoglalas inditasa, sajat foglalasok megtekintese, hibajegyek bejelentese.                                        |
| **Portas**   | Kulcsok fizikai kiadasanak es visszavetelenek gyors rogzitese, kulcsstatuszok lekerdezese (benn/kint), serulesek es vesztesegek azonnali jelzese.               |
| **Admin**    | Felhasznaloi fiokok es szerepkorok kezelese, uj termek es kulcsok felvitele, karbantartasok elrendelese, mesterkulcs-jogok kiosztasa, teljes audit log elerese. |
| **Igazgato** | Statisztikai kimutatasok, kihasznaltsagi riportok es hibanaplok megtekintese (olvasasi jog).                                                                    |

---

## 3. Rendszerinditas es fejlesztoi kornyezet

A projekt egy tobbkonteneres kornyezetben fut, amely magaban foglalja a **React** frontendet, a **.NET Core** backend REST API-t, valamint az **MSSQL** adatbazis-kiszolgalot. A teljes infrastruktura a legegyszerubben Docker segitsegevel indithato el.

### Elokovetelmenyek

- [Docker Desktop](https://www.docker.com/) telepitve es futtatva
- [Git](https://git-scm.com/)

### Gyors inditas

A projekt gyokerkonyvtaraban futtasd a kovetkezo parancsot:

```bash
docker compose up --build
```

A kontener-infrastruktura leallitasa:

```bash
docker compose down
```

### A futo szolgaltatasok portjai:

- Backend: 5000:8080
- Frontend: 3000:80
- Database: 1433:1433

---

## 4. Rendszerarchitektura (C4 Container szint)

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

## 5. Adatbazis sema (ER Diagram)

```mermaid
erDiagram
    Roles ||--o{ Users : "has"

    Users ||--o{ Bookings : "creates"
    Users ||--o{ KeyMovements : "performs"
    Users ||--o{ IssueTickets : "reports"
    Users ||--o{ Maintenances : "orders"
    Users ||--o{ MasterKeyPermissions : "granted_to"
    Users ||--o{ MasterKeyPermissions : "granted_by"

    Buildings ||--o{ Floors : "contains"
    Floors ||--o{ ClassRooms : "contains"
    ClassRooms ||--o{ Bookings : "booked_in"
    ClassRooms ||--o{ Maintenances : "maintained"
    ClassRooms ||--o{ IssueTickets : "reported_for"
    ClassRooms ||--o{ Keys : "has_standard_key"
    ClassRooms ||--o{ MasterKeyAccess : "accessible_via"

    Keys ||--o{ MasterKeyAccess : "opens"
    Keys ||--o{ MasterKeyPermissions : "authorized_for"
    Keys ||--o{ KeyMovements : "moved"
    Keys ||--o{ IssueTickets : "reported_for"

    Bookings |o--o{ KeyMovements : "associated_with"

    Roles {
        int Id PK
        int Name
    }

    Users {
        int Id PK
        int RoleId FK
        string Name
        string Email
        string PasswordHash
    }

    Bookings {
        int Id PK
        int UserId FK
        string RoomId FK
        datetime StartTime
        datetime EndTime
        string Status
        string SpecialRequest
    }

    Buildings {
        int Id PK
        string BuildingCode UK
        string BuildingName
    }

    Floors {
        int Id PK
        int Number
        int BuildingId FK
    }

    ClassRooms {
        string Id PK
        int FloorId FK
        string Name
        int Capacity
        string Equipment
    }

    Maintenances {
        int Id PK
        string RoomId FK
        int AdminId FK
        datetime StartTime
        datetime EndTime
    }

    IssueTickets {
        int Id PK
        int KeyId FK "nullable"
        string RoomId FK
        int ReporterId FK
        string Description
        string Status
    }

    KeyMovements {
        int Id PK
        int KeyId FK
        int UserId FK
        int BookingId FK "nullable"
        datetime Timestamp
        string IdentificationMethod
    }

    Keys {
        int Id PK
        string RoomId FK "nullable"
        string KeyType
        string MasterKeyName "nullable"
    }

    MasterKeyAccess {
        string AccessibleRoomsId PK,FK
        int MasterKeyId PK,FK
    }

    MasterKeyPermissions {
        int Id PK
        int UserId FK
        int KeyId FK
        int GrantedByAdminId FK
        datetime GrantedAt
    }
```

## 6. API dokumentáció

**Health**

- GET /api/Health: A rendszer es az adatbazis-kapcsolat valos ideju allapotanak lekerdezese.

**KulcsRendszer.API**

- GET / : Alapertelmezett gyoker vegpont az API futasanak es elerhetosegenek ellenorzesere.

**User**

- GET /api/User/{id} : Egy adott azonositoju felhasznalo alapveto adatainak lekerdezese.
- DELETE /api/User/{id} : Felhasznalo torlese a rendszerbol id alapjan.
- POST /api/User/create : Uj felhasznalo regisztracioja / letrehozasa a rendszerben.
- PUT /api/User/update/{id} : Meglevo felhasznalo adatainak modositasa.
- POST /api/User/change-password/{id} : Felhasznalo jelszavanak biztonsagos megvaltoztatasa.
- POST /api/User/role/{userId} : Szerepkor hozzarendelese egy adott azonositoju felhasznalohoz.
- POST /api/User/role : Uj szerepkor hozzaadasa / letrehozasa a rendszerben.
- DELETE /api/User/role/{Id} : Szerepkor torlese a rendszerbol id alapjan.
- DELETE /api/User/role/remove/{Id} : Szerepkor eltavolita egy felhasznalotol vagy a rendszerbol id alapjan.

**Floor**

- GET /api/Floor/getall : Az osszes emelet listajanak lekerdezese a rendszerbol.
- GET /api/Floor/get/{id} : Egy adott azonositoju emelet alapadatainak lekerdezese.
- GET /api/Floor/info/{id} : Egy adott azonositoju emelet reszletes informacioinak lekerdezese.
- GET /api/Floor/{floorId}/rooms : Egy adott emelethez tartozo termek listajanak es azok tulajdonsagainak lekerdezese.
- POST /api/Floor/create : Uj emelet letrehozasa a rendszerben.
- PUT /api/Floor/update/{id} : Meglevo emelet adatainak modositasa id alapjan.
- DELETE /api/Floor/delete/{id} : Emelet torlese a rendszerbol id alapjan.

**Building**

- GET /api/Building/getall : Az epuletek teljes listajanak lekerdezese a rendszerbol.
- GET /api/Building/get/{id} : Egy adott azonositoju epulet alapadatainak lekerdezese.
- GET /api/Building/info/{id} : Egy adott azonositoju epulet reszletes informacioinak lekerdezese.
- POST /api/Building/create : Uj epulet letrehozasa a rendszerben.
- PUT /api/Building/update/{id} : Meglevo epulet adatainak modositasa id alapjan.
- DELETE /api/Building/delete/{id} : Epulet torlese a rendszerbol id alapjan.
