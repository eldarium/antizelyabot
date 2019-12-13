using Microsoft.EntityFrameworkCore;

namespace ZelyaDushitelBot
{
    public class RememberMessage
    {
        public long Id { get; set; }
        public long MessageId { get; set; }
        public long AuthorId{get;set;}
        public long ChatId{get;set;}
    }

    public class RememberMessageContext : DbContext{
        public DbSet<RememberMessage> Messages{get;set;}

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=messages.db");
    }
}