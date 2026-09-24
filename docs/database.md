# Skema Database (Backend Absensi)

Dokumen ini menjelaskan skema database MySQL `absensi`: 9 tabel, relasi foreign key beserta perilaku `ON DELETE`/`ON UPDATE`, seed data, dan bootstrap yang dijalankan saat startup. Sumber kebenaran adalah [`setup.sql`](../setup.sql) (DDL + seed); sisi runtime dilengkapi oleh `Services/Infrastructure/DatabaseBootstrap.cs`. Dokumen terkait: [Autentikasi](authentication.md), [API Hosting](api-hosting.md), [Arsitektur](architecture.md).

---

## Ringkasan Umum

- Database: MySQL 8.0, nama `absensi`, semua tabel `ENGINE=InnoDB` dengan `DEFAULT CHARSET=utf8mb4`.
- Semua PK adalah `INT(11) NOT NULL AUTO_INCREMENT`.
- Ada 9 tabel: `divisi`, `role`, `status`, `project`, `user`, `project_anggota`, `target`, `absensi`, `hosting_request`.
- Seed data (role, divisi, status) memakai `INSERT IGNORE`, sehingga aman dijalankan ulang.

---

## Daftar Tabel

| # | Tabel            | Fungsi singkat                                      |
| - | ---------------- | --------------------------------------------------- |
| 1 | `divisi`         | Master divisi (Backend, Frontend, Game)             |
| 2 | `role`           | Master role user (Admin, PM, Guru, Anggota, DevOps) |
| 3 | `status`         | Master status pengerjaan target/absensi             |
| 4 | `project`        | Master project                                      |
| 5 | `user`           | Akun user (login, role, divisi, refresh token)      |
| 6 | `project_anggota`| Keanggotaan user dalam project                      |
| 7 | `target`         | Target harian user per project                      |
| 8 | `absensi`        | Rekaman absen masuk/pulang per target               |
| 9 | `hosting_request`| Pengajuan hosting project oleh Anggota              |

---

## Detail Tabel

### 1. `divisi`

| Kolom  | Tipe         | Constraint / Default        |
| ------ | ------------ | --------------------------- |
| `id`   | INT(11)      | PK, AUTO_INCREMENT, NOT NULL |
| `nama` | VARCHAR(255) | DEFAULT NULL                 |

### 2. `role`

| Kolom  | Tipe         | Constraint / Default        |
| ------ | ------------ | --------------------------- |
| `id`   | INT(11)      | PK, AUTO_INCREMENT, NOT NULL |
| `nama` | VARCHAR(255) | DEFAULT NULL                 |

### 3. `status`

| Kolom  | Tipe         | Constraint / Default        |
| ------ | ------------ | --------------------------- |
| `id`   | INT(11)      | PK, AUTO_INCREMENT, NOT NULL |
| `nama` | VARCHAR(255) | DEFAULT NULL                 |

### 4. `project`

| Kolom  | Tipe         | Constraint / Default        |
| ------ | ------------ | --------------------------- |
| `id`   | INT(11)      | PK, AUTO_INCREMENT, NOT NULL |
| `nama` | VARCHAR(255) | DEFAULT NULL                 |

### 5. `user`

| Kolom                  | Tipe         | Constraint / Default                                 |
| ---------------------- | ------------ | ---------------------------------------------------- |
| `id`                   | INT(11)      | PK, AUTO_INCREMENT, NOT NULL                          |
| `nama`                 | VARCHAR(255) | NOT NULL, UNIQUE KEY `uq_user_nama`                   |
| `password`             | VARCHAR(255) | NOT NULL (hash BCrypt, bukan plain text)              |
| `id_role`              | INT(11)      | NOT NULL, FK → `role.id`                             |
| `id_divisi`            | INT(11)      | DEFAULT NULL, FK → `divisi.id`                       |
| `refresh_token`        | VARCHAR(255) | DEFAULT NULL (token refresh JWT)                      |
| `refresh_token_expired`| DATETIME     | DEFAULT NULL (kadaluarsa refresh token)               |

Relasi:

| Constraint       | Kolom        | References  | ON DELETE | ON UPDATE |
| ---------------- | ------------ | ----------- | --------- | --------- |
| `fk_user_role`   | `id_role`    | `role(id)`  | RESTRICT  | CASCADE   |
| `fk_user_divisi` | `id_divisi`  | `divisi(id)`| SET NULL  | CASCADE   |

Catatan:

- `id_role` **NOT NULL** (kode menang atas README lama yang menyebut nullable): user wajib punya role; menghapus role yang masih dipakai akan ditolak MySQL (`RESTRICT`).
- Kolom `refresh_token` dan `refresh_token_expired` menyimpan refresh token JWT (berlaku 20 hari) — lihat [authentication.md](authentication.md).
- Model C# `Models/User.cs` memetakan field ini (`User.refreshTokenExpired`, `IdRole`, `IdDivisi`).

### 6. `project_anggota`

| Kolom        | Tipe    | Constraint / Default                |
| ------------ | ------- | ----------------------------------- |
| `id`         | INT(11) | PK, AUTO_INCREMENT, NOT NULL         |
| `id_user`    | INT(11) | DEFAULT NULL, FK → `user.id`        |
| `id_project` | INT(11) | DEFAULT NULL, FK → `project.id`     |

Relasi:

| Constraint     | Kolom        | References    | ON DELETE | ON UPDATE |
| -------------- | ------------ | ------------- | --------- | --------- |
| `fk_pa_user`   | `id_user`    | `user(id)`    | CASCADE   | CASCADE   |
| `fk_pa_project`| `id_project` | `project(id)` | CASCADE   | CASCADE   |

### 7. `target`

| Kolom        | Tipe         | Constraint / Default                    |
| ------------ | ------------ | --------------------------------------- |
| `id`         | INT(11)      | PK, AUTO_INCREMENT, NOT NULL             |
| `id_user`    | INT(11)      | DEFAULT NULL, FK → `user.id`            |
| `id_project` | INT(11)      | DEFAULT NULL, FK → `project.id`         |
| `target`     | VARCHAR(255) | DEFAULT NULL (deskripsi target harian)   |
| `id_status`  | INT(11)      | DEFAULT NULL, FK → `status.id`          |

Relasi:

| Constraint        | Kolom       | References     | ON DELETE | ON UPDATE |
| ----------------- | ----------- | -------------- | --------- | --------- |
| `fk_target_user`  | `id_user`   | `user(id)`     | CASCADE   | CASCADE   |
| `fk_target_project`| `id_project`| `project(id)` | CASCADE   | CASCADE   |
| `fk_target_status`| `id_status` | `status(id)`   | SET NULL  | CASCADE   |

### 8. `absensi`

| Kolom        | Tipe | Constraint / Default                               |
| ------------ | ---- | -------------------------------------------------- |
| `id`         | INT(11) | PK, AUTO_INCREMENT, NOT NULL                       |
| `tanggal`    | DATE | DEFAULT NULL (diisi aplikasi dengan `CURRENT_DATE()`) |
| `id_target`  | INT(11) | DEFAULT NULL, FK → `target.id`                     |
| `jam_masuk`  | TIME | DEFAULT NULL (diisi `CURRENT_TIME()` saat absen masuk) |
| `jam_pulang` | TIME | DEFAULT NULL (diisi saat absen pulang)             |

Relasi:

| Constraint          | Kolom       | References    | ON DELETE | ON UPDATE |
| ------------------- | ----------- | ------------- | --------- | --------- |
| `fk_absensi_target` | `id_target` | `target(id)`  | CASCADE   | CASCADE   |

### 9. `hosting_request`

| Kolom               | Tipe                | Constraint / Default                                            |
| ------------------- | ------------------- | --------------------------------------------------------------- |
| `id`                | INT(11)             | PK, AUTO_INCREMENT, NOT NULL                                     |
| `id_user`           | INT(11)             | NOT NULL, FK → `user.id` (pemohon)                               |
| `id_project`        | INT(11)             | NOT NULL, FK → `project.id`                                      |
| `contact_name`      | VARCHAR(255)        | NOT NULL                                                         |
| `contact_email`     | VARCHAR(255)        | NOT NULL                                                         |
| `contact_phone`     | VARCHAR(50)         | NOT NULL                                                         |
| `project_description` | TEXT              | nullable                                                         |
| `tech_stack`        | TEXT                | nullable                                                         |
| `repository_url`    | VARCHAR(500)        | nullable                                                         |
| `documentation_url` | VARCHAR(500)        | nullable                                                         |
| `status`            | ENUM                | DEFAULT `'pending'` — nilai: `'pending'`, `'approved'`, `'rejected'`, `'in_progress'`, `'completed'`, `'cancelled'` |
| `id_pm_reviewer`    | INT(11)             | DEFAULT NULL, FK → `user.id` (PM reviewer)                       |
| `pm_notes`          | TEXT                | nullable                                                         |
| `pm_reviewed_at`    | DATETIME            | DEFAULT NULL                                                     |
| `id_devops_handler` | INT(11)             | DEFAULT NULL, FK → `user.id` (handler DevOps)                    |
| `devops_notes`      | TEXT                | nullable                                                         |
| `hosting_url`       | VARCHAR(500)        | nullable                                                         |
| `created_at`        | DATETIME            | DEFAULT `CURRENT_TIMESTAMP`                                      |
| `updated_at`        | DATETIME            | DEFAULT `CURRENT_TIMESTAMP` ON UPDATE `CURRENT_TIMESTAMP`        |

Relasi:

| Constraint       | Kolom              | References  | ON DELETE | ON UPDATE |
| ---------------- | ------------------ | ----------- | --------- | --------- |
| `fk_hr_user`     | `id_user`          | `user(id)`  | CASCADE   | CASCADE   |
| `fk_hr_project`  | `id_project`       | `project(id)` | CASCADE | CASCADE   |
| `fk_hr_pm`       | `id_pm_reviewer`   | `user(id)`  | SET NULL  | CASCADE   |
| `fk_hr_devops`   | `id_devops_handler`| `user(id)`  | SET NULL  | CASCADE   |

Nilai ENUM status dicerminkan di konstanta `HostingStatus` (`Models/Constants.cs`): `Pending`, `Approved`, `Rejected`, `InProgress`, `Completed`, `Cancelled`. Detail alur review ada di [api-hosting.md](api-hosting.md).

---

## Relasi Lengkap (FK)

| # | Constraint              | Tabel anak       | Kolom anak          | Tabel induk | Kolom induk | ON DELETE | ON UPDATE |
| - | ----------------------- | ---------------- | ------------------- | ----------- | ----------- | --------- | --------- |
| 1 | `fk_user_role`          | `user`           | `id_role`           | `role`      | `id`        | RESTRICT  | CASCADE   |
| 2 | `fk_user_divisi`        | `user`           | `id_divisi`         | `divisi`    | `id`        | SET NULL   | CASCADE   |
| 3 | `fk_pa_user`            | `project_anggota`| `id_user`           | `user`      | `id`        | CASCADE   | CASCADE   |
| 4 | `fk_pa_project`         | `project_anggota`| `id_project`        | `project`   | `id`        | CASCADE   | CASCADE   |
| 5 | `fk_target_user`        | `target`         | `id_user`           | `user`      | `id`        | CASCADE   | CASCADE   |
| 6 | `fk_target_project`     | `target`         | `id_project`        | `project`   | `id`        | CASCADE   | CASCADE   |
| 7 | `fk_target_status`      | `target`         | `id_status`         | `status`    | `id`        | SET NULL   | CASCADE   |
| 8 | `fk_absensi_target`     | `absensi`        | `id_target`         | `target`    | `id`        | CASCADE   | CASCADE   |
| 9 | `fk_hr_user`            | `hosting_request`| `id_user`           | `user`      | `id`        | CASCADE   | CASCADE   |
| 10| `fk_hr_project`         | `hosting_request`| `id_project`        | `project`   | `id`        | CASCADE   | CASCADE   |
| 11| `fk_hr_pm`              | `hosting_request`| `id_pm_reviewer`    | `user`      | `id`        | SET NULL   | CASCADE   |
| 12| `fk_hr_devops`          | `hosting_request`| `id_devops_handler` | `user`      | `id`        | SET NULL   | CASCADE   |

Pola perilaku:

- **CASCADE delete**: menghapus induk (`user`, `project`, `target`) ikut menghapus baris anak (keanggotaan, target, absensi, pengajuan hosting oleh user tsb).
- **SET NULL**: menghapus `divisi`/`status`, atau menghapus user yang menjabat sebagai reviewer/handler, membuat kolom FK jadi NULL — baris anak tetap ada.
- **RESTRICT**: `role` tidak bisa dihapus selama masih dirujuk `user.id_role`.
- Semua FK memakai `ON UPDATE CASCADE` — aman mengubah `id` induk secara manual (meski praktiknya jarang, karena PK auto-increment).

---

## ERD

```mermaid
erDiagram
    divisi ||--o{ user : "id_divisi (SET NULL)"
    role ||--o{ user : "id_role (RESTRICT)"
    user ||--o{ project_anggota : "id_user (CASCADE)"
    project ||--o{ project_anggota : "id_project (CASCADE)"
    user ||--o{ target : "id_user (CASCADE)"
    project ||--o{ target : "id_project (CASCADE)"
    status ||--o{ target : "id_status (SET NULL)"
    target ||--o{ absensi : "id_target (CASCADE)"
    user ||--o{ hosting_request : "id_user pemohon (CASCADE)"
    project ||--o{ hosting_request : "id_project (CASCADE)"
    user ||--o{ hosting_request : "id_pm_reviewer (SET NULL)"
    user ||--o{ hosting_request : "id_devops_handler (SET NULL)"

    divisi {
        int id PK
        varchar nama
    }
    role {
        int id PK
        varchar nama
    }
    status {
        int id PK
        varchar nama
    }
    project {
        int id PK
        varchar nama
    }
    user {
        int id PK
        varchar nama UQ
        varchar password
        int id_role FK "NOT NULL"
        int id_divisi FK "nullable"
        varchar refresh_token "nullable"
        datetime refresh_token_expired "nullable"
    }
    project_anggota {
        int id PK
        int id_user FK
        int id_project FK
    }
    target {
        int id PK
        int id_user FK
        int id_project FK
        varchar target
        int id_status FK
    }
    absensi {
        int id PK
        date tanggal
        int id_target FK
        time jam_masuk
        time jam_pulang
    }
    hosting_request {
        int id PK
        int id_user FK "NOT NULL"
        int id_project FK "NOT NULL"
        varchar contact_name
        varchar contact_email
        varchar contact_phone
        text project_description
        text tech_stack
        varchar repository_url
        varchar documentation_url
        enum status "default pending"
        int id_pm_reviewer FK
        text pm_notes
        datetime pm_reviewed_at
        int id_devops_handler FK
        text devops_notes
        varchar hosting_url
        datetime created_at
        datetime updated_at
    }
```

---

## Seed Data

Dihasilkan oleh `setup.sql` (dan dijamin ulang oleh `DatabaseBootstrap`):

**`role`** (`INSERT IGNORE`)

| id | nama    |
| -- | ------- |
| 1  | Admin   |
| 2  | PM      |
| 3  | Guru    |
| 4  | Anggota |
| 5  | DevOps  |

**`divisi`** (`INSERT IGNORE`)

| id | nama      |
| -- | --------- |
| 1  | Backend   |
| 2  | Frontend  |
| 3  | Game      |

**`status`** (`INSERT IGNORE`)

| id | nama        |
| -- | ----------- |
| 1  | Null        |
| 2  | On Progress |
| 3  | Done        |
| 4  | Izin        |
| 5  | Sakit       |

Catatan seed ulang:

- `INSERT IGNORE` hanya mengisi baris yang **id-nya belum ada**; baris yang sudah ada (meski namanya salah) tidak disentuh oleh `INSERT IGNORE`.
- ID role dikode keras di `Models/Constants.cs` (`RoleIds`: Admin=1, PM=2, Guru=3, Anggota=4, DevOps=5) dan dipakai banyak query (mis. cek `user.id_role = 5` untuk DevOps), jadi nama pada id tsb harus tepat.
- Karena itu `DatabaseBootstrap` juga menjalankan `UPDATE ... WHERE id = N AND nama <> ...` untuk mengembalikan nama kanonik role, divisi, dan status bila volume lama punya nama salah.
- Menjalankan ulang `setup.sql` aman: semua DDL memakai `IF NOT EXISTS` dan semua seed memakai `INSERT IGNORE`.

---

## DatabaseBootstrap.EnsureAsync (Startup)

`Services/Infrastructure/DatabaseBootstrap.cs` dipanggil saat aplikasi start, di atas koneksi `ConnectionStrings:DefaultConnection`. Docker init script (`setup.sql`) hanya sekali saat volume DB **baru** dibuat; bootstrap inilah yang menjaga skema tetap lengkap pada volume lama.

Yang dijalankan saat startup:

1. **Retry koneksi MySQL** — 7 percobaan dengan jeda 2, 5, 10, 20, 30, 45 detik (total menunggu ± 2 menit) agar backend tidak crash-loop ketika MySQL belum siap. Koneksi dibuat ulang tiap percobaan.
2. `INSERT IGNORE INTO role` — `(1,'Admin'), (2,'PM'), (3,'Guru'), (4,'Anggota'), (5,'DevOps')` — memastikan role **DevOps** (id 5) ikut terpasang di volume lama yang di-seed sebelum DevOps ada.
3. `UPDATE role` per-id — mengembalikan nama kanonik Admin/PM/Guru/Anggota/DevOps bila salah.
4. `INSERT IGNORE` + `UPDATE` untuk `divisi` (Backend/Frontend/Game) dan `status` (Null/On Progress/Done/Izin/Sakit) — pola sama.
5. `CREATE TABLE IF NOT EXISTS hosting_request` — membuat tabel hosting_request (lengkap dengan FK-nya) bila belum ada pada volume Docker lama.

Implikasi untuk volume Docker lama:

- Tidak perlu `docker compose down -v` / reset volume hanya untuk mendapat tabel `hosting_request` atau role DevOps: cukup restart backend, bootstrap akan melengkapinya.
- Bootstrap tidak meng-upgrade kolom tabel yang sudah ada selain lewat statements di atas (misal tidak menambah kolom baru ke tabel lama) — perubahan DDL tabel non-hosting tetap perlu `setup.sql` pada volume baru atau migrasi manual.
- Jika bootstrap gagal setelah semua retry, aplikasi melempar `InvalidOperationException` ("Gagal melakukan database bootstrap setelah beberapa percobaan") dan tidak lanjut start.

---

## Catatan Khusus

1. **ENUM `status` pada `hosting_request`** — nilai hanya `'pending'`, `'approved'`, `'rejected'`, `'in_progress'`, `'completed'`, `'cancelled'`, default `'pending'`. Nilai di luar enum akan ditolak MySQL. Konstanta C#-nya di `HostingStatus` (`Models/Constants.cs`).
2. **Kolom `refresh_token` di tabel `user`** — menyimpan refresh token JWT per user bersama `refresh_token_expired` (berlaku 20 hari). Kolom ini nullable; lihat alur login/refresh di [authentication.md](authentication.md).
3. **`updated_at` dengan `ON UPDATE CURRENT_TIMESTAMP`** — hanya di tabel `hosting_request`: setiap `UPDATE` baris memperbarui `updated_at` otomatis (kecuali kolom itu ikut diset eksplisit). Tabel lain tidak punya kolom timestamp otomatis.
4. **Charset & engine** — semua tabel `ENGINE=InnoDB DEFAULT CHARSET=utf8mb4` (aman untuk emoji/Unicode, dukungan FK penuh InnoDB).
5. **`user.nama` UNIQUE** — username unik (`uq_user_nama`); register/login memakai `nama` sebagai identifier.
6. **Password disimpan ter-hash** — kolom `user.password` berisi hash BCrypt, bukan plain text.
7. **Tabel `absensi` tanpa FK ke `user`** — absensi dirujuk lewat `target.id_target`; rantainya: `user` → `target` → `absensi`.
8. **`project` hanya punya `id` + `nama`** — tidak ada FK dari `project` ke tabel lain.

---

## Dokumen terkait

- [Arsitektur](architecture.md)
- [Autentikasi](authentication.md)
- [API Absensi](api-absensi.md)
- [API Hosting](api-hosting.md)
- [API Manajemen](api-management.md)
