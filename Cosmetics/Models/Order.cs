﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using Cosmetics.Enum;
using System;
using System.Collections.Generic;

namespace Cosmetics.Models;

public partial class Order
{
    public Guid OrderId { get; set; }

    public int? CustomerId { get; set; }

    public int? SalesStaffId { get; set; }

    public decimal? TotalAmount { get; set; }

    public OrderStatus Status { get; set; }

    public DateTime? OrderDate { get; set; }

    public string PaymentMethod { get; set; }

    public string Address { get; set; }

    public virtual User Customer { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual PaymentTransaction PaymentTransaction { get; set; }
}