CREATE DATABASE IF NOT EXISTS `absensi`;
USE `absensi`;

-- 1. Tabel divisi
CREATE TABLE IF NOT EXISTS `divisi` (
  `id` INT(11) NOT NULL AUTO_INCREMENT,
  `nama` VARCHAR(255) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 2. Tabel role
CREATE TABLE IF NOT EXISTS `role` (
  `id` INT(11) NOT NULL AUTO_INCREMENT,
  `nama` VARCHAR(255) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 3. Tabel status
CREATE TABLE IF NOT EXISTS `status` (
  `id` INT(11) NOT NULL AUTO_INCREMENT,
  `nama` VARCHAR(255) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 4. Tabel project
CREATE TABLE IF NOT EXISTS `project` (
  `id` INT(11) NOT NULL AUTO_INCREMENT,
  `nama` VARCHAR(255) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 5. Tabel user
CREATE TABLE IF NOT EXISTS `user` (
  `id` INT(11) NOT NULL AUTO_INCREMENT,
  `nama` VARCHAR(255) DEFAULT NULL,
  `password` VARCHAR(255) DEFAULT NULL,
  `id_role` INT(11) DEFAULT NULL,
  `id_divisi` INT(11) DEFAULT NULL,
  `refresh_token` VARCHAR(255) DEFAULT NULL,
  `refresh_token_expired` DATETIME DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_user_role` (`id_role`),
  KEY `fk_user_divisi` (`id_divisi`),
  CONSTRAINT `fk_user_divisi` FOREIGN KEY (`id_divisi`) REFERENCES `divisi` (`id`) ON DELETE SET NULL ON UPDATE CASCADE,
  CONSTRAINT `fk_user_role` FOREIGN KEY (`id_role`) REFERENCES `role` (`id`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 6. Tabel project_anggota
CREATE TABLE IF NOT EXISTS `project_anggota` (
  `id` INT(11) NOT NULL AUTO_INCREMENT,
  `id_user` INT(11) DEFAULT NULL,
  `id_project` INT(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_pa_user` (`id_user`),
  KEY `fk_pa_project` (`id_project`),
  CONSTRAINT `fk_pa_project` FOREIGN KEY (`id_project`) REFERENCES `project` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_pa_user` FOREIGN KEY (`id_user`) REFERENCES `user` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 7. Tabel target
CREATE TABLE IF NOT EXISTS `target` (
  `id` INT(11) NOT NULL AUTO_INCREMENT,
  `id_user` INT(11) DEFAULT NULL,
  `id_project` INT(11) DEFAULT NULL,
  `target` VARCHAR(255) DEFAULT NULL,
  `id_status` INT(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_target_user` (`id_user`),
  KEY `fk_target_project` (`id_project`),
  KEY `fk_target_status` (`id_status`),
  CONSTRAINT `fk_target_project` FOREIGN KEY (`id_project`) REFERENCES `project` (`id`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_target_status` FOREIGN KEY (`id_status`) REFERENCES `status` (`id`) ON DELETE SET NULL ON UPDATE CASCADE,
  CONSTRAINT `fk_target_user` FOREIGN KEY (`id_user`) REFERENCES `user` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 8. Tabel absensi
CREATE TABLE IF NOT EXISTS `absensi` (
  `id` INT(11) NOT NULL AUTO_INCREMENT,
  `tanggal` DATE DEFAULT NULL,
  `id_target` INT(11) DEFAULT NULL,
  `jam_masuk` TIME DEFAULT NULL,
  `jam_pulang` TIME DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_absensi_target` (`id_target`),
  CONSTRAINT `fk_absensi_target` FOREIGN KEY (`id_target`) REFERENCES `target` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Data Awal (Seed Data)
INSERT INTO `role` (`id`, `nama`) VALUES (1, 'Admin'), (2, 'PM'), (3, 'Guru'), (4, 'Anggota');
INSERT INTO `divisi` (`id`, `nama`) VALUES (1, 'Backend'), (2, 'Frontend'), (3. 'Game');
INSERT INTO `status` (`id`, `nama`) VALUES (1, 'Null'), (2, 'On Progress'), (3, 'Done'), (4, 'Izin'), (5, 'Sakit');
