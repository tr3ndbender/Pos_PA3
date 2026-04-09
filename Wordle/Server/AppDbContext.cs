using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Server.Model;


namespace Server
{
    internal class AppDbContext : DbContext
    {
        public DbSet<Word> Words => Set<Word>(); //ändern

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=wordle.db");
    }
}
