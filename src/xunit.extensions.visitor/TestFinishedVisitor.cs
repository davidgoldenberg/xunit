namespace xunit.extensions.visitor
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity;
    using System.Data.Entity.ModelConfiguration;
    
    using Xunit;
    using Xunit.Abstractions;

    public class TestFinishedVisitor : TestMessageVisitor
    {
        public static ConcurrentDictionary<ITestCase, TestData> _dictionary = new ConcurrentDictionary<ITestCase,TestData>();

        protected override bool Visit(ITestAssemblyStarting assemblyStarting)
        {
            _dictionary.Clear();
            return base.Visit(assemblyStarting);
        }

        protected override bool Visit(ITestAssemblyFinished assemblyFinished)
        {
            var context = new TestDataContext();
            new TestDataRepository(context).Add(_dictionary.Values);
            context.SaveChanges();
            return base.Visit(assemblyFinished);
        }

        protected override bool Visit(ITestCaseStarting value)
        {
            _dictionary.TryAdd(value.TestCase, new TestData{DisplayName = value.TestCase.DisplayName});
            return base.Visit(value);
        }

        protected override bool Visit(ITestFinished value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            var t = _dictionary[value.TestCase];
            t.RunTime = value.ExecutionTime;
            t.Time = DateTime.Now;
            return base.Visit(value);
        }

        protected override bool Visit(ITestPassed value)
        {
            _dictionary[value.TestCase].Passed = true;
            return base.Visit(value);
        }
    }

    public class TestData
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Fully qualified name of the test (assembly.methodname)
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// True if the test passed
        /// </summary>
        public bool Passed { get; set; }

        /// <summary>
        /// In seconds
        /// </summary>
        public decimal RunTime { get; set; }

        /// <summary>
        /// Time that the test completed
        /// </summary>
        public DateTime Time { get; set; }
    }

    public class TestDataMapping : EntityTypeConfiguration<TestData>
    {
        public TestDataMapping()
        {
            ToTable("TestData");
            HasKey(t => t.Id);
            Property(t => t.DisplayName).HasColumnType("nvarchar").HasMaxLength(500).HasColumnName("DisplayName").IsRequired();
            Property(t => t.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity).HasColumnType("bigint").HasColumnName("Id");
            Property(t => t.Passed).HasColumnType("bit").HasColumnName("Passed").IsRequired();
            Property(t => t.RunTime).HasColumnType("decimal").HasColumnName("RunTime").HasPrecision(7, 3).IsRequired();
            Property(t => t.Time).HasColumnType("datetime").HasColumnName("Time").IsRequired();
        }
    }

    public class TestDataContext : DbContext, ITestDataContext
    {
        static TestDataContext()
        {
            //Database.SetInitializer<TestDataContext>(null);
        }
        public TestDataContext()
            : base("Server=localhost;Integrated Security=True;Persist Security Info=True;Database=TestData;")
        {}

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new TestDataMapping());
            base.OnModelCreating(modelBuilder);
        }

        public IDbSet<T> GetDbSet<T>() where T : class
        {
            return Set<T>();
        }
    }

    public interface ITestDataContext
    {
        IDbSet<T> GetDbSet<T>() where T : class;
        int SaveChanges();
    }

    public class TestDataRepository
    {
        private readonly ITestDataContext _context;
        private readonly IDbSet<TestData> _testData;

        public TestDataRepository(ITestDataContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            _context = context;
            _testData = context.GetDbSet<TestData>();
            if (_testData == null)
                throw new ArgumentException("Null DbSet retrieved from context");
        }

        public void Add(IEnumerable<TestData> testData)
        {
            if (testData == null)
                return;

            foreach (var t in testData)
                _testData.Add(t);
        }
    }
}