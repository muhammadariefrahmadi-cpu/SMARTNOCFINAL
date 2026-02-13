using SMART_NOC.Models;
using SMART_NOC.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMART_NOC.Services
{
    public static class TicketStoreService
    {
        private static readonly List<TransmissionTicket> _tickets = new();

        public static event EventHandler? TicketsChanged;

        static TicketStoreService()
        {
            SeedInitialSample();
        }

        public static IReadOnlyList<TransmissionTicket> GetAll()
        {
            return _tickets
                .OrderByDescending(t => t.CreatedAt)
                .ToList();
        }

        public static void Add(TransmissionTicket ticket)
        {
            _tickets.Insert(0, ticket);
            TicketsChanged?.Invoke(null, EventArgs.Empty);
        }

        private static void SeedInitialSample()
        {
            const string sampleCoordinate = "6°24'10\"S, 106°49'37\"E";
            CoordinateParser.TryParseDms(sampleCoordinate, out var lat, out var lng);

            var sample = new TransmissionTicket
            {
                Title = "*[FLP_3rd_MANDAU][Open - Major] 13CBN0958_COHAK_NAGRAK_PL(100363)<>13DPK0406_NILAM_DEPOK_PL(03DPK106) - DOWN - DATACOM-INC-20260131-00025654*",
                OccurTime = "31-01-2026 19:35",
                DispatchTime = "13-02-2026 01:11",
                Pic = "Sangga(Depok)",
                RootCause = "Still Investigation",
                CutPoint = "Still Investigation",
                CoordinateDms = sampleCoordinate,
                UpdateProgress =
@"01:50 We already open TT MDU-ISAT20210000068669 (Segment TT PM 100363-100161),team otw jc km 2 dari 100161 eta 35menit

03:35 team request off tx 100363,namun aktual dilokasi masih ada power

04:42 team coba pengecekan jc km 5 dari 100161

05:40 sudah pindah jc aktual dilokasi tetap masih ada power,team request jovis ke2 sisi guna N to N",
                Latitude = lat,
                Longitude = lng,
                CreatedAt = DateTime.Now
            };

            sample.TemplatePreview = BuildTemplate(sample);
            _tickets.Add(sample);
        }

        public static string BuildTemplate(TransmissionTicket ticket)
        {
            return
$@"{ticket.Title}

Occur Time = {ticket.OccurTime}

Dispacth Time = {ticket.DispatchTime}

PIC = {ticket.Pic}

Rootcause = {ticket.RootCause}

Cut Point = {ticket.CutPoint}



Update Progress 

{ticket.UpdateProgress}";
        }
    }
}
