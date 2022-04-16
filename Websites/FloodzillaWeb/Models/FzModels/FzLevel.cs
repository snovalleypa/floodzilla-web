using System.ComponentModel.DataAnnotations.Schema;

namespace FloodzillaWeb.Models.FzModels
{
    public partial class FzLevel
    {
        public long Id { get; set; }
        public int DeviceId { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? Iteration { get; set; }
        public int? HeaderId { get; set; }
        public int? LocationId { get; set; }
        public double? L1 { get; set; }
        public double? L2 { get; set; }
        public double? L3 { get; set; }
        public double? L4 { get; set; }
        public double? L5 { get; set; }
        public double? L6 { get; set; }
        public double? L7 { get; set; }
        public double? L8 { get; set; }
        public double? L9 { get; set; }
        public double? L10 { get; set; }
        public double? L11 { get; set; }
        public double? L12 { get; set; }
        public double? L13 { get; set; }
        public double? L14 { get; set; }
        public double? L15 { get; set; }
        public double? L16 { get; set; }
        public double? L17 { get; set; }
        public double? L18 { get; set; }
        public double? L19 { get; set; }
        public double? L20 { get; set; }
        public double? L21 { get; set; }
        public double? L22 { get; set; }
        public double? L23 { get; set; }
        public double? L24 { get; set; }
        public double? L25 { get; set; }
        public double? L26 { get; set; }
        public double? L27 { get; set; }
        public double? L28 { get; set; }
        public double? L29 { get; set; }
        public double? L30 { get; set; }
        public double? L31 { get; set; }
        public double? L32 { get; set; }
        public double? L33 { get; set; }
        public double? L34 { get; set; }
        public double? L35 { get; set; }
        public double? L36 { get; set; }
        public double? L37 { get; set; }
        public double? L38 { get; set; }
        public double? L39 { get; set; }
        public double? L40 { get; set; }
        public double? L41 { get; set; }
        public double? L42 { get; set; }
        public double? L43 { get; set; }
        public double? L44 { get; set; }
        public double? L45 { get; set; }
        public double? L46 { get; set; }
        public double? L47 { get; set; }
        public double? L48 { get; set; }
        public double? L49 { get; set; }
        public double? L50 { get; set; }
        public double? L51 { get; set; }
        public double? L52 { get; set; }
        public double? L53 { get; set; }
        public double? L54 { get; set; }
        public double? L55 { get; set; }
        public double? L56 { get; set; }
        public double? L57 { get; set; }
        public double? L58 { get; set; }
        public double? L59 { get; set; }
        public double? L60 { get; set; }
        public double? L61 { get; set; }
        public double? L62 { get; set; }
        public double? L63 { get; set; }
        public double? L64 { get; set; }
        public double? L65 { get; set; }
        public double? L66 { get; set; }
        public double? L67 { get; set; }
        public double? L68 { get; set; }
        public double? L69 { get; set; }
        public double? L70 { get; set; }
        public double? L71 { get; set; }
        public double? L72 { get; set; }
        public double? L73 { get; set; }
        public double? L74 { get; set; }
        public double? L75 { get; set; }
        public double? L76 { get; set; }
        public double? L77 { get; set; }
        public double? L78 { get; set; }
        public double? L79 { get; set; }
        public double? L80 { get; set; }
        public double? L81 { get; set; }
        public double? L82 { get; set; }
        public double? L83 { get; set; }
        public double? L84 { get; set; }
        public double? L85 { get; set; }
        public double? L86 { get; set; }
        public double? L87 { get; set; }
        public double? L88 { get; set; }
        public double? L89 { get; set; }
        public double? L90 { get; set; }
        public double? AvgL { get; set; }
        public double? StDevL { get; set; }
        public double? WaterHeight { get; set; }
        public bool IsDeleted { get; set; }
        [NotMapped]
        public double? Temperature { get; set; }
        [NotMapped]
        public double? Precip1HourMM { get; set; }
        [NotMapped]
        public string LocationName { get; set; }
        [NotMapped]
        public double? GroundHeight { get; set; }

    }
}
