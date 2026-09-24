# API Manajemen User & Master Data (Backend Absensi)

Dokumen ini mendeskripsikan endpoint **CRUD user** (Admin dan per-role: Guru, PM, Anggota, DevOps) serta **master data** (Role, Status, Divisi) pada backend absensi. Semua path dan aturan di bawah diverifikasi langsung terhadap atribut route di controller, service, dan `Absensi.Tests/EndpointContractTests.cs` — bukan dari README lama (beberapa baris README tidak lagi cocok dengan kode; kode yang menang).

Base URL default: `http://localhost:5072`. Semua endpoint memerlukan header:

```
Authorization: Bearer <token>
```

kecuali dinyatakan lain. Detail login/refresh token: [Autentikasi](authentication.md).

JSON request memakai `PropertyNameCaseInsensitive = true`, sehingga `nama`/`Nama` dan setara camelCase saling diterima.

---

## 1. Admin User CRUD — `/api/v1/admin/users`

Kontroler: `Controller/Admin/AdminUserController.cs` (MVC `[ApiController]`), delegasi ke `Services/Features/AdminUserService.cs`.

**Autorisasi:** seluruh controller `[Authorize(Roles = "Admin")]` — hanya role **Admin**.

| Method | Endpoint | Auth | Deskripsi |
| ------ | -------- | ---- | --------- |
| GET | `/api/v1/admin/users` | Admin | Daftar semua user (opsional filter role) |
| GET | `/api/v1/admin/users/{id}` | Admin | Detail satu user |
| POST | `/api/v1/admin/users` | Admin | Buat user baru |
| PUT | `/api/v1/admin/users/{id}` | Admin | Perbarui user |
| DELETE | `/api/v1/admin/users/{id}` | Admin | Hapus user |

Tidak ada alias legacy untuk controller ini.

### GET `/api/v1/admin/users`

**Query parameter:**

| Parameter | Tipe | Keterangan |
| --------- | ---- | ---------- |
| `roleId` | int? | Jika diisi, hanya user dengan `id_role` tersebut |

**Response 200** — array `UserDTO`:

```json
[
  {
    "id": 1,
    "nama": "AdminUtama",
    "role": "Admin",
    "divisi": null
  }
]
```

### GET `/api/v1/admin/users/{id}`

- **200** — objek `UserDTO` seperti di atas.
- **404** — `{ "message": "User tidak ditemukan." }`

### POST `/api/v1/admin/users`

**Request body** (`AdminUserCreateDTO` — camelCase; alias snake_case juga diterima):

| Field | Tipe | Wajib | Keterangan |
| ----- | ---- | ----- | ---------- |
| `nama` | string | ya | Unik; maks 255 karakter; tidak boleh kosong |
| `password` | string | ya | Min 8 karakter; di-hash BCrypt |
| `idRole` | int | ya | 1–5 (Admin…DevOps); wajib ada di tabel `role` |
| `idDivisi` | int? | tidak | `null` / `0` dinormalkan ke `NULL`; jika > 0 wajib ada di tabel `divisi` |

Alias body (dipetakan ke field yang sama): `id_role` → `idRole`, `id_divisi` → `idDivisi`.

Contoh:

```json
{
  "nama": "Budi PM",
  "password": "password123",
  "idRole": 2,
  "idDivisi": 2
}
```

atau:

```json
{
  "nama": "Budi PM",
  "password": "password123",
  "id_role": 2,
  "id_divisi": 2
}
```

**Response:**

- **201** — `Location` ke aksi GetById; body: `{ "id": 12, "message": "User berhasil dibuat." }`
- **400** — body kosong/tanpa nama/password: `{ "message": "Nama, password, dan role wajib diisi." }`, atau pesan validasi service, misal:
  - `"Password minimal 8 karakter."`
  - `"Role tidak valid."`
  - `"Role tidak ditemukan."`
  - `"Divisi tidak ditemukan."`
  - `"Nama user sudah digunakan."`
  - `"Nama wajib diisi dan maksimal 255 karakter."`

### PUT `/api/v1/admin/users/{id}`

**Request body** (`AdminUserUpdateDTO`):

| Field | Tipe | Wajib | Keterangan |
| ----- | ---- | ----- | ---------- |
| `nama` | string | ya | Sama aturan seperti create |
| `password` | string? | tidak | Jika terisi (non-whitespace), minimal 8 karakter dan di-hash ulang; jika kosong/absen, password lama dipertahankan |
| `idRole` | int | ya | Wajib valid & ada di `role` |
| `idDivisi` | int? | tidak | Sama seperti create |

**Aturan bisnis (dari `AdminUserService.Update`):**

1. **Admin tidak dapat menurunkan role diri sendiri** — jika `actorId == id` dan `idRole != 1` (Admin), request dibalas **400** `"Admin tidak dapat menurunkan role dirinya sendiri."`
2. **Revoke refresh token saat role target diubah oleh admin lain** — jika `idRole` target berbeda dari role lama **dan** pelaku bukan target itu sendiri, kolom `refresh_token` dan `refresh_token_expired` di-set `NULL` (target dipaksa login ulang saat refresh berikutnya).
3. Nama harus unik (kecuali diri sendiri); referensi role/divisi divalidasi.

**Response:**

- **200** — `{ "message": "User berhasil diperbarui." }`
- **400** — pesan validasi (`ArgumentException` / `InvalidOperationException`), termasuk aturan no-self-demotion
- **401** — token tidak memuat user id
- **404** — `{ "message": "User tidak ditemukan." }`

### DELETE `/api/v1/admin/users/{id}`

**Aturan bisnis (dari `AdminUserService.Delete`):**

1. **Tidak bisa menghapus akun sendiri** — `actorId == id` → **400** `"Admin tidak dapat menghapus akun sendiri."`
2. **Admin terakhir tidak bisa dihapus** — jika target role Admin dan hanya tersisa 1 Admin → **400** `"Admin terakhir tidak dapat dihapus."`
3. Dalam satu transaksi: `hosting_request` milik target dihapus lebih dulu, lalu baris `user` (relasi lain ikut tertangani FK CASCADE).

**Response:**

- **200** — `{ "message": "User berhasil dihapus." }`
- **400** — pesan `InvalidOperationException` di atas
- **404** — `{ "message": "User tidak ditemukan." }`

---

## 2. CRUD User per Role

Empat kontroler berikut semuanya menurunkan logika dari **`RoleUserService`** (`Services/Features/RoleUserService.cs`) lewat subclass service masing-masing:

| Controller | Path versioned | Alias legacy | Service (RoleIds) |
| ---------- | -------------- | ------------ | ----------------- |
| `GuruController` (Minimal API) | `/api/v1/guru` | — (tidak ada) | `GuruService` (Guru = 3) |
| `PMController` (MVC) | `/api/v1/pm` | `/api/PM` | `PMService` (PM = 2) |
| `AnggotaController` (MVC) | `/api/v1/anggota` | `/api/Anggota` | `AnggotaService` (Anggota = 4) |
| `DevOpsController` (MVC) | `/api/v1/devops` | `/api/DevOps` | `DevOpsService` (DevOps = 5) |

Dual route PM/Anggota/DevOps diverifikasi oleh `EndpointContractTests.UserControllers_UseVersionedApiRoutes` dan `UserControllers_KeepLegacyAliases`. Alias legacy memakai kapitalisasi persis: `/api/PM`, `/api/Anggota`, `/api/DevOps`.

**Perilaku umum `RoleUserService`:**

- **List/detail** selalu difilter `WHERE u.id_role = @RoleId` — hanya user ber-role itu yang muncul; id di luar role → 404/null.
- **Register/Create**: field `id_role` pada body **diabaikan**; role selalu diambil dari konstanta service. `id_divisi` divalidasi ke tabel `divisi` (jika > 0); `<= 0` disimpan sebagai `NULL`. Password di-hash BCrypt.
- **Update**: hanya mengubah `nama` dan `id_divisi` (divisi divalidasi). **Password dan role tidak diubah** lewat endpoint per-role ini.
- **Delete**: `DELETE … WHERE id = @id AND id_role = @RoleId` — hanya user ber-role itu.

**Response umum list** — array `UserDTO` (`id`, `nama`, `role`, `divisi`).

Body create/update memakai `UserOTD` (snake_case): `nama`, `password`, `id_role`, `id_divisi`.

---

### 2.1 Guru — `/api/v1/guru` (Minimal API)

Kontroler: `Controller/User/GuruController.cs` via `MapGuru()`. **Hanya path versioned** — tanpa alias `/api/Guru`.

| Method | Endpoint | Auth | Deskripsi |
| ------ | -------- | ---- | --------- |
| GET | `/api/v1/guru` | JWT (semua role login) | Semua guru |
| GET | `/api/v1/guru/{id}` | JWT | Guru by ID |
| POST | `/api/v1/guru` | **Admin** | Tambah guru (`Create` → `Register`) |
| PUT | `/api/v1/guru/{id}` | **Admin** | Update nama/divisi guru |
| DELETE | `/api/v1/guru/{id}` | **Admin** | Hapus guru |

**Request body POST/PUT:**

```json
{
  "nama": "Bu Sari",
  "password": "password123",
  "id_role": 3,
  "id_divisi": 1
}
```

Catatan: pada PUT, `password` tidak dipakai service (hanya `nama` + `id_divisi`).

**Response:**

- **200 GET** — array `UserDTO`, contoh: `[{ "id": 3, "nama": "Bu Sari", "role": "Guru", "divisi": "Backend" }]`
- **200 POST** — `{ "message": "Data guru berhasil ditambahkan" }` (bukan 201)
- **400 POST** — `{ "message": "Gagal menambahkan data guru" }` atau pesan error proses
- **200 PUT** — `{ "message": "Data guru berhasil diperbarui" }`
- **404 PUT** — `{ "message": "Data guru tidak ditemukan atau gagal diperbarui" }`
- **200 DELETE** — `{ "message": "Data guru berhasil dihapus" }`
- **404 DELETE/GET** — `{ "message": "Data guru tidak ditemukan" }`

---

### 2.2 Project Manager — `/api/v1/pm` + `/api/PM`

Kontroler: `Controller/User/PMController.cs`.

**Class-level:** `[Authorize]` — semua endpoint butuh JWT.  
**Mutasi** (`register`, PUT, DELETE): `[Authorize(Roles = "Admin")]`.

| Method | Endpoint (kedua path) | Auth | Deskripsi |
| ------ | --------------------- | ---- | --------- |
| GET | `/api/v1/pm` · `/api/PM` | JWT | Semua PM |
| GET | `/api/v1/pm/{id}` · `/api/PM/{id}` | JWT | PM by ID |
| POST | `/api/v1/pm/register` · `/api/PM/register` | **Admin** | Daftar PM |
| PUT | `/api/v1/pm/{id}` · `/api/PM/{id}` | **Admin** | Update PM |
| DELETE | `/api/v1/pm/{id}` · `/api/PM/{id}` | **Admin** | Hapus PM |

**Request body POST/PUT:**

```json
{
  "nama": "Budi PM",
  "password": "password123",
  "id_role": 2,
  "id_divisi": 2
}
```

**Validasi controller:** body null → 400 `"Data Tidak Boleh Kosong!"` (register); PUT: `nama` wajib → 400 `"Nama tidak boleh kosong"`.

**Response:**

- **200 GET** — array `UserDTO`
- **200 POST** — `{ "message": "Project Manager berhasil ditambahkan" }`
- **400 POST** — `{ "message": "Gagal mendaftarkan Project Manager" }` atau validasi di atas
- **200 PUT** — `{ "message": "Project Manager berhasil diperbarui" }`
- **404** — `{ "message": "Project Manager tidak ditemukan" }`
- **200 DELETE** — `{ "message": "Project Manager berhasil dihapus" }`

---

### 2.3 Anggota — `/api/v1/anggota` + `/api/Anggota`

Kontroler: `Controller/User/AnggotaController.cs`.

**Class-level:** `[Authorize]`.  
**Mutasi:** `[Authorize(Roles = "Admin")]`.

| Method | Endpoint (kedua path) | Auth | Deskripsi |
| ------ | --------------------- | ---- | --------- |
| GET | `/api/v1/anggota` · `/api/Anggota` | JWT | Semua anggota |
| GET | `/api/v1/anggota/{id}` · `/api/Anggota/{id}` | JWT | Anggota by ID |
| POST | `/api/v1/anggota/register` · `/api/Anggota/register` | **Admin** | Daftar anggota |
| PUT | `/api/v1/anggota/{id}` · `/api/Anggota/{id}` | **Admin** | Update anggota |
| DELETE | `/api/v1/anggota/{id}` · `/api/Anggota/{id}` | **Admin** | Hapus anggota |

**Validasi controller:** register — `nama` & `password` wajib → 400 `"Data Nama dan Password Tidak Boleh Kosong!"`; PUT — `nama` wajib → 400 `"Nama tidak boleh kosong"`.

**Response:**

- **200 GET** — array `UserDTO`
- **200 POST** — `{ "message": "Anggota berhasil ditambahkan" }`
- **400 POST** — `{ "message": "Gagal menambahkan anggota" }`
- **200 PUT** — `{ "message": "Anggota berhasil diperbarui" }`
- **200 DELETE** — `{ "message": "Anggota berhasil dihapus" }`
- **404** — `{ "message": "Anggota tidak ditemukan" }`

---

### 2.4 DevOps — `/api/v1/devops` + `/api/DevOps`

Kontroler: `Controller/User/DevOpsController.cs`.

**Class-level:** `[Authorize(Roles = "Admin")]` — **seluruh** endpoint (termasuk GET) hanya untuk Admin. Tidak ada atribut `[Authorize]` per-aksi yang melonggarkan ini.

| Method | Endpoint (kedua path) | Auth | Deskripsi |
| ------ | --------------------- | ---- | --------- |
| GET | `/api/v1/devops` · `/api/DevOps` | **Admin** | Semua DevOps |
| GET | `/api/v1/devops/{id}` · `/api/DevOps/{id}` | **Admin** | DevOps by ID |
| POST | `/api/v1/devops/register` · `/api/DevOps/register` | **Admin** | Daftar DevOps |
| PUT | `/api/v1/devops/{id}` · `/api/DevOps/{id}` | **Admin** | Update DevOps |
| DELETE | `/api/v1/devops/{id}` · `/api/DevOps/{id}` | **Admin** | Hapus DevOps |

**Validasi controller:** register — `nama` & `password` wajib → 400 `"Nama dan password tidak boleh kosong"`; body null pada PUT → 400 `"Data tidak boleh kosong"`.

**Response:**

- **200 GET** — array `UserDTO`
- **200 POST** — `{ "message": "DevOps user berhasil ditambahkan" }`
- **400 POST** — `{ "message": "Gagal menambahkan DevOps user" }`
- **200 PUT** — `{ "message": "DevOps user berhasil diperbarui" }`
- **200 DELETE** — `{ "message": "DevOps user berhasil dihapus" }`
- **404** — `{ "message": "DevOps user tidak ditemukan" }`

---

## 3. Master Data

### 3.1 Role — `/api/v1/role`

Kontroler: `Controller/Admin/RoleController.cs` (Minimal API `MapRole`), service `RoleServices` (`SELECT * FROM role`).

| Method | Endpoint | Auth | Deskripsi |
| ------ | -------- | ---- | --------- |
| GET | `/api/v1/role` | JWT (semua role login) | Ambil semua role |

Tidak ada endpoint create/update/delete role di kode.

**Response 200:**

```json
[
  { "id": 1, "nama": "Admin" },
  { "id": 2, "nama": "PM" },
  { "id": 3, "nama": "Guru" },
  { "id": 4, "nama": "Anggota" },
  { "id": 5, "nama": "DevOps" }
]
```

**Response error** — `Results.Problem` (500) `"Gagal mengambil data role"`.

> **Koreksi terhadap README lama:** README menandai endpoint role tanpa autentikasi; di kode, group memanggil `.RequireAuthorization()`, sehingga **JWT wajib**.

---

### 3.2 Status — `/api/v1/status`

Kontroler: `Controller/Admin/StatusController.cs` (Minimal API `MapStatus`), service `StatusService` (`SELECT * FROM status`).

| Method | Endpoint | Auth | Deskripsi |
| ------ | -------- | ---- | --------- |
| GET | `/api/v1/status` | JWT (semua role login) | Ambil semua status target |

**Response 200:**

```json
[
  { "id": 1, "nama": "Null" },
  { "id": 2, "nama": "On Progress" },
  { "id": 3, "nama": "Done" },
  { "id": 4, "nama": "Izin" },
  { "id": 5, "nama": "Sakit" }
]
```

**Response error** — 500 `"Gagal mengambil data status"`.

> README lama menandai tanpa autentikasi; kode: **JWT wajib**.

---

### 3.3 Divisi — `/api/v1/divisi`

Kontroler: `Controller/Admin/DivisiController.cs` (Minimal API `MapDivisi`), service `DivisiService`.

| Method | Endpoint | Auth | Deskripsi |
| ------ | -------- | ---- | --------- |
| GET | `/api/v1/divisi` | JWT | Semua divisi |
| GET | `/api/v1/divisi/{id}` | JWT | Divisi by ID |
| POST | `/api/v1/divisi` | **Admin** (policy `"Admin"`) | Tambah divisi |
| PUT | `/api/v1/divisi/{id}` | **Admin** | Update divisi |
| DELETE | `/api/v1/divisi/{id}` | **Admin** | Hapus divisi |

**Request body POST/PUT** (`DivisiDTO`):

```json
{
  "id": 0,
  "nama": "Mobile"
}
```

(Pada PUT, `id` diambil dari route; field body `id` ditimpa.)

**Response:**

- **200 GET list** — `[{ "id": 1, "nama": "Backend" }, …]`
- **200 GET by id** — `{ "id": 1, "nama": "Backend" }`
- **404 GET by id** — string `"Divisi tidak ditemukan"`
- **201 POST** — body echo DTO yang dikirim, header `Location: /api/v1/divisi`
- **200 PUT** — string `"data berhasil diperbarui"`
- **200 DELETE** — string `"data berhasil di hapus"`
- **404 PUT/DELETE** — string `"Divisi tidak ditemukan"` (rowsAffected = 0)
- **500** — `Problem` `"Gagal …"` (ambil/tambah/update/hapus)

> README lama menandai semua operasi divisi tanpa autentikasi; kode: **GET butuh JWT, mutasi butuh role Admin**.

---

## 4. Matriks Autorisasi (per endpoint)

| Area | Endpoint | Admin | PM | Guru | Anggota | DevOps |
| ----|----------|:-----:|:--:|:----:|:-------:|:------:|
| Admin users | GET/POST/PUT/DELETE `/api/v1/admin/users…` | ✅ | ❌ | ❌ | ❌ | ❌ |
| Guru | GET `/api/v1/guru`, GET `{id}` | ✅ | ✅ | ✅ | ✅ | ✅ |
| Guru | POST, PUT, DELETE | ✅ | ❌ | ❌ | ❌ | ❌ |
| PM | GET `/api/v1/pm`, `/api/PM` (+ `{id}`) | ✅ | ✅ | ✅ | ✅ | ✅ |
| PM | POST `…/register`, PUT, DELETE (kedua path) | ✅ | ❌ | ❌ | ❌ | ❌ |
| Anggota | GET `/api/v1/anggota`, `/api/Anggota` (+ `{id}`) | ✅ | ✅ | ✅ | ✅ | ✅ |
| Anggota | POST `…/register`, PUT, DELETE (kedua path) | ✅ | ❌ | ❌ | ❌ | ❌ |
| DevOps | GET, POST `…/register`, PUT, DELETE (kedua path) | ✅ | ❌ | ❌ | ❌ | ❌ |
| Role | GET `/api/v1/role` | ✅ | ✅ | ✅ | ✅ | ✅ |
| Status | GET `/api/v1/status` | ✅ | ✅ | ✅ | ✅ | ✅ |
| Divisi | GET `/api/v1/divisi`, GET `{id}` | ✅ | ✅ | ✅ | ✅ | ✅ |
| Divisi | POST, PUT `{id}`, DELETE `{id}` | ✅ | ❌ | ❌ | ❌ | ❌ |

Kebijakan bernama terdaftar di `Models/Auth.cs` → `Policies` (`Admin` = role Admin, `PM` = role PM, dst.), dipakai via `RequireAuthorization("Admin")` (divisi) dan `[Authorize(Roles = "Admin")]` / `RequireRole("Admin")` (controller lain).

---

## 5. Ringkasan perbedaan README lama vs kode

| Topik | README lama | Kode (menang) |
| ----- | ----------- | ------------- |
| Auth GET role/status/divisi | tanpa auth | **JWT wajib** (`RequireAuthorization` pada group) |
| Auth mutasi divisi | tanpa auth | **Admin only** |
| Path PM/Anggota | hanya `/api/PM`, `/api/Anggota` | **`/api/v1/pm` + `/api/PM`**, **`/api/v1/anggota` + `/api/Anggota`** (dan DevOps: `/api/v1/devops` + `/api/DevOps`) — diuji `EndpointContractTests` |
| CRUD PM/Anggota di README | hanya GET + register, auth salah | lengkap GET/POST/PUT/DELETE; GET = JWT, mutasi = Admin |
| Guru legacy path | — | **tidak ada** alias legacy; hanya `/api/v1/guru` |
| DevOps | tidak didokumentasikan di README | controller penuh, **semua endpoint Admin** |
| Admin users `/api/v1/admin/users` | tidak ada di README API | CRUD penuh + aturan no-self-demotion / no-self-delete / last-admin / revoke refresh token saat ganti role orang lain |
| Role seed di README | 4 role | **5 role** (termasuk DevOps = 5) |

---

## Dokumen terkait

- [Arsitektur](architecture.md)
- [Database](database.md)
- [Autentikasi](authentication.md)
- [API Absensi](api-absensi.md)
- [API Hosting](api-hosting.md)
