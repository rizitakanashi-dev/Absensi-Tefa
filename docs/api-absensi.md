# API Absensi, Target & Proyek (Backend Absensi)

Dokumen ini mendokumentasikan endpoint **absensi harian**, **target kerja**, **project**, dan **keanggotaan project** pada backend Absensi. Keempat grup endpoint diimplementasikan dengan pola hybrid: Absen/Target/Project memakai Minimal API (`MapGroup`), sedangkan Project Anggota memakai MVC Controller (`[ApiController]`). Semua endpoint di bawah memerlukan JWT Bearer kecuali yang disebutkan lain; klaim `sub`/`NameIdentifier` dipakai sebagai ID user yang sedang login.

Base URL contoh: `http://localhost:5072`. Header wajib:

```
Authorization: Bearer <token>
```

---

## 1. Absensi — `/api/v1/absen`

Sumber: `Controller/User/AbsenController.cs` (class `AbsensiController`, method `MapAbsensiEndpoints`), `Services/Features/AbsensiService.cs`, `Models/Absen.cs`.

Seluruh grup dibuat dengan `app.MapGroup("/api/v1/absen").RequireAuthorization()` — **semua endpoint butuh JWT**. Tidak ada alias legacy untuk grup ini.

### Ringkasan endpoint

| Method | Path | Role yang diizinkan | Request body | Response sukses |
| ------ | ---- | ------------------- | ------------ | --------------- |
| GET | `/api/v1/absen?tanggal&page&pageSize` | Semua role terautentikasi (rekap mengecualikan user role Admin) | — (query string) | `200` `{ data, pagination }` |
| POST | `/api/v1/absen/masuk` | Semua role terautentikasi; ID user diambil dari JWT | `AbsenMasukDTO` | `200` `{ "message": "absen masuk berhasil" }` |
| PUT | `/api/v1/absen/pulang` | Semua role terautentikasi; hanya absensi milik sendiri | `AbsenPulangDTO` | `200` `{ "message": "Absen pulang berhasil" }` |

### GET `/api/v1/absen`

Mengambil rekap absensi per tanggal (join `absensi` → `target` → `user`/`divisi`/`project`/`status`), difilter `u.id_role != RoleIds.Admin` (Admin, `id_role = 1`, tidak muncul di rekap), diurutkan `a.id DESC`, dengan paginasi.

**Query parameter:**

| Parameter | Tipe | Wajib | Default | Keterangan |
| --------- | ---- | :---: | ------- | ---------- |
| `tanggal` | string | ✅ | — | Format **wajib** `yyyy-MM-dd` (divalidasi via `DateTime.TryParseExact`) |
| `page` | int | ❌ | `1` | Nomor halaman; `< 1` dikoreksi menjadi `1` |
| `pageSize` | int | ❌ | `50` | Batas atas **100**; `< 1` atau `> 100` dikoreksi menjadi `50` |

**Siapa boleh lihat apa:** semua user terautentikasi dapat memanggil endpoint ini dan melihat rekap siapa pun **kecuali user berrole Admin**. Tidak ada pembatasan per-role pada level controller; pembatasan hanya pada filter `id_role != Admin` di query SQL.

**Response 200:**

```json
{
  "data": [
    {
      "idAbsensi": 1,
      "idTarget": 1,
      "tanggal": "2026-09-08",
      "nama": "Andi Anggota",
      "divisi": "Backend",
      "project": "Project Alpha",
      "target": "Membuat API endpoint auth",
      "status": "On Progress",
      "jamMasuk": "08:30",
      "jamPulang": null
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "totalRecords": 1,
    "totalPages": 1
  }
}
```

Field `divisi`, `project`, `target` memakai `COALESCE(..., '-')`; `status` memakai `COALESCE(s.nama, 'Null')`. `totalPages` = `ceil(totalRecords / pageSize)`.

**Response error:**

| Status | Kondisi | Body |
| ------ | ------- | ---- |
| `400` | `tanggal` kosong | `{ "message": "parameter tanggal wajib diisi" }` |
| `400` | Format tanggal salah | `{ "message": "Format tanggal harus yyyy-MM-dd" }` |
| `400` | Exception saat query | `{ "message": "Gagal mengambil rekap absen" }` (log `ABSEN GET: ...`) |

> **Catatan vs README lama:** README menampilkan respons sebagai array polos. Kode saat ini **membungkus** hasil dalam objek `{ data, pagination }` — kode yang menang.

---

### POST `/api/v1/absen/masuk`

Absen masuk harian. ID user **tidak** dikirim di body — diambil dari klaim JWT via `TryGetUserId`.

**Request body (`AbsenMasukDTO`):**

| Field | Tipe | Keterangan |
| ----- | ---- | ---------- |
| `idProject` | int | ID project yang dikerjakan; `<= 0` langsung gagal |
| `target` | string | Deskripsi target hari ini |
| `idStatus` | int | Status awal target; referensi ke tabel `status` (`id_status`) — lihat [Database](database.md) |

Contoh:

```json
{
  "idProject": 1,
  "target": "Membuat API endpoint auth",
  "idStatus": 2
}
```

**Alur di dalam `AbsensiService.AbsenMasuk` (satu transaksi, satu koneksi):**

1. Validasi `IdProject > 0`.
2. Cek keanggotaan: `SELECT COUNT(*) FROM project_anggota WHERE id_user = @IdUser AND id_project = @IdProject` — jika bukan anggota → gagal.
3. Cegah absen ganda: tolak jika sudah ada absensi milik user dengan `tanggal = CURRENT_DATE()` dan `jam_pulang IS NULL`.
4. `INSERT INTO target (id_user, id_project, target, id_status) VALUES (...)`.
5. `SELECT LAST_INSERT_ID()` **pada koneksi dan transaksi yang sama** agar mengembalikan ID baris `target` yang baru saja dibuat (tidak ditutup/dibuka antar query).
6. `INSERT INTO absensi (tanggal, id_target, jam_masuk) VALUES (CURRENT_DATE(), @IdTarget, CURRENT_TIME())`.
7. Commit.

**Response 200:**

```json
{ "message": "absen masuk berhasil" }
```

**Response 400 (bisnis):**

```json
{
  "message": "Absen masuk gagal. Anda bukan anggota project tersebut, sudah absen hari ini, atau project tidak valid."
}
```

**Response 400 (exception):** `{ "message": "Gagal melakukan absen masuk" }` — prefix log `ABSEN MASUK:`.  
**Response 401:** klaim user ID tidak bisa dibaca dari token.

---

### PUT `/api/v1/absen/pulang`

Mengisi `jam_pulang` dan memperbarui `target.id_status` sekaligus, hanya untuk absensi yang **milik user login** dan belum memiliki `jam_pulang`.

**Request body (`AbsenPulangDTO`):**

| Field | Tipe | Keterangan |
| ----- | ---- | ---------- |
| `idAbsensi` | int | ID baris `absensi` |
| `idTarget` | int | ID `target` yang harus cocok dengan `absensi.id_target` |
| `idStatus` | int | Status akhir; referensi `status.id` |

Contoh:

```json
{
  "idAbsensi": 1,
  "idTarget": 1,
  "idStatus": 3
}
```

**Logika SQL (transaksi):** `UPDATE absensi a INNER JOIN target t ON t.id = a.id_target SET a.jam_pulang = CURRENT_TIME(), t.id_status = @IdStatus WHERE a.id = @IdAbsensi AND a.id_target = @IdTarget AND t.id_user = @UserId AND a.jam_pulang IS NULL`.

**Response 200:** `{ "message": "Absen pulang berhasil" }`  
**Response 400 (bisnis):** `{ "message": "gagal melakukan absen pulang, data tidak ditemukan atau bukan milik anda" }`  
**Response 400 (exception):** `{ "message": "Gagal melakukan absen pulang" }` — prefix log `ABSEN PULANG:`.  
**Response 401:** klaim user ID tidak valid.

---

## 2. Target — `/api/v1/target`

Sumber: `Controller/User/TargetController.cs` (class `TargetController`, method `MapTarget`), `Services/Features/TargetService.cs`, `Models/Target.cs`.

Grup: `app.MapGroup("/api/v1/target").RequireAuthorization()`. **Tidak ada section Target di README lama** — dokumentasi di bawah ditulis murni dari kode. Tidak ada alias legacy.

### Ringkasan endpoint

| Method | Path | Role yang diizinkan | Request body | Response sukses |
| ------ | ---- | ------------------- | ------------ | --------------- |
| GET | `/api/v1/target/my` | Semua role terautentikasi (hanya milik sendiri) | — | `200` array `TargetDTO` |
| GET | `/api/v1/target?userId&projectId&statusId` | Semua role; **Anggota dipaksa** `userId` = diri sendiri | — | `200` array `TargetDTO` |
| GET | `/api/v1/target/{id}` | Semua role; Anggota hanya miliknya (selain itu `403`) | — | `200` `TargetDTO` / `404` |
| POST | `/api/v1/target` | **Admin, PM, Guru** | `TargetCreateDTO` | `201` `{ id, message }` |
| PUT | `/api/v1/target/{id}` | Semua role; Anggota hanya boleh update miliknya | `TargetUpdateDTO` | `200` / `400` / `404` / `403` |
| DELETE | `/api/v1/target/{id}` | **Admin** saja | — | `200` / `404` |

> Catatan routing: `/my` didaftarkan **sebelum** `/{id}` agar tidak tertangkap pola `/{id:int}`.

### Field DTO

**`TargetCreateDTO` (request POST):**

| Field | Tipe | Keterangan |
| ----- | ---- | ---------- |
| `idUser` | int | Pemilik target |
| `idProject` | int | FK → `project.id` |
| `target` | string | Deskripsi target |
| `idStatus` | int | FK → `status.id` |

**`TargetUpdateDTO` (request PUT):** update bersifat **sebagian** (hanya field yang diisi ikut di-`SET`):

| Field | Tipe | Keterangan |
| ----- | ---- | ---------- |
| `id` | int | Diisi otomatis dari route `{id}` oleh controller |
| `idProject` | int? | Opsional |
| `target` | string? | Opsional; string kosong diabaikan |
| `idStatus` | int? | Opsional |

Jika tidak ada satu pun field yang bisa di-`SET`, service mengembalikan `false` → `400` `{ "message": "Tidak ada data yang diperbarui" }`.

**`TargetDTO` (response GET/POST data gabungan):**

| Field | Keterangan |
| ----- | ---------- |
| `id` | PK `target` |
| `idUser` / `userName` | Pemilik + nama dari join `user` |
| `idProject` / `projectName` | Project + nama dari join `project` |
| `target` | Deskripsi target |
| `idStatus` / `statusName` | Status + nama dari join `status` |

### Referensi status (`id_status`)

`target.id_status` adalah FK ke tabel `status`. Nilai seed (lihat juga [Database](database.md)):

| id | nama |
| -- | ---- |
| 1 | Null |
| 2 | On Progress |
| 3 | Done |
| 4 | Izin |
| 5 | Sakit |

Konstanta tersedia di `Models/Constants.cs` → `StatusIds` (`Null = 1`, `OnProgress = 2`, `Done = 3`, `Izin = 4`, `Sakit = 5`).

### Detail perilaku

- **GET `/my`:** mengembalikan `GetByUserId(idUser dari JWT)` — setiap user hanya melihat target miliknya sendiri, tanpa memandang role.
- **GET `/`:** parameter query `userId`, `projectId`, `statusId` semuanya opsional dan dapat digabung (AND). Jika role caller adalah **`Anggota`**, parameter `userId` **di-overwrite** dengan ID caller — Anggota tidak bisa melihat target user lain lewat filter. Role selain Anggota (Admin/PM/Guru/DevOps) bebas memfilter.
- **GET `/{id}`:** `404` `{ "message": "Target tidak ditemukan" }` bila tidak ada; role Anggota dengan `target.IdUser != currentUserId` → **`403 Forbid`**.
- **POST:** setelah create, `201 Created` ke Location `/api/v1/target/{newId}` dengan body `{ "id": newId, "message": "Target berhasil dibuat" }`.
- **PUT:** load dulu data lama untuk cek kepemilikan (Anggota yang bukan pemilik → `403`); `404` bila tidak ada; `200` `{ "message": "Target berhasil diperbarui" }`.
- **DELETE:** hanya policy `RequireRole("Admin")`; `200` `{ "message": "Target berhasil dihapus" }` atau `404` `{ "message": "Target tidak ditemukan" }`.

**Error handling (semua endpoint Target):** `try/catch` → `Console.WriteLine($"TARGET <OP>: {e.Message}")` → `Results.Problem("...")` dengan pesan statis (`"Gagal mengambil data target"`, `"Gagal menambahkan target"`, `"Gagal memperbarui target"`, `"Gagal menghapus target"`).

---

## 3. Project — `/api/v1/project`

Sumber: `Controller/User/ProjectController.cs` (class `ProjectController`, method `MapProject`), `Services/Features/ProjectService.cs`, `Models/Project.cs`.

Grup: `app.MapGroup("/api/v1/project")` lalu **`g.RequireAuthorization()`** — semua method butuh JWT. Tidak ada alias legacy.

> **Catatan vs README lama:** README menandai seluruh endpoint project sebagai tanpa auth (`❌`). **Kode yang menang:** GET butuh JWT; mutasi butuh role Admin atau PM.

### Ringkasan endpoint

| Method | Path | Role yang diizinkan | Request body | Response sukses |
| ------ | ---- | ------------------- | ------------ | --------------- |
| GET | `/api/v1/project/` | Semua role terautentikasi | — | `200` array `ProjectDTO` |
| GET | `/api/v1/project/{id}` | Semua role terautentikasi | — | `200` `ProjectDTO` / `404` |
| POST | `/api/v1/project/` | **Admin, PM** | `ProjectDTO` | `201` echo DTO |
| PUT | `/api/v1/project/{id}` | **Admin, PM** | `ProjectDTO` | `200` / `404` |
| DELETE | `/api/v1/project/{id}` | **Admin, PM** | — | `200` / `404` |

### Field DTO

**`ProjectDTO`** (request & response):

| Field | Tipe | Keterangan |
| ----- | ---- | ---------- |
| `id` | int | PK; pada PUT di-overwrite dari route `{id}` |
| `nama` | string | Nama project |

(`ProjectOTD` di file yang sama hanya memuat `nama`; hanya dideklarasikan di `Models/Project.cs` dan tidak direferensikan controller/service mana pun — dead code.)

### Detail perilaku

- **GET `/`:** `SELECT * FROM project` → `List<ProjectDTO>`.
- **GET `/{id}`:** `null` → `404` `"Project tidak ditemukan"`.
- **POST:** `INSERT INTO project(nama)` dalam transaksi; respons `201 Created` Location `/api/v1/project` dengan body DTO yang dikirim (echo, bukan ID baru).
- **PUT:** `rowsAffected == 0` → `404` `"Project tidak ditemukan"`; selain itu `200` `"data berhasil diperbarui"`.
- **DELETE:** `rowsAffected == 0` → `404` `"Project tidak ditemukan"`; selain itu `200` `"data berhasil di hapus"`.

**Role mutasi:** semua `RequireAuthorization(policy => policy.RequireRole("Admin", "PM"))` pada POST/PUT/DELETE. Role Guru/Anggota/DevOps hanya bisa GET.

**Error handling:** `try/catch` → `Console.WriteLine($"PROJECT <OP>: {e.Message}")` → `Results.Problem(...)` (`"Gagal mengambil data project"`, `"Gagal menambahkan project"`, `"Gagal memperbarui project"`, `"Gagal menghapus project"`).

---

## 4. Project Anggota — `/api/v1/project-anggota`

Sumber: `Controller/User/ProjectAnggotaController.cs` (MVC `[ApiController]`, `[Route("api/v1/project-anggota")]`, `[Authorize]` di class), `Services/Features/ProjectAnggotaService.cs`, `Models/ProjectAnggota.cs`.

Tidak ada alias legacy. Otorisasi mutasi dijaga oleh `[Authorize(Roles = "Admin,PM")]` pada `Create` dan `Delete`, dan diverifikasi oleh `Absensi.Tests/EndpointContractTests.cs` (`ProjectAnggota_Mutations_RequireAdminOrPm`).

### Ringkasan endpoint

| Method | Path | Role yang diizinkan | Request body | Response sukses |
| ------ | ---- | ------------------- | ------------ | --------------- |
| GET | `/api/v1/project-anggota` | Semua role terautentikasi (class-level `[Authorize]`) | — | `200` array `ProjectAnggota` |
| POST | `/api/v1/project-anggota` | **Admin, PM** | `ProjectAnggotaDto` | `200` `{ message }` |
| DELETE | `/api/v1/project-anggota/{id}` | **Admin, PM** | — | `200` `{ message }` |

### Field DTO

**`ProjectAnggotaDto` (request POST):**

| Field | Tipe | Validasi | Keterangan |
| ----- | ---- | -------- | ---------- |
| `user` | int | Harus `> 0` | ID user yang ditambahkan (`id_user`) |
| `project` | int | Harus `> 0` | ID project (`id_project`) |

**`ProjectAnggota` (response GET):**

| Field | Keterangan |
| ----- | ---------- |
| `id` | PK `project_anggota` |
| `idUser` / `idProject` | FK user & project |
| `username` | `user.nama` (alias kolom `Username` dari join) |
| `project` | `project.nama` (alias kolom `Project` dari join) |

GET hanya mengembalikan baris yang user & project-nya cocok (`JOIN` di kedua sisi — baris dengan FK yatim tidak muncul).

### Detail perilaku

- **GET:** tanpa filter; seluruh membership.
- **POST:** validasi awal `req == null || req.User <= 0 || req.Project <= 0` → `400` `{ "message": "Data user/id project tidak valid" }`. Insert `project_anggota (id_user, id_project)` dalam transaksi. Insert menghasilkan rows 0 (return `false` tanpa exception) → `400` `{ "message": "Gagal menambahkan anggota ke project" }`. Bila terjadi exception, `ProjectAnggotaService.Create` melakukan rollback lalu melempar ulang; controller tidak punya `try/catch`, sehingga exception diteruskan ke `GlobalExceptionHandler` dan menghasilkan `500`/`503`/`409`/`401` dengan body `ErrorResponse` (bukan `400` dengan pesan di atas). Sukses → `200` `{ "message": "Berhasil menambahkan anggota ke project" }`.
- **DELETE `{id}`:** `DELETE FROM project_anggota WHERE id = @Id`. Tidak ada baris → `404` `{ "message": "Data tidak ditemukan atau gagal dihapus" }`. Sukses → `200` `{ "message": "Berhasil menghapus anggota dari project" }`.

**Catatan MVC vs Minimal API:** handler ini tidak memakai blok `try/catch` + `Results.Problem`; exception diteruskan ke middleware `GlobalExceptionHandler` global. Pesan bisnis memakai anon object `{ message = ... }`.

---

## 5. DTO utama ( Models/ & Services/Features )

| Class | File | Peran |
| ----- | ---- | ----- |
| `AbsenRekapDTO` | `Models/Absen.cs` | Baris rekap GET `/absen` (`IdAbsensi`, `IdTarget`, `Tanggal`, `Nama`, `Divisi`, `Project`, `Target`, `Status`, `JamMasuk`, `JamPulang`) |
| `AbsenMasukDTO` | `Models/Absen.cs` | Body POST `/absen/masuk` (`IdProject`, `Target`, `IdStatus`) |
| `AbsenPulangDTO` | `Models/Absen.cs` | Body PUT `/absen/pulang` (`IdAbsensi`, `IdTarget`, `IdStatus`) |
| `Target` | `Models/Target.cs` | Entity target + navigation (`UserName`, `ProjectName`, `StatusName`) |
| `TargetCreateDTO` | `Models/Target.cs` | Body POST `/target` |
| `TargetUpdateDTO` | `Models/Target.cs` | Body PUT `/target/{id}` (partial) |
| `TargetDTO` | `Models/Target.cs` | Output target (join user/project/status) |
| `ProjectOTD` | `Models/Project.cs` | Deklarasi input project tanpa ID (`nama`); tidak dipakai (dead code) |
| `ProjectDTO` | `Models/Project.cs` | Input/output project (`id`, `nama`) |
| `ProjectAnggota` | `Models/ProjectAnggota.cs` | Output membership (join) |
| `ProjectAnggotaDto` | `Models/ProjectAnggota.cs` | Body tambah anggota (`User`, `Project`) |
| `StatusOTD` / `StatusDTO` | `Models/Status.cs` | Input/output master status (`nama` / `id` + `nama`) |
| `StatusIds` | `Models/Constants.cs` | Konstanta `id_status` seed (Null…Sakit) |
| `RoleIds` | `Models/Constants.cs` | Konstanta role (Admin=1 … DevOps=5); dipakai filter rekap absen |

---

## 6. Pola error handling controller

**Minimal API (Absen, Target, Project)** — setiap handler membungkus logic dalam `try/catch`:

1. Log ke konsol dengan prefix per-endpoint: `ABSEN GET:`, `ABSEN MASUK:`, `ABSEN PULANG:`, `TARGET GET MY:`, `TARGET GET:`, `TARGET GET BY ID:`, `TARGET POST:`, `TARGET PUT:`, `TARGET DELETE:`, `PROJECT GET:`, `PROJECT GET BY ID:`, `PROJECT POST:`, `PROJECT PUT:`, `PROJECT DELETE:` diikuti `e.Message`.
2. Return `Results.Problem("<pesan statis>")` (Target & Project) atau `Results.BadRequest(new { message = "..." })` (Absen) — tanpa membocorkan detail exception ke client.
3. Kondisi bisnis (tidak ditemukan / bukan milik sendiri / tidak ada field diubah) ditangani **sebelum** catch, memakai `Results.NotFound` / `Results.Forbid` / `Results.BadRequest`.

**MVC (Project Anggota)** — validasi input manual → `BadRequest`/`NotFound` dengan `{ message }`; tidak ada try/catch lokal.

**Transaksi di service:** `AbsensiService`, `TargetService`, `ProjectService`, `ProjectAnggotaService` semuanya membuka koneksi → `BeginTransaction` → commit; `catch { rollback; throw; }`. Pada `AbsenMasuk` dan `TargetService.Create`, `LAST_INSERT_ID()` dijalankan pada koneksi/transaksi yang sama dengan INSERT-nya.

---

## Dokumen terkait

- [Arsitektur](architecture.md)
- [Database](database.md)
- [Autentikasi](authentication.md)
- [API Hosting](api-hosting.md)
- [API Manajemen](api-management.md)
