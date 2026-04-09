using Microsoft.EntityFrameworkCore;
using System;
using Server.Models;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    public class AppDbContext : DbContext
    {
        public DbSet<Word> Words => Set<Word>();

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=wordle.db");
    }
}
