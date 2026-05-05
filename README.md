# Inkomen-Lasten Toets (ILT)

> Sollicitatie-opdracht — vereenvoudigde Inkomen-Lasten Toets.

Implementatie van een ILT op basis van banktransacties die via een REST-endpoint worden opgehaald. Geschreven in C# / .NET 8.

## Architectuur

Klassieke gelaagde opzet met scheiding van zorgen:

```
src/
  ILT.Domain          // POCO's: Transaction, MonthlyIlt, IltResult
  ILT.Application     // configuratie, classifier, calculator, service, abstracties
  ILT.Infrastructure  // HttpTransactionSource (REST-client), DI-extensions
  ILT.Api             // ASP.NET Core Web API + Swagger
tests/
  ILT.Tests           // xUnit + FluentAssertions
```

De Domain-laag heeft geen externe dependencies. De Application-laag kent alleen de Domain-laag en `Microsoft.Extensions.Options`. De Api-laag bedraadt alles via DI; vervangen van `ITransactionSource` (bv. door een database-implementatie) raakt de berekening niet.

## Snel starten

```bash
dotnet build ILT.sln
dotnet test  tests/ILT.Tests/ILT.Tests.csproj
dotnet run --project src/ILT.Api/ILT.Api.csproj
```

De API draait op `http://localhost:5080`. Swagger UI: `http://localhost:5080/swagger`.

### Endpoint

```
GET /api/ilt
GET /api/ilt?accounts=NL67BANK0123456789&accounts=NL21PART0987654321
```

Zonder `accounts`-parameter worden de in `ilt-config.json` geconfigureerde rekeningen gebruikt. Voorbeeld-respons (ingekort):

```json
{
  "accountNumbers": ["NL67BANK0123456789", "NL21PART0987654321"],
  "months": [
    { "period": "2025-01", "income": 3600.00, "expenses": 1875.25, "disposable": 1724.75 }
  ],
  "averageMonthlyIncome": 4473.08,
  "averageMonthlyExpenses": 1757.37,
  "averageMonthlyDisposable": 2715.71,
  "transactionCount": 72,
  "ignoredTransactionCount": 12,
  "unknownCategories": ["Cadeaus", "Elektronica", "Freelance", "Horeca", "OV"]
}
```

`unknownCategories` bevat categorieën die noch als inkomen, noch als last, noch als expliciet genegeerd zijn ingedeeld. Dit dwingt de business niet tot een keuze, maar maakt de keuze wél zichtbaar.

## Configuratie

Alle systematiek staat in `src/ILT.Api/ilt-config.json` (gekopieerd naar de output-folder). Het bestand wordt **hot-reloaded** — wijzigingen werken zonder herstart dankzij `IOptionsMonitor<T>`.

```json
{
  "Ilt": {
    "IncomeCategories":  ["Salaris", "Belastingteruggave", "Dividend", ...],
    "ExpenseCategories": ["Huur", "Boodschappen", "Streaming", ...],
    "IgnoredCategories": ["Sparen", "Eigen overboeking"],
    "MultiMonthHandling": "AssignToTransactionDate",
    "MinAbsoluteAmount": 0,
    "ExcludedCounterAccounts": []
  },
  "TransactionsApi": {
    "BaseUrl": "https://solliciteren.edrgroup.nl/.../",
    "PathTemplate": "{accountNumber}.json?/q=/Transactions",
    "AccountNumbers": ["NL67BANK0123456789", "NL21PART0987654321"],
    "TimeoutSeconds": 30
  }
}
```

| Knop | Effect |
|---|---|
| `IncomeCategories` | Telt mee als inkomen |
| `ExpenseCategories` | Telt mee als last |
| `IgnoredCategories` | Expliciet genegeerd (geen waarschuwing) |
| `MultiMonthHandling` | `AssignToTransactionDate` (default) of `SpreadEvenly` — bij `SpreadEvenly` wordt het bedrag gespreid over `[PeriodStart, PeriodEnd]` als die velden aanwezig zijn |
| `MinAbsoluteAmount` | Drempelwaarde — kleine transacties worden overgeslagen |
| `ExcludedCounterAccounts` | Bv. eigen spaarrekeningen uitsluiten |

## Aannames en ontwerpkeuzes

- **Categorie-gestuurde classificatie**, niet teken-gestuurd. Sommige banken sturen lasten als positief getal; door op de categorie te leunen werkt de logica ongeacht het teken. In de calculator wordt altijd `Math.Abs(...)` opgeteld, zodat een negatieve "Salaris"-correctie ook netjes als inkomen telt.
- **Onbekende categorieën worden gerapporteerd, niet geraden.** De curl-output bevat categorieën als `Elektronica` (eenmalige laptopaankoop) en `Freelance`. Of die als last/inkomen tellen is een business-keuze; ze verschijnen daarom in `unknownCategories` zodat de keuze expliciet is.
- **Multi-month transacties**: de huidige API-spec kent geen periodevelden. Het `Transaction`-model in de Domain-laag kent al optionele `PeriodStart`/`PeriodEnd`, en de calculator ondersteunt evenredige spreiding. Zodra het contract wordt uitgebreid is alleen mapping nodig in `HttpTransactionSource.TransactionDto.ToDomain()`.
- **Forward-compat op het JSON-contract**: `TransactionDto` gebruikt `[JsonExtensionData]` zodat onbekende velden (de aankondigde uitbreiding) bewaard worden in `Transaction.AdditionalProperties` zonder te crashen. De deserialisatie accepteert daarnaast zowel een platte array (huidige spec) als een envelope-object met `transactions`-veld.
- **Hot-reload van configuratie** via `IOptionsMonitor<IltConfiguration>`. Bij elke aanroep wordt `CurrentValue` gelezen.
- **Concurrency**: transacties voor meerdere rekeningen worden parallel opgehaald (`Task.WhenAll` in `IltService`).
- **Foutafhandeling**: HTTP-fouten van de upstream resulteren in HTTP 502. Validatie op startup faalt fast als `BaseUrl` of `AccountNumbers` ontbreekt.

## Tests

17 unit tests (xUnit + FluentAssertions) dekken:

- Categorie-classificatie (case-insensitive, ignored, unknown)
- Maand-aggregatie van inkomen/lasten
- Drempelfilter (`MinAbsoluteAmount`)
- Uitsluiting van counter-accounts
- Spreiding over meerdere maanden
- Tolerantie voor positieve én negatieve bedragen bij dezelfde categorie
- Deserialisatie van het exacte snake_case-contract uit de OpenAPI-spec
- Forward-compat: extra velden breken niet
- End-to-end test met de échte payload uit de live-endpoint, om regressies in de configuratie te vangen

```bash
dotnet test --nologo
# Passed: 17, Failed: 0
```

## Pluspunten die zijn meegenomen

- Dependency Injection via `Microsoft.Extensions.DependencyInjection`
- Configuratie zonder herstart (`reloadOnChange: true` + `IOptionsMonitor<T>`)
- Logging via `ILogger<T>`
- Foutafhandeling op upstream-API (502 met heldere body)
- Swagger UI op `/swagger`
- `TreatWarningsAsErrors` in alle productie-projecten
# ilt-api
