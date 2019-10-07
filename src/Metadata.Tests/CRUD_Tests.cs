using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneCSharp.Metadata.Server;
using OneCSharp.Metadata.Shared;
using OneCSharp.Persistence.Shared;
using System;

namespace OneCSharp.Metadata.Tests
{
    [TestClass]
    public class CRUD_Tests
    {
        private static IPersistentContext context;
        private static string connectionString = "Data Source=ZHICHKIN;Initial Catalog=one-c-sharp;Integrated Security=True";

        private static Guid testKey = new Guid("C2FCA0B4-CE8B-4B4D-B3A2-90A62C4BF04A");
        private static Guid _namespaceTestKey1 = new Guid("A9A23DD5-F897-4DF5-9CA7-897CAE12D67B");
        private static Guid _namespaceTestKey2 = new Guid("83FF8A8C-4341-48FE-BC40-3C2AACE43111");

        static CRUD_Tests()
        {
            Initialize();
        }
        public static void Initialize()
        {
            context = new PersistentContext(connectionString);
            var factory = new MetadataObjectFactory();

            context.AddDataPersister(typeof(InfoBase), new InfoBaseDataPersister(context));
            context.AddDataPersister(typeof(Namespace), new NamespaceDataPersister(context));

            context.AddObjectFactory(typeof(InfoBase), factory);
            context.AddObjectFactory(typeof(Namespace), factory);
        }
        [TestMethod]
        public void GetInfoBaseDataPersisterByType()
        {
            IDataPersister persister = context.GetDataPersister(typeof(InfoBase));
            var expected = persister as InfoBaseDataPersister;
            Assert.AreEqual(typeof(InfoBaseDataPersister), expected.GetType());
        }
        [TestMethod]
        public void GetInfoBaseDataPersisterByTypeCode()
        {
            InfoBase ib = new InfoBase(new Guid("A074E116-B84E-4044-8E4D-9B75F86D84D1"));
            
            IDataPersister persister = context.GetDataPersister(ib.TypeCode);
            var expected = persister as InfoBaseDataPersister;
            Assert.AreEqual(typeof(InfoBaseDataPersister), expected.GetType());
        }

        [TestMethod]
        public void _00_InfoBase_Insert()
        {
            InfoBase ib = new InfoBase(testKey);
            ib.Name = "Test";
            ib.Alias = "Test";
            ib.Server = "Test";
            ib.Database = "Test";
            ib.UserName = "Test";
            ib.Password = "Test";

            Assert.AreEqual(PersistentState.New, ib.State);
            context.Save(ib);
            Assert.AreEqual(PersistentState.Original, ib.State);
        }
        [TestMethod]
        public void _01_InfoBase_Load()
        {
            InfoBase ib = new InfoBase(testKey);
            Assert.AreEqual(PersistentState.New, ib.State);

            context.Load(ib);
            Assert.AreEqual(PersistentState.Original, ib.State);

            Console.WriteLine("InfoBase.ToString() == " + ib.ToString());
        }
        [TestMethod]
        public void _02_InfoBase_ObjectReference()
        {
            InfoBase ib = new InfoBase(testKey);
            context.Load(ib);

            var obj1 = ib.GetReference();
            var obj2 = new ObjectReference(ib);
            var obj3 = new ObjectReference(ib.TypeCode, ib.PrimaryKey, ib.ToString());
            Assert.AreEqual(obj1, obj2);
            Assert.AreEqual(obj1, obj3);
            Assert.AreEqual(obj2, obj3);

            bool equal = (obj1 == obj2);
            Assert.AreEqual(true, equal);

            bool notEqual = object.ReferenceEquals(obj1, obj2);
            Assert.AreEqual(false, notEqual);

            Assert.AreEqual(obj1.Presentation, obj2.Presentation);
        }
        [TestMethod]
        public void _03_InfoBase_Update()
        {
            InfoBase ib = new InfoBase(testKey);
            
            Assert.AreEqual(PersistentState.New, ib.State);
            context.Load(ib);
            Assert.AreEqual(PersistentState.Original, ib.State);

            ib.Name = "Test*";
            ib.Alias = "Test*";
            ib.Server = "Test*";
            ib.Database = "Test*";
            ib.UserName = "Test*";
            ib.Password = "Test*";
            Assert.AreEqual(PersistentState.Changed, ib.State);
            context.Save(ib);
            Assert.AreEqual(PersistentState.Original, ib.State);
        }
        [TestMethod]
        public void _04_InfoBase_Delete()
        {
            InfoBase ib = new InfoBase(testKey);

            Assert.AreEqual(PersistentState.New, ib.State);
            context.Load(ib);
            Assert.AreEqual(PersistentState.Original, ib.State);

            context.Kill(ib);
            Assert.AreEqual(PersistentState.Deleted, ib.State);
        }

        [TestMethod]
        public void _05_Namespace_Insert()
        {
            InfoBase ib = new InfoBase(testKey);
            ib.Name = "Test";
            ib.Alias = "Test";
            ib.Server = "Test";
            ib.Database = "Test";
            ib.UserName = "Test";
            ib.Password = "Test";
            context.Save(ib);

            Namespace ns1 = new Namespace(_namespaceTestKey1);
            ns1.Name = "Namespace 1 (name)";
            ns1.Alias = "Namespace 1 (alias)";
            ns1.Owner = ib.GetReference();
            context.Save(ns1);
            Assert.AreEqual(PersistentState.Original, ns1.State);

            Namespace ns2 = new Namespace(_namespaceTestKey2);
            ns2.Name = "Namespace 2 (name)";
            ns2.Alias = "Namespace 2 (alias)";
            ns2.Owner = ns1.GetReference();
            context.Save(ns2);
            Assert.AreEqual(PersistentState.Original, ns2.State);

            Namespace nsTest2 = new Namespace(_namespaceTestKey2);
            context.Load(nsTest2);
            Console.WriteLine("nsTest2.ToString() == " + nsTest2.ToString());
            Console.WriteLine("nsTest2.Name == " + nsTest2.Name);
            Console.WriteLine("nsTest2.Alias == " + nsTest2.Alias);
            Console.WriteLine("nsTest2.Owner.ToString() == " + nsTest2.Owner.ToString());
            Namespace nsTest1 = new Namespace(nsTest2.Owner.PrimaryKey);
            context.Load(nsTest1);
            Console.WriteLine("nsTest1.ToString() == " + nsTest1.ToString());

            InfoBase ibTest = new InfoBase(nsTest1.Owner.PrimaryKey);
            context.Load(ibTest);
            Console.WriteLine("ibTest.ToString() == " + ibTest.ToString());

            context.Kill(ns2);
            context.Kill(ns1);
            context.Kill(ib);
        }
    }
}
