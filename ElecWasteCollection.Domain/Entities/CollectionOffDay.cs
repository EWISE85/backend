using ElecWasteCollection.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("CollectionOffDays")] 
public class CollectionOffDay
{
    [Key]
    public Guid Id { get; set; }
    public string? CompanyId { get; set; }
    public virtual Company? Company { get; set; }
    public string? CollectionUnitId { get; set; }
    public virtual CollectionUnit? CollectionUnits { get; set; }
    public DateOnly OffDate { get; set; }

    public string? Reason { get; set; } 

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}