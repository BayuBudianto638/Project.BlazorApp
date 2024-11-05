using Project.Database.BaseEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Database.Models
{
    public class Customer : Table
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
    }
}
