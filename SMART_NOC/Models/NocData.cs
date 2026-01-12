namespace SMART_NOC.Models
{
    public class NocData
    {
        // Kolom A (Tampilan di Dropdown Segment PM)
        public string SegmentRoute { get; set; } = "";

        // Kolom J (Hidden ID buat Logic Pencarian Regex)
        public string SpanId { get; set; } = "";

        // Kolom B (Team)
        public string TeamSerpo { get; set; } = "";

        // Kolom C (Nama)
        public string PicName { get; set; } = "";

        // Kolom D (No HP)
        public string PicPhone { get; set; } = "";

        // Format Tampilan Dropdown PIC: "Deden Permana | Jakarta Timur | 089xxx"
        public string FullPicInfo => $"{PicName} | {TeamSerpo} | {PicPhone}";
    }
}