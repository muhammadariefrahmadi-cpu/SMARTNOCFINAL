using Microsoft.EntityFrameworkCore;
using SMART_NOC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMART_NOC.Services.Database
{
    public class SqlService
    {
        // --- 1. INISIALISASI DATABASE (Wajib dipanggil pas start app) ---
        public async Task InitializeAsync()
        {
            using (var db = new AppDbContext())
            {
                // Perintah sakti: Cek apakah file .db ada? Kalau belum, buatkan otomatis!
                await db.Database.EnsureCreatedAsync();
            }
        }

        // --- 2. GET ALL TICKETS (READ) ---
        public async Task<List<TicketLog>> GetAllTicketsAsync()
        {
            using (var db = new AppDbContext())
            {
                // AsNoTracking() bikin pembacaan lebih cepat karena data tidak di-cache untuk edit
                return await db.Tickets
                    .AsNoTracking()
                    .OrderByDescending(t => t.OccurTime) // Urutkan dari yang terbaru
                    .ToListAsync();
            }
        }

        // --- 3. ADD NEW TICKET (CREATE) ---
        public async Task<bool> AddTicketAsync(TicketLog ticket)
        {
            if (ticket == null || string.IsNullOrEmpty(ticket.TT_IOH)) return false;

            using (var db = new AppDbContext())
            {
                try
                {
                    // Cek apakah tiket dengan ID ini sudah ada? (Mencegah duplikat)
                    bool exists = await db.Tickets.AnyAsync(t => t.TT_IOH == ticket.TT_IOH);
                    if (exists)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SQL] Ticket {ticket.TT_IOH} already exists.");
                        return false;
                    }

                    await db.Tickets.AddAsync(ticket);
                    await db.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SQL Insert Error]: {ex.Message}");
                    return false;
                }
            }
        }

        // --- 4. UPDATE TICKET (EDIT) ---
        public async Task<bool> UpdateTicketAsync(TicketLog ticket)
        {
            if (ticket == null) return false;

            using (var db = new AppDbContext())
            {
                try
                {
                    // EF Core cukup pintar, Update() akan mencari data berdasarkan Primary Key (TT_IOH)
                    db.Tickets.Update(ticket);
                    await db.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SQL Update Error]: {ex.Message}");
                    return false;
                }
            }
        }

        // --- 5. DELETE TICKET (HAPUS) ---
        public async Task<bool> DeleteTicketAsync(string ticketId)
        {
            using (var db = new AppDbContext())
            {
                // Cari dulu datanya
                var target = await db.Tickets.FindAsync(ticketId);
                if (target != null)
                {
                    db.Tickets.Remove(target);
                    await db.SaveChangesAsync();
                    return true;
                }
                return false; // Data tidak ditemukan
            }
        }

        // --- 6. IMPORT DATA (BATCH) ---
        // Berguna kalau mau migrasi banyak data sekaligus dari Excel
        public async Task BulkInsertAsync(List<TicketLog> tickets)
        {
            if (tickets == null || tickets.Count == 0) return;

            using (var db = new AppDbContext())
            {
                foreach (var t in tickets)
                {
                    // Cek duplikat sederhana
                    if (!await db.Tickets.AnyAsync(x => x.TT_IOH == t.TT_IOH))
                    {
                        await db.Tickets.AddAsync(t);
                    }
                }
                await db.SaveChangesAsync();
            }
        }
    }
}