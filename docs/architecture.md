# Arsitektur & Infrastruktur (Backend Absensi)

Backend absensi dibangun dengan ASP.NET Core 10 (`net10.0`) dalam pola hybrid Minimal API + MVC Controller, berbicara ke MySQL 8.0 melalui Dapper, dan dijalankan baik via Docker Compose maupun `dotnet run` lokal. Dokumen ini menjelaskan tech stack, struktur folder, urutan request pipeline, registrasi dependency injection, konfigurasi, setup, serta pengujian kontrak.

---

## Tech Stack

Berdasarkan `absensi.csproj` (`TargetFramework`: `net10.0`):

| Komponen | Teknologi |
| --- | --- |
| Framework | ASP.NET Core 10 (Minimal API + MVC hybrid) |
| Runtime / Target | .NET 10 (`net10.0`) |
| Database | MySQL 8.0 |
| ORM / akses data | Dapper (micro-ORM) |
| Koneksi MySQL | MySql.Data (`MySqlClient`) — satu-satunya yang dipakai di kode |
| Hashing password | BCrypt.Net-Next |
| Autentikasi | JWT Bearer |
| API docs | `Microsoft.AspNetCore.OpenApi` (`AddOpenApi`/`MapOpenApi`) + Scalar |
| Container | Docker + Docker Compose |

Daftar paket NuGet lengkap beserta versinya:

| Package | Versi |
| --- | --- |
| `BCrypt.Net-Next` | 4.2.0 |
| `Dapper` | 2.1.79 |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.10 |
| `Microsoft.AspNetCore.OpenApi` | 10.0.11 |
| `Microsoft.OpenApi` | 2.7.5 |
| `MySql.Data` | 26.7.0 |
| `MySqlConnector` | 2.6.1 |
| `Scalar.AspNetCore` | 2.17.2 |
| `Swashbuckle.AspNetCore` | 10.2.3 |

> Catatan: berkas `Dockerfile` memakai image `mcr.microsoft.com/dotnet/sdk:10.0` dan `mcr.microsoft.com/dotnet/aspnet:10.0` (komentar lama di file tersebut menyebut ".NET 8", tetapi versi yang benar mengikuti kode: .NET 10).
>
> Catatan paket terpasang tetapi tidak dipakai di kode: `Swashbuckle.AspNetCore` (tidak ada `AddSwaggerGen`/`UseSwagger`; OpenAPI dihasilkan `AddOpenApi()` + `MapOpenApi()`) dan `MySqlConnector` (seluruh akses DB memakai `MySql.Data.MySqlClient`).

---

## Struktur Folder

```
absensi/
├── Controller/
│   ├── AuthController.cs               # Minimal API — auth
│   ├── Admin/                          # Endpoint berbasis admin/master
│   │   ├── AdminUserController.cs      # MVC — CRUD semua user (Admin)
│   │   ├── DivisiController.cs         # Minimal API — divisi
│   │   ├── RoleController.cs           # Minimal API — role
│   │   └── StatusController.cs         # Minimal API — status
│   └── User/                           # Endpoint berbasis peran pengguna
│       ├── AnggotaController.cs        # MVC — CRUD Anggota (+ alias legacy)
│       ├── PMController.cs             # MVC — CRUD PM (+ alias legacy)
│       ├── DevOpsController.cs         # MVC — CRUD DevOps (+ alias legacy)
│       ├── GuruController.cs           # Minimal API — CRUD Guru
│       ├── ProjectController.cs        # Minimal API — project
│       ├── ProjectAnggotaController.cs # MVC — membership project
│       ├── TargetController.cs         # Minimal API — target
│       ├── HostingRequestController.cs # Minimal API — hosting request
│       └── AbsenController.cs          # Minimal API — absen
├── Models/                             # DTO request/response + konstanta
├── Services/
│   ├── Auth/                           # Autentikasi (register, login, token)
│   ├── Features/                       # Business logic per fitur
│   ├── MasterData/                     # Data referensi (role, divisi, status)
│   ├── Infrastructure/                 # Database, DatabaseBootstrap, ClaimsPrincipalExtensions
│   └── Interfaces/                     # Kontrak (JWT, Password, Env)
├── Middlewares/                        # GlobalExceptionHandler, UserStateValidationMiddleware, DatabaseHealthCheck
├── Absensi.Tests/                      # Unit/contract tests
├── Program.cs                          # Composition root & pipeline
├── setup.sql                           # DDL + seed (sekali saat volume DB baru)
├── appsettings.json                    # Konfigurasi
├── Dockerfile
└── docker-compose.yml
```

### Layer Arsitektur

Alur request mengikuti pola **Controller → Service → Dapper → MySQL**:

| Layer | Folder | Tanggung jawab |
| --- | --- | --- |
| Controller | `Controller/` | Menerima HTTP request, validasi dasar, mengembalikan response |
| Service | `Services/Features/` | Business logic utama (CRUD, query kompleks) |
| Master Data | `Services/MasterData/` | Query data referensi (role, divisi, status) |
| Auth | `Services/Auth/` | Autentikasi — register, login, manajemen token |
| Infrastructure | `Services/Infrastructure/` | Koneksi database (`Database`), bootstrap schema, klaim principal |
| Interfaces | `Services/Interfaces/` | Kontrak abstraksi JWT, Password, Env |
| Models | `Models/` | DTO/OTD untuk request & response |

Proyek memakai **hybrid pattern**: sebagian endpoint Minimal API (`Map*` static class), sebagian MVC Controller (`[ApiController]`); keduanya didaftarkan di `Program.cs`.

---

## Request Pipeline (Program.cs)

Urutan middleware dan pemetaan endpoint persis seperti di `Program.cs`:

1. **`GlobalExceptionHandler`** (`UseGlobalExceptionHandler`) — menangkap exception tak tertangani dan mengubahnya ke response JSON `ErrorResponse` dengan kode error yang sesuai.
2. **CORS** (`UseCors("AllowFrontend")`) — kebijakan `AllowFrontend`: bila `AllowedOrigins` kosong (mis. `dotnet run` lokal) maka `AllowAnyOrigin`; bila terisi, `WithOrigins(...)` + `AllowCredentials`.
3. **Rate Limiter** (`UseRateLimiter`) — `AddRateLimiter` (`Program.cs:24-60`) mendaftarkan dua policy fixed-window per IP, tetapi **tidak ada `FallbackPolicy`** dan pemasangan ke endpoint hanya melalui `RequireRateLimiting("auth")` pada tiga endpoint saja — `POST /register-admin`, `POST /login`, `POST /refresh` (`Controller/AuthController.cs:36,85,123`):
   - `auth`: 5 request / menit (queue limit 2) — hanya dipasang ke tiga endpoint autentikasi tersebut; `logout` dan `GET /me` tidak di-rate-limit.
   - `api`: 100 request / menit (queue limit 5) — policy terdaftar tetapi **tidak pernah dipasang ke endpoint mana pun**, sehingga API umum tidak di-rate-limit.
   - Saat limit terlampaui: respons `429` dengan pesan `"Terlalu banyak request. Silakan coba lagi nanti."` dan metadata `retryAfter`.
4. **OpenAPI & Scalar** (hanya environment Development) — `MapOpenApi()` + `MapScalarApiReference()`.
5. **Stopwatch logging** (middleware inline `app.Use`) — mencatat IP, method, path, status code, dan durasi request (ms) ke console; exception dicetak ulang lalu diteruskan.
6. **`UseAuthentication`** — JWT Bearer (issuer/audience/lifetime divalidasi, `ClockSkew` 1 menit).
7. **`UseUserStateValidation`** (`UserStateValidationMiddleware`) — memvalidasi state user terhadap DB via `ActiveUserService.IsStillActiveAsync(userId, tokenRole, ...)`. Request tanpa autentikasi dilewati (endpoint publik: login, register, health, docs); token tanpa `userId` atau user yang sudah tidak aktif/divonis di-revoke mendapat `401`.
8. **`UseAuthorization`** — kebijakan dari `Policies.Register`.
9. **Health check & endpoint** — `MapHealthChecks("/health")` (check `database`), lalu pemetaan endpoint: `MapDivisi`, `MapProject`, `MapRole`, `MapStatus`, `MapAuth`, `MapGuru`, `MapTarget`, `MapHostingRequest`, `MapControllers`, `MapAbsensiEndpoints`.

Sebelum pipeline berjalan, setelah `builder.Build()`: konfigurasi disimpan ke `Env.Value`, lalu **`DatabaseBootstrap.EnsureAsync`** dijalankan (dengan retry) untuk memastikan seed role/divisi/status dan tabel `hosting_request` ada.

---

## Registrasi Dependency Injection

Pendaftaran relevan di `Program.cs` (semua service fitur ber-scope **Scoped**):

| Registrasi | Tipe |
| --- | --- |
| `Database` | Scoped — koneksi MySQL dari `ConnectionStrings:DefaultConnection` |
| Health check `database` | `DatabaseHealthCheck` |
| Authorization policies | `Policies.Register` |
| Rate limiter | Policy `auth` & `api` |
| `IPasswordService` → `PasswordService` | Scoped (BCrypt) |
| `IJWTService` → `JWTService` | Scoped |
| `RoleServices`, `ProjectService`, `StatusService`, `AuthServices`, `DivisiService`, `AnggotaService`, `GuruService`, `PMService`, `AbsensiService`, `ProjectAnggotaService`, `TargetService`, `HostingRequestService`, `DevOpsService`, `AdminUserService`, `ActiveUserService` | Scoped, satu per fitur |
| Authentication | JWT Bearer (`AddAuthentication` + `AddJwtBearer`) |
| CORS | Policy `AllowFrontend` dari `AllowedOrigins` |

---

## Konfigurasi

### `appsettings.json`

| Key | Keterangan |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | Connection string MySQL (server, port, database, user, password) |
| `JWT:Key` | Secret signing key JWT (minimal 32 karakter) |
| `JWT:Issuer` | Issuer JWT (contoh: `absensi`) |
| `JWT:Audience` | Audience JWT (contoh: `absensiFrontend`) |
| `AllowedOrigins` | Daftar origin frontend dipisah koma untuk CORS (opsional; kosong = allow any) |

### Override environment variable (docker-compose)

Pada `docker-compose.yml`, service `backend` meng-override konfigurasi lewat env (format ASP.NET Core: `__` sebagai pemisah section):

| Variabel | Keterangan |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | Connection string ke service `db` di dalam network Docker |
| `JWT__Key` | Secret JWT (dari host `${JWT_KEY}`, fallback placeholder) |
| `AllowedOrigins` | Origin frontend yang diizinkan (default `http://localhost:5173,http://localhost:5174`) |
| `ASPNETCORE_URLS` | `http://+:8080` |
| `ASPNETCORE_ENVIRONMENT` | `Development` |

Service `db` juga menerima `MYSQL_ROOT_PASSWORD`, `MYSQL_DATABASE`, `MYSQL_USER`, `MYSQL_PASSWORD` (dengan default dev).

---

## Setup & Menjalankan

### Docker Compose (direkomendasikan)

```bash
docker compose up --build
```

| Service | Container | Port host → container |
| --- | --- | --- |
| Backend | `absensi_backend` | `5072` → `8080` |
| MySQL | `absensi_db` | `3307` → `3306` |

- Healthcheck `db` (`mysqladmin ping`, interval 5s, retries 20, start_period 15s); service `backend` memakai `depends_on` dengan `condition: service_healthy`, sehingga backend hanya start setelah MySQL siap.
- `setup.sql` dijalankan otomatis saat volume MySQL pertama dibuat; `DatabaseBootstrap` melengkapi seed/tabel saat startup aplikasi (dengan retry hingga ±2 menit).
- Perintah pendukung: `docker compose up --build -d`, `docker compose logs -f backend`, `docker compose down`, `docker compose down -v` (reset volume DB).

### Local (tanpa Docker)

Prasyarat: .NET 10 SDK, MySQL 8.0 lokal, database `absensi` dibuat (jalankan `setup.sql` manual), sesuaikan `ConnectionStrings:DefaultConnection` di `appsettings.json`.

```bash
dotnet restore
dotnet run
```

### Dokumentasi API

Hanya di environment **Development**: buka `http://localhost:5072/scalar` (UI interaktif Scalar) dan spec OpenAPI di `/openapi`. Endpoint kesehatan: `GET /health` (mengecek koneksi database).

---

## Pengujian

Proyek uji: `Absensi.Tests` (xUnit).

| Berkas | Cakupan |
| --- | --- |
| `EndpointContractTests.cs` | Kontrak route controller versioned (`api/v1/pm`, `api/v1/anggota`, `api/v1/devops`) + alias legacy (`api/PM`, `api/Anggota`, `api/DevOps`); otorisasi mutasi `ProjectAnggotaController` (Create/Delete wajib role `Admin`/`PM`); signature `AbsensiService.AbsenMasuk`/`AbsenPulang` wajib parameter `userId`/`idUser`; signature `ActiveUserService.IsStillActiveAsync` untuk validasi state per-request |
| `ClaimsPrincipalExtensionsTests.cs` | `TryGetUserId` membaca `ClaimTypes.NameIdentifier` dan fallback ke `sub`; `GetRoleName` untuk klaim role |

Cara menjalankan:

```bash
dotnet test
```

---

## Dokumen terkait

- [Arsitektur](architecture.md)
- [Database](database.md)
- [Autentikasi](authentication.md)
- [API Absensi](api-absensi.md)
- [API Hosting](api-hosting.md)
- [API Manajemen](api-management.md)
