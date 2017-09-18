using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace EFCore
{
    class Program
    {
        public static void Main(string[] args)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            Console.WriteLine("開始");

            var db = new AppDbContext(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=EntityFrameworkCodeFirstTest.MyContext;Integrated Security=True");
            var http = new System.Net.Http.HttpClient();

            //フィードを適当なサイトから取得
            foreach (var targetUrl in
                new[] {
                    "http://www3.asahi.com/rss/index.rdf",
                    "http://rss.rssad.jp/rss/codezine/new/20/index.xml",
                })
            {
                //フィードxmlをDL & Parse
                //xmlは名前空間で面倒が生じないよう名前空間情報を除染
                var rssTxt = http.GetStringAsync(targetUrl).Result;
                var rss = System.Xml.Linq.XElement.Parse(rssTxt);
                foreach (var item in rss.Descendants())
                    item.Name = item.Name.LocalName;

                //フィードの記事をModelオブジェクトへ移し替える
                var articles = rss
                    .Descendants("item")
                    .Select(item =>
                        new Article()
                        {
                            Title = item.Element("title").Value,
                            LinkUrl = item.Element("link").Value,
                            Description = item.Element("description").Value,
                            ChannelTitle = rss.Element("channel").Element("title").Value,
                        });

                //DBに未追加の記事をDBへ保存する
                foreach (var item in articles)
                {
                    if (db.Articles.Any(_ => _.LinkUrl == item.LinkUrl))
                        continue;

                    Console.WriteLine(item.Title);
                    db.Articles.Add(item);
                }
            }
            //DBへの保存を確定
            db.SaveChanges();
            Console.WriteLine("終了");
            Console.Read();
        }
    }
}

//エンティティクラス
public class Article
{
    public int Id { get; set; }
    public string LinkUrl { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string ChannelTitle { get; set; }
}

public class Beta
{
    [Key]
    public string PrimaryKey { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
}

public class Charry
{
    public int Id { get; set; }
    public string Str { get; set; }
}

//コンテキストクラス
public class AppDbContext : DbContext
{
    string dbConnection;

    public AppDbContext(string dbConnection) : base()
    {
        this.dbConnection = dbConnection;
    }

    public AppDbContext() : base()
    {
        dbConnection = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=EntityFrameworkCodeFirstTest.MyContext;Integrated Security=True";
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        //ここでは接続先のDB名はhellocoredbとする
        optionsBuilder.UseSqlServer(dbConnection);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // add your own confguration here

        base.OnModelCreating(modelBuilder);

        new CharryMap(modelBuilder.Entity<Charry>());
    }

    public DbSet<Article> Articles { get; set; }
    public DbSet<Beta> Betas { get; set; }
    public DbSet<Charry> Charrys { get; private set; }
}

public class CharryMap
{
    public CharryMap(EntityTypeBuilder<Charry> entity)
    {
        entity.HasKey(t => t.Id);

        entity.Property(e => e.Str).IsRequired().HasColumnType("varchar(12)");
    }
}


