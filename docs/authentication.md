# Autentikasi & Otorisasi (Backend Absensi)

Backend menggunakan **JWT Bearer** (access token 7 hari + refresh token 20 hari) dengan signing HS256, password di-hash **BCrypt**, dan satu middleware (`UserStateValidationMiddleware`) yang memvalidasi state user terhadap database di **setiap request terautentikasi** — sehingga akun yang dihapus atau role yang diturunkan langsung kehilangan akses meski token masih berlaku.

---

## 1. Ringkasan Arsitektur Auth

| Komponen | File | Peran |
| -------- | ---- | ----- |
| Endpoint auth | `Controller/AuthController.cs` | Minimal API `/api/v1/auth/*` |
| Layanan auth | `Services/Auth/AuthServices.cs` | Register, login, refresh, logout, profil |
| JWT & refresh token | `Services/Interfaces/IJWTService.cs` (`JWTService`) | Generate access token & refresh token acak |
| Hashing password | `Services/Interfaces/IPasswordService.cs` (`PasswordService`) | BCrypt hash/verify |
| Validasi state per-request | `Middlewares/UserStateValidationMiddleware.cs` + `Services/Auth/ActiveUserService.cs` | Cek user masih ada & role DB == role token |
| Helper claim | `Services/Infrastructure/ClaimsPrincipalExtensions.cs` | `TryGetUserId`, `GetRoleName` |
| Konfigurasi & pipeline | `Program.cs` | `TokenValidationParameters`, urutan middleware |
| Policy authorization | `Models/Auth.cs` (`Policies`) | `RequireRole` per role |
| Role ID | `Models/Constants.cs` (`RoleIds`) | Admin=1 … DevOps=5 |
| CRUD user admin | `Services/Features/AdminUserService.cs` | Pencabutan refresh token saat hapus/demote |

Urutan pipeline relevan di `Program.cs`:

```
UseRateLimiter → UseAuthentication → UseUserStateValidation → UseAuthorization → endpoint
```

---

## 2. JWT (Access Token)

### 2.1 Klaim yang di-generate

Dari `JWTService.GenerateToken(User user)`:

| Claim | Sumber | Nilai |
| ----- | ------ | ----- |
| `sub` (`JwtRegisteredClaimNames.Sub`) | `user.id` | ID user (string) |
| `ClaimTypes.NameIdentifier` | `user.id` | ID user (string) — dipakai `NameClaimType` |
| `ClaimTypes.Name` | `user.Nama` | Nama user |
| `ClaimTypes.Role` | `user.Role` | Nama role (`Admin`, `PM`, `Guru`, `Anggota`, `DevOps`) |

### 2.2 Parameter token

| Properti | Nilai |
| -------- | ----- |
| Lifetime | **7 hari** (`DateTime.UtcNow.AddDays(7)`) |
| Signing | **HMAC-SHA256** (`SecurityAlgorithms.HmacSha256`) dengan `JWT:Key` |
| Issuer | `JWT:Issuer` (konfigurasi) |
| Audience | `JWT:Audience` (konfigurasi) |

### 2.3 Validasi di `Program.cs` (`TokenValidationParameters`)

| Parameter | Nilai |
| --------- | ----- |
| `ValidateIssuer` / `ValidIssuer` | `true` / `JWT:Issuer` |
| `ValidateAudience` / `ValidAudience` | `true` / `JWT:Audience` |
| `ValidateLifetime` | `true` |
| `ClockSkew` | **1 menit** |
| `ValidateIssuerSigningKey` | `true` (symmetric key dari `JWT:Key`) |
| `NameClaimType` | `ClaimTypes.NameIdentifier` |
| `RoleClaimType` | `ClaimTypes.Role` |

`JWT:Key`, `JWT:Issuer`, dan `JWT:Audience` wajib terisi; aplikasi langsung `throw` jika kosong.

### 2.4 Refresh token

- Di-generate oleh `JWTService.GenerateRefreshToken()`: 64 byte dari `RandomNumberGenerator`, dikodekan Base64.
- Disimpan di kolom `user.refresh_token` beserta `user.refresh_token_expired`.
- Masa berlaku saat login/refresh: **20 hari** (`DateTime.UtcNow.AddDays(20)`).

---

## 3. Password (BCrypt)

`IPasswordService` / `PasswordService`:

| Method | perilaku |
| ------ | -------- |
| `HashPassword` | `BCrypt.Net.BCrypt.HashPassword` (salt otomatis) |
| `VerifyPassword` | `BCrypt.Net.BCrypt.Verify` |

Dipakai saat register admin dan create user admin (`AdminUserService`). Pada endpoint `POST /login`, verifikasi dilakukan langsung dengan `BCrypt.Net.BCrypt.Verify(login.password, user.password)` di controller.

---

## 4. Endpoint Auth — `/api/v1/auth`

Base URL: `http://localhost:5072` (lihat README untuk konfigurasi). Endpoint `register-admin`, `login`, dan `refresh` memakai rate limit policy **`auth`**: **5 request/menit per IP** (queue 2); jika dilampaui → **429** `{ "message": "Terlalu banyak request. Silakan coba lagi nanti.", "retryAfter": … }`. Endpoint `GET /me` dan `POST /logout` hanya memasang `RequireAuthorization()` tanpa rate limiting.

| Method | Endpoint | Auth | Role | Rate limit |
| ------ | -------- | ---- | ---- | ---------- |
| POST | `/api/v1/auth/register-admin` | Publik | — (sekali saja) | `auth` |
| POST | `/api/v1/auth/login` | Publik | Semua role | `auth` |
| POST | `/api/v1/auth/refresh` | Publik | — (refresh token valid) | `auth` |
| GET | `/api/v1/auth/me` | JWT | `RequireAuthorization` (login apa pun) | — |
| POST | `/api/v1/auth/logout` | JWT | `RequireAuthorization` (login apa pun) | — |

Catatan penamaan field response: serialisasi JSON memakai camelCase, sehingga properti `LoginResponse.Refresh_Token` muncul sebagai **`refresh_Token`** (hanya huruf pertama huruf kecil).

### 4.1 `POST /register-admin` — daftar admin (sekali saja)

Mendaftarkan admin pertama. `AuthServices.IsRegistered()` mengecek `COUNT(*) user WHERE id_role = 1`; jika sudah ada → **400**. Role selalu dipaksa `RoleIds.Admin` (1) — field `id_role` pada body tidak menentukan role akhir.

**Request body** (`AdminOTD`):

| Field | Tipe | Keterangan |
| ----- | ---- | ---------- |
| `nama` | string | Username |
| `password` | string | Akan di-hash BCrypt |
| `id_role` | int | Diabaikan; admin selalu `id_role = 1` |
| `id_divisi` | int \| null | Divisi; `<= 0` dinormalkan ke `null` |

```json
{
  "nama": "AdminUtama",
  "password": "password123",
  "id_role": 1,
  "id_divisi": null
}
```

**Response:**

| Status | Body |
| ------ | ---- |
| 200 | `{ "message": "Admin berhasil didaftarkan" }` |
| 400 | `{ "message": "Registrasi admin ditutup karena admin sudah ada" }` |
| 400 | `{ "message": "Gagal mendaftarkan admin" }` / `{ "message": "Terjadi kesalahan saat registrasi" }` |

### 4.2 `POST /login`

**Request body** (`Login`): `nama`, `password` (keduanya wajib).

**Response 200** (`LoginResponse`):

| Field (JSON) | Tipe | Keterangan |
| ------------ | ---- | ---------- |
| `token` | string | JWT access token (7 hari) |
| `refresh_Token` | string | Refresh token Base64 (20 hari) |
| `nama` | string | Nama user |
| `role` | string | Nama role |

**Response lain:**

| Status | Kondisi |
| ------ | ------- |
| 400 | `nama`/`password` kosong → `{ "message": "Nama dan password wajib diisi" }` |
| 401 | User tidak ada, atau password salah (BCrypt gagal) — tanpa pesan detail |
| 400 | Exception internal → `{ "message": "Terjadi kesalahan saat login" }` |

Side effect sukses: `AuthServices.UpdateRefreshToken` menulis `refresh_token` + `refresh_token_expired` (UTC+20 hari) ke tabel `user`.

### 4.3 `POST /refresh`

Menukar refresh token lama dengan access token + refresh token baru (rotasi).

**Request body** (`RefreshRequest`):

| Field (JSON) | Tipe |
| ------------ | ---- |
| `refreshToken` | string |

```json
{ "refreshToken": "base64encodedrefreshtoken..." }
```

**Alur:**

1. `AuthServices.RefreshTokenService` → `SELECT … WHERE u.refresh_token = @refreshToken` (join `role`).
2. Jika user tidak ketemu **atau** `refreshTokenExpired < DateTime.UtcNow` → **401**.
3. Jika valid → generate JWT baru + refresh token baru, `UPDATE` ke `user`, response 200 dengan struktur sama seperti login (`token`, `refresh_Token`, `nama`, `role`).

Refresh token lama hangus karena digantikan di kolom yang sama (hanya satu refresh token aktif per user).

### 4.4 `GET /me`

`RequireAuthorization`. Ambil ID dari claim via `TryGetUserId`, lalu `AuthServices.GetMe` (join `role` + `divisi`).

**Response 200** (`UserDTO`):

| Field | Tipe |
| ----- | ---- |
| `id` | int |
| `nama` | string |
| `role` | string |
| `divisi` | string \| null |

**Response lain:** 401 (tanpa/invalid token); 404 `{ "message": "User tidak ditemukan" }`.

### 4.5 `POST /logout`

`RequireAuthorization`. Menghapus refresh token user (`refresh_token = NULL`, `refresh_token_expired = NULL`). Access token yang sudah terbit tetap berlaku sampai expired 7 hari, tetapi `UserStateValidationMiddleware` tetap memvalidasi keberadaan user di DB.

**Response 200:** `{ "message": "Logout berhasil" }`  
**Response 400:** `{ "message": "Gagal melakukan logout" }` / `{ "message": "Terjadi kesalahan saat logout" }`

---

## 5. Validasi State Per-Request (mengapa JWT stateless tidak cukup)

Access token berlaku **7 hari** tanpa blacklist. Tanpa pemeriksaan tambahan, user yang sudah dihapus atau diturunkan role-nya tetap bisa memakai token lama sampai expired, dan tetap bisa refresh selama refresh token tidak dihapus.

### 5.1 `UserStateValidationMiddleware`

Dijalankan setelah `UseAuthentication`, sebelum `UseAuthorization` (`Program.cs`).

| Kondisi | Hasil |
| ------- | ----- |
| Request tidak terautentikasi (endpoint publik: login, register, dsb.) | Lanjut tanpa cek DB |
| Tidak bisa ekstrak `userId` dari claim | **401** `{ "message": "Token tidak valid." }` |
| `ActiveUserService.IsStillActiveAsync` = false | **401** `{ "message": "Sesi tidak valid. Silakan login kembali." }` |
| Aktif | Lanjut ke authorization/endpoint |

### 5.2 `ActiveUserService.IsStillActiveAsync`

Query: ambil `role.nama` dari `user INNER JOIN role WHERE user.id = @Id`.

| Kondisi | Aktif? |
| ------- | ------ |
| User tidak ada di DB (dihapus) | Tidak |
| Role DB ≠ role token (case-insensitive) | Tidak |
| Role token kosong pada klaim | Ya (selama user ada) |
| Role DB == role token | Ya |

Jadi **dua sumber kebenaran**: JWT untuk identitas/otorisasi cepat, database untuk status keanggotaan & role terkini.

---

## 6. Pencabutan Akses Admin (hapus / demote user)

`AdminUserService` (dipakai CRUD user admin) menjamin akses hangus bersamaan dengan validasi middleware:

| Aksi | Perilaku di `AdminUserService` |
| ---- | ------------------------------ |
| **Update role** oleh actor lain (`data.IdRole != current.RoleId && actorId != id`) | `refresh_token` dan `refresh_token_expired` di-`NULL`-kan — refresh token target hangus, dipaksa login ulang |
| **Admin demote diri sendiri** | Ditolak: `InvalidOperationException("Admin tidak dapat menurunkan role dirinya sendiri.")` |
| **Password diganti** | Hash BCrypt baru ditulis; (komentar kode menyebut paksa login ulang — yang memblokir sesi lama secara langsung tetap middleware bila role ikut berubah, atau refresh token expired/logout) |
| **Hapus user** | Ditolak jika `actorId == id` (`"Admin tidak dapat menghapus akun sendiri."`) dan jika target admin terakhir (`"Admin terakhir tidak dapat dihapus."`). Row `user` dihapus (FK cascade) + `hosting_request` terkait ikut dihapus dalam satu transaksi |
| **Setelah hapus / setelah role berubah** | `IsStillActiveAsync` gagal → setiap request terautentikasi token lama memperoleh **401** `"Sesi tidak valid. Silakan login kembali."` meski `exp` JWT belum lewat |

Commit terbaru (`fix(auth): validate user state per-request and revoke admin access on delete/demote`) menggabungkan kedua lapisan: **pencabutan refresh token** saat demote, dan **validasi state per-request** yang menutup celah access token 7 hari.

---

## 7. Role, ID, dan Policy Authorization

### 7.1 `RoleIds` (`Models/Constants.cs`) — sesuai seed database

| Role | ID (`RoleIds`) |
| ---- | -------------- |
| Admin | 1 |
| PM | 2 |
| Guru | 3 |
| Anggota | 4 |
| DevOps | 5 |

### 7.2 Policy (`Policies.Register` di `Models/Auth.cs`)

Didaftarkan via `builder.Services.AddAuthorization(Policies.Register)` di `Program.cs`. Semua memakai `RequireRole(...)` terhadap claim role di token (`RoleClaimType = ClaimTypes.Role`).

| Policy name | `RequireRole` |
| ----------- | ------------- |
| `Admin` | `Admin` |
| `PM` | `PM` |
| `Guru` | `Guru` |
| `Anggota` | `Anggota` |
| `DevOps` | `DevOps` |

Penerapan policy per kelompok endpoint (absensi, manajemen user/role/divisi, hosting, dst.) didokumentasikan terpisah:

- [API Manajemen](api-management.md) — endpoint master data & CRUD user beserta policy-nya
- [API Hosting](api-hosting.md) — alur hosting request (PM review, DevOps handler) beserta policy-nya
- [API Absensi](api-absensi.md) — endpoint absen yang menuntut JWT login

---

## 8. `ClaimsPrincipalExtensions`

File: `Services/Infrastructure/ClaimsPrincipalExtensions.cs`.

### `TryGetUserId(this ClaimsPrincipal user, out int userId)`

Urutan pembacaan klaim ID:

1. `ClaimTypes.NameIdentifier`
2. `JwtRegisteredClaimNames.Sub` (`sub` JWT)
3. Literal `"sub"`

Hasil string diparse ke `int`; sukses hanya jika `userId > 0`. Dipakai middleware, `GET /me`, `POST /logout`, dan endpoint absensi.

### `GetRoleName(this ClaimsPrincipal user)`

Urutan: `ClaimTypes.Role` → `"role"` → klaim yang tipenya diakhiri `/identity/claims/role`. Dipakai `UserStateValidationMiddleware` untuk membandingkan role token dengan role DB.

---

## 9. Konfigurasi terkait

| Key | Dipakai untuk |
| --- | ------------- |
| `JWT:Key` | Signing & validasi HMAC-SHA256 (env `JWT__Key`) |
| `JWT:Issuer` | `ValidIssuer` |
| `JWT:Audience` | `ValidAudience` |

Registrasi DI di `Program.cs`: `IPasswordService`, `IJWTService`, `AuthServices`, `ActiveUserService`, `AdminUserService` (scoped).

---

## Dokumen terkait

- [Arsitektur](architecture.md)
- [Database](database.md)
- [API Absensi](api-absensi.md)
- [API Hosting](api-hosting.md)
- [API Manajemen](api-management.md)
