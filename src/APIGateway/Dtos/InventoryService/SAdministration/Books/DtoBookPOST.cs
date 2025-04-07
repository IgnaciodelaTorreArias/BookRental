using System.ComponentModel.DataAnnotations;
using InventoryService.Services.Administration;

namespace APIGateway.Dtos.InventoryService.SAdministration.Books;

public class DtoBookPOST : IDtoIN<Book>, IValidatableObject
{
    [Required]
    [StringLength(255)]
    public string BookName { get; set; } = null!;
    [Required]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "The field IsoLanguageCode must be a string with a length of 3 (ISO 639-3).")]
    public string IsoLanguageCode { get; set; } = null!;
    [StringLength(255)]
    public string? AuthorName { get; set; }
    public DateOnly? PublishedDate { get; set; }
    [StringLength(5000)]
    public string? Description { get; set; }
    public uint? RentalFee { get; set; }
    public bool? Visible { get; set; }
    public bool? PublishedDateUnknown { get; set; }

    public Book Message()
    {
        Book message = new()
        {
            BookName = BookName,
            IsoLanguageCode = IsoLanguageCode,
        };
        if (AuthorName != null)
            message.AuthorName = AuthorName;
        if (PublishedDate.HasValue)
            message.PublishedDate = (uint)PublishedDate.Value.DayNumber;
        if (RentalFee.HasValue)
            message.RentalFee = RentalFee.Value;
        if (Description != null)
            message.Description = Description;
        if (Visible.HasValue)
            message.Visible = Visible.Value;
        if (PublishedDateUnknown.HasValue)
            message.PublishedDateUnknown = PublishedDateUnknown.Value;
        return message;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PublishedDateUnknown.HasValue)
        {
            if (PublishedDateUnknown.Value != PublishedDate.HasValue)
            {
                yield return new ValidationResult("`PublishedDateUnknow` and `PublishedDate` must be consistent");
            }
        }
    }
}
