# 📋 Backend Sistem Absensi

Backend REST API untuk sistem absensi berbasis **project & target harian**, dibangun dengan **ASP.NET Core 10 Minimal API + MVC**, **MySQL 8.0**, dan autentikasi **JWT Bearer**.

Dokumentasi lengkap terbagi per topik di folder [`docs/`](docs/):

| Dokumen | Isi |
| --- | --- |
| [Arsitektur & Infrastruktur](docs/architecture.md) | Tech stack, struktur folder, request pipeline, DI, konfigurasi environment, setup Docker/local, pengujian |
| [Skema Database](docs/database.md) | 9 tabel, relasi/FK, ERD (Mermaid), seed & bootstrap saat startup |
| [Autentikasi & Otorisasi](docs/authentication.md) | JWT, endpoint auth, refresh token, validasi state per-request, daftar role |
| [API Absensi, Target & Proyek](docs/api-absensi.md) | Endpoint absensi (rekap/masuk/pulang), target, project, project-anggota |
| [API Hosting Request](docs/api-hosting.md) | State machine, 14 endpoint hosting, aturan handler & admin |
| [API Manajemen User & Master Data](docs/api-management.md) | Admin user CRUD, CRUD per role (+alias legacy), divisi/role/status |

**Mulai cepat:**

```bash
docker compose up --build          # backend :5072, MySQL :3307 — detail di docs/architecture.md
```

Dokumentasi API interaktif (mode Development): `http://localhost:5072/scalar`
