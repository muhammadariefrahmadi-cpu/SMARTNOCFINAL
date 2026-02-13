using Microsoft.EntityFrameworkCore;
using SMART_NOC.Models;
using System;
using System.IO;

namespace SMART_NOC.Services.Database
{
    public class AppDbContext : DbContext
    {
        // Tabel Database: Disini kita daftarkan model yang mau disimpan
        public DbSet<TicketLog> Tickets { get; set; }

        // Jika nanti mau simpan HandoverLog, tinggal uncomment baris ini:
        // public DbSet<HandoverLog> Handovers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Tentukan lokasi file database
            // Kita simpan di LocalApplicationData agar aman dan tidak perlu izin admin
            var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var path = Path.Combine(folderPath, "SMART_NOC");

            // Pastikan folder penyimpanan ada
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            // Nama file database: smart_noc.db
            var dbPath = Path.Combine(path, "smart_noc.db");

            // Konfigurasi menggunakan SQLite
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Konfigurasi tambahan (Opsional)
            // Memastikan TT_IOH dianggap sebagai Primary Key (Kunci Unik)
            modelBuilder.Entity<TicketLog>()
                .HasKey(t => t.TT_IOH);
        }
    }
}