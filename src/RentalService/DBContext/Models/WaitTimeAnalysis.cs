using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RentalService.DBContext.Models;

[Table("wait_time_analysis", Schema = "rental")]
public partial class WaitTimeAnalysis
{
    [Key]
    [Column("identifier")]
    public long Identifier { get; set; }

    [Column("book_id")]
    public int BookId { get; set; }

    [Column("waited_since", TypeName = "timestamp without time zone")]
    public DateTime WaitedSince { get; set; }

    [Column("waited_for")]
    public TimeSpan WaitedFor { get; set; }
}
