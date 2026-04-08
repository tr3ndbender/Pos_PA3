using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Server.Models;

namespace Server
{
    internal class AppDbContext : DbContext
    {

        public DbSet<Word> Words { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder o)
        => o.UseSqlite("Data Source=wordle.db");
    }
}
