using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RentalService.DBContext.Models;

[Table("delivery_time_analysis", Schema = "rental")]
public partial class DeliveryTimeAnalysis
{
    [Key]
    [Column("identifier")]
    public long Identifier { get; set; }

    [Column("rental_id")]
    public long RentalId { get; set; }

    [Column("delivery_person")]
    public int DeliveryPerson { get; set; }

    [Column("started_at", TypeName = "timestamp without time zone")]
    public DateTime StartedAt { get; set; }

    [Column("delivery_time")]
    public TimeSpan DeliveryTime { get; set; }
}
