using MuhasibPro.Domain.Enum.DatabaseEnum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MuhasibPro.Domain.Entities.SistemEntity;

[Table("MaliDonemler")]
public class MaliDonem : BaseEntity
{
    [Required]
    public long FirmaId { get; set; }

    [Required]
    public int MaliYil { get; set; }


    [Required]
    public string DatabaseName { get; set; }
   
    public DatabaseType DatabaseType { get; set; }

    public Firma Firma { get; set; }
    public string BuildSearchTerms() => $"{Id} {FirmaId} {MaliYil}".ToLower();
}
